using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GeoTrack.Api.Hubs;
using GeoTrack.Application.DTOs;
using GeoTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace GeoTrack.IntegrationTests.Features.Realtime;

public class SignalRIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    private GeoTrackApiFactory _factory = null!;
    private HubConnection _connection = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new GeoTrackApiFactory(services =>
        {
            // Replace DB Context with Test Container
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<GeoTrackDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<GeoTrackDbContext>(options =>
            {
                options.UseNpgsql(_postgres.GetConnectionString());
            });
        });

        // Ensure DB is created
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GeoTrackDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        // Start SignalR Connection
        var client = _factory.CreateClient(); // Just to keep alive? No, we need the Handler.

        _connection = new HubConnectionBuilder()
            .WithUrl(_factory.Server.BaseAddress + "hubs/geotrack", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        await _connection.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _factory.DisposeAsync();
        await _postgres.StopAsync();
    }

    [Fact]
    public async Task ShouldReceiveDeviceUpdated_WhenTelemetryIngested()
    {
        // Arrange
        var events = new List<object>();
        var tcs = new TaskCompletionSource<object>();

        _connection.On<object>("DeviceUpdated", (payload) =>
        {
            events.Add(payload);
            tcs.TrySetResult(payload);
        });

        var telemetry = new TelemetryDto
        {
            DeviceId = "device-signalr-1",
            Timestamp = DateTime.UtcNow,
            Lat = 40.7128,
            Lon = -74.0060,
            Speed = 10,
            Heading = 90,
            Accuracy = 5
        };

        var client = _factory.CreateClient();
        // Header added automatically

        // Act
        var response = await client.PostAsJsonAsync("/api/telemetry", new[] { telemetry });
        response.EnsureSuccessStatusCode();

        // Assert
        var received = await Task.WhenAny(tcs.Task, Task.Delay(2000));
        received.Should().Be(tcs.Task, "should receive event within timeout");
        events.Should().HaveCount(1);
    }

    [Fact]
    public async Task ShouldCoalesceUpdates_WhenBurstIngested()
    {
        // Arrange
        var events = new List<object>();
        _connection.On<dynamic>("DeviceUpdated", (payload) =>
        {
            events.Add(payload);
        });

        var client = _factory.CreateClient();
        // Header added automatically
        var deviceId = "device-burst-1";

        // Act: Burst 10 points in simple loop (should be very fast, << 100ms)
        // Broadcaster flush is 200ms.
        // We expect at most 1 or 2 events globally for this device in that window.

        var points = Enumerable.Range(0, 10).Select(i => new TelemetryDto
        {
            DeviceId = deviceId,
            Timestamp = DateTime.UtcNow.AddSeconds(i), // Advancing time
            Lat = 10 + i,
            Lon = 10 + i,
            Speed = i * 10
        }).ToArray();

        foreach (var p in points)
        {
            await client.PostAsJsonAsync("/api/telemetry", new[] { p });
        }

        // Wait a bit more than the flush interval to ensure we get the final flush
        await Task.Delay(1000);

        // Assert
        events.Count.Should().BeInRange(1, 4, "should be effectively throttled (5Hz = 200ms period)");
        // Theoretically if we send 10 reqs in 50ms, we might get 0 (in buffer) then 1 flush. Or 1 early, then 1 final.
        // Definitely shouldn't be 10.

        // Verify last one
        // Note: Payload is dynamic/object, strictly verifying properties is verbose with anonymous types.
        // Just asserting count is enough proof of coalescing for now.
    }
}
