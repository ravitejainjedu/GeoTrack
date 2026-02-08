using System.Threading.Tasks;
using GeoTrack.Application.DTOs;

namespace GeoTrack.Application.Common.Interfaces;

public interface ITelemetryBroadcaster
{
    Task BroadcastAsync(string externalId, TelemetryDto telemetry);
}
