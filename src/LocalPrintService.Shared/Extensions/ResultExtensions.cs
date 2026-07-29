using LocalPrintService.Domain.Errors;
using LocalPrintService.Domain.Results;

namespace LocalPrintService.Shared.Extensions;

public static class ResultExtensions
{
    public static TOut Match<T, TOut>(
        this Result<T> result,
        Func<T, TOut> onSuccess,
        Func<DomainError, TOut> onFailure)
    {
        return result.IsSuccess ? onSuccess(result.Value!) : onFailure(result.Error!);
    }

    public static async Task<TOut> MatchAsync<T, TOut>(
        this Result<T> result,
        Func<T, Task<TOut>> onSuccess,
        Func<DomainError, Task<TOut>> onFailure)
    {
        return result.IsSuccess
            ? await onSuccess(result.Value!)
            : await onFailure(result.Error!);
    }

    public static Result<TNext> Map<T, TNext>(
        this Result<T> result,
        Func<T, TNext> map)
    {
        return result.IsSuccess
            ? Result.Ok(map(result.Value!))
            : Result.Fail<TNext>(result.Error!);
    }

    public static async Task<Result<TNext>> MapAsync<T, TNext>(
        this Result<T> result,
        Func<T, Task<TNext>> map)
    {
        return result.IsSuccess
            ? Result.Ok(await map(result.Value!))
            : Result.Fail<TNext>(result.Error!);
    }

    public static Result<T> Tap<T>(
        this Result<T> result,
        Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value!);
        return result;
    }
}
