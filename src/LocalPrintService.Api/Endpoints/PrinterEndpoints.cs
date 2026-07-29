using LocalPrintService.Application.Services;
using LocalPrintService.Domain.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LocalPrintService.Api.Endpoints;

public static class PrinterEndpoints
{
    public static IEndpointRouteBuilder MapPrinterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/printers")
            .WithTags("Printers")
            .RequireCors("AllowLocalhost");

        group.MapGet("/", GetAllPrinters)
            .WithName("GetAllPrinters")
            .WithOpenApi();

        group.MapGet("/default", GetDefaultPrinter)
            .WithName("GetDefaultPrinter")
            .WithOpenApi();

        group.MapGet("/{printerName}", GetPrinterByName)
            .WithName("GetPrinterByName")
            .WithOpenApi();

        group.MapPost("/test", PrintTestPage)
            .WithName("PrintTestPage")
            .WithOpenApi();

        return app;
    }

    private static IResult GetAllPrinters(PrinterQueryService service)
    {
        var printers = service.GetAllPrinters();
        return Results.Ok(printers);
    }

    private static IResult GetDefaultPrinter(PrinterQueryService service)
    {
        var printer = service.GetDefaultPrinter();
        return printer is not null ? Results.Ok(printer) : Results.NotFound(new { error = "No default printer found" });
    }

    private static IResult GetPrinterByName(string printerName, PrinterQueryService service)
    {
        var printer = service.GetPrinterByName(printerName);
        return printer is not null ? Results.Ok(printer) : Results.NotFound(new { error = $"Printer '{printerName}' not found" });
    }

    private static async Task<IResult> PrintTestPage(string printerName, PrinterQueryService service, PrintJobService jobService)
    {
        var printer = service.GetPrinterByName(printerName);
        if (printer is null)
            return Results.NotFound(new { error = $"Printer '{printerName}' not found" });

        var result = await jobService.CreateJobAsync(new Application.DTOs.Requests.CreatePrintJobRequest
        {
            Printer = printerName,
            DocumentType = "text",
            Copies = 1,
            Payload = "Test Page - Local Print Service"
        });

        return result.Match<IResult>(
            job => Results.Ok(new { message = "Test page queued", jobId = job.Id }),
            error => Results.BadRequest(new { error = error.Code, message = error.Message }));
    }
}
