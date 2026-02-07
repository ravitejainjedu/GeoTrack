using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GeoTrack.Application.DTOs;
using GeoTrack.Application.Features.Telemetry.Commands.IngestTelemetry;
using GeoTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace GeoTrack.IntegrationTests.Features.Telemetry;

public class IngestTelemetryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<GeoTrackDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add DbContext with container connection string
                    services.AddDbContext<GeoTrackDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));
                });
            });

        // Apply migrations
        /*
        // NOTE: In integration tests for WebApplicationFactory, the Program.cs migration logic runs if env is Development.
        // But here we might want to be explicit or ensure it runs.
        // Let's rely on `EnsureCreated` or strict Migrate for test.
        */
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoTrackDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Ingest_ShouldPersistValidTelemetry_AndIgnoreDuplicates()
    {
        // Arrange
        var client = _factory.CreateClient();
        var deviceId = "test-device-01";
        var now = DateTime.UtcNow;

        var payload = new List<TelemetryDto>
        {
            new TelemetryDto { DeviceId = deviceId, Timestamp = now, Lat = 10, Lon = 20 },
            new TelemetryDto { DeviceId = deviceId, Timestamp = now.AddSeconds(1), Lat = 11, Lon = 21 }
        };

        // Act 1: Ingest first batch
        var response1 = await client.PostAsJsonAsync("/api/telemetry", payload);
        response1.EnsureSuccessStatusCode();
        var result1 = await response1.Content.ReadFromJsonAsync<IngestTelemetryResult>();

        // Assert 1
        result1.Should().NotBeNull();
        result1!.Accepted.Should().Be(2);
        result1.Duplicates.Should().Be(0);

        // Act 2: Ingest duplicates + 1 new
        var payload2 = new List<TelemetryDto>
        {
            new TelemetryDto { DeviceId = deviceId, Timestamp = now, Lat = 10, Lon = 20 },      // Duplicate
            new TelemetryDto { DeviceId = deviceId, Timestamp = now.AddSeconds(2), Lat = 12, Lon = 22 } // New
        };

        var response2 = await client.PostAsJsonAsync("/api/telemetry", payload2);
        response2.EnsureSuccessStatusCode();
        var result2 = await response2.Content.ReadFromJsonAsync<IngestTelemetryResult>();

        // Assert 2
        result2.Should().NotBeNull();
        result2!.Accepted.Should().Be(1);
        result2.Duplicates.Should().Be(1);

        // Verify DB State
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoTrackDbContext>();
        var count = await db.Locations.CountAsync();
        count.Should().Be(3);
    }
}
