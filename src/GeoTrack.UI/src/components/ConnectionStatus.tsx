

interface ConnectionStatusProps {
    state: 'Connected' | 'Connecting' | 'Disconnected';
}

export function ConnectionStatus({ state }: ConnectionStatusProps) {
    const getStatusStyle = () => {
        switch (state) {
            case 'Connected':
                return { color: '#10b981', icon: '🟢' };
            case 'Connecting':
                return { color: '#f59e0b', icon: '🟡' };
            case 'Disconnected':
                return { color: '#ef4444', icon: '🔴' };
        }
    };

    const { color, icon } = getStatusStyle();

    return (
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '14px' }}>
            <span>{icon}</span>
            <span style={{ color, fontWeight: 500 }}>{state}</span>
        </div>
    );
}
