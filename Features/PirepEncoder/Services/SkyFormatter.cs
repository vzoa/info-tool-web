using System.Collections.Generic;
using System.Text;
using ZoaReference.Features.PirepEncoder.Models;

namespace ZoaReference.Features.PirepEncoder.Services;

public static class SkyFormatter
{
    public static string Format(IReadOnlyList<CloudLayer> layers)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < layers.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('/');
            }
            var layer = layers[i];
            sb.Append(layer.Base).Append(' ').Append(layer.Cover);
            if (!string.IsNullOrWhiteSpace(layer.Tops))
            {
                sb.Append(' ').Append(layer.Tops);
            }
        }
        return sb.ToString();
    }
}
