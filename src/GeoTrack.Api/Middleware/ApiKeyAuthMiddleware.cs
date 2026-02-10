using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GeoTrack.Api.Middleware
{
    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiKeyAuthMiddleware> _logger;
        private const string ApiKeyHeaderName = "X-API-KEY";

        public ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyAuthMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only protect POST /api/telemetry
            if (context.Request.Method == HttpMethods.Post && 
                context.Request.Path.StartsWithSegments("/api/telemetry"))
            {
                if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { message = "API Key missing" });
                    return;
                }

                var apiKey = _configuration.GetValue<string>("GeoTrack:ApiKey");
                
                if (string.IsNullOrEmpty(apiKey) || !apiKey.Equals(extractedApiKey))
                {
                    _logger.LogWarning("Invalid API Key provided.");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { message = "Unauthorized" });
                    return;
                }
            }

            await _next(context);
        }
    }
}
