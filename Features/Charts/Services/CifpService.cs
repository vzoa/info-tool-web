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

    [GeneratedRegex(@"\s*\(RNAV\)$")]
    private static partial Regex RnavSuffixRegex();

    [GeneratedRegex(@"^([A-Z]+)\s*(\d|ONE|TWO|THREE|FOUR|FIVE|SIX|SEVEN|EIGHT|NINE)$")]
    private static partial Regex StarNameRegex();

    [GeneratedRegex(@"^(\d{1,2}[LRC]?)")]
    private static partial Regex RunwayRegex();

    [GeneratedRegex(@"^[A-Z]+\d")]
    private static partial Regex StarBaseNameRegex();
}
