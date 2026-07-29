namespace LocalPrintService.Domain.Errors;

public sealed class PrintEngineError : DomainError
{
    public string PrinterName { get; }

    public PrintEngineError(string printerName, string message)
        : base("PRINT_ENGINE_ERROR", $"Print engine error for '{printerName}': {message}.")
    {
        PrinterName = printerName;
    }
}
