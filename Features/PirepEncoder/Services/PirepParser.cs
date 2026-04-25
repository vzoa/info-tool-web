using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ZoaReference.Features.PirepEncoder.Models;

namespace ZoaReference.Features.PirepEncoder.Services;

public enum ParseWarningLevel
{
    Info,
    Warning,
}

public sealed record ParseWarning(ParseWarningLevel Level, string Message, string? Field = null);

public sealed record ParseResult(Pirep Pirep, IReadOnlyList<ParseWarning> Warnings);

/// <summary>
/// Tolerant parser for PIREP strings per FAA Form 7110-2.
/// Unknown tokens surface as <see cref="ParseWarning"/>s rather than failing.
/// </summary>
public static partial class PirepParser
{
    private static readonly string[] KnownCodes =
        ["OV", "TM", "FL", "TP", "SK", "WX", "TA", "WV", "TB", "IC", "RM"];

    [GeneratedRegex(@"/(OV|TM|FL|TP|SK|WX|TA|WV|TB|IC|RM)", RegexOptions.CultureInvariant)]
    private static partial Regex FieldCodeRegex();

    public static ParseResult Parse(string input)
    {
        var warnings = new List<ParseWarning>();
        var text = (input ?? "").Trim();

        string? said = null;
        var reportType = ReportType.Routine;

        var headerMatch = Regex.Match(text, @"^(?:([A-Z0-9]{3,4})\s+)?(UA|UUA)\b", RegexOptions.CultureInvariant);
        if (!headerMatch.Success)
        {
            warnings.Add(new ParseWarning(ParseWarningLevel.Warning, "Could not find UA/UUA report type; assuming UA."));
        }
        else
        {
            if (headerMatch.Groups[1].Success)
            {
                said = headerMatch.Groups[1].Value;
            }
            reportType = ReportTypeExtensions.FromCode(headerMatch.Groups[2].Value);
            text = text[headerMatch.Length..];
        }

        var fields = SplitFields(text);

        var pirep = new Pirep
        {
            SaIdentifier = said,
            ReportType = reportType,
        };

        foreach (var (code, raw) in fields)
        {
            var value = raw.Trim();
            switch (code)
            {
                case "OV":
                    pirep = pirep with { Location = ParseLocation(value, warnings) };
                    break;
                case "TM":
                    pirep = pirep with { Time = value };
                    break;
                case "FL":
                    pirep = pirep with { FlightLevel = string.IsNullOrWhiteSpace(value) ? "UNKN" : value };
                    break;
                case "TP":
                    pirep = pirep with { AircraftType = string.IsNullOrWhiteSpace(value) ? "UNKN" : value };
                    break;
                case "SK":
                    pirep = pirep with
                    {
                        SkyCoverRaw = value,
                        SkyCover = TryParseSkyLayers(value, warnings),
                    };
                    break;
                case "WX":
                    pirep = pirep with
                    {
                        WeatherRaw = value,
                        Weather = TryParseWeather(value),
                    };
                    break;
                case "TA":
                    if (int.TryParse(value.Replace("M", "-", System.StringComparison.OrdinalIgnoreCase),
                                      NumberStyles.Integer, CultureInfo.InvariantCulture, out var t))
                    {
                        pirep = pirep with { TemperatureC = t };
                    }
                    else
                    {
                        warnings.Add(new ParseWarning(ParseWarningLevel.Warning, $"Could not parse temperature: '{value}'.", "TA"));
                    }
                    break;
                case "WV":
                    var wind = value.Replace(" ", "", System.StringComparison.Ordinal);
                    if (wind.Length == 6 &&
                        int.TryParse(wind[..3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dir) &&
                        int.TryParse(wind[3..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var spd))
                    {
                        pirep = pirep with { WindDirection = dir, WindSpeedKt = spd };
                    }
                    else
                    {
                        warnings.Add(new ParseWarning(ParseWarningLevel.Warning, $"Wind must be 6 digits (direction+speed): '{value}'.", "WV"));
                    }
                    break;
                case "TB":
                    pirep = pirep with
                    {
                        TurbulenceRaw = value,
                        Turbulence = TryParseTurbulence(value),
                    };
                    break;
                case "IC":
                    pirep = pirep with
                    {
                        IcingRaw = value,
                        Icing = TryParseIcing(value),
                    };
                    break;
                case "RM":
                    pirep = pirep with { Remarks = value };
                    break;
                default:
                    warnings.Add(new ParseWarning(ParseWarningLevel.Warning, $"Unrecognized field /{code}.", code));
                    break;
            }
        }

        return new ParseResult(pirep, warnings);
    }

    private static IReadOnlyList<(string Code, string Value)> SplitFields(string text)
    {
        var result = new List<(string, string)>();
        var matches = FieldCodeRegex().Matches(text);
        for (var i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            var code = m.Groups[1].Value;
            var start = m.Index + m.Length;
            var end = (i + 1 < matches.Count) ? matches[i + 1].Index : text.Length;
            var raw = text[start..end];
            result.Add((code, raw));
        }
        return result;
    }

    private static Location ParseLocation(string value, List<ParseWarning> warnings)
    {
        var segments = new List<LocationSegment>();
        foreach (var part in value.Split('-', System.StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }
            var tokens = trimmed.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                continue;
            }
            var fix = tokens[0];
            string? rd = null;
            if (tokens.Length >= 2)
            {
                rd = tokens[1];
            }
            if (tokens.Length > 2)
            {
                warnings.Add(new ParseWarning(ParseWarningLevel.Info, $"Extra tokens in location segment '{trimmed}' preserved as remarks-style text.", "OV"));
            }
            segments.Add(new LocationSegment { Fix = fix, RadialDistance = rd });
        }
        return new Location { Segments = segments };
    }

    private static IReadOnlyList<CloudLayer>? TryParseSkyLayers(string value, List<ParseWarning> warnings)
    {
        var layers = new List<CloudLayer>();
        foreach (var layer in value.Split('/', System.StringSplitOptions.RemoveEmptyEntries))
        {
            var tokens = layer.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2)
            {
                warnings.Add(new ParseWarning(ParseWarningLevel.Warning, $"Could not parse sky layer '{layer}'.", "SK"));
                return null;
            }
            if (!System.Enum.TryParse<SkyCover>(tokens[1], out var cover))
            {
                warnings.Add(new ParseWarning(ParseWarningLevel.Warning, $"Unknown cloud cover symbol '{tokens[1]}'.", "SK"));
                return null;
            }
            layers.Add(new CloudLayer
            {
                Base = tokens[0],
                Cover = cover,
                Tops = tokens.Length >= 3 ? tokens[2] : null,
            });
        }
        return layers.Count > 0 ? layers : null;
    }

    private static Weather? TryParseWeather(string value)
    {
        var tokens = value.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return null;
        }
        int? fv = null;
        var startIdx = 0;
        if (tokens[0].StartsWith("FV", System.StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(tokens[0].AsSpan(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedFv))
        {
            fv = parsedFv;
            startIdx = 1;
        }
        var contractions = tokens.Skip(startIdx).ToList();
        return new Weather { FlightVisibilitySm = fv, Contractions = contractions };
    }

    private static Turbulence? TryParseTurbulence(string value)
    {
        var tokens = value.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return null;
        }
        var intensity = tokens[0] switch
        {
            "NEG" => TurbulenceIntensity.Negative,
            "LGT" => TurbulenceIntensity.LGT,
            "LGT-MOD" => TurbulenceIntensity.LGT_MOD,
            "MOD" => TurbulenceIntensity.MOD,
            "MOD-SVR" => TurbulenceIntensity.MOD_SVR,
            "SVR" => TurbulenceIntensity.SVR,
            "EXTRM" => TurbulenceIntensity.EXTRM,
            _ => (TurbulenceIntensity?)null,
        };
        if (intensity is null)
        {
            return null;
        }
        var type = TurbulenceType.None;
        string? band = null;
        for (var i = 1; i < tokens.Length; i++)
        {
            if (tokens[i] is "CAT" or "CHOP")
            {
                type = System.Enum.Parse<TurbulenceType>(tokens[i]);
            }
            else
            {
                band = tokens[i];
            }
        }
        return new Turbulence { Intensity = intensity.Value, Type = type, AltitudeBand = band };
    }

    private static Icing? TryParseIcing(string value)
    {
        var tokens = value.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return null;
        }
        var intensity = tokens[0] switch
        {
            "TRACE" => IcingIntensity.TRACE,
            "LGT" => IcingIntensity.LGT,
            "LGT-MDT" => IcingIntensity.LGT_MDT,
            "MDT" => IcingIntensity.MDT,
            "MDT-SVR" => IcingIntensity.MDT_SVR,
            "SVR" => IcingIntensity.SVR,
            _ => (IcingIntensity?)null,
        };
        if (intensity is null)
        {
            return null;
        }
        var type = IcingType.None;
        string? band = null;
        for (var i = 1; i < tokens.Length; i++)
        {
            if (tokens[i] is "RIME" or "CLR" or "MX")
            {
                type = System.Enum.Parse<IcingType>(tokens[i]);
            }
            else
            {
                band = tokens[i];
            }
        }
        return new Icing { Intensity = intensity.Value, Type = type, AltitudeBand = band };
    }
}
