using System.Text;
using ZoaReference.Features.PirepEncoder.Models;

namespace ZoaReference.Features.PirepEncoder.Services;

public static class TurbulenceFormatter
{
    public static string Format(Turbulence tb)
    {
        var sb = new StringBuilder();
        sb.Append(IntensityCode(tb.Intensity));
        if (tb.Type != TurbulenceType.None)
        {
            sb.Append(' ').Append(tb.Type);
        }
        if (!string.IsNullOrWhiteSpace(tb.AltitudeBand))
        {
            sb.Append(' ').Append(tb.AltitudeBand);
        }
        return sb.ToString();
    }

    public static string IntensityCode(TurbulenceIntensity i) => i switch
    {
        TurbulenceIntensity.Negative => "NEG",
        TurbulenceIntensity.LGT => "LGT",
        TurbulenceIntensity.LGT_MOD => "LGT-MOD",
        TurbulenceIntensity.MOD => "MOD",
        TurbulenceIntensity.MOD_SVR => "MOD-SVR",
        TurbulenceIntensity.SVR => "SVR",
        TurbulenceIntensity.EXTRM => "EXTRM",
        _ => "LGT",
    };
}
