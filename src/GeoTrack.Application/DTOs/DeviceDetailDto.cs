using System;

namespace GeoTrack.Application.DTOs;

public class DeviceDetailDto
{
    public required string Id { get; set; } // ExternalId
    public string? Name { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool IsActive { get; set; }
    public LocationDto? LatestLocation { get; set; }
}
