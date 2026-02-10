using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoTrack.Api.Hubs;
using GeoTrack.Application.Common.Interfaces;
using GeoTrack.Application.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GeoTrack.Api.Services;

public class SignalRTelemetryBroadcaster : BackgroundService, ITelemetryBroadcaster
{
    private readonly IHubContext<GeoTrackHub> _hubContext;
    private readonly ILogger<SignalRTelemetryBroadcaster> _logger;

    // Key: ExternalId, Value: Latest Telemetry
    private readonly ConcurrentDictionary<string, TelemetryDto> _pendingUpdates = new();

    // Configuration
    private const int FlushIntervalMs = 200; // 5 Hz
    private const int MaxPendingSize = 10000; // Safety cap

    public SignalRTelemetryBroadcaster(
        IHubContext<GeoTrackHub> hubContext,
        ILogger<SignalRTelemetryBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task BroadcastAsync(string externalId, TelemetryDto telemetry)
    {
        // Safety cap check
        if (_pendingUpdates.Count >= MaxPendingSize && !_pendingUpdates.ContainsKey(externalId))
        {
            _logger.LogWarning("Dropping telemetry update for {ExternalId}. Pending queue full ({Size}).", externalId, _pendingUpdates.Count);
            return Task.CompletedTask;
        }

        // Add or update the pending dictionary with the latest point (Last Write Wins)
        _pendingUpdates[externalId] = telemetry;
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SignalR Telemetry Broadcaster started");

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(FlushIntervalMs));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (_pendingUpdates.IsEmpty) continue;

            // Snapshot keys to process
            var keys = _pendingUpdates.Keys.ToArray();

            foreach (var key in keys)
            {
                if (_pendingUpdates.TryRemove(key, out var telemetry))
                {
                    try
                    {
                        // Broadcast "DeviceUpdated" event
                        // Payload: externalId, timestamp, lat, lon, etc.
                        await _hubContext.Clients.All.SendAsync("DeviceUpdated", new
                        {
                            externalId = key,
                            timestamp = telemetry.Timestamp,
                            lat = telemetry.Lat,
                            lon = telemetry.Lon,
                            speed = telemetry.Speed,
                            heading = telemetry.Heading,
                            accuracy = telemetry.Accuracy
                        }, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error broadcasting telemetry for {ExternalId}", key);
                        // Don't rethrow - continue processing other updates
                    }
                }
            }
        }

        _logger.LogInformation("SignalR Telemetry Broadcaster stopped");
    }
}
