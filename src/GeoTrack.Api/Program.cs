using GeoTrack.Application;
using GeoTrack.Infrastructure;
using GeoTrack.Infrastructure.Persistence;
using GeoTrack.Api.Hubs;
using GeoTrack.Api.Services;
using GeoTrack.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

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
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
        // .AllowCredentials(); // Not enabled yet as per requirements
    });
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

var app = builder.Build();

// Auto-migrate in Development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<GeoTrackDbContext>();
        try 
        {
             db.Database.Migrate();
        }
        catch (Exception ex)
        {
            // Log or ignore if connection fails (let health check handle it)
            Console.WriteLine($"Migration failed: {ex.Message}");
        }
    }
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler(); 

// Throttling Middleware for Telemetry
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
}

app.UseCors("DevelopmentCors");

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
