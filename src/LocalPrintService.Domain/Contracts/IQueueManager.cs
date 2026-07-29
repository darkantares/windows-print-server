using LocalPrintService.Domain.Entities;

namespace LocalPrintService.Domain.Contracts;

public interface IQueueManager
{
    Task<Guid> EnqueueAsync(PrintJob job);
    Task<PrintJob?> DequeueAsync(CancellationToken cancellationToken = default);
    Task<PrintJob?> GetJobAsync(Guid jobId);
    Task<IReadOnlyList<PrintJob>> GetAllJobsAsync();
    Task<bool> CancelJobAsync(Guid jobId);
    int PendingCount { get; }
    int CompletedCount { get; }
    int FailedCount { get; }
}
