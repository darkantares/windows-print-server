using LocalPrintService.Api.Endpoints;
using LocalPrintService.Api.Middleware;
using LocalPrintService.Application.Extensions;
using LocalPrintService.Domain.Configuration;
using LocalPrintService.Infrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext();
});

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
