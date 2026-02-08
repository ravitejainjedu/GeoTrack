import { useEffect, useState, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import type { DeviceUpdateEvent } from '../types/device';
import { useDeviceStore } from '../stores/deviceStore';

type ConnectionState = 'Connected' | 'Connecting' | 'Disconnected';

interface UseSignalRResult {
    connectionState: ConnectionState;
    recentEvents: DeviceUpdateEvent[];
}

const API_URL = import.meta.env.VITE_API_URL || 'http://127.0.0.1:5000';
// Fix for Windows: Ensure we use 127.0.0.1 for WebSockets to avoid localhost resolution issues
const HUB_URL = `${API_URL.replace('localhost', '127.0.0.1')}/hubs/geotrack`;

export function useSignalR(): UseSignalRResult {
    const [connectionState, setConnectionState] = useState<ConnectionState>('Disconnected');
    const [recentEvents, setRecentEvents] = useState<DeviceUpdateEvent[]>([]);
    const connectionRef = useRef<signalR.HubConnection | null>(null);
    const startedRef = useRef(false);
    const updateDevice = useDeviceStore((state) => state.updateDevice);

    useEffect(() => {
        // Prevent double connection in StrictMode
        if (startedRef.current) return;
        startedRef.current = true;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(HUB_URL, {
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        connectionRef.current = connection;

        // Connection state handlers
        connection.onreconnecting(() => {
            setConnectionState('Connecting');
            console.log(`[${new Date().toISOString()}] SignalR: Reconnecting...`);
        });

        connection.onreconnected(() => {
            setConnectionState('Connected');
            console.log(`[${new Date().toISOString()}] SignalR: Reconnected`);
        });

        connection.onclose(() => {
            setConnectionState('Disconnected');
            console.log(`[${new Date().toISOString()}] SignalR: Disconnected`);
            startedRef.current = false;
        });

        // Listen for events
        connection.on('DeviceUpdated', (event: DeviceUpdateEvent) => {
            console.log('SignalR: Received DeviceUpdated', event);
            updateDevice(event);
            setRecentEvents((prev) => [event, ...prev].slice(0, 20));
        });

        // Start connection
        const startConnection = async () => {
            try {
                setConnectionState('Connecting');
                await connection.start();
                setConnectionState('Connected');
                console.log(`[${new Date().toISOString()}] SignalR: Connected to ${HUB_URL}`);
            } catch (err) {
                console.error('SignalR: Start failed', err);
                setConnectionState('Disconnected');
                startedRef.current = false;
            }
        };

        startConnection();

        return () => {
            // Only stop if it's the same connection instance
            if (connectionRef.current === connection) {
                console.log(`[${new Date().toISOString()}] SignalR: Stopping connection`);
                connection.stop();
                connectionRef.current = null;
                startedRef.current = false;
            }
        };
    }, [updateDevice]);

    return { connectionState, recentEvents };
}
