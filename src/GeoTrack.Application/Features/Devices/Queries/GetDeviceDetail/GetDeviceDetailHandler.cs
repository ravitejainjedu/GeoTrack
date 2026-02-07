using System;
using System.Threading;
using System.Threading.Tasks;
using GeoTrack.Application.Common.Interfaces;
using GeoTrack.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeoTrack.Application.Features.Devices.Queries.GetDeviceDetail;

public record GetDeviceDetailQuery(string ExternalId) : IRequest<DeviceDetailDto?>;

public class GetDeviceDetailHandler : IRequestHandler<GetDeviceDetailQuery, DeviceDetailDto?>
{
    private readonly IGeoTrackDbContext _context;

    public GetDeviceDetailHandler(IGeoTrackDbContext context)
    {
        _context = context;
    }

    public async Task<DeviceDetailDto?> Handle(GetDeviceDetailQuery request, CancellationToken cancellationToken)
    {
        var activeThreshold = DateTime.UtcNow.AddMinutes(-2);

        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.ExternalId == request.ExternalId, cancellationToken);

        if (device == null) return null;

        var dto = new DeviceDetailDto
        {
            Id = device.ExternalId,
            Name = device.Name,
            LastSeen = device.LastSeenAt,
            IsActive = device.LastSeenAt.HasValue && device.LastSeenAt.Value >= activeThreshold,
            LatestLocation = null
        };

        // Fetch latest location using the performant index (DeviceId, Timestamp Desc)
        // We need the internal Id for the FK
        var latestLoc = await _context.Locations
            .AsNoTracking()
            .Where(l => l.DeviceId == device.Id)
            .OrderByDescending(l => l.Timestamp)
            .Select(l => new LocationDto
            {
                Timestamp = l.Timestamp,
                Lat = l.Lat,
                Lon = l.Lon,
                Speed = l.Speed,
                Heading = l.Heading,
                Accuracy = l.Accuracy
            })
            .FirstOrDefaultAsync(cancellationToken);

        dto.LatestLocation = latestLoc;

        return dto;
    }
}
