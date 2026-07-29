using LocalPrintService.Domain.Enums;

namespace LocalPrintService.Domain.Contracts;

public interface IDocumentProcessorFactory
{
    IDocumentProcessor GetProcessor(DocumentType documentType);
    bool SupportsType(DocumentType documentType);
    IReadOnlyList<DocumentType> SupportedTypes { get; }
}
