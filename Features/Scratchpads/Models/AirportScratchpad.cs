using System.Text.Json.Serialization;

namespace ZoaReference.Features.Scratchpads.Models;

public record AirportScratchpad(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("scratchpads")] IReadOnlyList<Scratchpad> Scratchpads
);

public record Scratchpad(
    [property: JsonPropertyName("entry")] string Entry,
    [property: JsonPropertyName("description")] string Description
);
