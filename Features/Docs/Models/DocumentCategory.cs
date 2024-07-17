using System.Text.Json.Serialization;

namespace ZoaReference.Features.Docs.Models;

public record DocumentCategory
{
    public string Name { get; init; } = "";
    public List<Document> Documents { get; init; } = [];
}
