# GeoTrack 🌍

**GeoTrack** is a real-time geospatial telemetry system that ingests high-frequency location data from devices and displays live movements on an interactive map with history trails.

## Features
- 🚀 **High-Throughput Ingestion**: Handles thousands of updates per second via .NET 9 API.
- 📡 **Real-time Streaming**: SignalR broadcasts updates to connected clients instantly.
- 🗺️ **Live Dashboard**: React + Leaflet UI showing moving devices and status.
- ⏱️ **History Trails**: View past paths with adjustable time windows.
- 🐳 **Dockerized**: Full stack (PostgreSQL + TimescaleDB, API, UI) in one command.

## Quick Start

### Prerequisites
- Docker Desktop
- Windows/Linux/Mac

### Run
```bash
docker compose up --build
```
*Note for Windows users: The UI communicates with the API via `127.0.0.1`. If you experience connection issues, ensure your browser and simulator use `127.0.0.1` instead of `localhost`.*

### Access
- **UI**: [http://127.0.0.1:5173](http://127.0.0.1:5173)
- **API**: [http://127.0.0.1:5000](http://127.0.0.1:5000)
- **Swagger**: [http://127.0.0.1:5000/swagger](http://127.0.0.1:5000/swagger)

## Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `VITE_API_URL` | UI API Connection URL | `http://127.0.0.1:5000` |
| `GeoTrack__ApiKey` | API Key for `POST /api/telemetry` | `dev-key-123` |

## Documentation
- [API Documentation](docs/API.md)
- [Milestones & History](docs/MILESTONES.md)

## Development
- **Backend**: C# .NET 9 (Clean Architecture)
- **Frontend**: React 18 + TypeScript + Vite
- **Database**: PostgreSQL 16 + PostGIS
