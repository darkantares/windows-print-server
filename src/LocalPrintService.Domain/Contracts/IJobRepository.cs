using LocalPrintService.Domain.Entities;

namespace LocalPrintService.Domain.Contracts;

public interface IJobRepository
{
    Task AddAsync(PrintJob job);
    Task<PrintJob?> GetByIdAsync(Guid jobId);
    Task<IReadOnlyList<PrintJob>> GetAllAsync();
    Task UpdateAsync(PrintJob job);
    Task RemoveAsync(Guid jobId);
    int PendingCount { get; }
    int CompletedCount { get; }
    int FailedCount { get; }
}
