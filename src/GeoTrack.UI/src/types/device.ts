export interface Device {
    externalId: string;
    name: string | null;
    lat: number | null;
    lon: number | null;
    lastLocationTime: string | null;
    lastSeen: string | null;
    isActive: boolean;
}

export interface DeviceUpdateEvent {
    externalId: string;
    timestamp: string;
    lat: number;
    lon: number;
    speed?: number;
    heading?: number;
    accuracy?: number;
}
