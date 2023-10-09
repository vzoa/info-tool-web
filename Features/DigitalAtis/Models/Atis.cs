using System.Text.RegularExpressions;

namespace ZoaReference.Features.DigitalAtis.Models;

public class Atis
{
    public string IcaoId { get; set; }
    public AtisType Type { get; set; }
    public char InfoLetter { get; set; }
    public DateTime IssueTime { get; set; }
    public int Altimeter { get; set; }
    public string RawText { get; set; }
    public string WeatherText { get; set; }
    public string StatusText { get; set; }
    public string UniqueId { get; private set; }

    public static bool TryParseFromClowdAtis(ClowdDatisDto clowdAtis, out Atis? newAtis)
    {
        newAtis = null;

        try
        {
            newAtis = new Atis
            {
                IcaoId = clowdAtis.Airport,
                Type = ParseAtisType(clowdAtis.Type),
                RawText = clowdAtis.Datis
            };
            newAtis.UniqueId = newAtis.IcaoId + newAtis.Type;

            // Parse letter and time from first sentence and add to new Atis
            var letterTimePattern = @"INFO ([A-Z]) ([0-9]{2})([0-9]{2})Z";
            var infoMatch = Regex.Match(clowdAtis.Datis, letterTimePattern);
            if (infoMatch.Success)
            {
                newAtis.InfoLetter = char.Parse(infoMatch.Groups[1].Value);
                newAtis.IssueTime = new DateTime(
                    DateTime.UtcNow.Year,
                    DateTime.UtcNow.Month,
                    DateTime.UtcNow.Day,
                    int.Parse(infoMatch.Groups[2].Value), // Hours
                    int.Parse(infoMatch.Groups[3].Value), // Minutes
                    0,
                    DateTimeKind.Utc
                );
                if (newAtis.IssueTime > DateTime.UtcNow)
                {
                    newAtis.IssueTime -= TimeSpan.FromDays(1);
                }
            }

            // Parse altimeter from weather string
            var altimeterPattern = @"A[0-9]{4}";
            var altimeterMatch = Regex.Match(clowdAtis.Datis, altimeterPattern);
            if (altimeterMatch.Success)
            {
                newAtis.Altimeter = int.Parse(altimeterMatch.Groups[0].Value[1..]);
            }

            // Take 2nd sentence as WX string (by convention)
            newAtis.WeatherText = clowdAtis.Datis.Split(". ")[1];

            // Take the rest of the string as status string (by convention)
            newAtis.StatusText = clowdAtis.Datis.Split(". ", 3)[2];

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static AtisType ParseAtisType(string type)
    {
        return type switch
        {
            "combined" => AtisType.Combined,
            "dep" => AtisType.Departure,
            "arr" => AtisType.Arrival,
            _ => throw new NotImplementedException()
        };
    }

    public enum AtisType
    {
        Combined,
        Departure,
        Arrival
    }
}
