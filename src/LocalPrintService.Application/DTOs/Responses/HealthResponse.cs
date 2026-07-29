namespace LocalPrintService.Application.DTOs.Responses;

public sealed class HealthResponse
{
    public required string Version { get; init; }
    public required string Uptime { get; init; }
    public int PendingJobs { get; init; }
    public int CompletedJobs { get; init; }
    public int FailedJobs { get; init; }
    public long MemoryUsedMb { get; init; }
    public required string Status { get; init; }
}
