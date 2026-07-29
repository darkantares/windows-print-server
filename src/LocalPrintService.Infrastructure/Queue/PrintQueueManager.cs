using System.Collections.Concurrent;
using LocalPrintService.Domain.Contracts;
using LocalPrintService.Domain.Entities;
using LocalPrintService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LocalPrintService.Infrastructure.Queue;

public sealed class PrintQueueManager : IQueueManager
{
    private readonly ConcurrentQueue<Guid> _queue = new();
    private readonly ConcurrentDictionary<Guid, PrintJob> _jobs = new();
    private readonly ILogger<PrintQueueManager> _logger;

    public int PendingCount => _jobs.Values.Count(j => j.Status == PrintJobStatus.Pending);
    public int CompletedCount => _jobs.Values.Count(j => j.Status == PrintJobStatus.Completed);
    public int FailedCount => _jobs.Values.Count(j => j.Status == PrintJobStatus.Failed);

    public PrintQueueManager(ILogger<PrintQueueManager> logger)
    {
        _logger = logger;
    }

    public Task<Guid> EnqueueAsync(PrintJob job)
    {
        _jobs.TryAdd(job.Id, job);
        _queue.Enqueue(job.Id);
        _logger.LogDebug("Job {JobId} enqueued. Queue depth: {Depth}", job.Id, _queue.Count);
        return Task.FromResult(job.Id);
    }

    public Task<PrintJob?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        while (_queue.TryDequeue(out var jobId))
        {
            if (_jobs.TryGetValue(jobId, out var job) && job.Status == PrintJobStatus.Pending)
            {
                return Task.FromResult<PrintJob?>(job);
            }
        }

        return Task.FromResult<PrintJob?>(null);
    }

    public Task<PrintJob?> GetJobAsync(Guid jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    public Task<IReadOnlyList<PrintJob>> GetAllJobsAsync()
    {
        var jobs = _jobs.Values
            .OrderByDescending(j => j.CreatedAt)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<PrintJob>>(jobs);
    }

    public Task<bool> CancelJobAsync(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job) && job.Status == PrintJobStatus.Pending)
        {
            job.Status = PrintJobStatus.Cancelled;
            _logger.LogInformation("Job {JobId} cancelled", jobId);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
