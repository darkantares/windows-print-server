using LocalPrintService.Application.DTOs.Requests;
using LocalPrintService.Application.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LocalPrintService.Api.Endpoints;

public static class JobEndpoints
{
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/jobs")
            .WithTags("Print Jobs")
            .RequireCors("AllowLocalhost");

        group.MapGet("/", GetAllJobs)
            .WithName("GetAllJobs")
            .WithOpenApi();

        group.MapGet("/{id:guid}", GetJobById)
            .WithName("GetJobById")
            .WithOpenApi();

        group.MapPost("/", CreateJob)
            .WithName("CreateJob")
            .WithOpenApi();

        group.MapDelete("/{id:guid}", CancelJob)
            .WithName("CancelJob")
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetAllJobs(PrintJobService service)
    {
        var jobs = await service.GetAllJobsAsync();
        return Results.Ok(jobs);
    }

    private static IResult GetJobById(Guid id, PrintJobService service)
    {
        var job = service.GetJob(id);
        return job is not null ? Results.Ok(job) : Results.NotFound(new { error = $"Job '{id}' not found" });
    }

    private static async Task<IResult> CreateJob(CreatePrintJobRequest request, PrintJobService service)
    {
        var result = await service.CreateJobAsync(request);
        return result.Match<IResult>(
            job => Results.Created($"/api/jobs/{job.Id}", job),
            error => Results.BadRequest(new { error = error.Code, message = error.Message }));
    }

    private static async Task<IResult> CancelJob(Guid id, PrintJobService service)
    {
        var result = await service.CancelJobAsync(id);
        return result.Match<IResult>(
            success => success ? Results.Ok(new { message = "Job cancelled" }) : Results.Conflict(new { error = "Job cannot be cancelled" }),
            error => error.Code switch
            {
                "JOB_NOT_FOUND" => Results.NotFound(new { error = error.Code, message = error.Message }),
                _ => Results.BadRequest(new { error = error.Code, message = error.Message })
            });
    }
}
