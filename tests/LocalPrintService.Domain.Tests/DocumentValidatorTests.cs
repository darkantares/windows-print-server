using LocalPrintService.Domain.Configuration;
using LocalPrintService.Shared.Validators;

namespace LocalPrintService.Domain.Tests;

public class DocumentValidatorTests
{
    private readonly PrintServiceSettings _settings = new();

    [Fact]
    public void Validate_EmptyPayload_ShouldFail()
    {
        var result = DocumentValidator.Validate("", "text", _settings);

        Assert.True(result.IsFailure);
        Assert.Contains("empty", result.Error!.Message);
    }

    [Fact]
    public void Validate_NullPayload_ShouldFail()
    {
        var result = DocumentValidator.Validate(null!, "text", _settings);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Validate_ValidText_ShouldSucceed()
    {
        var result = DocumentValidator.Validate("Hello World", "text", _settings);

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello World", result.Value);
    }

    [Fact]
    public void Validate_OversizedDocument_ShouldFail()
    {
        var smallSettings = new PrintServiceSettings { MaxDocumentSizeBytes = 10 };
        var result = DocumentValidator.Validate(new string('A', 100), "text", smallSettings);

        Assert.True(result.IsFailure);
        Assert.Contains("exceeds", result.Error!.Message);
    }

    [Fact]
    public void ValidateMimeType_ValidType_ShouldSucceed()
    {
        var result = DocumentValidator.ValidateMimeType("text/plain", _settings);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateMimeType_InvalidType_ShouldFail()
    {
        var result = DocumentValidator.ValidateMimeType("application/executable", _settings);

        Assert.True(result.IsFailure);
        Assert.Contains("not allowed", result.Error!.Message);
    }

    [Fact]
    public void ValidateMimeType_Empty_ShouldFail()
    {
        var result = DocumentValidator.ValidateMimeType("", _settings);

        Assert.True(result.IsFailure);
    }
}
