import { create } from 'zustand';
import type { Device, DeviceUpdateEvent } from '../types/device';

interface DeviceStore {
    devicesById: Record<string, Device>;
    selectedDeviceId: string | null;

    // Actions
    setDevices: (devices: Device[]) => void;
    updateDevice: (event: DeviceUpdateEvent) => void;
    selectDevice: (deviceId: string | null) => void;
}

export const useDeviceStore = create<DeviceStore>((set) => ({
    devicesById: {},
    selectedDeviceId: null,

    setDevices: (devices) => {
        const devicesById: Record<string, Device> = {};
        devices.forEach((device) => {
            devicesById[device.externalId] = device;
        });
        set({ devicesById });
    },

    updateDevice: (event) => {
        set((state) => {
            const existingDevice = state.devicesById[event.externalId];

            const updatedDevice: Device = existingDevice
                ? {
                    ...existingDevice,
                    lat: event.lat,
                    lon: event.lon,
                    lastLocationTime: event.timestamp,
                    lastSeen: event.timestamp,
                    isActive: true, // Just received update, so active
                }
                : {
                    // New device from SignalR
                    externalId: event.externalId,
                    name: event.externalId, // Use ID as name until we get proper name
                    lat: event.lat,
                    lon: event.lon,
                    lastLocationTime: event.timestamp,
                    lastSeen: event.timestamp,
                    isActive: true,
                };

            return {
                devicesById: {
                    ...state.devicesById,
                    [event.externalId]: updatedDevice,
                },
            };
        });
    },

    selectDevice: (deviceId) => set({ selectedDeviceId: deviceId }),
}));
