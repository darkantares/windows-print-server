using LocalPrintService.Domain.Contracts;
using LocalPrintService.Infrastructure.DocumentProcessors;
using LocalPrintService.Infrastructure.Printing;
using LocalPrintService.Infrastructure.Queue;
using LocalPrintService.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LocalPrintService.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IPrinterManager, WindowsPrinterManager>();
        services.AddSingleton<IPrintEngine, WindowsPrintEngine>();
        services.AddSingleton<IQueueManager, PrintQueueManager>();
        services.AddSingleton<IJobRepository, InMemoryJobRepository>();

        services.AddSingleton<IDocumentProcessor, TextProcessor>();
        services.AddSingleton<IDocumentProcessor, PdfProcessor>();
        services.AddSingleton<IDocumentProcessor, ImageProcessor>();
        services.AddSingleton<IDocumentProcessor, HtmlProcessor>();
        services.AddSingleton<IDocumentProcessor, RawProcessor>();
        services.AddSingleton<DocumentProcessorFactory>();
        services.AddSingleton<IDocumentProcessorFactory>(sp => sp.GetRequiredService<DocumentProcessorFactory>());

        services.AddHostedService<PrintWorker>();

        return services;
    }
}
