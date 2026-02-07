using System.Collections.Generic;
using GeoTrack.Application.DTOs;

namespace GeoTrack.Application.Features.Devices.Queries.GetDeviceHistory;

public class PagedHistoryResponse
{
    public IEnumerable<LocationDto> Data { get; set; } = new List<LocationDto>();
    public string? NextCursor { get; set; }
}
