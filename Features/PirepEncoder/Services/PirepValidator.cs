using System.Collections.Generic;
using System.Text.RegularExpressions;
using ZoaReference.Features.PirepEncoder.Models;

namespace ZoaReference.Features.PirepEncoder.Services;

public sealed record ValidationError(string Field, string Message);

public static partial class PirepValidator
{
    [GeneratedRegex(@"^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex ThreeLetterFix();

    [GeneratedRegex(@"^\d{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex SixDigits();

    [GeneratedRegex(@"^\d{4}$", RegexOptions.CultureInvariant)]
    private static partial Regex FourDigits();

    [GeneratedRegex(@"^(\d{3}|UNKN)$", RegexOptions.CultureInvariant)]
    private static partial Regex ThreeDigitsOrUnkn();

    public static IReadOnlyList<ValidationError> Validate(Pirep pirep)
    {
        var errors = new List<ValidationError>();

        if (pirep.Location.Segments.Count == 0)
        {
            errors.Add(new ValidationError("OV", "Location is required."));
        }
        else
        {
            foreach (var seg in pirep.Location.Segments)
            {
                if (!ThreeLetterFix().IsMatch(seg.Fix))
                {
                    errors.Add(new ValidationError("OV", $"Fix '{seg.Fix}' must be a 3-letter NAVAID identifier."));
                }
                if (!string.IsNullOrWhiteSpace(seg.RadialDistance) && !SixDigits().IsMatch(seg.RadialDistance))
                {
                    errors.Add(new ValidationError("OV", $"Radial/distance '{seg.RadialDistance}' must be exactly 6 digits (rrrddd)."));
                }
            }
        }

        if (string.IsNullOrWhiteSpace(pirep.Time))
        {
            errors.Add(new ValidationError("TM", "Time is required."));
        }
        else if (!FourDigits().IsMatch(pirep.Time))
        {
            errors.Add(new ValidationError("TM", "Time must be 4 digits UTC (hhmm)."));
        }
        else
        {
            var hh = int.Parse(pirep.Time[..2], System.Globalization.CultureInfo.InvariantCulture);
            var mm = int.Parse(pirep.Time[2..], System.Globalization.CultureInfo.InvariantCulture);
            if (hh > 23 || mm > 59)
            {
                errors.Add(new ValidationError("TM", "Time must be a valid UTC clock value (00:00 - 23:59)."));
            }
        }

        if (string.IsNullOrWhiteSpace(pirep.FlightLevel) || !ThreeDigitsOrUnkn().IsMatch(pirep.FlightLevel))
        {
            errors.Add(new ValidationError("FL", "Altitude must be 3 digits in hundreds of feet, or UNKN."));
        }

        if (string.IsNullOrWhiteSpace(pirep.AircraftType))
        {
            errors.Add(new ValidationError("TP", "Aircraft type is required (up to 4 chars, or UNKN)."));
        }
        else if (pirep.AircraftType.Length > 4 && pirep.AircraftType != "UNKN")
        {
            errors.Add(new ValidationError("TP", "Aircraft type must be 4 characters or fewer (or UNKN)."));
        }

        if (pirep.WindDirection is int dir && (dir < 0 || dir > 360))
        {
            errors.Add(new ValidationError("WV", "Wind direction must be 0-360 degrees."));
        }
        if (pirep.WindSpeedKt is int spd && (spd < 0 || spd > 999))
        {
            errors.Add(new ValidationError("WV", "Wind speed must be 0-999 knots."));
        }
        if (pirep.WindDirection is not null ^ pirep.WindSpeedKt is not null)
        {
            errors.Add(new ValidationError("WV", "Wind requires both direction and speed."));
        }

        if (pirep.SkyCover is { Count: > 0 } layers)
        {
            for (var i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                if (!ThreeDigitsOrUnkn().IsMatch(layer.Base))
                {
                    errors.Add(new ValidationError("SK", $"Layer {i + 1} base must be 3 digits or UNKN."));
                }
                if (!string.IsNullOrWhiteSpace(layer.Tops) && !ThreeDigitsOrUnkn().IsMatch(layer.Tops))
                {
                    errors.Add(new ValidationError("SK", $"Layer {i + 1} tops must be 3 digits or UNKN."));
                }
            }
        }

        return errors;
    }
}
