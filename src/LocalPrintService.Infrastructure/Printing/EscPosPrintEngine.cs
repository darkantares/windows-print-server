using LocalPrintService.Domain.Contracts;
using Microsoft.Extensions.Logging;

namespace LocalPrintService.Infrastructure.Printing;

public sealed class EscPosPrintEngine : IPrintEngine
{
    private readonly ILogger<EscPosPrintEngine> _logger;

    public EscPosPrintEngine(ILogger<EscPosPrintEngine> logger)
    {
        _logger = logger;
    }

    public Task<bool> PrintAsync(Domain.Entities.PrintJob job, byte[] documentData, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("EscPosPrintEngine not yet implemented. Job {JobId} will not be printed.", job.Id);
        return Task.FromResult(false);
    }

    public bool SupportsDocumentType(string documentType)
    {
        return string.Equals(documentType, "escpos", StringComparison.OrdinalIgnoreCase);
    }
}
