using System.Text.Json.Serialization;

namespace ZoaReference.Features.Docs.Models;

public class ZoaDocumentCategory
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("files")]
    public List<ZoaDocument> Documents { get; set; } = [];

    public DocumentCategory ToGenericDocumentCategory()
    {
        return new DocumentCategory
        {
            Name = Name,
            Documents = Documents.Select(d => new Document(d.Name, d.Url)).ToList()
        };
    }
}