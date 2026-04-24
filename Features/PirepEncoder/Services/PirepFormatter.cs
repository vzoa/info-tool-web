using System.Globalization;
using System.Text;
using ZoaReference.Features.PirepEncoder.Models;

namespace ZoaReference.Features.PirepEncoder.Services;

/// <summary>
/// Produces a canonical encoded PIREP string from a <see cref="Pirep"/>.
/// Per FAA Form 7110-2. Pure function: no side effects, no UI refs.
/// </summary>
public static class PirepFormatter
{
    public static string Format(Pirep pirep, PirepSettings? settings = null)
    {
        settings ??= new PirepSettings();
        var sb = new StringBuilder();

        var said = !string.IsNullOrWhiteSpace(pirep.SaIdentifier) ? pirep.SaIdentifier : settings.DefaultSaIdentifier;
        if (settings.PrefixWithSaIdentifier && !string.IsNullOrWhiteSpace(said))
        {
            sb.Append(said).Append(' ');
        }

        sb.Append(pirep.ReportType.ToCode());

        sb.Append(" /OV ").Append(LocationFormatter.Format(pirep.Location));
        sb.Append("/TM ").Append(pirep.Time);
        sb.Append("/FL").Append(pirep.FlightLevel);
        sb.Append("/TP ").Append(pirep.AircraftType);

        var firstOptional = true;
        void AppendOptional(string code, string value)
        {
            if (firstOptional)
            {
                sb.Append(' ');
                firstOptional = false;
            }
            sb.Append(code).Append(' ').Append(value);
        }

        var sk = ResolveSky(pirep);
        if (sk is not null)
        {
            AppendOptional("/SK", sk);
        }

        var wx = ResolveWeather(pirep);
        if (wx is not null)
        {
            AppendOptional("/WX", wx);
        }

        if (pirep.TemperatureC is int t)
        {
            AppendOptional("/TA", FormatTemperature(t));
        }

        if (pirep.WindDirection is int dir && pirep.WindSpeedKt is int spd)
        {
            AppendOptional("/WV", FormatWind(dir, spd));
        }

        var tb = ResolveTurbulence(pirep);
        if (tb is not null)
        {
            AppendOptional("/TB", tb);
        }

        var ic = ResolveIcing(pirep);
        if (ic is not null)
        {
            AppendOptional("/IC", ic);
        }

        if (!string.IsNullOrWhiteSpace(pirep.Remarks))
        {
            AppendOptional("/RM", pirep.Remarks.Trim());
        }

        return sb.ToString();
    }

    private static string? ResolveSky(Pirep p)
    {
        if (!string.IsNullOrWhiteSpace(p.SkyCoverRaw))
        {
            return p.SkyCoverRaw.Trim();
        }
        if (p.SkyCover is { Count: > 0 } layers)
        {
            return SkyFormatter.Format(layers);
        }
        return null;
    }

    private static string? ResolveWeather(Pirep p)
    {
        if (!string.IsNullOrWhiteSpace(p.WeatherRaw))
        {
            return p.WeatherRaw.Trim();
        }
        if (p.Weather is { } wx)
        {
            var s = WeatherFormatter.Format(wx);
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
        return null;
    }

    private static string? ResolveTurbulence(Pirep p)
    {
        if (!string.IsNullOrWhiteSpace(p.TurbulenceRaw))
        {
            return p.TurbulenceRaw.Trim();
        }
        if (p.Turbulence is { } tb)
        {
            return TurbulenceFormatter.Format(tb);
        }
        return null;
    }

    private static string? ResolveIcing(Pirep p)
    {
        if (!string.IsNullOrWhiteSpace(p.IcingRaw))
        {
            return p.IcingRaw.Trim();
        }
        if (p.Icing is { } ic)
        {
            return IcingFormatter.Format(ic);
        }
        return null;
    }

    public static string FormatTemperature(int celsius)
    {
        var magnitude = System.Math.Abs(celsius).ToString("D2", CultureInfo.InvariantCulture);
        return celsius < 0 ? "-" + magnitude : magnitude;
    }

    public static string FormatWind(int direction, int speedKt)
    {
        var dir = direction.ToString("D3", CultureInfo.InvariantCulture);
        var spd = speedKt.ToString("D3", CultureInfo.InvariantCulture);
        return dir + spd;
    }
}
