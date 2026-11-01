using System.Text.Json.Serialization;

namespace LocalPrintService.Api.CloudRelay;

/// <summary>Trabajo de impresion recibido del backend (evento print:job).</summary>
public sealed class PrintJobEnvelope
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("printer")]
    public string? Printer { get; set; }

    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; } = "pdf";

    [JsonPropertyName("copies")]
    public int Copies { get; set; } = 1;

    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;
}
