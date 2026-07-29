using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Text.RegularExpressions;
using LocalPrintService.Domain.Contracts;
using LocalPrintService.Domain.Enums;
using LocalPrintService.Domain.Errors;
using Microsoft.Extensions.Logging;

namespace LocalPrintService.Infrastructure.DocumentProcessors;

public sealed partial class HtmlProcessor : IDocumentProcessor
{
    private readonly ILogger<HtmlProcessor> _logger;

    public HtmlProcessor(ILogger<HtmlProcessor> logger)
    {
        _logger = logger;
    }

    public DocumentType SupportedType => DocumentType.Html;

    public async Task<byte[]> ProcessAsync(string payload, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Processing HTML document, length={Length}", payload.Length);

        try
        {
            return await RenderHtmlToImageAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HTML image render failed, falling back to text extraction");
            return await ExtractTextFromHtmlAsync(payload);
        }
    }

    private static async Task<byte[]> RenderHtmlToImageAsync(string html, CancellationToken cancellationToken)
    {
        var fullHtml = EnsureHtmlDocument(html);

        using var bitmap = new Bitmap(800, 600);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var text = StripHtmlTags(fullHtml);
        var lines = WordWrap(text, 780);
        var y = 20;

        using var font = new Font("Consolas", 10);
        using var brush = new SolidBrush(Color.Black);

        foreach (var line in lines)
        {
            if (y > 580) break;
            graphics.DrawString(line, font, brush, 10, y);
            y += 16;
        }

        using var outputStream = new MemoryStream();
        bitmap.Save(outputStream, ImageFormat.Bmp);
        return await Task.FromResult(outputStream.ToArray());
    }

    private static async Task<byte[]> ExtractTextFromHtmlAsync(string html)
    {
        var text = StripHtmlTags(html);
        var bytes = Encoding.UTF8.GetBytes(text);
        return await Task.FromResult(bytes);
    }

    private static string EnsureHtmlDocument(string html)
    {
        if (html.Contains("<html", StringComparison.OrdinalIgnoreCase))
            return html;

        var css = @"body { font-family: Arial, sans-serif; margin: 20px; }
table { border-collapse: collapse; width: 100%; }
th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
th { background-color: #f2f2f2; }";

        return "<!DOCTYPE html><html><head><meta charset=\"utf-8\">" +
               "<style>" + css + "</style></head><body>" + html + "</body></html>";
    }

    private static string StripHtmlTags(string html)
    {
        var withoutScripts = ScriptTagRegex().Replace(html, string.Empty);
        var withoutStyles = StyleTagRegex().Replace(withoutScripts, string.Empty);
        var withoutTags = HtmlTagRegex().Replace(withoutStyles, " ");
        var decoded = System.Net.WebUtility.HtmlDecode(withoutTags);
        var collapsed = WhitespaceRegex().Replace(decoded, " ").Trim();
        return collapsed;
    }

    private static List<string> WordWrap(string text, int maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            if (currentLine.Length + word.Length + 1 > maxWidth)
            {
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                }
                currentLine.Append(word);
            }
            else
            {
                if (currentLine.Length > 0)
                    currentLine.Append(' ');
                currentLine.Append(word);
            }
        }

        if (currentLine.Length > 0)
            lines.Add(currentLine.ToString());

        return lines;
    }

    [GeneratedRegex(@"<script[^>]*>.*?</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex(@"<style[^>]*>.*?</style>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex StyleTagRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
