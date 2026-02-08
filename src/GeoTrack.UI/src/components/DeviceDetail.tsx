import React from 'react';
import type { Device } from '../types/device';
import type { TimeRange } from '../hooks/useDeviceHistory';

interface DeviceDetailProps {
    device: Device | null;
    onClose: () => void;
    historyStats?: { count: number; isLoading: boolean };
    timeRange?: TimeRange;
    onTimeRangeChange?: (range: TimeRange) => void;
}

// Field Component for consistent typography
const Field = ({ label, value, mono = false }: { label: string, value: React.ReactNode, mono?: boolean }) => (
    <div>
        <div className="text-xs" style={{ marginBottom: '2px', color: 'var(--color-text-nav)' }}>{label}</div>
        <div className={`text-body ${mono ? 'text-mono' : ''}`} style={{ fontWeight: 500 }}>
            {value}
        </div>
    </div>
);

export function DeviceDetail({ device, onClose, historyStats, timeRange, onTimeRangeChange }: DeviceDetailProps) {
    if (!device) {
        return (
            <div style={{ padding: 'var(--space-lg)', color: 'var(--color-text-secondary)', textAlign: 'center', backgroundColor: 'var(--color-bg-body)', margin: 'var(--space-md)', borderRadius: '8px', border: '1px dashed var(--color-border)' }}>
                <div style={{ fontSize: '24px', marginBottom: '8px' }}>📍</div>
                <div style={{ fontWeight: 500, marginBottom: '4px' }}>No Device Selected</div>
                <div className="text-sm">Select a device from the list or map to view details.</div>
            </div>
        );
    }

    return (
        <div style={{ maxHeight: '400px', overflowY: 'auto' }}>
            <div style={{
                padding: '12px var(--space-md)',
                borderBottom: '1px solid var(--color-border)',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                backgroundColor: 'var(--color-bg-hover)',
                position: 'sticky',
                top: 0,
                zIndex: 10
            }}>
                <h3 className="text-h3">Device Details</h3>
                <button
                    onClick={onClose}
                    style={{
                        background: 'none',
                        border: 'none',
                        cursor: 'pointer',
                        fontSize: '18px',
                        color: 'var(--color-text-secondary)',
                        padding: '0 4px',
                        lineHeight: 1
                    }}
                    title="Close"
                >
                    ✕
                </button>
            </div>

            <div style={{ padding: 'var(--space-md)', display: 'flex', flexDirection: 'column', gap: 'var(--space-md)' }}>
                <Field label="ID" value={device.externalId} />
                <Field label="Name" value={device.name || 'N/A'} />

                <Field
                    label="Status"
                    value={
                        <span style={{
                            display: 'inline-flex', alignItems: 'center', gap: '6px',
                            color: device.isActive ? 'var(--color-success-text)' : 'var(--color-text-secondary)',
                            backgroundColor: device.isActive ? 'var(--color-success-bg)' : 'var(--color-bg-body)',
                            padding: '2px 8px', borderRadius: '999px', fontSize: '12px'
                        }}>
                            {device.isActive ? '● Active' : '○ Inactive'}
                        </span>
                    }
                />

                {/* History Control Section */}
                <div style={{ marginTop: '4px', borderTop: '1px solid var(--color-border)', paddingTop: '12px' }}>
                    <div style={{ marginBottom: '8px' }}>
                        <div className="text-xs" style={{ color: 'var(--color-text-primary)', fontWeight: 600, marginBottom: '4px' }}>
                            HISTORY: {timeRange ? (timeRange < 60 ? `${timeRange}m` : `${timeRange / 60}h`) : ''} | {historyStats ? historyStats.count : 0} PTS
                        </div>
                        {historyStats && historyStats.count === 0 && !historyStats.isLoading && (
                            <div className="text-xs" style={{ color: 'var(--color-text-secondary)', fontStyle: 'italic', marginBottom: '8px' }}>
                                No stored history yet. Showing live trail only.
                            </div>
                        )}
                        {historyStats && historyStats.isLoading && (
                            <div className="text-xs" style={{ color: 'var(--color-text-secondary)', marginBottom: '8px' }}>
                                Loading history...
                            </div>
                        )}
                    </div>

                    {timeRange && onTimeRangeChange && (
                        <div style={{ display: 'flex', gap: '4px' }}>
                            {[15, 30, 60, 360].map((mins) => (
                                <button
                                    key={mins}
                                    onClick={() => onTimeRangeChange(mins as TimeRange)}
                                    style={{
                                        flex: 1,
                                        padding: '6px 0',
                                        fontSize: '11px',
                                        fontWeight: 500,
                                        borderRadius: '4px',
                                        border: `1px solid ${timeRange === mins ? 'var(--color-primary)' : 'var(--color-border)'}`,
                                        backgroundColor: timeRange === mins ? 'var(--color-primary)' : 'transparent',
                                        color: timeRange === mins ? '#fff' : 'var(--color-text-primary)',
                                        cursor: 'pointer',
                                        transition: 'all 0.2s'
                                    }}
                                >
                                    {mins < 60 ? `${mins}m` : `${mins / 60}h`}
                                </button>
                            ))}
                        </div>
                    )}
                </div>

                {device.lat && device.lon && (
                    <Field label="Location" value={`${device.lat.toFixed(6)}, ${device.lon.toFixed(6)}`} mono />
                )}

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
                    <Field
                        label="Last Update"
                        value={device.lastLocationTime ? new Date(device.lastLocationTime).toLocaleTimeString() : 'N/A'}
                    />
                    <Field
                        label="Last Seen"
                        value={device.lastSeen ? new Date(device.lastSeen).toLocaleTimeString() : 'N/A'}
                    />
                </div>
            </div>
        </div>
    );
}
