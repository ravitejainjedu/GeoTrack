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

    // State refs to prevent continuous re-fitting/flying
    const hasFitInitial = React.useRef(false);
    const lastSelectedId = React.useRef<string | null>(null);
    const userInteracted = React.useRef(false);

    // Track user interaction to disable auto-centering
    useMapEvents({
        click(e) {
            if (onBackgroundClick) onBackgroundClick();
        },
        dragstart() {
            userInteracted.current = true;
        },
        zoomstart() {
            userInteracted.current = true;
        }
    });

    // Handle Reset View Trigger (Home Button)
    useEffect(() => {
        if (resetViewTrigger && devices.length > 0) {
            console.log('[Map] Home clicked, resetting view');
            userInteracted.current = false; // Reset interaction flag
            const bounds = L.latLngBounds(devices.filter(d => d.lat && d.lon).map(d => [d.lat!, d.lon!]));
            if (bounds.isValid()) {
                map.fitBounds(bounds, { padding: [50, 50], maxZoom: 15, animate: true, duration: 1 });
            }
        }
    }, [resetViewTrigger, map, devices]);

    // Initial FitBounds (only once)
    useEffect(() => {
        if (!hasFitInitial.current && devices.length > 0) {
            const bounds = L.latLngBounds(devices.filter(d => d.lat && d.lon).map(d => [d.lat!, d.lon!]));
            if (bounds.isValid()) {
                console.log('[Map] Initial fitBounds');
                map.fitBounds(bounds, { padding: [50, 50], maxZoom: 15 });
                hasFitInitial.current = true;
            }
        }
    }, [devices, map]);

    // FlyTo Selection - ONLY on selection change, NOT on coordinate update
    useEffect(() => {
        // If selection changed
        if (selectedDeviceId !== lastSelectedId.current) {
            lastSelectedId.current = selectedDeviceId;

            if (selectedDeviceId) {
                const selected = devices.find(d => d.externalId === selectedDeviceId);
                // Only fly if we have a location AND user hasn't dragged away (unless it's a fresh selection)
                if (selected && selected.lat && selected.lon) {
                    console.log(`[Map] Flying to selected: ${selectedDeviceId}`);
                    map.flyTo([selected.lat, selected.lon], 16, {
                        duration: 1.5,
                    });
                    // Reset interaction on fresh selection so we track them again? 
                    // Or keep it true? User asked: "flyTo that device once".
                    // Let's NOT reset userInteracted here, because simply selecting doesn't mean we want to lock view.
                    // Actually, if I click a device in the list, I expect to see it.
                    userInteracted.current = false;
                }
            }
        }
        // NOTE: We deliberately do NOT put 'devices' in dependency array to avoid re-running on update.
        // We only want to run when selectedDeviceId changes.
        // However, if the device list is empty when selected, we might miss it?
        // But 'selectedDeviceId' coming from store ensures we usually have devices.
        // To be safe, we can check if we need to fly to an existing selection that just appeared?
        // For now, adhere to "Selection flyTo only once per selection".
    }, [selectedDeviceId, map, devices]); // 'devices' is needed to find the lat/lon. 

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
