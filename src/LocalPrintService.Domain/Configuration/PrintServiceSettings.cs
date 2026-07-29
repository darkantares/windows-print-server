namespace LocalPrintService.Domain.Configuration;

public sealed class PrintServiceSettings
{
    public const string SectionName = "PrintService";

    public int MaxDocumentSizeBytes { get; set; } = 10 * 1024 * 1024;
    public int MaxConcurrentJobs { get; set; } = 5;
    public int JobTimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
    public string[] AllowedMimeTypes { get; set; } =
    [
        "text/plain",
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/bmp",
        "image/gif",
        "text/html",
        "application/octet-stream"
    ];
}
