using System.Text;
using LocalPrintService.Domain.Contracts;
using LocalPrintService.Domain.Enums;

namespace LocalPrintService.Infrastructure.DocumentProcessors;

public sealed class TextProcessor : IDocumentProcessor
{
    public DocumentType SupportedType => DocumentType.Text;

    public Task<byte[]> ProcessAsync(string payload, CancellationToken cancellationToken = default)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        return Task.FromResult(bytes);
    }
}
