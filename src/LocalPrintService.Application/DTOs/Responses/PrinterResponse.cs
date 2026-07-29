using LocalPrintService.Domain.Enums;

namespace LocalPrintService.Application.DTOs.Responses;

public sealed class PrinterResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string DriverName { get; init; }
    public required string PortName { get; init; }
    public required string Status { get; init; }
    public required bool IsDefault { get; init; }
    public required bool IsAvailable { get; init; }
    public string[] Capabilities { get; init; } = [];
    public string[] SupportedPaperSizes { get; init; } = [];
    public string[] SupportedResolutions { get; init; } = [];
}
