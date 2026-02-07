const https = require('http'); // using http for localhost

const API_URL = process.env.API_URL || 'http://localhost:5000/api/telemetry';
const DEVICE_COUNT = 5;
const INTERVAL_MS = 1000;

// Initial positions (approx Dallas, TX)
const devices = Array.from({ length: DEVICE_COUNT }, (_, i) => ({
  deviceId: `device-${i + 1}`,
  lat: 32.7767 + (Math.random() - 0.5) * 0.1,
  lon: -96.7970 + (Math.random() - 0.5) * 0.1,
  speed: 0,
  heading: Math.random() * 360
}));

function move(device) {
  // Random walk
  const speed = 10 + Math.random() * 20; // 10-30 m/s
  const deltaHeading = (Math.random() - 0.5) * 10;
  device.heading = (device.heading + deltaHeading + 360) % 360;
  device.speed = speed;

  // Simple lat/lon update (approximate)
  const dist = speed * (INTERVAL_MS / 1000) / 111000; // degrees
  const rad = device.heading * Math.PI / 180;
  
  device.lat += dist * Math.cos(rad);
  device.lon += dist * Math.sin(rad);

  return {
    deviceId: device.deviceId,
    timestamp: new Date().toISOString(),
    lat: device.lat,
    lon: device.lon,
    speed: device.speed,
    heading: device.heading,
    accuracy: 5.0
  };
}

async function sendTelemetry(payload) {
  return new Promise((resolve, reject) => {
    const url = new URL(API_URL);
    const options = {
      hostname: url.hostname,
      port: url.port,
      path: url.pathname,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(JSON.stringify(payload))
      }
    };

    const req = https.request(options, (res) => {
      if (res.statusCode >= 200 && res.statusCode < 300) {
        resolve();
      } else {
        reject(new Error(`Status Code: ${res.statusCode}`));
      }
    });

    req.on('error', (e) => reject(e));
    req.write(JSON.stringify(payload));
    req.end();
  });
}

async function run() {
  console.log(`Starting simulator for ${DEVICE_COUNT} devices...`);
  console.log(`Target: ${API_URL}`);

  setInterval(async () => {
    const payloads = devices.map(move);
    
    // Send individually or batch? Requirements say single or batch. 
    // Let's send individually for now to test load, or batch if API supports it.
    // The simulator requirement didn't specify, but "Ingest GPS points reliably (single + batch)" suggests both.
    // Let's send single points for now to create more traffic.
    
    for (const p of payloads) {
      try {
        await sendTelemetry(p);
        // console.log(`Sent ${p.deviceId}`);
      } catch (err) {
        console.error(`Failed ${p.deviceId}: ${err.message}`);
      }
    }
  }, INTERVAL_MS);
}

run();
