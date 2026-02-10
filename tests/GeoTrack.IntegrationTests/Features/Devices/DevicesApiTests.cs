using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GeoTrack.Application.DTOs;
using GeoTrack.Application.Features.Devices.Queries.GetDeviceHistory; // For PagedHistoryResponse (if exported) or just verify structure
using GeoTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace GeoTrack.IntegrationTests.Features.Devices;

public class DevicesApiTests : IAsyncLifetime
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
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Microsoft.EntityFrameworkCore.DbContextOptions<GeoTrackDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<GeoTrackDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));
                });
            });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoTrackDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task CompleteFlow_ShouldWork()
    {
        var client = _factory.CreateClient();
        var deviceId = "m2-test-device";
        var now = DateTime.UtcNow;

        // 1. Ingest Data (3 points)
        var points = new[]
        {
            new TelemetryDto { DeviceId = deviceId, Timestamp = now.AddMinutes(-10), Lat = 10, Lon = 10 },
            new TelemetryDto { DeviceId = deviceId, Timestamp = now.AddMinutes(-5), Lat = 11, Lon = 11 },
            new TelemetryDto { DeviceId = deviceId, Timestamp = now, Lat = 12, Lon = 12 } // Latest
        };

        var ingestResp = await client.PostAsJsonAsync("/api/telemetry", points);
        ingestResp.EnsureSuccessStatusCode();

        // 2. List Devices
        var listResp = await client.GetFromJsonAsync<List<DeviceSummaryDto>>("/api/devices");
        listResp.Should().Contain(d => d.Id == deviceId && d.IsActive == true); // Active because "now" is recent

        // 3. Get Detail
        var detailResp = await client.GetFromJsonAsync<DeviceDetailDto>($"/api/devices/{deviceId}");
        detailResp.Should().NotBeNull();
        detailResp!.LatestLocation.Should().NotBeNull();
        detailResp.LatestLocation!.Lat.Should().Be(12); // Verify latest

        // 4. Get Latest Direct
        var latestResp = await client.GetFromJsonAsync<LocationDto>($"/api/devices/{deviceId}/latest");
        latestResp.Should().NotBeNull();
        latestResp!.Timestamp.Should().BeCloseTo(now, TimeSpan.FromMilliseconds(100)); // Timestamp precision from previous step

        // 5. History with Limit
        var historyResp = await client.GetFromJsonAsync<PagedHistoryResponse>($"/api/devices/{deviceId}/locations?limit=2");
        historyResp.Should().NotBeNull();
        historyResp!.Data.Count().Should().Be(2);
        historyResp.NextCursor.Should().NotBeNull();

        // Ordering check (Ascending)
        historyResp.Data.First().Lat.Should().Be(10);
        historyResp.Data.Last().Lat.Should().Be(11);

        // 6. Pagination (Next Page)
        var cursor = historyResp.NextCursor;
        var page2Resp = await client.GetFromJsonAsync<PagedHistoryResponse>($"/api/devices/{deviceId}/locations?limit=2&cursor={cursor}");
        page2Resp!.Data.Count().Should().Be(1);
        page2Resp.Data.First().Lat.Should().Be(12);
        page2Resp.NextCursor.Should().BeNull(); // No more data

        // 7. 404 check
        var notFoundResp = await client.GetAsync("/api/devices/unknown-device");
        notFoundResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
