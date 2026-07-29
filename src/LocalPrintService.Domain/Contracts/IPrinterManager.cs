using LocalPrintService.Domain.Entities;

namespace LocalPrintService.Domain.Contracts;

public interface IPrinterManager
{
    IReadOnlyList<PrinterInfo> GetAllPrinters();
    PrinterInfo? GetPrinterByName(string printerName);
    PrinterInfo? GetDefaultPrinter();
    bool PrinterExists(string printerName);
}
