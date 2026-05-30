param(
    [string]$EnvFile = ".env",
    [string]$TunnelName = "cam-tunnel"
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$Text) {
    Write-Host ""
    Write-Host "==> $Text" -ForegroundColor Cyan
}

function Ensure-EnvFile([string]$Path) {
    if (Test-Path $Path) { return }

    $source = if (Test-Path ".env.example") { ".env.example" } elseif (Test-Path ".env.docker.example") { ".env.docker.example" } else { $null }
    if (-not $source) {
        throw "Khong tim thay .env.example hoac .env.docker.example de tao $Path"
    }

    Copy-Item $source $Path
}

function Set-Or-AddEnv([string]$Path, [string]$Key, [string]$Value) {
    $lines = @()
    if (Test-Path $Path) {
        $lines = Get-Content $Path
    }

    $updated = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "^\s*${Key}\s*=") {
            $lines[$i] = "${Key}=${Value}"
            $updated = $true
        }
    }

    if (-not $updated) {
        $lines += "${Key}=${Value}"
    }

    Set-Content -Path $Path -Value $lines -Encoding UTF8
}

function Assert-PortFree([int]$Port, [string]$Name) {
    $inUse = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue | Where-Object { $_.LocalPort -eq $Port }
    if ($inUse) {
        Write-Host "Canh bao: Port $Port dang duoc su dung cho $Name. Neu la container hien tai cua du an thi co the bo qua." -ForegroundColor Yellow
    }
}

Write-Step "Buoc 0: Tao file env neu chua co"
Ensure-EnvFile -Path $EnvFile

$publicHost = Read-Host "Nhap domain public (vi du app.example.com)"
if ([string]::IsNullOrWhiteSpace($publicHost)) {
    throw "Domain public khong duoc de trong."
}
$publicHost = $publicHost.Trim().Replace("https://", "").Replace("http://", "").TrimEnd("/")

Write-Step "Buoc 1: Nhap token tunnel"
$token = Read-Host "Nhap CLOUDFLARED_TUNNEL_TOKEN (dang eyJh...)"
if ([string]::IsNullOrWhiteSpace($token)) {
    throw "Token khong duoc de trong."
}

Write-Step "Buoc 2: Preflight ports"
Assert-PortFree -Port 1433 -Name "db"
Assert-PortFree -Port 5107 -Name "api"
Assert-PortFree -Port 5173 -Name "frontend"

Write-Step "Buoc 3: Ghi cau hinh vao .env"
Set-Or-AddEnv -Path $EnvFile -Key "CLOUDFLARED_TUNNEL_TOKEN" -Value $token.Trim()
Set-Or-AddEnv -Path $EnvFile -Key "APP_FRONTEND_URL" -Value ("https://" + $publicHost)
Set-Or-AddEnv -Path $EnvFile -Key "APP_GO2RTC_PUBLIC_BASE_URL" -Value ("https://" + $publicHost + "/go2rtc")
Set-Or-AddEnv -Path $EnvFile -Key "APP_ALLOW_CROSS_ORIGIN_GO2RTC_FRAME" -Value "false"
Set-Or-AddEnv -Path $EnvFile -Key "GO2RTC_WEBRTC_CANDIDATES" -Value ""
Set-Or-AddEnv -Path $EnvFile -Key "GO2RTC_WEBRTC_PORT" -Value "8555"
Set-Or-AddEnv -Path $EnvFile -Key "GO2RTC_STREAM_MODE" -Value "webrtc"

Write-Step "Buoc 4: Patch appsettings.json (giong setup-public-domain.bat)"
$root = Split-Path -Parent $PSScriptRoot
$appsettingsPath = Join-Path $root "API\\API\\API\\appsettings.json"
$updateScript = Join-Path $root "scripts\\update-public-domain-appsettings.ps1"
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $updateScript `
    -AppsettingsPath $appsettingsPath `
    -TunnelName $TunnelName `
    -PublicHostname $publicHost `
    -TargetService "http://localhost:5173"
if ($LASTEXITCODE -ne 0) {
    throw "Patch appsettings that bai."
}

Write-Step "Buoc 5: Khoi dong core stack"
& docker compose up -d --build db go2rtc api frontend
if ($LASTEXITCODE -ne 0) {
    throw "Khoi dong core stack that bai."
}

Write-Step "Buoc 6: Cho API healthy"
$ok = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $resp = Invoke-RestMethod -Uri "http://localhost:5107/health" -TimeoutSec 3
        if ($resp.status -eq "ok") { $ok = $true; break }
    } catch {}
    Start-Sleep -Seconds 2
}
if (-not $ok) {
    throw "API khong healthy sau khi khoi dong."
}

Write-Step "Buoc 7: Bat tunnel profile"
& docker compose --profile tunnel up -d cloudflared
if ($LASTEXITCODE -ne 0) {
    throw "Khoi dong cloudflared that bai."
}

Write-Step "Buoc 8: Reload go2rtc"
Invoke-RestMethod -Method Post -Uri "http://localhost:5107/api/camera-runtime/reload-go2rtc" -TimeoutSec 15 | Out-Null

Write-Step "Buoc 9: Kiem tra endpoint stream/ws"
try {
    $streamCheck = Invoke-WebRequest -Uri ("https://" + $publicHost + "/go2rtc/stream.html?src=cam1&mode=webrtc") -TimeoutSec 15
    Write-Host ("stream.html status: " + $streamCheck.StatusCode) -ForegroundColor Green
} catch {
    Write-Host "Canh bao: stream.html chua san sang (co the DNS/tunnel chua cap nhat)." -ForegroundColor Yellow
}

Write-Host ""
Write-Host ("Hoan tat. Domain: https://" + $publicHost) -ForegroundColor Green
Write-Host "Neu moi cap DNS/tunnel, cho 1-2 phut roi mo lai stream." -ForegroundColor Yellow
