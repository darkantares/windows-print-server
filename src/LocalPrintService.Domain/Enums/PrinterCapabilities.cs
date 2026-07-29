namespace LocalPrintService.Domain.Enums;

[Flags]
public enum PrinterCapabilities
{
    None = 0,
    Color = 1,
    Duplex = 2,
    Collation = 4,
    Staple = 8,
    HolePunch = 16,
    PaperSize = 32,
    Resolution = 64,
    Copy = 128
}
