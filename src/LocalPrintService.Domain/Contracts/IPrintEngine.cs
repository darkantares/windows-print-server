using LocalPrintService.Domain.Entities;

namespace LocalPrintService.Domain.Contracts;

public interface IPrintEngine
{
    Task<bool> PrintAsync(PrintJob job, byte[] documentData, CancellationToken cancellationToken = default);
    bool SupportsDocumentType(string documentType);
}
