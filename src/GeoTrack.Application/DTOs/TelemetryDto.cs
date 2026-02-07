using System;

namespace GeoTrack.Application.DTOs;

public class TelemetryDto
{
    public required string DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public float? Speed { get; set; }
    public float? Heading { get; set; }
    public float? Accuracy { get; set; }
}
