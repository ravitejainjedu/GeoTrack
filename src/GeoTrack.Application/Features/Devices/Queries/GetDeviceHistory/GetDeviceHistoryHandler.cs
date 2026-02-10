using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using GeoTrack.Application.Common.Interfaces;
using GeoTrack.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeoTrack.Application.Features.Devices.Queries.GetDeviceHistory;

public record GetDeviceHistoryQuery(string ExternalId, DateTime? From, DateTime? To, int Limit = 1000, string? Cursor = null)
    : IRequest<PagedHistoryResponse?>;

public class GetDeviceHistoryHandler : IRequestHandler<GetDeviceHistoryQuery, PagedHistoryResponse?>
{
    private readonly IGeoTrackDbContext _context;

    public GetDeviceHistoryHandler(IGeoTrackDbContext context)
    {
        _context = context;
    }

    public async Task<PagedHistoryResponse?> Handle(GetDeviceHistoryQuery request, CancellationToken cancellationToken)
    {
        // 1. Resolve Device
        var device = await _context.Devices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ExternalId == request.ExternalId, cancellationToken);

        if (device == null) return null;

        // 2. Build Query
        var query = _context.Locations.AsNoTracking().Where(l => l.DeviceId == device.Id);

        // Date Range
        if (request.From.HasValue)
            query = query.Where(l => l.Timestamp >= request.From.Value); // Inclusive start

        if (request.To.HasValue)
            query = query.Where(l => l.Timestamp <= request.To.Value); // Inclusive end (or exclusive? req says allow to=now. Inclusive is standard for history range).

        // Cursor (Timestamp-based paging)
        // Cursor means "return points AFTER this cursor timestamp".
        // Since we order by Timestamp ASC, cursor usually represents the LAST timestamp of the previous page.
        // So we want Timestamp > Cursor.
        if (!string.IsNullOrEmpty(request.Cursor) && DateTime.TryParse(request.Cursor, null, System.Globalization.DateTimeStyles.RoundtripKind, out var cursorTime))
        {
            query = query.Where(l => l.Timestamp > cursorTime);
        }

        // Ordering (Ascending as requested)
        query = query.OrderBy(l => l.Timestamp);

        // Limit (Max 5000 enforced in validator or controller? Enforce here too safely)
        int effectiveLimit = request.Limit > 5000 ? 5000 : request.Limit;
        if (effectiveLimit <= 0) effectiveLimit = 1000;

        // Fetch Limit + 1 to check if there is a next page
        var points = await query
            .Take(effectiveLimit + 1)
            .Select(l => new LocationDto
            {
                Timestamp = l.Timestamp,
                Lat = l.Lat,
                Lon = l.Lon,
                Speed = l.Speed,
                Heading = l.Heading,
                Accuracy = l.Accuracy
            })
            .ToListAsync(cancellationToken);

        string? nextCursor = null;
        if (points.Count > effectiveLimit)
        {
            points.RemoveAt(points.Count - 1); // Remove the extra item
            // Cursor is the timestamp of the last item in the *current* page
            var lastItem = points.Last();
            nextCursor = lastItem.Timestamp.ToString("o");
        }

        return new PagedHistoryResponse
        {
            Data = points,
            NextCursor = nextCursor
        };
    }
}
