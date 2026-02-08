import { useState, useEffect, useRef } from 'react';
import type { DeviceUpdateEvent } from '../types/device';

const API_URL = import.meta.env.VITE_API_URL || 'http://127.0.0.1:5000';

export interface HistoryPoint {
    lat: number;
    lon: number;
    timestamp: string;
    speed?: number;
}

export type TimeRange = 15 | 30 | 60 | 360; // Minutes

export function useDeviceHistory(
    deviceId: string | null,
    timeRangeMinutes: TimeRange,
    latestEvent: DeviceUpdateEvent | null
) {
    const [history, setHistory] = useState<HistoryPoint[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    // Keep track of the last processed event to avoid duplicates
    const lastEventTimeRef = useRef<string | null>(null);

    // Fetch history when device or range changes
    useEffect(() => {
        if (!deviceId) {
            console.log('[History] No device selected, clearing history');
            setHistory([]);
            return;
        }

        console.log(`[History] selectedDeviceId: ${deviceId}, range: ${timeRangeMinutes}m`);

        const fetchHistory = async () => {
            setIsLoading(true);
            try {
                const to = new Date();
                const from = new Date(to.getTime() - timeRangeMinutes * 60 * 1000);

                const url = `${API_URL}/api/devices/${deviceId}/locations?from=${from.toISOString()}&to=${to.toISOString()}&limit=500`;
                console.log(`[History] GET ${url}`);

                const res = await fetch(url);
                if (!res.ok) throw new Error('Failed to fetch history');

                const data = await res.json();
                const count = data.data?.length || 0;
                console.log(`[History] points received: ${count}`);

                // Map API response to simple points
                // Backend returns: Data: LocationDto[] { lat, lon, timestamp, speed }
                const points = (data.data || []).map((item: any) => ({
                    lat: item.lat,
                    lon: item.lon,
                    timestamp: item.timestamp,
                    speed: item.speed
                }));
                // Verify ordering: Backend sorts by Timestamp ASC.

                setHistory(points);
                if (points.length > 0) {
                    lastEventTimeRef.current = points[points.length - 1].timestamp;
                } else {
                    lastEventTimeRef.current = null; // Accept any new live point if history is empty
                }
            } catch (err) {
                console.error('[History] Error fetching:', err);
                setHistory([]);
            } finally {
                setIsLoading(false);
            }
        };

        fetchHistory();
    }, [deviceId, timeRangeMinutes]);

    // Append real-time events
    useEffect(() => {
        if (!deviceId || !latestEvent) return;
        if (latestEvent.externalId !== deviceId) return;

        // Dedup based on timestamp
        if (lastEventTimeRef.current === latestEvent.timestamp) return;

        lastEventTimeRef.current = latestEvent.timestamp;

        console.log(`[History] Appending live point: ${latestEvent.timestamp}`);

        setHistory(prev => {
            const newPoint: HistoryPoint = {
                lat: latestEvent.lat,
                lon: latestEvent.lon,
                timestamp: latestEvent.timestamp,
                speed: latestEvent.speed
            };

            // Append and slice to keep size manageable (e.g. 500)
            const newHistory = [...prev, newPoint];
            if (newHistory.length > 500) {
                return newHistory.slice(newHistory.length - 500);
            }
            return newHistory;
        });
    }, [latestEvent, deviceId]);

    return { history, isLoading };
}
