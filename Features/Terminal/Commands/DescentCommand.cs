using System.Text;
using ZoaReference.Features.Nasr.Services;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class DescentCommand(NasrDataService nasrDataService) : ITerminalCommand
{
    public string Name => "descent";
    public string[] Aliases => ["desc", "des"];
    public string Summary => "Calculate descent planning (318 ft/nm glideslope)";
    public string Usage => "descent <from_alt> <to_alt_or_distance>\n" +
                           "    descent 350 100     — Distance needed from FL350 to FL100\n" +
                           "    descent 350 25      — Altitude after 25nm of descent from FL350\n" +
                           "    descent SJC MOD     — Fix-to-fix distance using NASR data\n" +
                           "    Values >= 100 are treated as altitudes (in hundreds of feet)\n" +
                           "    Values < 100 are treated as distance (in nm)";

    private const double FeetPerNm = 318.0;
    private const double EarthRadiusNm = 3440.065;

    public async Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 2)
        {
            return CommandResult.FromError("Usage: descent <from_alt> <to_alt_or_distance>");
        }

        // Check for fix-to-fix mode (both args are non-numeric)
        var isFirstNumeric = double.TryParse(args.Positional[0], out var fromValue);
        var isSecondNumeric = double.TryParse(args.Positional[1], out var toValue);

        if (!isFirstNumeric && !isSecondNumeric)
        {
            return await FixToFixDescent(args.Positional[0], args.Positional[1]);
        }

        if (!isFirstNumeric)
        {
            return CommandResult.FromError($"Invalid altitude: '{args.Positional[0]}'");
        }

        if (!isSecondNumeric)
        {
            return CommandResult.FromError($"Invalid value: '{args.Positional[1]}'");
        }

        // Convert flight level shorthand to feet (e.g., 350 → 35000)
        var fromAltFeet = fromValue >= 100 ? fromValue * 100 : fromValue;

        if (toValue >= 100)
        {
            // Both are altitudes — calculate distance needed
            var toAltFeet = toValue * 100;
            var altDiff = fromAltFeet - toAltFeet;
            if (altDiff <= 0)
            {
                return CommandResult.FromError("Start altitude must be higher than target altitude.");
            }
            var distanceNm = altDiff / FeetPerNm;
            return FormatDistanceResult(fromAltFeet, toAltFeet, distanceNm);
        }
        else
        {
            // Second value is distance — calculate altitude after descent
            var distanceNm = toValue;
            var altLost = distanceNm * FeetPerNm;
            var resultAlt = fromAltFeet - altLost;
            return FormatAltitudeResult(fromAltFeet, distanceNm, resultAlt);
        }
    }

    private async Task<CommandResult> FixToFixDescent(string fix1Name, string fix2Name)
    {
        var fix1 = fix1Name.ToUpperInvariant();
        var fix2 = fix2Name.ToUpperInvariant();

        var coords1 = await nasrDataService.GetWaypointCoordinates(fix1);
        var coords2 = await nasrDataService.GetWaypointCoordinates(fix2);

        if (coords1 is null)
        {
            return CommandResult.FromError($"Fix '{fix1}' not found in NASR data");
        }
        if (coords2 is null)
        {
            return CommandResult.FromError($"Fix '{fix2}' not found in NASR data");
        }

        var distance = HaversineDistanceNm(coords1.Value.Lat, coords1.Value.Lon,
            coords2.Value.Lat, coords2.Value.Lon);
        var altitudeLost = distance * FeetPerNm;

        var sb = new StringBuilder();
        sb.AppendLine(TextFormatter.Colorize("  Fix-to-Fix Descent", AnsiColor.Orange));
        sb.AppendLine();
        sb.AppendLine($"  From:       {TextFormatter.Colorize(fix1, AnsiColor.White)} ({coords1.Value.Lat:F4}, {coords1.Value.Lon:F4})");
        sb.AppendLine($"  To:         {TextFormatter.Colorize(fix2, AnsiColor.White)} ({coords2.Value.Lat:F4}, {coords2.Value.Lon:F4})");
        sb.AppendLine($"  Distance:   {TextFormatter.Colorize($"{distance:F1} nm", AnsiColor.Green)}");
        sb.AppendLine($"  Alt Lost:   {TextFormatter.Colorize($"{altitudeLost:N0} ft", AnsiColor.Green)} at 318 ft/nm");
        return CommandResult.FromText(sb.ToString());
    }

    private static CommandResult FormatDistanceResult(double fromAlt, double toAlt, double distance)
    {
        var sb = new StringBuilder();
        sb.AppendLine(TextFormatter.Colorize("  Descent Calculation", AnsiColor.Orange));
        sb.AppendLine();
        sb.AppendLine($"  From:     {TextFormatter.Colorize(FormatAltitude(fromAlt), AnsiColor.White)}");
        sb.AppendLine($"  To:       {TextFormatter.Colorize(FormatAltitude(toAlt), AnsiColor.White)}");
        sb.AppendLine($"  Distance: {TextFormatter.Colorize($"{distance:F1} nm", AnsiColor.Green)}");
        sb.AppendLine($"  Rate:     318 ft/nm (3° glideslope)");
        return CommandResult.FromText(sb.ToString());
    }

    private static CommandResult FormatAltitudeResult(double fromAlt, double distance, double resultAlt)
    {
        var sb = new StringBuilder();
        sb.AppendLine(TextFormatter.Colorize("  Descent Calculation", AnsiColor.Orange));
        sb.AppendLine();
        sb.AppendLine($"  From:       {TextFormatter.Colorize(FormatAltitude(fromAlt), AnsiColor.White)}");
        sb.AppendLine($"  After:      {TextFormatter.Colorize($"{distance:F0} nm", AnsiColor.White)}");
        sb.AppendLine($"  Altitude:   {TextFormatter.Colorize(FormatAltitude(resultAlt), AnsiColor.Green)}");
        sb.AppendLine($"  Rate:       318 ft/nm (3° glideslope)");
        return CommandResult.FromText(sb.ToString());
    }

    private static string FormatAltitude(double feet)
    {
        if (feet >= 18000)
        {
            return $"FL{feet / 100:F0} ({feet:N0} ft)";
        }
        return $"{feet:N0} ft";
    }

    private static double HaversineDistanceNm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusNm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
