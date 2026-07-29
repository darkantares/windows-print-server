namespace LocalPrintService.Domain.Errors;

public sealed class JobNotFoundError : DomainError
{
    public Guid JobId { get; }

    public JobNotFoundError(Guid jobId)
        : base("JOB_NOT_FOUND", $"Print job '{jobId}' was not found.")
    {
        JobId = jobId;
    }
}
