using LocalPrintService.Domain.Errors;

namespace LocalPrintService.Domain.Results;

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public DomainError? Error { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = null;
    }

    private Result(DomainError error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(DomainError error) => new(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<DomainError, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }
}

public static class Result
{
    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);
    public static Result<T> Fail<T>(DomainError error) => Result<T>.Fail(error);
}
