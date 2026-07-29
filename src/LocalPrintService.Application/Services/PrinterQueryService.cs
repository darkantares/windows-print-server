using LocalPrintService.Application.DTOs.Responses;
using LocalPrintService.Domain.Contracts;

namespace LocalPrintService.Application.Services;

public sealed class PrinterQueryService
{
    private readonly IPrinterManager _printerManager;

    public PrinterQueryService(IPrinterManager printerManager)
    {
        _printerManager = printerManager;
    }

    public IReadOnlyList<PrinterResponse> GetAllPrinters()
    {
        return _printerManager.GetAllPrinters()
            .Select(p => new PrinterResponse
            {
                Id = p.Id,
                Name = p.Name,
                DriverName = p.DriverName,
                PortName = p.PortName,
                Status = p.Status.ToString(),
                IsDefault = p.IsDefault,
                IsAvailable = p.IsAvailable,
                Capabilities = p.Capabilities.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries),
                SupportedPaperSizes = p.SupportedPaperSizes,
                SupportedResolutions = p.SupportedResolutions
            })
            .ToList()
            .AsReadOnly();
    }

    public PrinterResponse? GetPrinterByName(string printerName)
    {
        var printer = _printerManager.GetPrinterByName(printerName);
        return printer is null ? null : MapToResponse(printer);
    }

    public PrinterResponse? GetDefaultPrinter()
    {
        var printer = _printerManager.GetDefaultPrinter();
        return printer is null ? null : MapToResponse(printer);
    }

    private static PrinterResponse MapToResponse(Domain.Entities.PrinterInfo printer)
    {
        return new PrinterResponse
        {
            Id = printer.Id,
            Name = printer.Name,
            DriverName = printer.DriverName,
            PortName = printer.PortName,
            Status = printer.Status.ToString(),
            IsDefault = printer.IsDefault,
            IsAvailable = printer.IsAvailable,
            Capabilities = printer.Capabilities.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries),
            SupportedPaperSizes = printer.SupportedPaperSizes,
            SupportedResolutions = printer.SupportedResolutions
        };
    }
}
