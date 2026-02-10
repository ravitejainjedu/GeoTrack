# Project Milestones

## M1: Foundation 🏗️
- initialized Git repository & solution structure.
- Docker Compose setup for PostgreSQL and API.
- Basic Health Checks.

## M2: Query APIs 🔍
- implemented `GET /api/devices` and history endpoints.
- PostgreSQL schema with TimeScaleDB-ready structure.
- Integration tests for data retrieval.

## M3: Ingestion & Real-time 🚀
- High-throughput ingestion (`POST /api/telemetry`).
- SignalR broadcasting (`/hubs/geotrack`).
- Throttling and Concurrency mgmt (`IIngestionGate`).
- **Stabilization**: Resolved `ECONNRESET` issues via batching and backpressure.

## M4: Live UI 🗺️
- React + Leaflet frontend.
- Live map with moving markers.
- Device list with realtime status updates.

## M5: History Trails ⏱️
- Visual history trails (polylines).
- Time range selection (15m, 1h, 6h).
- Optimized rendering with point capping.

## M6: Hardening & Release 🛡️
- **Authentication**: API Key protection for ingestion.
- **Documentation**: Comprehensive README and API docs.
- **Polish**: Production-ready configurations.
