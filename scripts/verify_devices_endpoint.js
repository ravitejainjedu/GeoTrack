const http = require('http');

const options = {
    hostname: '127.0.0.1',
    port: 5000,
    path: '/api/devices',
    method: 'GET',
    headers: {
        'Accept': 'application/json'
    }
};

const req = http.request(options, (res) => {
    let body = '';
    console.log(`STATUS: ${res.statusCode}`);

    res.setEncoding('utf8');
    res.on('data', (chunk) => {
        body += chunk;
    });

    res.on('end', () => {
        if (res.statusCode !== 200) {
            console.error('FAILED: Expected 200 OK');
            console.error('BODY:', body);
            process.exit(1);
        }

        try {
            const data = JSON.parse(body);
            if (!Array.isArray(data)) {
                console.error('FAILED: Expected JSON array');
                console.error('BODY:', body);
                process.exit(1);
            }
            console.log('SUCCESS: Got valid JSON array');
            console.log('Count:', data.length);
        } catch (e) {
            console.error('FAILED: Invalid JSON');
            console.error('BODY:', body);
            process.exit(1);
        }
    });
});

req.on('error', (e) => {
    console.error(`problem with request: ${e.message}`);
    process.exit(1);
});

req.end();
