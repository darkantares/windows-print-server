using System.Diagnostics;
using LocalPrintService.Api;
using LocalPrintService.Api.Endpoints;
using LocalPrintService.Api.Middleware;
using LocalPrintService.Application.Extensions;
using LocalPrintService.Domain.Configuration;
using LocalPrintService.Infrastructure.Extensions;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

var exePath = Environment.ProcessPath
    ?? Process.GetCurrentProcess().MainModule?.FileName
    ?? Path.Combine(Directory.GetCurrentDirectory(), "LocalPrintService.Api.exe");
var exeDir = Path.GetDirectoryName(exePath) ?? Directory.GetCurrentDirectory();

// Los rutas relativas (appsettings, certs, logs) se resuelven siempre junto al exe,
// tambien cuando el proceso corre como servicio de Windows (cwd = System32).
Directory.SetCurrentDirectory(exeDir);

// Gestión de servicio por línea de comandos (usado por el helper elevado y usuarios avanzados)
if (args.Contains("--install") || args.Contains("--install-service"))
{
    return ServiceInstaller.Install(exePath);
}

if (args.Contains("--uninstall") || args.Contains("--uninstall-service"))
{
    return ServiceInstaller.Uninstall(exePath);
}

if (args.Contains("--start"))
{
    return ServiceInstaller.Start();
}

if (args.Contains("--stop"))
{
    return ServiceInstaller.Stop();
}

// Lanzado por el Administrador de servicios de Windows
if (!Environment.UserInteractive)
{
    return RunServer(exeDir);
}

// Servidor en primer plano (desarrollo/pruebas)
if (args.Contains("--run") || args.Contains("--console"))
{
    return RunServer(exeDir);
}

// Doble clic: instalador/gestor simple
return InstallerUi.Run(exePath);

static int RunServer(string exeDir)
{
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = Array.Empty<string>(),
        ContentRootPath = exeDir,
    });

    builder.Host.UseSerilog((context, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext();
    });

    builder.Host.UseWindowsService();

    // Puerto por defecto cuando no hay appsettings.json (ej: instalación single-file sin archivos al lado del exe)
    if (!builder.Configuration.GetSection("Kestrel").GetChildren().Any())
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(5200);
            var certPath = Path.Combine(exeDir, "certs", "print-local.pfx");
            if (File.Exists(certPath))
            {
                options.ListenLocalhost(5201, listen => listen.UseHttps(certPath, "LocalPrint2024!"));
            }
        });
    }

    builder.Services.Configure<PrintServiceSettings>(
        builder.Configuration.GetSection(PrintServiceSettings.SectionName));

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowLocalhost", policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Local Print Service API",
            Version = "1.0.0",
            Description = "Universal Print Service for Windows - Local Print Server API"
        });
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Local Print Service API v1");
            options.RoutePrefix = "swagger";
        });
    }

    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseCors("AllowLocalhost");

    app.MapPrinterEndpoints();
    app.MapJobEndpoints();
    app.MapHealthEndpoints();

    app.Run();
    return 0;
}