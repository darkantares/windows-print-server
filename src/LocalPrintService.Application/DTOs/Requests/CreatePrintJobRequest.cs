using System.ComponentModel.DataAnnotations;

namespace LocalPrintService.Application.DTOs.Requests;

public sealed class CreatePrintJobRequest
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public required string Printer { get; init; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public required string DocumentType { get; init; }

    [Range(1, 100)]
    public int Copies { get; init; } = 1;

    [Required]
    public required string Payload { get; init; }
}
