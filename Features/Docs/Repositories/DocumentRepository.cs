using ZoaReference.Features.Docs.Models;

namespace ZoaReference.Features.Docs.Repositories;

public class DocumentRepository
{
    private readonly List<DocumentCategory> _repository = new List<DocumentCategory>();

    public IEnumerable<DocumentCategory> Categories  => _repository;

    public IEnumerable<Document> Documents => _repository.SelectMany(c => c.Documents);

    public void AddDocumentCategory(DocumentCategory documentCategory) => _repository.Add(documentCategory);

    public bool TryGetDocumentCategory(string name, out DocumentCategory? documentCategory)
    {
        documentCategory = _repository.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return documentCategory is not null;
    }

    public void ClearAllDocumentCategories() => _repository.Clear();

    public void AddDocumentCategories(IEnumerable<DocumentCategory> categories) => _repository.AddRange(categories);

}