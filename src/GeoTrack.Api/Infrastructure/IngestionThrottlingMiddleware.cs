using System.IO;
using System.Threading.Tasks;
using GeoTrack.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GeoTrack.Api.Infrastructure;

public class IngestionThrottlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IIngestionGate _gate;
    private readonly ILogger<IngestionThrottlingMiddleware> _logger;

    public IngestionThrottlingMiddleware(RequestDelegate next, IIngestionGate gate, ILogger<IngestionThrottlingMiddleware> logger)
    {
        _next = next;
        _gate = gate;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip throttling for OPTIONS preflight requests
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Check if backpressure is needed (gate full)
        if (!await _gate.TryEnterAsync(200)) // 200ms timeout
        {
            _logger.LogWarning("Ingestion gate rejected request {RequestId}. Draining body and returning 429.", context.TraceIdentifier);

            // Drain body to prevent ECONNRESET
            context.Request.EnableBuffering();
            await context.Request.Body.CopyToAsync(Stream.Null, context.RequestAborted);
            context.Request.Body.Position = 0;

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new { message = "Ingestion pipeline busy. Please try again later." });
            return;
        }

        try
        {
            await _next(context);
        }
        finally
        {
            _gate.Exit();
        }
    }
}
