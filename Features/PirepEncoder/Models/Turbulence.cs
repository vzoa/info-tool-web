namespace ZoaReference.Features.PirepEncoder.Models;

public enum TurbulenceIntensity
{
    Negative,
    LGT,
    LGT_MOD,
    MOD,
    MOD_SVR,
    SVR,
    EXTRM,
}

public enum TurbulenceType
{
    None,
    CAT,
    CHOP,
}

public sealed record Turbulence
{
    public TurbulenceIntensity Intensity { get; init; } = TurbulenceIntensity.LGT;

    public TurbulenceType Type { get; init; } = TurbulenceType.None;

    public string? AltitudeBand { get; init; }
}
