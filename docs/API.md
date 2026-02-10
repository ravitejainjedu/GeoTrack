# GeoTrack API Documentation

## Overview
The GeoTrack API provides endpoints for device management, location history, and high-throughput telemetry ingestion.

**Base URL**: `http://127.0.0.1:5000` (or configured value)

## Authentication
- **Read-Only Endpoints**: Public (no authentication required).
- **Ingestion Endpoint**: Requires API Key.
    - Header: `X-API-KEY`
    - Value: Configured in `appsettings.json` or `GeoTrack__ApiKey` env var.
    - Default Dev Key: `dev-key-123`

## Endpoints

### 1. Ingest Telemetry
Batch ingestion of device location updates.

- **URL**: `POST /api/telemetry`
- **Auth**: Required (`X-API-KEY`)
- **Content-Type**: `application/json`
- **Body**: Array of telemetry objects.

```json
[
  {
    "deviceId": "device-1",
    "lat": 32.7767,
    "lon": -96.7970,
    "speed": 15.5,
    "heading": 90.0,
    "accuracy": 5.0,
    "timestamp": "2023-10-27T10:00:00Z"
  }
]
```

- **Response**:
    - `202 Accepted`: Processing asynchronously.
    - `401 Unauthorized`: Invalid/Missing Key.
    - `429 Too Many Requests`: System overloaded (backpressure).

### 2. List Devices
Get all known devices and their current status.

- **URL**: `GET /api/devices`
- **Response**:

```json
[
  {
    "externalId": "device-1",
    "name": "Truck 1",
    "status": "Online",
    "lastLocation": { ... },
    "lastSeen": "2023-10-27T10:00:00Z"
  }
]
```

### 3. Get Device History
Get historical path for a specific device.

- **URL**: `GET /api/devices/{id}/locations`
- **Query Params**:
    - `from` (ISO8601)
    - `to` (ISO8601)
    - `limit` (max points, e.g. 500)
- **Response**:

```json
{
  "data": [
    { "lat": 32.7, "lon": -96.7, "timestamp": "...", "speed": 10 }
  ],
  "nextCursor": null
}
```

## Real-time (SignalR)
Connect to `/hubs/geotrack` to receive live updates.

### Events
- **`DeviceUpdated`**: Broadcast when a device reports a new location.

**Payload**:
```json
{
  "externalId": "device-1",
  "lat": 32.78,
  "lon": -96.80,
  "timestamp": "2023-10-27T10:00:05Z",
  "speed": 16.0,
  "heading": 92.0
}
```
