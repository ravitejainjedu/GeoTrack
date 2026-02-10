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

// Cap points based on range to prevent rendering lag
const MAX_POINTS_BY_RANGE: Record<TimeRange, number> = {
    15: 300,
    30: 600,
    60: 1200,
    360: 2500
};

export function useDeviceHistory(
    deviceId: string | null,
    timeRangeMinutes: TimeRange,
    latestEvent: DeviceUpdateEvent | null
) {
    const [history, setHistory] = useState<HistoryPoint[]>([]);
    const [isLoading, setIsLoading] = useState(false);

    // Refs for concurrency control and deduplication
    const lastEventTimeRef = useRef<string | null>(null);
    const requestIdRef = useRef(0);
    const abortControllerRef = useRef<AbortController | null>(null);

    // Fetch history when device or range changes
    useEffect(() => {
        // 1. Reset State Immediately
        const currentRequestId = ++requestIdRef.current;

        // Cancel previous request if running
        if (abortControllerRef.current) {
            abortControllerRef.current.abort();
        }

        // Create new controller
        const controller = new AbortController();
        abortControllerRef.current = controller;

        if (!deviceId) {
            console.log('[History] No device, clearing');
            setHistory([]);
            setIsLoading(false);
            return;
        }

        console.log(`[History] Range changed to ${timeRangeMinutes}m, resetting points`);
        setHistory([]); // Visible reset
        setIsLoading(true);
        lastEventTimeRef.current = null;

        const fetchHistory = async () => {
            try {
                const to = new Date();
                const from = new Date(to.getTime() - timeRangeMinutes * 60 * 1000);
                const limit = MAX_POINTS_BY_RANGE[timeRangeMinutes] || 500;

                const url = `${API_URL}/api/devices/${deviceId}/locations?from=${from.toISOString()}&to=${to.toISOString()}&limit=${limit}`;

                const res = await fetch(url, { signal: controller.signal });
                if (!res.ok) throw new Error('Failed to fetch history');

                const data = await res.json();

                // version check: if request changed, ignore result
                if (currentRequestId !== requestIdRef.current) return;

                const count = data.data?.length || 0;
                console.log(`[History] Loaded ${count} points`);

                const points = (data.data || []).map((item: Record<string, unknown>) => ({
                    lat: item.lat as number,
                    lon: item.lon as number,
                    timestamp: item.timestamp as string,
                    speed: item.speed as number | undefined
                }));

                setHistory(points);
                if (points.length > 0) {
                    lastEventTimeRef.current = points[points.length - 1].timestamp;
                }
            } catch (err: unknown) {
                if ((err as Error).name === 'AbortError') return;
                console.error('[History] Error fetching:', err);
                if (currentRequestId === requestIdRef.current) {
                    setHistory([]);
                }
            } finally {
                if (currentRequestId === requestIdRef.current) {
                    setIsLoading(false);
                }
            }
        };

        fetchHistory();

        return () => {
            controller.abort();
        };
    }, [deviceId, timeRangeMinutes]);

    // Append real-time events
    useEffect(() => {
        if (!deviceId || !latestEvent) return;
        if (latestEvent.externalId !== deviceId) return;

        // Dedup based on timestamp
        if (lastEventTimeRef.current === latestEvent.timestamp) return;

        // Filter out if older than current range window
        const eventTime = new Date(latestEvent.timestamp).getTime();
        const windowStart = Date.now() - (timeRangeMinutes * 60 * 1000);
        if (eventTime < windowStart) return;

        lastEventTimeRef.current = latestEvent.timestamp;

        setHistory(prev => {
            const newPoint: HistoryPoint = {
                lat: latestEvent.lat,
                lon: latestEvent.lon,
                timestamp: latestEvent.timestamp,
                speed: latestEvent.speed
            };

            const limit = MAX_POINTS_BY_RANGE[timeRangeMinutes] || 500;
            const newHistory = [...prev, newPoint];

            if (newHistory.length > limit) {
                return newHistory.slice(newHistory.length - limit);
            }
            return newHistory;
        });
    }, [latestEvent, deviceId, timeRangeMinutes]);

    return { history, isLoading };
}
