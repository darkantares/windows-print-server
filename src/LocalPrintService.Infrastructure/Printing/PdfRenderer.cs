using System.Text;
using Docnet.Core;

namespace LocalPrintService.Infrastructure.Printing;

public static class PdfRenderer
{
    public static string ExtractTextFromPdf(byte[] pdfData)
    {
        using var library = DocLib.Instance;
        using var docReader = library.GetDocReader(pdfData, new Docnet.Core.Models.PageDimensions(72, 72));

        var pageCount = docReader.GetPageCount();
        var allText = new StringBuilder();

        for (int i = 0; i < pageCount; i++)
        {
            using var pageReader = docReader.GetPageReader(i);
            var text = pageReader.GetText();

            if (!string.IsNullOrWhiteSpace(text))
            {
                allText.AppendLine(text);
                allText.AppendLine();
            }
        }

        return allText.ToString().Trim();
    }
}
