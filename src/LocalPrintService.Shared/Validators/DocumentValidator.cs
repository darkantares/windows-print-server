using LocalPrintService.Domain.Configuration;
using LocalPrintService.Domain.Errors;
using LocalPrintService.Domain.Results;

namespace LocalPrintService.Shared.Validators;

public static class DocumentValidator
{
    public static Result<string> Validate(string payload, string documentType, PrintServiceSettings settings)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return Result.Fail<string>(new InvalidDocumentError("Payload cannot be empty"));

        var expectedByteCount = CalculatePayloadSize(payload, documentType);
        if (expectedByteCount > settings.MaxDocumentSizeBytes)
        {
            var maxSizeMb = settings.MaxDocumentSizeBytes / (1024 * 1024);
            return Result.Fail<string>(new InvalidDocumentError(
                $"Document size ({expectedByteCount / (1024 * 1024)}MB) exceeds maximum allowed ({maxSizeMb}MB)"));
        }

        return Result.Ok(payload);
    }

    public static Result<string> ValidateMimeType(string mimeType, PrintServiceSettings settings)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return Result.Fail<string>(new InvalidDocumentError("MIME type cannot be empty"));

        if (!settings.AllowedMimeTypes.Contains(mimeType.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase))
        {
            return Result.Fail<string>(new InvalidDocumentError(
                $"MIME type '{mimeType}' is not allowed. Allowed types: {string.Join(", ", settings.AllowedMimeTypes)}"));
        }

        return Result.Ok(mimeType);
    }

    private static long CalculatePayloadSize(string payload, string documentType)
    {
        var isBase64 = documentType is "pdf" or "image";
        if (isBase64)
        {
            try
            {
                return Convert.FromBase64String(payload).Length;
            }
            catch (FormatException)
            {
                return payload.Length;
            }
        }

        return System.Text.Encoding.UTF8.GetByteCount(payload);
    }
}
