using System.Collections.Specialized;
using LocalPrintService.Application.DTOs.Requests;
using LocalPrintService.Application.Services;
using LocalPrintService.Domain.Configuration;
using LocalPrintService.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocketIOClient;

namespace LocalPrintService.Api.CloudRelay;

/// <summary>
/// Puente saliente hacia el backend: se conecta por Socket.IO al namespace
/// "/print" (WSS a traves de Traefik), recibe trabajos de impresion
/// (evento print:job) y confirma el resultado (evento print:result).
/// Si no hay BackendUrl/Token o esta deshabilitado, no hace nada.
/// </summary>
public sealed class CloudRelayService : BackgroundService
{
    private const string PrintNamespace = "/print";
    private static readonly TimeSpan CompletionPollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(120);

    private readonly IOptions<CloudRelaySettings> _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CloudRelayService> _logger;

    public CloudRelayService(
        IOptions<CloudRelaySettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger<CloudRelayService> logger)
    {
        _settings = settings;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = _settings.Value;
        if (!settings.Enabled)
        {
            _logger.LogInformation("CloudRelay deshabilitado (CloudRelay:Enabled=false)");
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.BackendUrl) || string.IsNullOrWhiteSpace(settings.Token))
        {
            _logger.LogWarning(
                "CloudRelay habilitado pero falta BackendUrl o Token; no se conectara");
            return;
        }

        var agentName = string.IsNullOrWhiteSpace(settings.AgentName)
            ? Environment.MachineName
            : settings.AgentName;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSessionAsync(settings, agentName, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CloudRelay: la sesion con el backend termino con error");
            }

            if (stoppingToken.IsCancellationRequested) break;

            var delaySeconds = Math.Clamp(settings.ReconnectDelaySeconds, 1, 300);
            _logger.LogInformation(
                "CloudRelay: reconexion al backend en {Delay}s", delaySeconds);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunSessionAsync(
        CloudRelaySettings settings,
        string agentName,
        CancellationToken stoppingToken)
    {
        var endpoint = BuildNamespaceUrl(settings.BackendUrl);
        var options = new SocketIOOptions
        {
            Transport = SocketIOClient.Common.TransportProtocol.WebSocket,
            ConnectionTimeout = TimeSpan.FromSeconds(15),
            // La reconexion la manejamos nosotros para poder registrar cada intento
            Reconnection = false,
            Query = new NameValueCollection
            {
                ["token"] = settings.Token,
                ["clientType"] = "print-agent",
                ["agentName"] = agentName,
            },
        };

        using var client = new SocketIO(new Uri(endpoint), options);

        client.On("print:job", async context =>
        {
            try
            {
                await HandlePrintJobAsync(client, context, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // apagado del servicio
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CloudRelay: error procesando print:job");
            }
        });

        client.OnDisconnected += (_, reason) =>
            _logger.LogWarning("CloudRelay: desconectado del backend ({Reason})", reason);
        client.OnError += (_, error) =>
            _logger.LogWarning("CloudRelay: error de socket ({Error})", error);

        await client.ConnectAsync(stoppingToken);
        _logger.LogInformation(
            "CloudRelay: conectado a {Endpoint} como agente \"{AgentName}\"",
            endpoint, agentName);

        // Mantener la sesion viva hasta que se caiga o se detenga el servicio
        while (client.Connected && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task HandlePrintJobAsync(
        SocketIO client,
        IEventContext context,
        CancellationToken stoppingToken)
    {
        PrintJobEnvelope? job;
        try
        {
            job = context.GetValue<PrintJobEnvelope>(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CloudRelay: payload de print:job invalido");
            return;
        }

        if (job is null || string.IsNullOrWhiteSpace(job.JobId))
        {
            _logger.LogWarning("CloudRelay: print:job sin jobId, se ignora");
            return;
        }

        var success = false;
        string? error = null;

        try
        {
            (success, error) = await ExecuteJobAsync(job, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CloudRelay: fallo el trabajo {JobId}", job.JobId);
            error = "RELAY_ERROR";
        }

        try
        {
            await client.EmitAsync(
                "print:result",
                new object[] { new { jobId = job.JobId, success, error } },
                stoppingToken);
            _logger.LogInformation(
                "CloudRelay: trabajo {JobId} {Resultado}{Error}",
                job.JobId,
                success ? "completado" : "fallido",
                error is null ? string.Empty : $" ({error})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CloudRelay: no se pudo confirmar el trabajo {JobId}", job.JobId);
        }
    }

    private async Task<(bool Success, string? Error)> ExecuteJobAsync(
        PrintJobEnvelope job,
        CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var printJobService = scope.ServiceProvider.GetRequiredService<PrintJobService>();
        var printerQueryService = scope.ServiceProvider.GetRequiredService<PrinterQueryService>();

        var printerName = ResolvePrinter(printerQueryService, job.Printer);
        if (printerName is null)
        {
            return (false, "PRINTERS_NOT_AVAILABLE");
        }

        var createResult = await printJobService.CreateJobAsync(new CreatePrintJobRequest
        {
            Printer = printerName,
            DocumentType = string.IsNullOrWhiteSpace(job.DocumentType) ? "pdf" : job.DocumentType,
            Copies = Math.Clamp(job.Copies, 1, 100),
            Payload = job.Payload,
        });

        if (createResult.IsFailure)
        {
            return (false, createResult.Error?.Code ?? "JOB_REJECTED");
        }

        var finalStatus = await WaitForCompletionAsync(
            printJobService, createResult.Value!.Id, stoppingToken);

        return finalStatus switch
        {
            PrintJobStatus.Completed => (true, null),
            PrintJobStatus.Failed => (false, "PRINT_FAILED"),
            PrintJobStatus.Cancelled => (false, "JOB_CANCELLED"),
            _ => (false, "PRINT_TIMEOUT"),
        };
    }

    private static string? ResolvePrinter(PrinterQueryService printers, string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested) &&
            printers.GetPrinterByName(requested) is { } requestedPrinter)
        {
            return requestedPrinter.Name;
        }

        // La impresora solicitada no existe en esta PC: usar la por defecto
        return printers.GetDefaultPrinter()?.Name;
    }

    private static async Task<PrintJobStatus?> WaitForCompletionAsync(
        PrintJobService printJobService,
        Guid jobId,
        CancellationToken stoppingToken)
    {
        var deadline = DateTime.UtcNow + CompletionTimeout;
        while (DateTime.UtcNow < deadline && !stoppingToken.IsCancellationRequested)
        {
            var job = printJobService.GetJob(jobId);
            if (job is null)
            {
                return null;
            }

            if (Enum.TryParse<PrintJobStatus>(job.Status, out var status) &&
                status is PrintJobStatus.Completed
                    or PrintJobStatus.Failed
                    or PrintJobStatus.Cancelled)
            {
                return status;
            }

            await Task.Delay(CompletionPollInterval, stoppingToken);
        }

        return null;
    }

    /// <summary>
    /// Convierte la URL base del backend en el endpoint del namespace /print.
    /// </summary>
    internal static string BuildNamespaceUrl(string backendUrl)
    {
        var url = backendUrl.Trim().TrimEnd('/');
        if (url.EndsWith(PrintNamespace, StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }
        return url + PrintNamespace;
    }
}
