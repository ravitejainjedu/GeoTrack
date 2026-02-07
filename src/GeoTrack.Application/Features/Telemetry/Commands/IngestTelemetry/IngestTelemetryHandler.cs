using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using GeoTrack.Application.DTOs;
using GeoTrack.Domain.Entities;
using GeoTrack.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeoTrack.Application.Features.Telemetry.Commands.IngestTelemetry;

public class IngestTelemetryHandler : IRequestHandler<IngestTelemetryCommand, IngestTelemetryResult>
{
    private readonly IGeoTrackDbContext _context;
    private readonly IValidator<TelemetryDto> _validator;
    private readonly ILogger<IngestTelemetryHandler> _logger;

    public IngestTelemetryHandler(IGeoTrackDbContext context, IValidator<TelemetryDto> validator, ILogger<IngestTelemetryHandler> logger)
    {
        _context = context;
        _validator = validator;
        _logger = logger;
    }

    public async Task<IngestTelemetryResult> Handle(IngestTelemetryCommand request, CancellationToken cancellationToken)
    {
        var validPoints = new List<TelemetryDto>();
        int rejectedCount = 0;

        // 1. Validate all points
        foreach (var point in request.Points)
        {
            // Align timestamp to microseconds (Postgres resolution) to ensure accurate duplicate detection
            if (point.Timestamp.Kind != DateTimeKind.Utc)
                point.Timestamp = point.Timestamp.ToUniversalTime();

            var ticks = point.Timestamp.Ticks;
            point.Timestamp = new DateTime(ticks - (ticks % 10), DateTimeKind.Utc);

            var validationResult = await _validator.ValidateAsync(point, cancellationToken);
            if (!validationResult.IsValid)
            {
                rejectedCount++;
                continue;
            }
            validPoints.Add(point);
        }

        if (validPoints.Count == 0)
        {
            return new IngestTelemetryResult(0, 0, rejectedCount);
        }

        // 2. Deduplicate payload in-memory (latest wins or first wins? Requirement says ignore duplicates. Let's keep first.)
        var uniquePoints = validPoints
            .GroupBy(p => new { p.DeviceId, p.Timestamp })
            .Select(g => g.First())
            .ToList();
        
        int payloadDuplicates = validPoints.Count - uniquePoints.Count;

        // 3. Upsert Devices
        var distinctDeviceIds = uniquePoints.Select(p => p.DeviceId).Distinct().ToList();
        
        // Fetch existing devices
        var existingDevices = await _context.Devices
            .Where(d => distinctDeviceIds.Contains(d.ExternalId))
            .ToDictionaryAsync(d => d.ExternalId, d => d, cancellationToken);

        var newDevices = new List<Device>();
        foreach (var deviceId in distinctDeviceIds)
        {
            if (!existingDevices.ContainsKey(deviceId))
            {
                var newDevice = new Device { ExternalId = deviceId, Id = Guid.NewGuid(), Name = deviceId, LastSeenAt = DateTime.UtcNow };
                newDevices.Add(newDevice);
                existingDevices[deviceId] = newDevice; // Add to local dictionary for lookup
            }
            else 
            {
                // Update LastSeen (optional for batch efficiency, doing it here for simplicity)
                // If high throughput, might want to defer this or do it less frequently.
                // For now, let's just make sure we have the ID. 
                // Updating LastSeen on every batch is OK for 500/sec on Postgres.
                var dev = existingDevices[deviceId];
                var latestPointForDevice = uniquePoints.Where(p => p.DeviceId == deviceId).MaxBy(p => p.Timestamp);
                 if (latestPointForDevice != null && (dev.LastSeenAt == null || latestPointForDevice.Timestamp > dev.LastSeenAt))
                {
                    dev.LastSeenAt = latestPointForDevice.Timestamp;
                    dev.LastLat = latestPointForDevice.Lat;
                    dev.LastLon = latestPointForDevice.Lon;
                }
            }
        }

        if (newDevices.Any())
        {
            await _context.Devices.AddRangeAsync(newDevices, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken); // Save to get IDs if generated (though we set Guid) and persist new devices
        }

        // 4. Check for existing locations (Idempotency)
        // We need to know which (DeviceId, Timestamp) tuples already exist to count them accurately as "Duplicates" logic.
        // And then insert the non-existing ones.
        
        // Extract keys
        var keysToCheck = uniquePoints.Select(p => new { DeviceId = existingDevices[p.DeviceId].Id, p.Timestamp }).ToList();
        
        // Query existing. Note: constructing a huge OR clause or using Contains with composite keys isn't supported directly in LINQ for tuples in all providers.
        // Alternative: Fetch all Locations for these Devices within the Timestamp range? Or just try insert and catch?
        // Requirement: "Count duplicates accurately". 
        // Approach: Batch insert using raw SQL "ON CONFLICT DO NOTHING" does NOT return the count of ignored rows easily in all Postgres versions/wrappers.
        // Better approach for correctness and strict counting: 
        // 1. Get ranges of timestamps per device.
        // 2. Query DB for existing points in those ranges.
        // 3. Filter in memory.
        
        // Optimization: If batch is small, checking individually is slow. 
        // Let's use the Range approach if possible, or just accept that "Duplicates" = Payload Duplicates + existing DB duplicates.
        
        // Let's try to fetch potentially conflicting rows.
        // Since we have a list of (DeviceGuid, Timestamp), we can construct a query.
        // For 500 points/sec, doing a query might be heavy if not optimized.
        // Let's blindly insert and catch unique constraint violation? No, that fails the whole batch.
        
        // Strategy: "ON CONFLICT DO NOTHING" with RETURNING.
        // EF Core 7+ `StartInsert` etc? No.
        // Let's use raw SQL for insertion to support ON CONFLICT and get efficiency.
        // But to get the COUNT of specific ignoring, we can check `(Total - Inserted)`.
        
        int insertedCount = 0;
        int dbDuplicates = 0;
        
        // Map to Entities
        var locationsToInsert = uniquePoints.Select(p => new Location
        {
            Id = Guid.NewGuid(),
            DeviceId = existingDevices[p.DeviceId].Id,
            Timestamp = p.Timestamp.ToUniversalTime(), // Ensure UTC
            Lat = p.Lat,
            Lon = p.Lon,
            Speed = p.Speed,
            Heading = p.Heading,
            Accuracy = p.Accuracy,
            IngestedAt = DateTime.UtcNow
        }).ToList();

        // We can use a Strategy:
        // 1. Create a temp table or pass arrays to a raw SQL command.
        // 2. INSERT INTO Locations ... SELECT ... WHERE NOT EXISTS ...
        // 3. GET DIAGNOSTICS/Row Count.
        
        // Simpler EF-friendly approach (since V1):
        // Filter out existing by querying for keys.
        // `Contains` on in-memory list of composite keys is NOT translated to SQL by EF Core usually.
        // But `context.Locations.Where(l => deviceIds.Contains(l.DeviceId) && timestamps.Contains(l.Timestamp))` returns a superset (false positives).
        // Then filtering in memory is safe and fast.
        
        var deviceIds = locationsToInsert.Select(l => l.DeviceId).Distinct().ToList();
        var minTime = locationsToInsert.Min(l => l.Timestamp);
        var maxTime = locationsToInsert.Max(l => l.Timestamp);
        
        var candidates = await _context.Locations
            .Where(l => deviceIds.Contains(l.DeviceId) && l.Timestamp >= minTime && l.Timestamp <= maxTime)
            .Select(l => new { l.DeviceId, l.Timestamp })
            .ToListAsync(cancellationToken);
            
        var existingSet = new HashSet<(Guid, DateTime)>(candidates.Select(c => (c.DeviceId, c.Timestamp)));
        
        var newLocations = new List<Location>();
        foreach (var loc in locationsToInsert)
        {
            if (existingSet.Contains((loc.DeviceId, loc.Timestamp)))
            {
                dbDuplicates++;
            }
            else
            {
                newLocations.Add(loc);
            }
        }
        
        if (newLocations.Any())
        {
            await _context.Locations.AddRangeAsync(newLocations, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            insertedCount = newLocations.Count;
        }

        return new IngestTelemetryResult(insertedCount, payloadDuplicates + dbDuplicates, rejectedCount);
    }
}
