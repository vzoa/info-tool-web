using ZoaReference.Features.Nasr.Models;

namespace ZoaReference.Features.Nasr.Services;

public static class NasrParser
{
    /// <summary>
    /// Parses NAV.txt fixed-width records for navaids (VOR, VORTAC, TACAN, NDB, etc.).
    /// NASR NAV1 records: positions based on FAA NASR layout specification.
    /// </summary>
    public static List<NavaidInfo> ParseNavaids(string text)
    {
        var navaids = new List<NavaidInfo>();
        using var reader = new StringReader(text);

        while (reader.ReadLine() is { } line)
        {
            if (line.Length < 100) continue;

            var recordType = SafeSubstring(line, 0, 4).Trim();
            if (recordType != "NAV1") continue;

            var type = SafeSubstring(line, 8, 20).Trim();
            var id = SafeSubstring(line, 28, 4).Trim();
            var name = SafeSubstring(line, 42, 30).Trim();

            // Latitude: formatted seconds at position 371, length 14
            var latStr = SafeSubstring(line, 371, 14).Trim();
            var lonStr = SafeSubstring(line, 396, 14).Trim();
            var lat = ParseNasrCoordinate(latStr);
            var lon = ParseNasrCoordinate(lonStr);

            var frequency = SafeSubstring(line, 533, 6).Trim();
            var variation = SafeSubstring(line, 413, 5).Trim();

            if (string.IsNullOrEmpty(id)) continue;

            navaids.Add(new NavaidInfo(id, name, type, frequency, lat, lon, variation));
        }

        return navaids;
    }

    /// <summary>
    /// Parses AWY.txt fixed-width records for airways and their fixes.
    /// NASR AWY2 records contain airway fix information.
    /// </summary>
    public static List<AirwayFix> ParseAirwayFixes(string text)
    {
        var fixes = new List<AirwayFix>();
        using var reader = new StringReader(text);

        while (reader.ReadLine() is { } line)
        {
            if (line.Length < 100) continue;

            var recordType = SafeSubstring(line, 0, 4).Trim();
            if (recordType != "AWY2") continue;

            var airwayId = SafeSubstring(line, 4, 5).Trim();
            var seqStr = SafeSubstring(line, 10, 5).Trim();
            var fixId = SafeSubstring(line, 15, 30).Trim();

            // Take just the navaid portion (first space-delimited token)
            var fixIdClean = fixId.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fixId;

            var latStr = SafeSubstring(line, 83, 14).Trim();
            var lonStr = SafeSubstring(line, 97, 14).Trim();
            var lat = ParseNasrCoordinate(latStr);
            var lon = ParseNasrCoordinate(lonStr);

            if (!int.TryParse(seqStr, out var sequence)) sequence = 0;

            if (string.IsNullOrEmpty(fixIdClean) || string.IsNullOrEmpty(airwayId)) continue;

            fixes.Add(new AirwayFix(fixIdClean, airwayId, sequence, lat, lon));
        }

        return fixes;
    }

    /// <summary>
    /// Parses AWY.txt fixed-width records for airway MEA/MOCA restrictions.
    /// NASR AWY3 records contain altitude restrictions between fixes.
    /// </summary>
    public static List<AirwayRestriction> ParseAirwayRestrictions(string text)
    {
        var restrictions = new List<AirwayRestriction>();
        using var reader = new StringReader(text);

        while (reader.ReadLine() is { } line)
        {
            if (line.Length < 80) continue;

            var recordType = SafeSubstring(line, 0, 4).Trim();
            if (recordType != "AWY3") continue;

            var airwayId = SafeSubstring(line, 4, 5).Trim();
            var fromFix = SafeSubstring(line, 10, 30).Trim().Split(' ').FirstOrDefault() ?? "";
            var toFix = SafeSubstring(line, 40, 30).Trim().Split(' ').FirstOrDefault() ?? "";
            var meaStr = SafeSubstring(line, 74, 5).Trim();
            var mocaStr = SafeSubstring(line, 114, 5).Trim();
            var direction = SafeSubstring(line, 70, 2).Trim();

            int? mea = int.TryParse(meaStr, out var m) ? m : null;
            int? moca = int.TryParse(mocaStr, out var mc) ? mc : null;

            if (string.IsNullOrEmpty(airwayId) || string.IsNullOrEmpty(fromFix)) continue;

            restrictions.Add(new AirwayRestriction(
                airwayId, fromFix, toFix,
                mea, moca,
                string.IsNullOrEmpty(direction) ? null : direction));
        }

        return restrictions;
    }

    /// <summary>
    /// Parses NASR-formatted coordinates like "37-37-08.070N" or "122-23-14.630W"
    /// Also handles decimal seconds format: "373708.070N"
    /// </summary>
    public static double ParseNasrCoordinate(string coord)
    {
        if (string.IsNullOrWhiteSpace(coord)) return 0.0;

        coord = coord.Trim();
        var hemisphere = coord[^1];
        var numPart = coord[..^1];

        double degrees, minutes, seconds;

        if (numPart.Contains('-'))
        {
            // Format: DD-MM-SS.SSS
            var parts = numPart.Split('-');
            if (parts.Length < 3) return 0.0;
            double.TryParse(parts[0], out degrees);
            double.TryParse(parts[1], out minutes);
            double.TryParse(parts[2], out seconds);
        }
        else
        {
            // Format: DDMMSS.SSS or DDDMMSS.SSS
            var dotIdx = numPart.IndexOf('.');
            var intPart = dotIdx >= 0 ? numPart[..dotIdx] : numPart;
            var fracPart = dotIdx >= 0 ? numPart[dotIdx..] : "";

            if (intPart.Length >= 6)
            {
                var degLen = intPart.Length - 4;
                double.TryParse(intPart[..degLen], out degrees);
                double.TryParse(intPart[degLen..(degLen + 2)], out minutes);
                double.TryParse(intPart[(degLen + 2)..] + fracPart, out seconds);
            }
            else
            {
                return 0.0;
            }
        }

        var result = degrees + (minutes / 60.0) + (seconds / 3600.0);
        if (hemisphere is 'S' or 'W') result = -result;
        return result;
    }

    private static string SafeSubstring(string s, int start, int length)
    {
        if (start >= s.Length) return "";
        var end = Math.Min(start + length, s.Length);
        return s[start..end];
    }
}
