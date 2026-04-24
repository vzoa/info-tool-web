using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using ZoaReference.Features.Nasr.Models;

namespace ZoaReference.Features.Nasr.Services;

public partial class NasrDataService(
    ILogger<NasrDataService> logger,
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache)
{
    private const string NavCacheKey = "NasrNavaids";
    private const string AwyCacheKey = "NasrAirwayFixes";
    private const string AwyRestrCacheKey = "NasrAirwayRestrictions";
    private const string WaypointCacheKey = "NasrWaypoints";

    private static readonly string[] VorFamilyTypes = ["VOR", "VOR/DME", "VORTAC"];

    public async Task<IReadOnlyList<NavaidInfo>> SearchNavaids(string query, CancellationToken ct = default)
    {
        var navaids = await GetNavaids(ct);
        var q = query.ToUpperInvariant();
        return navaids
            .Where(n =>
                n.Id.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                n.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                n.Type.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<IReadOnlyList<NavaidInfo>> SearchVors(string query, CancellationToken ct = default)
    {
        var navaids = await GetNavaids(ct);
        var q = query.Trim();
        return navaids
            .Where(n => VorFamilyTypes.Contains(n.Type))
            .Where(n =>
                n.Id.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                n.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<IReadOnlyList<AirwayFix>> GetAirwayFixes(string airwayId, CancellationToken ct = default)
    {
        var fixes = await GetAllAirwayFixes(ct);
        return fixes
            .Where(f => f.AirwayId.Equals(airwayId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Sequence)
            .ToList();
    }

    public async Task<IReadOnlyList<AirwayRestriction>> GetAirwayRestrictions(string airwayId, CancellationToken ct = default)
    {
        var restrictions = await GetAllRestrictions(ct);
        return restrictions
            .Where(r => r.AirwayId.Equals(airwayId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<IReadOnlyList<string>> FindAirwaysContainingFix(string fixId, CancellationToken ct = default)
    {
        var fixes = await GetAllAirwayFixes(ct);
        return fixes
            .Where(f => f.FixId.Equals(fixId, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.AirwayId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<NavaidInfo?> GetNavaidById(string id, CancellationToken ct = default)
    {
        var navaids = await GetNavaids(ct);
        return navaids.FirstOrDefault(n => n.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Rewrites chart-name-style inputs that use a navaid identifier into the navaid's
    /// station name, so fuzzy/substring matching can find the corresponding chart.
    /// Mirrors the CLI's <c>nasr.py:resolve_navaid_alias</c>.
    /// </summary>
    /// <remarks>
    /// Two patterns are recognized:
    /// <list type="bullet">
    /// <item><c>FMG1</c> → <c>MUSTANG1</c> (identifier + single digit)</item>
    /// <item><c>FMG FIVE</c> → <c>MUSTANG FIVE</c> (identifier + trailing words)</item>
    /// </list>
    /// When the leading token is not a known navaid identifier, the input is returned unchanged.
    /// Only the first word of the navaid's station name is used (e.g. "MUSTANG" from "MUSTANG VORTAC").
    /// </remarks>
    public async Task<string> ResolveNavaidAlias(string input, CancellationToken ct = default)
    {
        var trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            return input;
        }

        // Pattern 1: single token ending in one digit, e.g. "FMG1"
        var match1 = IdentWithDigitRegex().Match(trimmed);
        if (match1.Success)
        {
            var ident = match1.Groups[1].Value;
            var digit = match1.Groups[2].Value;
            var firstWord = await GetNavaidFirstWord(ident, ct);
            if (firstWord is not null)
            {
                return $"{firstWord}{digit}";
            }
            return input;
        }

        // Pattern 2: first token is a navaid identifier, followed by one or more words
        var spaceIdx = trimmed.IndexOf(' ');
        if (spaceIdx > 0)
        {
            var ident = trimmed[..spaceIdx];
            var rest = trimmed[(spaceIdx + 1)..];
            if (IdentOnlyRegex().IsMatch(ident))
            {
                var firstWord = await GetNavaidFirstWord(ident, ct);
                if (firstWord is not null)
                {
                    return $"{firstWord} {rest}";
                }
            }
        }

        return input;
    }

    private async Task<string?> GetNavaidFirstWord(string ident, CancellationToken ct)
    {
        var navaid = await GetNavaidById(ident, ct);
        if (navaid is null || string.IsNullOrWhiteSpace(navaid.Name))
        {
            return null;
        }
        var spaceIdx = navaid.Name.IndexOf(' ');
        return spaceIdx < 0 ? navaid.Name : navaid.Name[..spaceIdx];
    }

    [GeneratedRegex(@"^([A-Z]+)(\d)$")]
    private static partial Regex IdentWithDigitRegex();

    [GeneratedRegex(@"^[A-Z]+$")]
    private static partial Regex IdentOnlyRegex();

    /// <summary>
    /// Finds a navaid whose station name starts with the given word (as stored in NASR AWY records).
    /// NASR AWY fix names are the first space-delimited word of the navaid's station name,
    /// e.g. "WENATCHEE" for EAT, "KLAMATH" for LMT (KLAMATH FALLS), "MISSION" for MZB (MISSION BAY).
    /// When multiple matches exist, returns the one closest to the given coordinates.
    /// </summary>
    public async Task<NavaidInfo?> GetNavaidByStationName(string nameFirstWord, double lat, double lon, CancellationToken ct = default)
    {
        var navaids = await GetNavaids(ct);
        var candidates = navaids.Where(n =>
            n.Name.Equals(nameFirstWord, StringComparison.OrdinalIgnoreCase) ||
            n.Name.StartsWith(nameFirstWord + " ", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        // Disambiguate by proximity to the fix's coordinates
        return candidates
            .OrderBy(n => Math.Pow(n.Latitude - lat, 2) + Math.Pow(n.Longitude - lon, 2))
            .First();
    }

    public async Task<(double Lat, double Lon)?> GetWaypointCoordinates(string identifier, CancellationToken ct = default)
    {
        // Check navaids first
        var navaids = await GetNavaids(ct);
        var navaid = navaids.FirstOrDefault(n => n.Id.Equals(identifier, StringComparison.OrdinalIgnoreCase));
        if (navaid is not null)
        {
            return (navaid.Latitude, navaid.Longitude);
        }

        // Check airway fixes
        var fixes = await GetAllAirwayFixes(ct);
        var fix = fixes.FirstOrDefault(f => f.FixId.Equals(identifier, StringComparison.OrdinalIgnoreCase));
        if (fix is not null)
        {
            return (fix.Latitude, fix.Longitude);
        }

        return null;
    }

    public async Task FetchAndCacheData(CancellationToken ct = default)
    {
        var (cycleId, _, _) = AiracCycleHelper.GetCurrentCycle();
        logger.LogInformation("Fetching NASR data for AIRAC cycle {CycleId}", cycleId);

        try
        {
            var navText = await DownloadNasrFile("NAV.txt", ct);
            if (navText is not null)
            {
                var navaids = NasrParser.ParseNavaids(navText);
                var expiration = AiracCycleHelper.TimeUntilNextCycle();
                cache.Set(NavCacheKey, navaids, DateTimeOffset.UtcNow.Add(expiration));
                logger.LogInformation("Cached {Count} navaids", navaids.Count);
            }

            var awyText = await DownloadNasrFile("AWY.txt", ct);
            if (awyText is not null)
            {
                var fixes = NasrParser.ParseAirwayFixes(awyText);
                var restrictions = NasrParser.ParseAirwayRestrictions(awyText);
                var expiration = AiracCycleHelper.TimeUntilNextCycle();
                cache.Set(AwyCacheKey, fixes, DateTimeOffset.UtcNow.Add(expiration));
                cache.Set(AwyRestrCacheKey, restrictions, DateTimeOffset.UtcNow.Add(expiration));
                logger.LogInformation("Cached {FixCount} airway fixes, {RestrCount} restrictions",
                    fixes.Count, restrictions.Count);
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            logger.LogError(ex, "Failed to fetch NASR data");
        }
    }

    private async Task<List<NavaidInfo>> GetNavaids(CancellationToken ct)
    {
        if (cache.TryGetValue<List<NavaidInfo>>(NavCacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        await FetchAndCacheData(ct);
        return cache.TryGetValue<List<NavaidInfo>>(NavCacheKey, out var result) ? result ?? [] : [];
    }

    private async Task<List<AirwayFix>> GetAllAirwayFixes(CancellationToken ct)
    {
        if (cache.TryGetValue<List<AirwayFix>>(AwyCacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        await FetchAndCacheData(ct);
        return cache.TryGetValue<List<AirwayFix>>(AwyCacheKey, out var result) ? result ?? [] : [];
    }

    private async Task<List<AirwayRestriction>> GetAllRestrictions(CancellationToken ct)
    {
        if (cache.TryGetValue<List<AirwayRestriction>>(AwyRestrCacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        await FetchAndCacheData(ct);
        return cache.TryGetValue<List<AirwayRestriction>>(AwyRestrCacheKey, out var result) ? result ?? [] : [];
    }

    private async Task<string?> DownloadNasrFile(string fileName, CancellationToken ct)
    {
        // NASR files are served as individual ZIPs (e.g., NAV.zip containing NAV.txt)
        var baseUrl = AiracCycleHelper.GetNasrUrl();
        var fileStem = Path.GetFileNameWithoutExtension(fileName);
        var zipUrl = $"{baseUrl}{fileStem}.zip";
        logger.LogInformation("Downloading NASR file from {Url}", zipUrl);

        return await DownloadFromZip(zipUrl, fileName, ct);
    }

    private async Task<string?> DownloadFromZip(string zipUrl, string targetFile, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(180);
            var zipBytes = await client.GetByteArrayAsync(zipUrl, ct);

            using var zipStream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var entry = archive.Entries.FirstOrDefault(e =>
                e.Name.Equals(targetFile, StringComparison.OrdinalIgnoreCase) ||
                e.FullName.EndsWith(targetFile, StringComparison.OrdinalIgnoreCase));

            if (entry is null)
            {
                logger.LogWarning("File {TargetFile} not found in NASR ZIP", targetFile);
                return null;
            }

            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            return await reader.ReadToEndAsync(ct);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            logger.LogWarning(ex, "Failed to download NASR ZIP from {Url}", zipUrl);
            return null;
        }
    }
}
