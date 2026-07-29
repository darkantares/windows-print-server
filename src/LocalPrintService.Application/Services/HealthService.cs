using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using LocalPrintService.Application.DTOs.Responses;
using LocalPrintService.Domain.Contracts;

namespace LocalPrintService.Application.Services;

public sealed class HealthService
{
    private readonly IQueueManager _queueManager;
    private readonly IPrinterManager _printerManager;
    private readonly Stopwatch _uptime;

    public HealthService(IQueueManager queueManager, IPrinterManager printerManager)
    {
        _queueManager = queueManager;
        _printerManager = printerManager;
        _uptime = Stopwatch.StartNew();
    }

    public HealthResponse GetHealth()
    {
        _uptime.Stop();
        var uptimeSpan = _uptime.Elapsed;
        var process = Process.GetCurrentProcess();
        var gcInfo = GC.GetGCMemoryInfo();

        return new HealthResponse
        {
            Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0",
            Uptime = FormatUptime(uptimeSpan),
            PendingJobs = _queueManager.PendingCount,
            CompletedJobs = _queueManager.CompletedCount,
            FailedJobs = _queueManager.FailedCount,
            MemoryUsedMb = process.WorkingSet64 / (1024 * 1024),
            Status = "Running"
        };
    }

    private static string FormatUptime(TimeSpan span)
    {
        return span.TotalDays >= 1
            ? $"{(int)span.TotalDays}d {span.Hours}h {span.Minutes}m {span.Seconds}s"
            : span.TotalHours >= 1
                ? $"{(int)span.TotalHours}h {span.Minutes}m {span.Seconds}s"
                : $"{span.Minutes}m {span.Seconds}s";
    }
}
