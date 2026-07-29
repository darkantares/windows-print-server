namespace LocalPrintService.Domain.Enums;

public enum PrinterStatus
{
    Unknown = 0,
    Other = 1,
    Ready = 2,
    Paused = 3,
    Error = 4,
    Offline = 5,
    Processing = 6
}
