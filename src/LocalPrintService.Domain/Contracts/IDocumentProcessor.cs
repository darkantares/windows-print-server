using LocalPrintService.Domain.Enums;

namespace LocalPrintService.Domain.Contracts;

public interface IDocumentProcessor
{
    DocumentType SupportedType { get; }
    Task<byte[]> ProcessAsync(string payload, CancellationToken cancellationToken = default);
}
