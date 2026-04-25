using System.Text;
using ZoaReference.Features.PirepEncoder.Models;

namespace ZoaReference.Features.PirepEncoder.Services;

public static class IcingFormatter
{
    public static string Format(Icing ic)
    {
        var sb = new StringBuilder();
        sb.Append(IntensityCode(ic.Intensity));
        if (ic.Type != IcingType.None)
        {
            sb.Append(' ').Append(ic.Type);
        }
        if (!string.IsNullOrWhiteSpace(ic.AltitudeBand))
        {
            sb.Append(' ').Append(ic.AltitudeBand);
        }
        return sb.ToString();
    }

    public static string IntensityCode(IcingIntensity i) => i switch
    {
        IcingIntensity.TRACE => "TRACE",
        IcingIntensity.LGT => "LGT",
        IcingIntensity.LGT_MDT => "LGT-MDT",
        IcingIntensity.MDT => "MDT",
        IcingIntensity.MDT_SVR => "MDT-SVR",
        IcingIntensity.SVR => "SVR",
        _ => "LGT",
    };
}
