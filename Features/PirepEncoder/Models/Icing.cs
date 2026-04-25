namespace ZoaReference.Features.PirepEncoder.Models;

public enum IcingIntensity
{
    TRACE,
    LGT,
    LGT_MDT,
    MDT,
    MDT_SVR,
    SVR,
}

public enum IcingType
{
    None,
    RIME,
    CLR,
    MX,
}

public sealed record Icing
{
    public IcingIntensity Intensity { get; init; } = IcingIntensity.LGT;

    public IcingType Type { get; init; } = IcingType.None;

    public string? AltitudeBand { get; init; }
}
