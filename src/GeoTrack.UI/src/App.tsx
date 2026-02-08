import { useEffect, useState } from 'react';
import { Map } from './components/Map';
import { DeviceList } from './components/DeviceList';
import { DeviceDetail } from './components/DeviceDetail';
import { ConnectionStatus } from './components/ConnectionStatus';
import { DebugPanel } from './components/DebugPanel';
import { useSignalR } from './hooks/useSignalR';
import { useDeviceStore } from './stores/deviceStore';
import type { Device } from './types/device';
import './App.css';

const API_URL = import.meta.env.VITE_API_URL || 'http://127.0.0.1:5000';

function App() {
  const { connectionState, recentEvents } = useSignalR();
  const { devicesById, selectedDeviceId, setDevices, selectDevice } = useDeviceStore();
  const [resetViewTrigger, setResetViewTrigger] = useState(0);

  // Fetch devices from REST API on mount
  useEffect(() => {
    fetch(`${API_URL}/api/devices`)
      .then((res) => res.json())
      .then((data: any[]) => {
        const devices: Device[] = data.map((d) => ({
          externalId: d.id,
          name: d.name,
          lat: d.latitude,
          lon: d.longitude,
          lastLocationTime: d.lastLocationTime,
          lastSeen: d.lastSeen,
          isActive: d.isActive,
        }));
        setDevices(devices);
      })
      .catch((err) => console.error('Failed to fetch devices:', err));
  }, [setDevices]);

  // Optional: Poll every 60s to refresh isActive/lastSeen
  useEffect(() => {
    const interval = setInterval(() => {
      fetch(`${API_URL}/api/devices`)
        .then((res) => res.json())
        .then((data: any[]) => {
          const devices: Device[] = data.map((d) => ({
            externalId: d.id,
            name: d.name,
            lat: d.latitude,
            lon: d.longitude,
            lastLocationTime: d.lastLocationTime,
            lastSeen: d.lastSeen,
            isActive: d.isActive,
          }));
          setDevices(devices);
        })
        .catch((err) => console.error('Failed to refresh devices:', err));
    }, 60000); // 60 seconds

    return () => clearInterval(interval);
  }, [setDevices]);

  const devices = Object.values(devicesById);
  const selectedDevice = selectedDeviceId ? devicesById[selectedDeviceId] : null;

  const handleResetView = () => {
    selectDevice(null);
    setResetViewTrigger(Date.now());
  };

  return (
    <div style={{ height: '100vh', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
      {/* Header */}
      <div
        style={{
          height: 'var(--header-height)',
          padding: '0 var(--space-md)',
          borderBottom: '1px solid var(--color-border)',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          backgroundColor: 'var(--color-bg-card)',
          flexShrink: 0,
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <h1 className="text-h1">GeoTrack Live</h1>
          <span style={{ fontSize: '10px', color: 'var(--color-text-nav)', border: '1px solid var(--color-border)', padding: '2px 4px', borderRadius: '4px', fontFamily: 'monospace' }}>
            v{new Date().toISOString().split('T')[1].split('.')[0]}
          </span>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
          <button
            onClick={handleResetView}
            className="btn"
            style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
            title="Clear selection and fit to all devices"
          >
            <span>🏠</span>
            <span>Home</span>
          </button>
          <ConnectionStatus state={connectionState} />
        </div>
      </div>

      {/* Main Content */}
      <div style={{ flex: 1, display: 'flex', overflow: 'hidden' }}>
        {/* Left Sidebar */}
        <div
          style={{
            width: 'var(--sidebar-width)',
            borderRight: '1px solid var(--color-border)',
            display: 'flex',
            flexDirection: 'column',
            backgroundColor: 'var(--color-bg-card)',
            flexShrink: 0,
          }}
        >
          <div style={{ flex: 1, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
            <DeviceList
              devices={devices}
              selectedDeviceId={selectedDeviceId}
              onDeviceSelect={selectDevice}
            />
          </div>
          <div style={{ borderTop: '1px solid var(--color-border)' }}>
            <DeviceDetail
              device={selectedDevice}
              onClose={() => selectDevice(null)}
            />
          </div>
          <DebugPanel events={recentEvents} />
        </div>

        {/* Map */}
        <div style={{ flex: 1, position: 'relative' }}>
          <Map
            devices={devices}
            selectedDeviceId={selectedDeviceId}
            onDeviceSelect={selectDevice}
            onBackgroundClick={() => selectDevice(null)}
            resetViewTrigger={resetViewTrigger}
          />
        </div>
      </div>
    </div>
  );
}

export default App;
