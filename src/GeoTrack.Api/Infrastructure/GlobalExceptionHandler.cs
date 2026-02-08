using System;
using System.Threading;
using System.Threading.Tasks;
using GeoTrack.Application.Features.Telemetry.Commands.IngestTelemetry; // For RateLimitExceededException
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GeoTrack.Api.Infrastructure;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        if (exception is RateLimitExceededException rateLimitEx)
        {
            _logger.LogWarning("Rate limit exceeded: {Message}", rateLimitEx.Message);
            problemDetails.Title = "Rate Limit Exceeded";
            problemDetails.Detail = "Server is busy. Please try again later.";
            problemDetails.Status = StatusCodes.Status429TooManyRequests;
            problemDetails.Type = "https://tools.ietf.org/html/rfc6585#section-4";
        }
        else
        {
            _logger.LogError(exception, "An unhandled exception occurred.");
            problemDetails.Title = "An error occurred while processing your request.";
            problemDetails.Detail = exception.Message; // In prod, might want to hide this, but beneficial for this task
            problemDetails.Status = StatusCodes.Status500InternalServerError;
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
