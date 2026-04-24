using System.Text;
using ZoaReference.Features.PirepEncoder.Models;

namespace ZoaReference.Features.PirepEncoder.Services;

public static class LocationFormatter
{
    public static string Format(Location location)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < location.Segments.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('-');
            }
            var segment = location.Segments[i];
            sb.Append(segment.Fix);
            if (!string.IsNullOrWhiteSpace(segment.RadialDistance))
            {
                sb.Append(' ').Append(segment.RadialDistance);
            }
        }
        return sb.ToString();
    }
}
