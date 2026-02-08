using Microsoft.AspNetCore.SignalR;

namespace GeoTrack.Api.Hubs;

public class GeoTrackHub : Hub
{
    // Simple hub for now, clients just listen for "DeviceUpdated"
}
