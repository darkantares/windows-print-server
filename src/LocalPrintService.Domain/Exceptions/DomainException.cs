using LocalPrintService.Domain.Errors;

namespace LocalPrintService.Domain.Exceptions;

public sealed class DomainException : Exception
{
    public DomainError Error { get; }

    public DomainException(DomainError error)
        : base(error.Message)
    {
        Error = error;
    }

    public DomainException(DomainError error, Exception innerException)
        : base(error.Message, innerException)
    {
        Error = error;
    }
}
