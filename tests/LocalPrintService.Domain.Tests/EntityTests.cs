using LocalPrintService.Domain.Entities;
using LocalPrintService.Domain.Enums;

namespace LocalPrintService.Domain.Tests;

public class EntityTests
{
    [Fact]
    public void PrintJob_ShouldDefaultToPending()
    {
        var job = new PrintJob
        {
            Printer = "TestPrinter",
            DocumentType = DocumentType.Text,
            Payload = "Hello"
        };

        Assert.Equal(PrintJobStatus.Pending, job.Status);
        Assert.Equal(1, job.Copies);
        Assert.Equal(3, job.MaxRetries);
        Assert.Equal(0, job.AttemptCount);
        Assert.Null(job.StartedAt);
        Assert.Null(job.CompletedAt);
        Assert.Null(job.ErrorMessage);
    }

    [Fact]
    public void PrintJob_ShouldGenerateUniqueIds()
    {
        var job1 = new PrintJob { Printer = "A", DocumentType = DocumentType.Text, Payload = "1" };
        var job2 = new PrintJob { Printer = "B", DocumentType = DocumentType.Text, Payload = "2" };

        Assert.NotEqual(job1.Id, job2.Id);
    }

    [Fact]
    public void PrinterInfo_ShouldHaveDefaults()
    {
        var info = new PrinterInfo
        {
            Id = "test",
            Name = "Test Printer",
            DriverName = "Driver",
            PortName = "LPT1",
            Status = PrinterStatus.Ready,
            IsDefault = true,
            IsAvailable = true
        };

        Assert.Equal(PrinterCapabilities.None, info.Capabilities);
        Assert.Empty(info.SupportedPaperSizes);
        Assert.Empty(info.SupportedResolutions);
    }
}
