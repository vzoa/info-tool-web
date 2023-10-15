using System.Text.Json.Serialization;

namespace ZoaReference.Features.Docs.Models;

public record struct Document
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("details")]
    public string Details { get; set; }

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}