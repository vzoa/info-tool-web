using System.Collections.Generic;

namespace ZoaReference.Features.PirepEncoder.Models;

public sealed record Pirep
{
    public string? SaIdentifier { get; init; }

    public ReportType ReportType { get; init; } = ReportType.Routine;

    public Location Location { get; init; } = new();

    public string Time { get; init; } = "";

    public string FlightLevel { get; init; } = "UNKN";

    public string AircraftType { get; init; } = "UNKN";

    public IReadOnlyList<CloudLayer>? SkyCover { get; init; }

    public string? SkyCoverRaw { get; init; }

    public Weather? Weather { get; init; }

    public string? WeatherRaw { get; init; }

    public int? TemperatureC { get; init; }

    public int? WindDirection { get; init; }

    public int? WindSpeedKt { get; init; }

    public Turbulence? Turbulence { get; init; }

    public string? TurbulenceRaw { get; init; }

    public Icing? Icing { get; init; }

    public string? IcingRaw { get; init; }

    public string? Remarks { get; init; }
}
