using System.Drawing;
using System.Drawing.Imaging;
using LocalPrintService.Domain.Contracts;
using LocalPrintService.Domain.Enums;
using LocalPrintService.Domain.Errors;
using LocalPrintService.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace LocalPrintService.Infrastructure.DocumentProcessors;

public sealed class ImageProcessor : IDocumentProcessor
{
    private readonly ILogger<ImageProcessor> _logger;

    public ImageProcessor(ILogger<ImageProcessor> logger)
    {
        _logger = logger;
    }

    public DocumentType SupportedType => DocumentType.Image;

    public async Task<byte[]> ProcessAsync(string payload, CancellationToken cancellationToken = default)
    {
        var imageBytes = await DecodeImageAsync(payload);
        using var stream = new MemoryStream(imageBytes);
        using var image = Image.FromStream(stream);

        _logger.LogDebug(
            "Processing image: {Width}x{Height}, Format={Format}",
            image.Width, image.Height, image.RawFormat);

        return await ConvertToPrintableFormatAsync(image, cancellationToken);
    }

    private static async Task<byte[]> DecodeImageAsync(string payload)
    {
        var trimmed = payload.Trim();

        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = trimmed.IndexOf(',');
            if (commaIndex > 0)
                trimmed = trimmed[(commaIndex + 1)..];
        }

        try
        {
            return await Task.Run(() => Convert.FromBase64String(trimmed));
        }
        catch (FormatException)
        {
            throw new DomainException(new InvalidDocumentError("Invalid Base64 image data"));
        }
    }

    private static Task<byte[]> ConvertToPrintableFormatAsync(Image image, CancellationToken cancellationToken)
    {
        using var outputStream = new MemoryStream();

        var format = image.RawFormat.Equals(ImageFormat.MemoryBmp)
            ? ImageFormat.Bmp
            : image.RawFormat;

        if (!IsPrintableFormat(format))
            format = ImageFormat.Bmp;

        image.Save(outputStream, format);
        return Task.FromResult(outputStream.ToArray());
    }

    private static bool IsPrintableFormat(ImageFormat format)
    {
        return format.Equals(ImageFormat.Bmp)
            || format.Equals(ImageFormat.Png)
            || format.Equals(ImageFormat.Jpeg)
            || format.Equals(ImageFormat.Gif)
            || format.Equals(ImageFormat.Tiff);
    }
}
