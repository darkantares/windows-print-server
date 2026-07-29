using LocalPrintService.Application.DTOs.Requests;
using LocalPrintService.Application.DTOs.Responses;
using LocalPrintService.Domain.Configuration;
using LocalPrintService.Domain.Contracts;
using LocalPrintService.Domain.Entities;
using LocalPrintService.Domain.Enums;
using LocalPrintService.Domain.Errors;
using LocalPrintService.Domain.Results;
using LocalPrintService.Shared.Validators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LocalPrintService.Application.Services;

public sealed class PrintJobService
{
    private readonly IQueueManager _queueManager;
    private readonly IPrinterManager _printerManager;
    private readonly PrintServiceSettings _settings;
    private readonly ILogger<PrintJobService> _logger;

    public PrintJobService(
        IQueueManager queueManager,
        IPrinterManager printerManager,
        IOptions<PrintServiceSettings> settings,
        ILogger<PrintJobService> logger)
    {
        _queueManager = queueManager;
        _printerManager = printerManager;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Result<PrintJobResponse>> CreateJobAsync(CreatePrintJobRequest request)
    {
        var printerResult = ValidatePrinter(request.Printer);
        if (printerResult.IsFailure)
            return Result.Fail<PrintJobResponse>(printerResult.Error!);

        var documentTypeResult = ParseDocumentType(request.DocumentType);
        if (documentTypeResult.IsFailure)
            return Result.Fail<PrintJobResponse>(documentTypeResult.Error!);

        var validation = DocumentValidator.Validate(request.Payload, request.DocumentType, _settings);
        if (validation.IsFailure)
            return Result.Fail<PrintJobResponse>(validation.Error!);

        var job = new PrintJob
        {
            Printer = request.Printer,
            DocumentType = documentTypeResult.Value!,
            Copies = request.Copies,
            Payload = request.Payload,
            MaxRetries = _settings.MaxRetries
        };

        await _queueManager.EnqueueAsync(job);

        _logger.LogInformation(
            "Print job {JobId} created for printer '{Printer}', type '{DocumentType}', copies {Copies}",
            job.Id, job.Printer, job.DocumentType, job.Copies);

        return Result.Ok(MapToResponse(job));
    }

    public PrintJobResponse? GetJob(Guid jobId)
    {
        var job = _queueManager.GetJobAsync(jobId).GetAwaiter().GetResult();
        return job is null ? null : MapToResponse(job);
    }

    public async Task<IReadOnlyList<PrintJobResponse>> GetAllJobsAsync()
    {
        var jobs = await _queueManager.GetAllJobsAsync();
        return jobs
            .Select(MapToResponse)
            .ToList()
            .AsReadOnly();
    }

    public async Task<Result<bool>> CancelJobAsync(Guid jobId)
    {
        var job = await _queueManager.GetJobAsync(jobId);
        if (job is null)
            return Result.Fail<bool>(new JobNotFoundError(jobId));

        if (job.Status != PrintJobStatus.Pending)
            return Result.Fail<bool>(new JobValidationError("status", "Only pending jobs can be cancelled"));

        var cancelled = await _queueManager.CancelJobAsync(jobId);
        if (cancelled)
            _logger.LogInformation("Print job {JobId} cancelled", jobId);

        return Result.Ok(cancelled);
    }

    private Result<PrinterInfo> ValidatePrinter(string printerName)
    {
        if (!_printerManager.PrinterExists(printerName))
            return Result.Fail<PrinterInfo>(new PrinterNotFoundError(printerName));

        return Result.Ok(_printerManager.GetPrinterByName(printerName)!);
    }

    private static Result<DocumentType> ParseDocumentType(string documentType)
    {
        return documentType.ToLowerInvariant() switch
        {
            "text" => Result.Ok(DocumentType.Text),
            "pdf" => Result.Ok(DocumentType.Pdf),
            "image" => Result.Ok(DocumentType.Image),
            "raw" => Result.Ok(DocumentType.Raw),
            "html" => Result.Ok(DocumentType.Html),
            "escpos" => Result.Ok(DocumentType.EscPos),
            "zpl" => Result.Ok(DocumentType.Zpl),
            _ => Result.Fail<DocumentType>(new JobValidationError("documentType", $"Unsupported document type: '{documentType}'"))
        };
    }

    private static PrintJobResponse MapToResponse(PrintJob job)
    {
        return new PrintJobResponse
        {
            Id = job.Id,
            Printer = job.Printer,
            DocumentType = job.DocumentType.ToString(),
            Copies = job.Copies,
            Status = job.Status.ToString(),
            AttemptCount = job.AttemptCount,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            ErrorMessage = job.ErrorMessage
        };
    }
}
