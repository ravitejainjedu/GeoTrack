using System;

namespace GeoTrack.Application.DTOs;

public class DeviceSummaryDto
{
    public required string Id { get; set; } // ExternalId
    public string? Name { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool IsActive { get; set; }

    // Latest location for map display
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? LastLocationTime { get; set; }
}
