using System;

namespace GeoTrack.Domain.Entities;

public class Location
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public float? Speed { get; set; }
    public float? Heading { get; set; }
    public float? Accuracy { get; set; }
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Device? Device { get; set; }
}
