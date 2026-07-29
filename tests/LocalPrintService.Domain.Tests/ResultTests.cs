using LocalPrintService.Domain.Results;
using LocalPrintService.Domain.Errors;
using LocalPrintService.Shared.Extensions;

namespace LocalPrintService.Domain.Tests;

public class ResultTests
{
    [Fact]
    public void Ok_ShouldCreateSuccessResult()
    {
        var result = Result.Ok("test");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("test", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Fail_ShouldCreateFailureResult()
    {
        var error = new PrinterNotFoundError("test-printer");
        var result = Result.Fail<string>(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Match_OnSuccess_ShouldCallOnSuccess()
    {
        var result = Result.Ok(42);

        var output = result.Match(
            v => v * 2,
            _ => 0);

        Assert.Equal(84, output);
    }

    [Fact]
    public void Match_OnFailure_ShouldCallOnFailure()
    {
        var error = new PrinterNotFoundError("test");
        var result = Result.Fail<int>(error);

        var output = result.Match(
            v => v * 2,
            _ => -1);

        Assert.Equal(-1, output);
    }

    [Fact]
    public void Map_OnSuccess_ShouldTransform()
    {
        var result = Result.Ok(10);

        var mapped = result.Map(v => v.ToString());

        Assert.True(mapped.IsSuccess);
        Assert.Equal("10", mapped.Value);
    }

    [Fact]
    public void Map_OnFailure_ShouldPropagateError()
    {
        var error = new InvalidDocumentError("bad");
        var result = Result.Fail<int>(error);

        var mapped = result.Map(v => v.ToString());

        Assert.False(mapped.IsSuccess);
        Assert.Equal(error, mapped.Error);
    }
}
