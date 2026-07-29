namespace LocalPrintService.Domain.Errors;

public sealed class PrinterNotFoundError : DomainError
{
    public string PrinterName { get; }

    public PrinterNotFoundError(string printerName)
        : base("PRINTER_NOT_FOUND", $"Printer '{printerName}' was not found or is not available.")
    {
        PrinterName = printerName;
    }
}
