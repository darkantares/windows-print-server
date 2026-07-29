namespace LocalPrintService.Domain.Errors;

public sealed class JobValidationError : DomainError
{
    public string Field { get; }

    public JobValidationError(string field, string message)
        : base("JOB_VALIDATION", $"Validation error on '{field}': {message}.")
    {
        Field = field;
    }
}
