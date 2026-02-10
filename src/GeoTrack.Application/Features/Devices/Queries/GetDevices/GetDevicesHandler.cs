using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoTrack.Application.Common.Interfaces;
using GeoTrack.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeoTrack.Application.Features.Devices.Queries.GetDevices;

public record GetDevicesQuery : IRequest<IEnumerable<DeviceSummaryDto>>;

public class GetDevicesHandler : IRequestHandler<GetDevicesQuery, IEnumerable<DeviceSummaryDto>>
{
    private readonly IGeoTrackDbContext _context;

    public GetDevicesHandler(IGeoTrackDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DeviceSummaryDto>> Handle(GetDevicesQuery request, CancellationToken cancellationToken)
    {
        // Active threshold: 2 minutes
        var activeThreshold = DateTime.UtcNow.AddMinutes(-2);

        // 1. Fetch all devices
        var devices = await _context.Devices
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // 2. Fetch all locations and group by DeviceId in memory
        var allLocations = await _context.Locations
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Group and get latest for each device
        var latestByDevice = allLocations
            .GroupBy(l => l.DeviceId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(l => l.Timestamp).FirstOrDefault()
            );

        // 3. Join in memory
        return devices.Select(d =>
        {
            latestByDevice.TryGetValue(d.Id, out var loc);
            return new DeviceSummaryDto
            {
                Id = d.ExternalId,
                Name = d.Name,
                LastSeen = d.LastSeenAt,
                IsActive = d.LastSeenAt.HasValue && d.LastSeenAt.Value >= activeThreshold,
                Latitude = loc?.Lat,
                Longitude = loc?.Lon,
                LastLocationTime = loc?.Timestamp
            };
        }).ToList();
    }
}
