using LocalPrintService.Domain.Enums;

namespace LocalPrintService.Application.DTOs.Responses;

public sealed class PrintJobResponse
{
    public Guid Id { get; init; }
    public required string Printer { get; init; }
    public required string DocumentType { get; init; }
    public int Copies { get; init; }
    public required string Status { get; init; }
    public int AttemptCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
}
