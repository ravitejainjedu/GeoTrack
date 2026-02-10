using GeoTrack.Application;
using GeoTrack.Infrastructure;
using GeoTrack.Infrastructure.Persistence;
using GeoTrack.Api.Hubs;
using GeoTrack.Api.Services;
using GeoTrack.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using GeoTrack.Infrastructure;
using GeoTrack.Infrastructure.Persistence;
using GeoTrack.Api.Hubs;
using GeoTrack.Api.Services;
using GeoTrack.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

const string UiCors = "ui";

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<GeoTrack.Api.Infrastructure.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Register DbContext Health Check with Timeout using a custom check
builder.Services.AddTransient<GeoTrack.Api.Services.GeoTrackDbHealthCheck>();
builder.Services.AddHealthChecks()
    .AddCheck<GeoTrack.Api.Services.GeoTrackDbHealthCheck>("db", tags: new[] { "db" }, timeout: TimeSpan.FromSeconds(3));

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();

// Add SignalR
builder.Services.AddSignalR();

// Register Telemetry Broadcaster (as Singleton BackgroundService)
builder.Services.AddSingleton<SignalRTelemetryBroadcaster>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SignalRTelemetryBroadcaster>());
builder.Services.AddSingleton<ITelemetryBroadcaster>(sp => sp.GetRequiredService<SignalRTelemetryBroadcaster>());

// Register Ingestion Gate (Backpressure)
builder.Services.AddSingleton<IIngestionGate, IngestionGate>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(UiCors, policy =>
        policy.WithOrigins("http://127.0.0.1:5173", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Configure Kestrel to force HTTP/1.1 and increase// Configure Kestrel limits and protocols
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Apply HTTP/1.1 protocol to all endpoints (simpler and compatible with simulator/nginx/docker port mapping)
    serverOptions.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
    });

    serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

// Enable CORS debug logging
builder.Logging.AddFilter("Microsoft.AspNetCore.Cors", LogLevel.Debug);

var app = builder.Build();

// Auto-migrate in Development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<GeoTrackDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Starting database migration...");
            db.Database.Migrate();
            logger.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            // Log error but don't crash - let health check handle it
            logger.LogError(ex, "Migration failed: {Message}", ex.Message);
            Console.WriteLine($"Migration failed: {ex.Message}");
        }
    }
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();



app.UseRouting();

// CORS must run here, before anything can short-circuit
app.UseCors("ui");



// Throttling Middleware for Telemetry - AFTER CORS
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/telemetry"), appBuilder =>
{
    appBuilder.UseMiddleware<GeoTrack.Api.Infrastructure.IngestionThrottlingMiddleware>();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseCors(UiCors); // Re-added conditionally with the original policy name
}

app.UseMiddleware<GeoTrack.Api.Middleware.ApiKeyAuthMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GeoTrackHub>("/hubs/geotrack");



app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true
});

app.Run();

public partial class Program { }
