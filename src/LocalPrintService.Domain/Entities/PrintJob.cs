using LocalPrintService.Domain.Enums;

namespace LocalPrintService.Domain.Entities;

public sealed class PrintJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Printer { get; init; }
    public required DocumentType DocumentType { get; init; }
    public int Copies { get; init; } = 1;
    public required string Payload { get; init; }
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int MaxRetries { get; init; } = 3;
}
