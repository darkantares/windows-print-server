using System.Collections.Concurrent;
using LocalPrintService.Domain.Contracts;
using LocalPrintService.Domain.Entities;

namespace LocalPrintService.Infrastructure.Storage;

public sealed class InMemoryJobRepository : IJobRepository
{
    private readonly ConcurrentDictionary<Guid, PrintJob> _jobs = new();

    public int PendingCount => _jobs.Values.Count(j =>
        j.Status == Domain.Enums.PrintJobStatus.Pending);

    public int CompletedCount => _jobs.Values.Count(j =>
        j.Status == Domain.Enums.PrintJobStatus.Completed);

    public int FailedCount => _jobs.Values.Count(j =>
        j.Status == Domain.Enums.PrintJobStatus.Failed);

    public Task AddAsync(PrintJob job)
    {
        _jobs.TryAdd(job.Id, job);
        return Task.CompletedTask;
    }

    public Task<PrintJob?> GetByIdAsync(Guid jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    public Task<IReadOnlyList<PrintJob>> GetAllAsync()
    {
        var jobs = _jobs.Values
            .OrderByDescending(j => j.CreatedAt)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<PrintJob>>(jobs);
    }

    public Task UpdateAsync(PrintJob job)
    {
        _jobs[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid jobId)
    {
        _jobs.TryRemove(jobId, out _);
        return Task.CompletedTask;
    }
}
