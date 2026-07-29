using System.Drawing.Printing;
using System.Management;
using LocalPrintService.Domain.Contracts;
using LocalPrintService.Domain.Entities;
using LocalPrintService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LocalPrintService.Infrastructure.Printing;

public sealed class WindowsPrinterManager : IPrinterManager
{
    private readonly ILogger<WindowsPrinterManager> _logger;

    public WindowsPrinterManager(ILogger<WindowsPrinterManager> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<PrinterInfo> GetAllPrinters()
    {
        try
        {
            var printers = new List<PrinterInfo>();
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer");
            using var collection = searcher.Get();

            foreach (ManagementObject obj in collection)
            {
                try
                {
                    var info = MapToPrinterInfo(obj);
                    if (info is not null)
                        printers.Add(info);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to map printer object");
                }
            }

            _logger.LogDebug("Found {Count} printers", printers.Count);
            return printers.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate printers via WMI");
            return [];
        }
    }

    public PrinterInfo? GetPrinterByName(string printerName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT * FROM Win32_Printer WHERE Name = '{printerName.Replace("'", "''")}'");
            using var collection = searcher.Get();

            foreach (ManagementObject obj in collection)
            {
                return MapToPrinterInfo(obj);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get printer '{PrinterName}'", printerName);
            return null;
        }
    }

    public PrinterInfo? GetDefaultPrinter()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer WHERE Default = TRUE");
            using var collection = searcher.Get();

            foreach (ManagementObject obj in collection)
            {
                return MapToPrinterInfo(obj);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get default printer");
            return null;
        }
    }

    public bool PrinterExists(string printerName)
    {
        return GetPrinterByName(printerName) is not null;
    }

    private PrinterInfo? MapToPrinterInfo(ManagementObject obj)
    {
        try
        {
            var name = obj["Name"]?.ToString() ?? string.Empty;
            var driverName = obj["DriverName"]?.ToString() ?? string.Empty;
            var portName = obj["PortName"]?.ToString() ?? string.Empty;
            var defaultPrinter = obj["Default"] is true;
            var printerStatus = Convert.ToUInt16(obj["PrinterStatus"]);
            var status = MapPrinterStatus(printerStatus);
            var caps = MapCapabilities(obj);

            return new PrinterInfo
            {
                Id = name.ToLowerInvariant().Replace(" ", "-"),
                Name = name,
                DriverName = driverName,
                PortName = portName,
                Status = status,
                IsDefault = defaultPrinter,
                IsAvailable = status == PrinterStatus.Ready,
                Capabilities = caps,
                SupportedPaperSizes = GetPaperSizes(name),
                SupportedResolutions = GetResolutions(obj)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to map printer object to PrinterInfo");
            return null;
        }
    }

    private static PrinterStatus MapPrinterStatus(ushort printerStatus)
    {
        return printerStatus switch
        {
            1 => PrinterStatus.Other,
            2 => PrinterStatus.Unknown,
            3 => PrinterStatus.Ready,
            4 => PrinterStatus.Paused,
            5 => PrinterStatus.Error,
            6 => PrinterStatus.Offline,
            _ => PrinterStatus.Unknown
        };
    }

    private static PrinterCapabilities MapCapabilities(ManagementObject obj)
    {
        var caps = PrinterCapabilities.None;

        try
        {
            var capabilityDescriptions = obj["CapabilityDescriptions"]?.ToString();
            if (!string.IsNullOrEmpty(capabilityDescriptions))
            {
                if (capabilityDescriptions.Contains("Color", StringComparison.OrdinalIgnoreCase))
                    caps |= PrinterCapabilities.Color;
                if (capabilityDescriptions.Contains("Duplex", StringComparison.OrdinalIgnoreCase))
                    caps |= PrinterCapabilities.Duplex;
                if (capabilityDescriptions.Contains("Collate", StringComparison.OrdinalIgnoreCase))
                    caps |= PrinterCapabilities.Collation;
                if (capabilityDescriptions.Contains("Copy", StringComparison.OrdinalIgnoreCase))
                    caps |= PrinterCapabilities.Copy;
            }
        }
        catch
        {
            // Capability detection is best-effort
        }

        return caps;
    }

    private static string[] GetPaperSizes(string printerName)
    {
        try
        {
            var settings = new PrinterSettings();
            settings.PrinterName = printerName;

            var sizes = new List<string>();
            foreach (PaperSize paperSize in settings.PaperSizes)
            {
                sizes.Add($"{paperSize.PaperName} ({paperSize.Width}x{paperSize.Height})");
            }
            return sizes.ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static string[] GetResolutions(ManagementObject obj)
    {
        try
        {
            var resolutions = obj["PrinterResolutionsSupported"];
            if (resolutions is null)
                return [];

            var result = new List<string>();
            foreach (var res in (object[])resolutions)
            {
                result.Add(res.ToString() ?? string.Empty);
            }
            return result.ToArray();
        }
        catch
        {
            return [];
        }
    }
}
