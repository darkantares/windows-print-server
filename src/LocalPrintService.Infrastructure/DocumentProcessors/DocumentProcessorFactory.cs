using LocalPrintService.Domain.Contracts;
using LocalPrintService.Domain.Enums;

namespace LocalPrintService.Infrastructure.DocumentProcessors;

public sealed class DocumentProcessorFactory : IDocumentProcessorFactory
{
    private readonly Dictionary<DocumentType, IDocumentProcessor> _processors;

    public DocumentProcessorFactory(IEnumerable<IDocumentProcessor> processors)
    {
        _processors = processors.ToDictionary(p => p.SupportedType);
    }

    public IDocumentProcessor GetProcessor(DocumentType documentType)
    {
        if (_processors.TryGetValue(documentType, out var processor))
            return processor;

        throw new NotSupportedException($"Document type '{documentType}' is not supported.");
    }

    public bool SupportsType(DocumentType documentType)
    {
        return _processors.ContainsKey(documentType);
    }

    public IReadOnlyList<DocumentType> SupportedTypes => _processors.Keys.ToList().AsReadOnly();
}
