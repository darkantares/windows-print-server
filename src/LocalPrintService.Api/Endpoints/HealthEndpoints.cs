using LocalPrintService.Application.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LocalPrintService.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", GetHealth)
            .WithName("GetHealth")
            .WithTags("Health")
            .RequireCors("AllowLocalhost")
            .WithOpenApi();

        return app;
    }

    private static IResult GetHealth(HealthService service)
    {
        var health = service.GetHealth();
        return Results.Ok(health);
    }
}
