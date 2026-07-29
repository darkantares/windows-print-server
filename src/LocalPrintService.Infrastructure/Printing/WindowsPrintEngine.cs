using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using LocalPrintService.Domain.Contracts;
using LocalPrintService.Domain.Entities;
using LocalPrintService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LocalPrintService.Infrastructure.Printing;

public sealed class WindowsPrintEngine : IPrintEngine
{
    private readonly ILogger<WindowsPrintEngine> _logger;

    private static readonly HashSet<DocumentType> RawTypes = new()
    {
        DocumentType.Text, DocumentType.EscPos, DocumentType.Zpl, DocumentType.Raw
    };

    public WindowsPrintEngine(ILogger<WindowsPrintEngine> logger)
    {
        _logger = logger;
    }

    public async Task<bool> PrintAsync(PrintJob job, byte[] documentData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[PrintEngine] Job {JobId}: Type={Type}, Printer={Printer}, DataSize={Size} bytes",
            job.Id, job.DocumentType, job.Printer, documentData.Length);

        if (!PrinterExists(job.Printer))
        {
            _logger.LogError("[PrintEngine] Printer '{Printer}' NOT FOUND for job {JobId}", job.Printer, job.Id);
            return false;
        }

        _logger.LogInformation("[PrintEngine] Printer '{Printer}' exists, choosing path...", job.Printer);

        if (IsPdf(documentData))
        {
            _logger.LogInformation("[PrintEngine] Path: PDF -> PrintPdfAsync");
            return await PrintPdfAsync(documentData, job, cancellationToken);
        }

        if (IsImage(documentData))
        {
            _logger.LogInformation("[PrintEngine] Path: Image -> PrintImageAsync");
            return await PrintImageAsync(documentData, job, cancellationToken);
        }

        _logger.LogInformation("[PrintEngine] Path: Raw/Text -> PrintRawAsync");
        return await PrintRawAsync(documentData, job, cancellationToken);
    }

    public bool SupportsDocumentType(string documentType)
    {
        return Enum.TryParse<DocumentType>(documentType, true, out _);
    }

    private async Task<bool> PrintRawAsync(byte[] data, PrintJob job, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                for (int i = 0; i < job.Copies; i++)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    _logger.LogInformation("[PrintEngine] Raw copy {Copy}/{Total}, {Size} bytes -> '{Printer}'",
                        i + 1, job.Copies, data.Length, job.Printer);

                    var (success, error) = RawPrinterHelper.SendBytesToPrinter(job.Printer, data, $"Job-{job.Id}");
                    if (!success)
                    {
                        _logger.LogError("[PrintEngine] RawPrinterHelper FAILED for job {JobId}: {Error}", job.Id, error);
                        return false;
                    }
                    _logger.LogInformation("[PrintEngine] RawPrinterHelper OK: {Result}", error);
                }

                stopwatch.Stop();
                _logger.LogInformation("[PrintEngine] Job {JobId} (Raw) DONE in {Ms}ms", job.Id, stopwatch.ElapsedMilliseconds);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PrintEngine] ERROR in PrintRawAsync for job {JobId}", job.Id);
                return false;
            }
        }, cancellationToken);
    }

    private async Task<bool> PrintPdfAsync(byte[] pdfData, PrintJob job, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                _logger.LogInformation("[PrintEngine] Extracting text from PDF via Docnet...");
                var text = PdfRenderer.ExtractTextFromPdf(pdfData);

                _logger.LogInformation("[PrintEngine] PDF text extracted: {Length} chars", text.Length);
                _logger.LogInformation("[PrintEngine] PDF text content: '{Text}'", text.Replace("\r\n", "\\n").Replace("\n", "\\n"));
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("[PrintEngine] PDF has no extractable text, nothing to print");
                    return false;
                }

                var textBytes = Encoding.UTF8.GetBytes(text);

                for (int copy = 0; copy < job.Copies; copy++)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    _logger.LogInformation("[PrintEngine] PDF text copy {Copy}/{Total}", copy + 1, job.Copies);

                    var (success, error) = RawPrinterHelper.SendBytesToPrinter(job.Printer, textBytes, $"Job-{job.Id}-PDF");
                    if (!success)
                    {
                        _logger.LogError("[PrintEngine] PDF raw print FAILED: {Error}", error);
                        return false;
                    }
                }

                stopwatch.Stop();
                _logger.LogInformation("[PrintEngine] Job {JobId} (PDF) DONE in {Ms}ms", job.Id, stopwatch.ElapsedMilliseconds);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PrintEngine] ERROR in PrintPdfAsync for job {JobId}", job.Id);
                return false;
            }
        }, cancellationToken);
    }

    private async Task<bool> PrintImageAsync(byte[] imageData, PrintJob job, CancellationToken cancellationToken)
    {
        _logger.LogWarning("[PrintEngine] Image printing not supported on thermal printers. Job {JobId}", job.Id);
        return false;
    }

    private static bool IsPdf(byte[] data)
    {
        if (data.Length < 4) return false;
        return data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46;
    }

    private static bool IsImage(byte[] data)
    {
        if (data.Length < 4) return false;

        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) return true;
        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) return true;
        if (data[0] == 0x42 && data[1] == 0x4D) return true;
        if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46) return true;
        if (data[0] == 0x49 && data[1] == 0x49 && data[2] == 0x2A && data[3] == 0x00) return true;
        if (data[0] == 0x4D && data[1] == 0x4D && data[2] == 0x00 && data[3] == 0x2A) return true;

        return false;
    }

    private static bool PrinterExists(string printerName)
    {
        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            if (string.Equals(printer, printerName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
