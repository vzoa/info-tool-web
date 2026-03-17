using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using ZoaReference.Features.Charts.Models;

namespace ZoaReference.Features.Charts.Services;

public partial class CifpService(
    ILogger<CifpService> logger,
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache)
{
    private const string CifpBaseUrl = "https://aeronav.faa.gov/Upload_313-d/cifp/";
    private const string CifpZipCacheKey = "CifpZipData";
    private static readonly DateOnly AiracEpoch = new(2025, 1, 23);
    private const int CycleDays = 28;

    private static readonly Dictionary<char, string> WaypointDescCodes = new()
    {
        ['A'] = "IAF",
        ['B'] = "IF",
        ['D'] = "FAF",
        ['F'] = "FAF",
        ['I'] = "IAF",
        ['M'] = "MAHP",
    };

    private static readonly Dictionary<string, char> ApproachTypeCodes = new()
    {
        ["ILS"] = 'I', ["LOC"] = 'L', ["VOR"] = 'V', ["RNAV"] = 'H',
        ["RNP"] = 'R', ["GPS"] = 'P', ["NDB"] = 'N', ["LDA"] = 'X',
        ["SDF"] = 'U', ["TACAN"] = 'T'
    };

    public async Task<CifpStarData?> GetStarData(
        string airport, string starName, CancellationToken ct = default)
    {
        var lines = await GetCifpLinesForAirport(airport, ct);
        if (lines is null)
        {
            return null;
        }

        var normalizedAirport = NormalizeAirport(airport);
        var normalizedStarName = NormalizeStarName(starName);

        var starRecords = new Dictionary<string, List<(string Fix, int Sequence)>>();

        foreach (var line in lines)
        {
            var result = ParseStarRecord(line);
            if (result is null)
            {
                continue;
            }

            var (starId, transition, fixId, sequence) = result.Value;
            if (!starId.StartsWith(normalizedStarName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!starRecords.TryGetValue(transition, out var fixes))
            {
                fixes = [];
                starRecords[transition] = fixes;
            }

            fixes.Add((fixId, sequence));
        }

        if (starRecords.Count == 0)
        {
            return null;
        }

        var allWaypoints = new List<string>();
        var seen = new HashSet<string>();

        var commonKey = starRecords.ContainsKey("ALL") ? "ALL"
            : starRecords.ContainsKey("") ? ""
            : null;

        if (commonKey is not null)
        {
            foreach (var (fix, _) in starRecords[commonKey].OrderBy(x => x.Sequence))
            {
                if (seen.Add(fix))
                {
                    allWaypoints.Add(fix);
                }
            }
        }

        foreach (var (transName, fixes) in starRecords)
        {
            if (!transName.StartsWith("RW"))
            {
                continue;
            }

            foreach (var (fix, _) in fixes.OrderBy(x => x.Sequence))
            {
                if (seen.Add(fix))
                {
                    allWaypoints.Add(fix);
                }
            }
        }

        allWaypoints = allWaypoints
            .Where(w => !w.StartsWith("RW") && !w.EndsWith(normalizedAirport))
            .ToList();

        var transitions = starRecords.Keys
            .Where(t => !string.IsNullOrEmpty(t) && t != "ALL" && !t.StartsWith("RW"))
            .OrderBy(t => t)
            .ToList();

        return new CifpStarData(normalizedStarName, allWaypoints, transitions);
    }

    public async Task<List<string>> GetStarNamesForAirport(
        string airport, CancellationToken ct = default)
    {
        var cacheKey = $"CifpStarNames:{NormalizeAirport(airport)}";
        if (cache.TryGetValue<List<string>>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var lines = await GetCifpLinesForAirport(airport, ct);
        if (lines is null)
        {
            return [];
        }

        var starIds = new HashSet<string>();
        foreach (var line in lines)
        {
            var result = ParseStarRecord(line);
            if (result is null)
            {
                continue;
            }

            starIds.Add(result.Value.StarId);
        }

        var names = starIds
            .Select(id => StarBaseNameRegex().Match(id))
            .Where(m => m.Success)
            .Select(m => m.Value)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        cache.Set(cacheKey, names, DateTimeOffset.UtcNow.AddHours(24));
        return names;
    }

    public async Task<CifpProcedureDetail?> GetProcedureDetail(
        string airport, string procedureName, CancellationToken ct = default)
    {
        var lines = await GetCifpLinesForAirport(airport, ct);
        if (lines is null) return null;

        var normalizedAirport = NormalizeAirport(airport);
        procedureName = RnavSuffixRegex().Replace(procedureName.ToUpperInvariant().Trim(), "");

        List<string> procIdPrefixes;
        char? subsectionFilter = null;
        CifpProcedureType? detectedType = null;

        var approachMatch = ApproachPatternRegex().Match(procedureName);
        if (approachMatch.Success)
        {
            subsectionFilter = 'F';
            detectedType = CifpProcedureType.Approach;
            var typeCode = ApproachTypeCodes.GetValueOrDefault(approachMatch.Groups[1].Value, 'H');
            var runway = approachMatch.Groups[2].Value;
            var variant = approachMatch.Groups[3].Value;
            procIdPrefixes = [];
            if (!string.IsNullOrEmpty(variant))
                procIdPrefixes.Add($"{typeCode}{runway}{variant}");
            procIdPrefixes.Add($"{typeCode}{runway}");
        }
        else
        {
            var procMatch = StarNameRegex().Match(procedureName);
            if (procMatch.Success)
            {
                var baseName = procMatch.Groups[1].Value;
                var numPart = procMatch.Groups[2].Value switch
                {
                    "ONE" => "1", "TWO" => "2", "THREE" => "3", "FOUR" => "4", "FIVE" => "5",
                    "SIX" => "6", "SEVEN" => "7", "EIGHT" => "8", "NINE" => "9",
                    var n => n
                };
                procIdPrefixes = [$"{baseName}{numPart}"];
            }
            else
            {
                procIdPrefixes = [procedureName];
            }
        }

        var allLegs = new List<(int Sequence, CifpLeg Leg)>();
        var foundSubsection = subsectionFilter ?? 'D';

        foreach (var line in lines)
        {
            if (line.Length < 102) continue;
            var sub = line[12];

            if (subsectionFilter.HasValue)
            {
                if (sub != subsectionFilter.Value) continue;
            }
            else
            {
                if (sub != 'D' && sub != 'E') continue;
            }

            var procId = line[13..Math.Min(19, line.Length)].TrimEnd();
            var matched = procIdPrefixes.Any(prefix =>
                sub == 'F' && prefix.Length > 3 && "XYZWABCDEFGH".Contains(prefix[^1])
                    ? procId.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                    : procId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            if (!matched) continue;

            var leg = ParseFullProcedureLeg(line, sub);
            if (leg is null) continue;

            var seqStr = line[26..29].Trim();
            var seq = int.TryParse(seqStr, out var s) ? s : 0;
            allLegs.Add((seq, leg));
            foundSubsection = sub;
        }

        if (allLegs.Count == 0) return null;

        var procedureType = detectedType ?? foundSubsection switch
        {
            'D' => CifpProcedureType.SID,
            'E' => CifpProcedureType.STAR,
            _ => CifpProcedureType.Approach
        };

        return new CifpProcedureDetail(
            normalizedAirport,
            procIdPrefixes[0],
            procedureType,
            allLegs.OrderBy(x => x.Sequence).Select(x => x.Leg).ToList());
    }

    public async Task<Dictionary<string, CifpApproach>> GetApproachesForAirport(
        string airport, CancellationToken ct = default)
    {
        var cacheKey = $"CifpApproaches:{NormalizeAirport(airport)}";
        if (cache.TryGetValue<Dictionary<string, CifpApproach>>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var lines = await GetCifpLinesForAirport(airport, ct);
        if (lines is null)
        {
            return new Dictionary<string, CifpApproach>();
        }

        var normalizedAirport = NormalizeAirport(airport);
        var approaches = new Dictionary<string, CifpApproach>();

        foreach (var line in lines)
        {
            var fix = ParseApproachRecord(line);
            if (fix is null)
            {
                continue;
            }

            if (!approaches.TryGetValue(fix.ApproachId, out var approach))
            {
                approach = new CifpApproach(
                    normalizedAirport,
                    fix.ApproachId,
                    ParseRunwayFromApproachId(fix.ApproachId) ?? "")
                {
                    Fixes = []
                };
                approaches[fix.ApproachId] = approach;
            }

            approach.Fixes.Add(fix);
        }

        cache.Set(cacheKey, approaches, DateTimeOffset.UtcNow.AddHours(24));
        return approaches;
    }

    private async Task<List<string>?> GetCifpLinesForAirport(
        string airport, CancellationToken ct)
    {
        var normalizedAirport = NormalizeAirport(airport);
        var linesCacheKey = $"CifpLines:{normalizedAirport}";

        if (cache.TryGetValue<List<string>>(linesCacheKey, out var cachedLines) && cachedLines is not null)
        {
            return cachedLines;
        }

        var cifpText = await GetCifpText(ct);
        if (cifpText is null)
        {
            return null;
        }

        var searchPrefix = $"SUSAP K{normalizedAirport}";
        var lines = new List<string>();

        using var reader = new StringReader(cifpText);
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (line.StartsWith(searchPrefix))
            {
                lines.Add(line);
            }
        }

        cache.Set(linesCacheKey, lines, DateTimeOffset.UtcNow.AddHours(24));
        return lines;
    }

    private async Task<string?> GetCifpText(CancellationToken ct)
    {
        if (cache.TryGetValue<string>(CifpZipCacheKey, out var cachedText) && cachedText is not null)
        {
            return cachedText;
        }

        var url = GetCifpUrl();
        logger.LogInformation("Downloading CIFP data from {Url}", url);

        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(120);
            var zipBytes = await client.GetByteArrayAsync(url, ct);

            using var zipStream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var cifpEntry = archive.Entries.FirstOrDefault(e => e.Name.StartsWith("FAACIFP"));
            if (cifpEntry is null)
            {
                logger.LogWarning("FAACIFP file not found in zip archive");
                return null;
            }

            using var entryStream = cifpEntry.Open();
            using var reader = new StreamReader(entryStream, System.Text.Encoding.Latin1);
            var text = await reader.ReadToEndAsync(ct);

            var cycleDuration = TimeSpan.FromDays(CycleDays);
            cache.Set(CifpZipCacheKey, text, DateTimeOffset.UtcNow.Add(cycleDuration));

            logger.LogInformation("CIFP data cached ({Length} chars)", text.Length);
            return text;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to download CIFP data from {Url}", url);
            return null;
        }
    }

    private static string GetCifpUrl()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysSinceEpoch = today.DayNumber - AiracEpoch.DayNumber;
        var cycleNumber = daysSinceEpoch / CycleDays;
        var effectiveDate = AiracEpoch.AddDays(cycleNumber * CycleDays);
        return $"{CifpBaseUrl}CIFP_{effectiveDate:yyMMdd}.zip";
    }

    private static string NormalizeAirport(string airport)
    {
        airport = airport.ToUpperInvariant().Trim();
        if (airport.Length == 4 && airport.StartsWith('K'))
        {
            return airport[1..];
        }

        return airport;
    }

    private static string NormalizeStarName(string name)
    {
        name = name.ToUpperInvariant().Trim();
        name = RnavSuffixRegex().Replace(name, "");

        var match = StarNameRegex().Match(name);
        if (!match.Success)
        {
            return name;
        }

        var baseName = match.Groups[1].Value;
        var numPart = match.Groups[2].Value;

        numPart = numPart switch
        {
            "ONE" => "1",
            "TWO" => "2",
            "THREE" => "3",
            "FOUR" => "4",
            "FIVE" => "5",
            "SIX" => "6",
            "SEVEN" => "7",
            "EIGHT" => "8",
            "NINE" => "9",
            _ => numPart
        };

        return $"{baseName}{numPart}";
    }

    private static (string StarId, string Transition, string FixId, int Sequence)?
        ParseStarRecord(string line)
    {
        if (line.Length < 35 || !line.StartsWith("SUSAP"))
        {
            return null;
        }

        if (line[12] != 'E')
        {
            return null;
        }

        var starId = line[13..19].Trim();
        var transition = line[20..25].Trim();
        var sequenceStr = line[26..29].Trim();
        var fixId = line[29..34].Trim();

        if (string.IsNullOrEmpty(fixId) || string.IsNullOrEmpty(starId))
        {
            return null;
        }

        if (!int.TryParse(sequenceStr, out var sequence))
        {
            sequence = 0;
        }

        return (starId, transition, fixId, sequence);
    }

    private static CifpApproachFix? ParseApproachRecord(string line)
    {
        if (line.Length < 50 || !line.StartsWith("SUSAP"))
        {
            return null;
        }

        if (line[12] != 'F')
        {
            return null;
        }

        var approachId = line[13..19].Trim();
        var routeType = line.Length > 19 ? line[19] : ' ';
        var transition = line[20..25].Trim();
        var sequenceStr = line[26..29].Trim();
        var fixIdentifier = line[29..34].Trim();

        if (string.IsNullOrEmpty(fixIdentifier))
        {
            return null;
        }

        var waypointDesc = line.Length > 42 ? line[42] : ' ';
        var roleStr = WaypointDescCodes.GetValueOrDefault(waypointDesc, "");
        var role = roleStr switch
        {
            "IAF" => FixRole.IAF,
            "IF" => FixRole.IF,
            "FAF" => FixRole.FAF,
            _ => FixRole.None
        };

        if (!int.TryParse(sequenceStr, out var sequence))
        {
            sequence = 0;
        }

        if (routeType != 'A')
        {
            transition = "";
        }

        return new CifpApproachFix(approachId, transition, fixIdentifier, role, sequence);
    }

    private static string? ParseRunwayFromApproachId(string approachId)
    {
        if (approachId.Length < 2)
        {
            return null;
        }

        var rest = approachId[1..];
        var match = RunwayRegex().Match(rest);
        return match.Success ? match.Groups[1].Value : null;
    }

    // ARINC 424 column positions (0-indexed):
    // 12       subsection ('D'=SID, 'E'=STAR, 'F'=Approach)
    // 13-19    procedure ID
    // 19       route type
    // 20-25    transition
    // 26-29    sequence number
    // 29-34    fix identifier
    // 39-43    waypoint description (first char encodes fix role)
    // 47-49    path terminator
    // 82       altitude description
    // 83-88    altitude 1
    // 88-93    altitude 2
    // 99-102   speed limit
    // 117      speed limit description
    private static CifpLeg? ParseFullProcedureLeg(string line, char subsection)
    {
        if (line.Length < 102 || !line.StartsWith("SUSAP") || line[12] != subsection)
            return null;

        var routeType = line[19];
        var fixId = line[29..34].Trim();
        if (string.IsNullOrEmpty(fixId)) return null;

        var pathTerminator = line.Length > 48 ? line[47..49].Trim() : "";
        if (string.IsNullOrEmpty(pathTerminator)) return null;

        var waypointDescChar = line.Length > 39 ? line[39] : ' ';
        var fixTypeStr = WaypointDescCodes.GetValueOrDefault(waypointDescChar, "");
        var role = fixTypeStr switch
        {
            "IAF" => FixRole.IAF,
            "IF" => FixRole.IF,
            "FAF" => FixRole.FAF,
            _ => FixRole.None
        };

        var altDesc = line.Length > 82 ? line[82] : ' ';
        var alt1Str = line.Length > 87 ? line[83..88] : "";
        var alt2Str = line.Length > 92 ? line[88..93] : "";
        var alt1 = ParseAltitude(alt1Str);
        var alt2 = ParseAltitude(alt2Str);
        var altConstraint = FormatAltitude(altDesc, alt1, alt2);

        var speedStr = line.Length > 101 ? line[99..102] : "";
        var speedDesc = line.Length > 117 ? line[117] : ' ';
        var speedVal = ParseSpeed(speedStr);
        int? speedConstraint = speedVal.HasValue ? FormatSpeed(speedDesc, speedVal.Value) : null;

        return new CifpLeg(fixId, pathTerminator, altConstraint, speedConstraint, null, null, role);
    }

    private static int? ParseAltitude(string altStr)
    {
        altStr = altStr.Trim();
        if (string.IsNullOrEmpty(altStr)) return null;

        if (altStr.StartsWith("FL", StringComparison.OrdinalIgnoreCase))
        {
            var flNumStr = altStr[2..].Trim();
            if (int.TryParse(flNumStr, out var flNum))
                return flNum < 100 ? flNum * 1000 : flNum * 100;
            return null;
        }

        altStr = altStr.TrimStart('0');
        if (string.IsNullOrEmpty(altStr)) return null;
        return int.TryParse(altStr, out var val) ? val * 10 : null;
    }

    private static string? FormatAltitude(char desc, int? alt1, int? alt2)
    {
        if (alt1 is null) return null;

        var alt1Str = alt1.Value >= 18000 ? $"FL{alt1.Value / 100}" : $"{alt1.Value}";
        return desc switch
        {
            '+' or 'H' => $"{alt1Str}A",
            '-' => $"{alt1Str}B",
            'B' when alt2.HasValue => $"{alt1Str}-{(alt2.Value >= 18000 ? $"FL{alt2.Value / 100}" : $"{alt2.Value}")}",
            _ => alt1Str
        };
    }

    private static int? ParseSpeed(string speedStr)
    {
        speedStr = speedStr.Trim();
        return int.TryParse(speedStr, out var v) ? v : null;
    }

    private static int FormatSpeed(char desc, int speed) => speed;

    [GeneratedRegex(@"\s*\(RNAV\)$")]
    private static partial Regex RnavSuffixRegex();

    [GeneratedRegex(@"^([A-Z]+)\s*(\d|ONE|TWO|THREE|FOUR|FIVE|SIX|SEVEN|EIGHT|NINE)$")]
    private static partial Regex StarNameRegex();

    [GeneratedRegex(@"^(\d{1,2}[LRC]?)")]
    private static partial Regex RunwayRegex();

    [GeneratedRegex(@"^[A-Z]+\d")]
    private static partial Regex StarBaseNameRegex();

    [GeneratedRegex(@"^(ILS|LOC|VOR|RNAV|RNP|GPS|NDB|LDA|SDF|TACAN)\s*(?:[YZXW]\s+)?(?:OR\s+\w+\s+)?(?:RWY\s*)?(\d{1,2}[LRC]?)\s*([XYZWABCDEFGH])?$")]
    private static partial Regex ApproachPatternRegex();
}
