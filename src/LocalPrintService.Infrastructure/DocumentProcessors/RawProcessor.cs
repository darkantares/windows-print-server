using System.Text;
using LocalPrintService.Domain.Contracts;
using LocalPrintService.Domain.Enums;

namespace LocalPrintService.Infrastructure.DocumentProcessors;

public sealed class RawProcessor : IDocumentProcessor
{
    public DocumentType SupportedType => DocumentType.Raw;

    public Task<byte[]> ProcessAsync(string payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = Convert.FromBase64String(payload);
            return Task.FromResult(bytes);
        }
        catch (FormatException)
        {
            return Task.FromResult(Encoding.UTF8.GetBytes(payload));
        }
    }
}
