using MemeTokenHub.ClaimService.Api.Application;
using MemeTokenHub.ClaimService.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace MemeTokenHub.ClaimService.Api.Middleware;

public sealed partial class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        (int status, string title) = exception switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ClaimAccessDeniedException => (StatusCodes.Status403Forbidden, "Access denied"),
            ClaimConcurrencyException or MongoWriteException => (StatusCodes.Status409Conflict, "Conflict"),
            InvalidClaimTransitionException => (StatusCodes.Status422UnprocessableEntity, "Invalid claim transition"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            DependencyUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Dependency unavailable"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception, context.TraceIdentifier);
        }
        else
        {
            LogRequestFailure(logger, exception, status, context.TraceIdentifier);
        }

        ProblemDetails problem = new()
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : exception.Message,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "An unhandled exception occurred. Trace identifier: {TraceIdentifier}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string traceIdentifier);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request failed with status {StatusCode}. Trace identifier: {TraceIdentifier}")]
    private static partial void LogRequestFailure(ILogger logger, Exception exception, int statusCode, string traceIdentifier);
}
