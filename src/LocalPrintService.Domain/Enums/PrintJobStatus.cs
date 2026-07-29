namespace LocalPrintService.Domain.Enums;

public enum PrintJobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
