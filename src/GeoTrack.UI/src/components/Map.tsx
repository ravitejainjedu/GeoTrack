import React, { useEffect } from 'react';
import { MapContainer, TileLayer, CircleMarker, Popup, useMap, useMapEvents, Polyline } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import type { Device } from '../types/device';

interface MapProps {
    devices: Device[];
    selectedDeviceId: string | null;
    history?: { lat: number; lon: number }[];
    onDeviceSelect: (deviceId: string) => void;
    onBackgroundClick?: () => void;
    resetViewTrigger?: number;
}

function MapController({ devices, selectedDeviceId, onBackgroundClick, resetViewTrigger }: {
    devices: Device[];
    selectedDeviceId: string | null;
    onBackgroundClick?: () => void;
    resetViewTrigger?: number;
}) {
    const map = useMap();

    // Handle background clicks to clear selection
    useMapEvents({
        click(e) {
            // Only clear if clicking on the map background, not a marker
            // Leaflet handles bubbling, but we can trust useMapEvents for the map container
            if (onBackgroundClick) onBackgroundClick();
        },
    });

    // Handle Reset View Trigger
    useEffect(() => {
        if (resetViewTrigger && devices.length > 0) {
            const bounds = L.latLngBounds(devices.filter(d => d.lat && d.lon).map(d => [d.lat!, d.lon!]));
            if (bounds.isValid()) {
                map.fitBounds(bounds, { padding: [50, 50], maxZoom: 15, animate: true, duration: 1 });
            }
        }
    }, [resetViewTrigger, map, devices]);

    // Initial FitBounds (only on mount/first load)
    useEffect(() => {
        if (devices.length > 0) {
            const bounds = L.latLngBounds(devices.filter(d => d.lat && d.lon).map(d => [d.lat!, d.lon!]));
            if (bounds.isValid()) {
                map.fitBounds(bounds, { padding: [50, 50], maxZoom: 15 });
            }
        }
    }, []); // Run once

    // FlyTo Selection
    useEffect(() => {
        const selected = devices.find(d => d.externalId === selectedDeviceId);
        if (selected && selected.lat && selected.lon) {
            map.flyTo([selected.lat, selected.lon], 16, {
                duration: 1.5,
            });
        }
    }, [selectedDeviceId, map, devices]);

    return null;
}

export function Map({ devices, selectedDeviceId, history, onDeviceSelect, onBackgroundClick, resetViewTrigger }: MapProps) {
    const devicesWithLocation = devices.filter(
        (d) => d.lat !== null && d.lon !== null
    );

    const center: [number, number] = [37.7749, -122.4194];

    return (
        <MapContainer
            center={center}
            zoom={13}
            style={{ height: '100%', width: '100%', minHeight: '400px', zIndex: 0 }}
        >
            <TileLayer
                attribution='&copy; OpenStreetMap'
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            />

            <MapController
                devices={devicesWithLocation}
                selectedDeviceId={selectedDeviceId}
                onBackgroundClick={onBackgroundClick}
                resetViewTrigger={resetViewTrigger}
            />

            {history && history.length > 1 && (
                <Polyline
                    positions={history.map(p => [p.lat, p.lon])}
                    pathOptions={{ color: '#3b82f6', weight: 4, opacity: 0.6, dashArray: '10, 10', lineCap: 'round' }}
                />
            )}

            {devicesWithLocation.map((device) => (
                <React.Fragment key={device.externalId}>
                    <CircleMarker
                        center={[device.lat!, device.lon!]}
                        radius={8}
                        pathOptions={{
                            color: device.isActive ? '#10b981' : '#6b7280',
                            fillColor: device.isActive ? '#34d399' : '#9ca3af',
                            fillOpacity: 0.8,
                            weight: 2
                        }}
                        eventHandlers={{
                            click: (e) => {
                                L.DomEvent.stopPropagation(e); // Prevent map background click
                                onDeviceSelect(device.externalId);
                            },
                        }}
                    >
                        <Popup>
                            <div style={{ minWidth: '150px' }}>
                                <h3 style={{ margin: '0 0 4px', fontSize: '14px' }}>{device.name || device.externalId}</h3>
                                <div style={{ fontSize: '12px', color: '#4b5563' }}>
                                    <div>Status: <strong>{device.isActive ? 'Active' : 'Offline'}</strong></div>
                                    <div>Updated: {device.lastLocationTime ? new Date(device.lastLocationTime).toLocaleTimeString() : 'N/A'}</div>
                                    <hr style={{ margin: '4px 0', border: '0', borderTop: '1px solid #eee' }} />
                                    <div>Lat: {device.lat?.toFixed(5)}</div>
                                    <div>Lon: {device.lon?.toFixed(5)}</div>
                                </div>
                            </div>
                        </Popup>
                    </CircleMarker>
                </React.Fragment>
            ))}
        </MapContainer>
    );
}
