namespace LocalPrintService.Shared.Constants;

public static class MimeTypes
{
    public const string TextPlain = "text/plain";
    public const string ApplicationPdf = "application/pdf";
    public const string ImagePng = "image/png";
    public const string ImageJpeg = "image/jpeg";
    public const string ImageBmp = "image/bmp";
    public const string ImageGif = "image/gif";
    public const string TextHtml = "text/html";
    public const string ApplicationOctetStream = "application/octet-stream";

    public static bool IsValidMimeType(string mimeType)
    {
        return mimeType.ToLowerInvariant() switch
        {
            TextPlain => true,
            ApplicationPdf => true,
            ImagePng => true,
            ImageJpeg => true,
            ImageBmp => true,
            ImageGif => true,
            TextHtml => true,
            ApplicationOctetStream => true,
            _ => false
        };
    }
}
