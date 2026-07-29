using LocalPrintService.Domain.Errors;

namespace LocalPrintService.Domain.Tests;

public class DomainErrorTests
{
    [Fact]
    public void PrinterNotFoundError_ShouldHaveCorrectProperties()
    {
        var error = new PrinterNotFoundError("HP-LaserJet");

        Assert.Equal("PRINTER_NOT_FOUND", error.Code);
        Assert.Contains("HP-LaserJet", error.Message);
        Assert.Equal("HP-LaserJet", error.PrinterName);
    }

    [Fact]
    public void InvalidDocumentError_ShouldHaveCorrectProperties()
    {
        var error = new InvalidDocumentError("File too large");

        Assert.Equal("INVALID_DOCUMENT", error.Code);
        Assert.Contains("File too large", error.Message);
        Assert.Equal("File too large", error.Reason);
    }

    [Fact]
    public void JobValidationError_ShouldHaveCorrectProperties()
    {
        var error = new JobValidationError("printer", "Cannot be empty");

        Assert.Equal("JOB_VALIDATION", error.Code);
        Assert.Contains("printer", error.Message);
        Assert.Equal("printer", error.Field);
    }

    [Fact]
    public void JobNotFoundError_ShouldHaveCorrectProperties()
    {
        var id = Guid.NewGuid();
        var error = new JobNotFoundError(id);

        Assert.Equal("JOB_NOT_FOUND", error.Code);
        Assert.Contains(id.ToString(), error.Message);
        Assert.Equal(id, error.JobId);
    }

    [Fact]
    public void PrintEngineError_ShouldHaveCorrectProperties()
    {
        var error = new PrintEngineError("Printer-A", "Spooler failed");

        Assert.Equal("PRINT_ENGINE_ERROR", error.Code);
        Assert.Contains("Printer-A", error.Message);
        Assert.Equal("Printer-A", error.PrinterName);
    }
}
