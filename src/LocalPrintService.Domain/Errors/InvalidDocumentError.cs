namespace LocalPrintService.Domain.Errors;

public sealed class InvalidDocumentError : DomainError
{
    public string Reason { get; }

    public InvalidDocumentError(string reason)
        : base("INVALID_DOCUMENT", $"Invalid document: {reason}.")
    {
        Reason = reason;
    }
}
