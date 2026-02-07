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

        var devices = await _context.Devices
            .Select(d => new 
            {
                d.ExternalId,
                d.Name,
                d.LastSeenAt
            })
            .ToListAsync(cancellationToken);

        return devices.Select(d => new DeviceSummaryDto
        {
            Id = d.ExternalId,
            Name = d.Name,
            LastSeen = d.LastSeenAt,
            IsActive = d.LastSeenAt.HasValue && d.LastSeenAt.Value >= activeThreshold
        });
    }
}
