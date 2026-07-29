using LocalPrintService.Domain.Contracts;
using LocalPrintService.Domain.Enums;

namespace LocalPrintService.Infrastructure.DocumentProcessors;

public sealed class PdfProcessor : IDocumentProcessor
{
    public DocumentType SupportedType => DocumentType.Pdf;

    public Task<byte[]> ProcessAsync(string payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = Convert.FromBase64String(payload);
            return Task.FromResult(bytes);
        }
        catch (FormatException)
        {
            return Task.FromResult(System.Text.Encoding.UTF8.GetBytes(payload));
        }
    }
}
