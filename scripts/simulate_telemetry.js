const http = require('http'); // using http for localhost

const API_URL = process.env.API_URL || 'http://127.0.0.1:5000/api/telemetry';
const DEVICE_COUNT = 5;
const INTERVAL_MS = 1000;

// Keep-Alive Agent
const agent = new http.Agent({
  keepAlive: true,
  maxSockets: 10,
  timeout: 5000 // Socket timeout
});

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

function sendTelemetry(payload) {
  const data = JSON.stringify(payload);
  const url = new URL(API_URL); // Use the global API_URL constant

  const options = {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Content-Length': Buffer.byteLength(data),
      'Connection': 'keep-alive',
      'X-API-KEY': process.env.API_KEY || 'dev-key-123'
    },
    agent: agent,
    timeout: 5000 // Request timeout
  };

  return new Promise((resolve, reject) => {
    const req = http.request(url, options, (res) => {
      let body = '';
      res.on('data', chunk => body += chunk);
      res.on('end', () => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          resolve();
        } else {
          // Log response body for errors (e.g. 429 or 500)
          reject(new Error(`Status Code: ${res.statusCode} - Body: ${body}`));
        }
      });
    });

    req.on('error', (e) => reject(e));
    req.on('timeout', () => {
      req.destroy();
      reject(new Error('Request Timeout'));
    });

    req.write(data);
    req.end();
  });
}

async function run() {
  console.log(`Starting simulator for ${DEVICE_COUNT} devices...`);
  console.log(`Target: ${API_URL}`);

  setInterval(async () => {
    const payloads = devices.map(move);

    try {
      await sendTelemetry(payloads);
      // console.log(`Sent batch of ${payloads.length}`);
    } catch (err) {
      console.error(`Failed batch: ${err.message}`);
    }
  }, INTERVAL_MS);
}

run();
