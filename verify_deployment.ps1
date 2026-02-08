$ErrorActionPreference = "Stop"

function Test-Endpoint {
    param($Url, $Description, $Headers = @{})
    Write-Host "Testing $Description ($Url)..." -NoNewline
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -Headers $Headers -UseBasicParsing
        Write-Host " OK ($($response.StatusCode))" -ForegroundColor Green
        return $response
    } catch {
        Write-Host " FAILED ($($_))" -ForegroundColor Red
        return $null
    }
}

Write-Host "`n=== GeoTrack Verification ===" -ForegroundColor Cyan

# 1. API Health
Test-Endpoint "http://127.0.0.1:5000/health/live" "API Liveness" | Out-Null

# 2. REST CORS
$origin = "http://127.0.0.1:5173"
$corsHeaders = @{ "Origin" = $origin }
$resp = Test-Endpoint "http://127.0.0.1:5000/api/devices" "REST CORS" $corsHeaders
if ($resp) {
    $acao = $resp.Headers["Access-Control-Allow-Origin"]
    if ($acao -eq $origin) {
        Write-Host "  [+] Access-Control-Allow-Origin: $acao" -ForegroundColor Green
    } else {
        Write-Host "  [-] Missing/Wrong Access-Control-Allow-Origin: '$acao'" -ForegroundColor Red
    }
}

# 3. SignalR Negotiate CORS
# SignalR negotiate is a POST request usually, but GET works for CORS preflight check logic or initial verify
$negUrl = "http://127.0.0.1:5000/hubs/geotrack/negotiate?negotiateVersion=1"
Write-Host "Testing SignalR Negotiate CORS ($negUrl)..." -NoNewline
try {
    $resp = Invoke-WebRequest -Uri $negUrl -Method Post -Headers $corsHeaders -UseBasicParsing -Body ""
    Write-Host " OK" -ForegroundColor Green
    $acao = $resp.Headers["Access-Control-Allow-Origin"]
    if ($acao -eq $origin) {
        Write-Host "  [+] Access-Control-Allow-Origin: $acao" -ForegroundColor Green
    } else {
        Write-Host "  [-] Missing/Wrong CORS header on Negotiate" -ForegroundColor Red
    }
} catch {
    Write-Host " FAILED ($_)" -ForegroundColor Red
}

# 4. UI Availability
Test-Endpoint "http://127.0.0.1:5173" "UI Index" | Out-Null

Write-Host "`n=== Verification Complete ===" -ForegroundColor Cyan
