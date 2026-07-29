using LocalPrintService.Domain.Contracts;
using LocalPrintService.Domain.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalPrintService.Infrastructure.Queue;

public sealed class PrintWorker : BackgroundService
{
    private readonly IQueueManager _queueManager;
    private readonly IDocumentProcessorFactory _processorFactory;
    private readonly IPrintEngine _printEngine;
    private readonly ILogger<PrintWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(1);

    public PrintWorker(
        IQueueManager queueManager,
        IDocumentProcessorFactory processorFactory,
        IPrintEngine printEngine,
        ILogger<PrintWorker> logger)
    {
        _queueManager = queueManager;
        _processorFactory = processorFactory;
        _printEngine = printEngine;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PrintWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queueManager.DequeueAsync(stoppingToken);
                if (job is null)
                {
                    await Task.Delay(_pollInterval, stoppingToken);
                    continue;
                }

                await ProcessJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in PrintWorker");
                await Task.Delay(_pollInterval, stoppingToken);
            }
        }

        _logger.LogInformation("PrintWorker stopped");
    }

    private async Task ProcessJobAsync(Domain.Entities.PrintJob job, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing job {JobId} for printer '{Printer}', type '{DocumentType}'",
            job.Id, job.Printer, job.DocumentType);

        job.Status = PrintJobStatus.Processing;
        job.StartedAt = DateTime.UtcNow;
        job.AttemptCount++;

        try
        {
            var processor = _processorFactory.GetProcessor(job.DocumentType);
            var documentData = await processor.ProcessAsync(job.Payload, cancellationToken);

            var success = await _printEngine.PrintAsync(job, documentData, cancellationToken);

            if (success)
            {
                job.Status = PrintJobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "Job {JobId} completed in {Elapsed}ms",
                    job.Id,
                    (job.CompletedAt.Value - job.StartedAt.Value).TotalMilliseconds);
            }
            else
            {
                await HandleFailureAsync(job, "Print engine returned false");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", job.Id);
            await HandleFailureAsync(job, ex.Message);
        }
    }

    private async Task HandleFailureAsync(Domain.Entities.PrintJob job, string errorMessage)
    {
        job.ErrorMessage = errorMessage;

        if (job.AttemptCount >= job.MaxRetries)
        {
            job.Status = PrintJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            _logger.LogWarning(
                "Job {JobId} failed after {Attempts} attempts: {Error}",
                job.Id, job.AttemptCount, errorMessage);
        }
        else
        {
            job.Status = PrintJobStatus.Pending;
            _logger.LogWarning(
                "Job {JobId} failed (attempt {Attempt}/{MaxRetries}), will retry: {Error}",
                job.Id, job.AttemptCount, job.MaxRetries, errorMessage);

            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}
