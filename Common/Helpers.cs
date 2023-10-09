using System.Text.RegularExpressions;

namespace ZoaReference.Common;

public static partial class Helpers
{
    public static bool TryParseAltitude(string altitudeString, out int parsedAltitude)
    {
        var flMatches = FlightLevelRegex().Match(altitudeString);
        if (flMatches.Success)
        {
            var altitudeSubstring = flMatches.Groups[1].Value;
            var returnBool = int.TryParse(altitudeSubstring, out var tempAltitude);
            parsedAltitude = tempAltitude * 100;
            return returnBool;
        }
        else
        {
            var numMatch = NumRegex().Match(altitudeString);
            var parseString = numMatch.Success ? numMatch.Groups[0].Value.Replace(",", string.Empty) : altitudeString;
            return int.TryParse(parseString, out parsedAltitude);
        }
    }

    public static string SanitizeAirportIcaoToFaa(string airport)
    {
        return airport.Length == 4 && airport.StartsWith("K", StringComparison.OrdinalIgnoreCase)
            ? airport[1..].ToUpper()
            : airport.ToUpper();
    }

    [GeneratedRegex("FL([0-9]{3})")]
    private static partial Regex FlightLevelRegex();

    [GeneratedRegex("[0-9,]+")]
    private static partial Regex NumRegex();
}