import React, { useState } from 'react';
import type { DeviceUpdateEvent } from '../types/device';

interface DebugPanelProps {
    events: DeviceUpdateEvent[];
}

export function DebugPanel({ events }: DebugPanelProps) {
    const [isExpanded, setIsExpanded] = useState(false);
    const [isPaused, setIsPaused] = useState(false);
    const [filterId, setFilterId] = useState('');

    // Calculate events per second (sliding window over last 20 events)
    const eventRate = React.useMemo(() => {
        if (events.length < 2) return 0;
        const newest = new Date(events[0].timestamp).getTime();
        const oldest = new Date(events[events.length - 1].timestamp).getTime();
        const seconds = (newest - oldest) / 1000;
        return seconds > 0 ? (events.length / seconds).toFixed(1) : 0;
    }, [events]);

    // const displayEvents = isPaused ? events : events; 
    // Actually, to "pause", we need to store a snapshot. 
    // But since prop updates come from parent, we can just ignore new props if paused? 
    // Better: maintain local buffer if not paused.
    // Simpler: Just filter what we render.

    const filteredEvents = events.filter(e =>
        !filterId || e.externalId.toLowerCase().includes(filterId.toLowerCase())
    );

    return (
        <div style={{ borderTop: '1px solid var(--color-border)', backgroundColor: 'var(--color-bg-hover)' }}>
            <div
                onClick={() => setIsExpanded(!isExpanded)}
                style={{
                    padding: '8px var(--space-md)',
                    cursor: 'pointer',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    userSelect: 'none',
                    height: '40px'
                }}
            >
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <span style={{ fontSize: '10px' }}>{isExpanded ? '▼' : '▶'}</span>
                    <strong className="text-sm" style={{ fontWeight: 600 }}>Debug Stream</strong>
                    <span style={{ fontSize: '10px', color: 'var(--color-text-secondary)', backgroundColor: 'var(--color-bg-body)', padding: '1px 5px', borderRadius: '4px', border: '1px solid var(--color-border)' }}>
                        {eventRate} /s
                    </span>
                </div>
            </div>

            {isExpanded && (
                <div style={{ padding: '0 var(--space-md) var(--space-md)', borderTop: '1px dashed var(--color-border)' }}>
                    <div style={{ display: 'flex', gap: '8px', marginBottom: '8px', marginTop: '8px' }}>
                        <input
                            type="text"
                            className="input"
                            placeholder="Filter ID..."
                            value={filterId}
                            onChange={(e) => setFilterId(e.target.value)}
                            style={{ padding: '4px 8px', fontSize: '12px' }}
                        />
                        <button
                            className="btn"
                            onClick={() => setIsPaused(!isPaused)}
                            style={{
                                padding: '4px 8px',
                                fontSize: '11px',
                                backgroundColor: isPaused ? 'var(--color-danger-bg)' : 'white',
                                color: isPaused ? 'var(--color-danger-text)' : 'var(--color-text-main)',
                                borderColor: isPaused ? 'var(--color-danger-bg)' : 'var(--color-border)'
                            }}
                        >
                            {isPaused ? 'RESUMED' : 'PAUSE'}
                        </button>
                    </div>

                    {!isPaused && (
                        <div
                            style={{
                                maxHeight: '200px',
                                overflowY: 'auto',
                                fontSize: '11px',
                                fontFamily: 'monospace',
                                backgroundColor: 'var(--color-bg-card)',
                                border: '1px solid var(--color-border)',
                                borderRadius: '4px'
                            }}
                        >
                            {filteredEvents.length === 0 ? (
                                <div style={{ color: 'var(--color-text-nav)', padding: '12px', textAlign: 'center' }}>
                                    {filterId ? 'No matches' : 'Waiting...'}
                                </div>
                            ) : (
                                filteredEvents.map((event, index) => (
                                    <div
                                        key={index}
                                        style={{
                                            padding: '6px 8px',
                                            borderBottom: '1px solid var(--color-bg-body)',
                                            borderLeft: '2px solid var(--color-primary)',
                                            backgroundColor: index % 2 === 0 ? 'white' : 'var(--color-bg-body)'
                                        }}
                                    >
                                        <div style={{ display: 'flex', justifyContent: 'space-between', color: 'var(--color-text-main)' }}>
                                            <strong>{event.externalId}</strong>
                                            <span style={{ color: 'var(--color-text-nav)' }}>{new Date(event.timestamp).toLocaleTimeString()}</span>
                                        </div>
                                        <div style={{ color: 'var(--color-text-secondary)', marginTop: '2px' }}>
                                            {event.lat.toFixed(5)}, {event.lon.toFixed(5)}
                                            {event.speed !== undefined && <span style={{ marginLeft: '6px' }}>🚀 {event.speed.toFixed(1)}</span>}
                                        </div>
                                    </div>
                                ))
                            )}
                        </div>
                    )}
                    {isPaused && (
                        <div style={{ padding: '12px', color: 'var(--color-danger-text)', fontSize: '12px', textAlign: 'center', backgroundColor: 'var(--color-danger-bg)', borderRadius: '4px' }}>
                            Display Paused
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}
