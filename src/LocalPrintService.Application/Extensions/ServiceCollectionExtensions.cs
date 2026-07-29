using LocalPrintService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LocalPrintService.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<HealthService>();
        services.AddScoped<PrinterQueryService>();
        services.AddScoped<PrintJobService>();

        return services;
    }
}
