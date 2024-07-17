using System.Text.Json.Serialization;

namespace ZoaReference.Features.Docs.Models;

public class CustomDocumentCategory
{
    public string Name { get; set; } = "";

    public List<CustomDocument> Documents { get; set; } = [];
    
    public DocumentCategory ToGenericDocumentCategory()
    {
        return new DocumentCategory
        {
            Name = Name,
            Documents = Documents.Select(d => new Document(d.Name, d.Url)).ToList()
        };
    }
}
