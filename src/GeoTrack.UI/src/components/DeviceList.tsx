import { useEffect, useState } from 'react';
import type { Device } from '../types/device';

interface DeviceListProps {
    devices: Device[];
    selectedDeviceId: string | null;
    onDeviceSelect: (deviceId: string) => void;
}

function getRelativeTime(dateString: string | null | undefined): string {
    if (!dateString) return 'Never';
    const date = new Date(dateString);
    const now = new Date();
    const diff = Math.floor((now.getTime() - date.getTime()) / 1000);

    if (diff < 5) return 'Just now';
    if (diff < 60) return `${diff}s ago`;
    if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
    return `${Math.floor(diff / 3600)}h ago`;
}

function StatusBadge({ lastSeen }: { lastSeen: string | null | undefined }) {
    if (!lastSeen) return <span style={{ fontSize: '11px', color: '#9ca3af' }}>Offline</span>;

    const diff = (new Date().getTime() - new Date(lastSeen).getTime()) / 1000;
    const isLive = diff <= 15;

    return (
        <span
            style={{
                fontSize: '10px',
                padding: '2px 6px',
                borderRadius: '999px',
                backgroundColor: isLive ? '#dcfce7' : '#f3f4f6',
                color: isLive ? '#166534' : '#6b7280',
                fontWeight: 600,
                marginLeft: 'auto',
            }}
        >
            {isLive ? 'LIVE' : 'STALE'}
        </span>
    );
}

export function DeviceList({ devices, selectedDeviceId, onDeviceSelect }: DeviceListProps) {
    const [searchTerm, setSearchTerm] = useState('');
    // Force re-render every second to update relative times
    const [, setTick] = useState(0);
    useEffect(() => {
        const timer = setInterval(() => setTick(t => t + 1), 1000);
        return () => clearInterval(timer);
    }, []);

    const filteredDevices = devices.filter(d =>
        !searchTerm ||
        (d.name && d.name.toLowerCase().includes(searchTerm.toLowerCase())) ||
        d.externalId.toLowerCase().includes(searchTerm.toLowerCase())
    );

    const sortedDevices = [...filteredDevices].sort((a, b) => {
        // Active devices first
        if (a.isActive !== b.isActive) return a.isActive ? -1 : 1;
        // Then recent updates
        const timeA = new Date(a.lastLocationTime || 0).getTime();
        const timeB = new Date(b.lastLocationTime || 0).getTime();
        if (timeA !== timeB) return timeB - timeA;
        // Then by name
        return (a.name || a.externalId).localeCompare(b.name || b.externalId);
    });

    return (
        <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
            <div style={{ padding: 'var(--space-md)', borderBottom: '1px solid var(--color-border)' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                    <h3 className="text-h3">Devices ({devices.length})</h3>
                </div>
                <input
                    type="text"
                    className="input"
                    placeholder="Search devices..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                />
            </div>

            <div style={{ flex: 1, overflowY: 'auto', padding: 'var(--space-sm)' }}>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
                    {sortedDevices.map((device) => (
                        <div
                            key={device.externalId}
                            onClick={() => onDeviceSelect(device.externalId)}
                            className="card"
                            style={{
                                padding: '10px',
                                cursor: 'pointer',
                                backgroundColor: selectedDeviceId === device.externalId ? 'var(--color-bg-active)' : 'white',
                                borderColor: selectedDeviceId === device.externalId ? 'var(--color-border-active)' : 'var(--color-border)',
                                transition: 'all 0.1s',
                            }}
                        >
                            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '4px' }}>
                                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', overflow: 'hidden' }}>
                                    <span style={{ fontSize: '10px' }}>
                                        {device.isActive ? '🟢' : '⚪'}
                                    </span>
                                    <strong className="text-body" style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                                        {device.name || device.externalId}
                                    </strong>
                                </div>
                                <StatusBadge lastSeen={device.lastLocationTime || device.lastSeen} />
                            </div>

                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
                                <div className="text-xs">
                                    {device.lat && device.lon ? (
                                        <span className="text-mono">{device.lat.toFixed(4)}, {device.lon.toFixed(4)}</span>
                                    ) : (
                                        <span style={{ color: 'var(--color-text-nav)' }}>No location</span>
                                    )}
                                </div>
                                <div className="text-xs" style={{ color: 'var(--color-text-secondary)' }}>
                                    {getRelativeTime(device.lastLocationTime || device.lastSeen)}
                                </div>
                            </div>
                        </div>
                    ))}
                    {sortedDevices.length === 0 && (
                        <div style={{ padding: '20px', textAlign: 'center', color: 'var(--color-text-secondary)' }}>
                            No devices found
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
