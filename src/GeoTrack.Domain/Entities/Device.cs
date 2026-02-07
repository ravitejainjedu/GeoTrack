using System;

namespace GeoTrack.Domain.Entities;

public class Device
{
    public Guid Id { get; set; }
    public required string ExternalId { get; set; }
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAt { get; set; }
    public double? LastLat { get; set; }
    public double? LastLon { get; set; }
}
