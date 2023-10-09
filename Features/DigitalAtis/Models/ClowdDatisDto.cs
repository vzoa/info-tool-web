using System.Text.Json.Serialization;

namespace ZoaReference.Features.DigitalAtis.Models;

public class ClowdDatisDto
{
    [JsonPropertyName("airport")]
    public string Airport { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("datis")]
    public string Datis { get; set; }
}
