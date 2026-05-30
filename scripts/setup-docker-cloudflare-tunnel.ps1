param(
    [string]$EnvFile = ".env",
    [string]$TunnelName = "cam-tunnel",
    [ValidateSet("prompt", "manual", "auto")]
    [string]$Mode = "prompt"
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$Text) { Write-Host ""; Write-Host "==> $Text" -ForegroundColor Cyan }
function Write-Ok([string]$Text) { Write-Host "[OK] $Text" -ForegroundColor Green }
function Write-Warn([string]$Text) { Write-Host "[WARN] $Text" -ForegroundColor Yellow }
function Write-Err([string]$Text) { Write-Host "[ERR] $Text" -ForegroundColor Red }

function Ensure-EnvFile([string]$Path) {
    if (Test-Path $Path) { return }
    $source = if (Test-Path ".env.example") { ".env.example" } elseif (Test-Path ".env.docker.example") { ".env.docker.example" } else { $null }
    if (-not $source) { throw "Khong tim thay .env.example hoac .env.docker.example de tao $Path" }
    Copy-Item $source $Path
}

function Set-Or-AddEnv([string]$Path, [string]$Key, [string]$Value) {
    $lines = @()
    if (Test-Path $Path) { $lines = Get-Content $Path }
    $updated = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "^\s*${Key}\s*=") {
            $lines[$i] = "${Key}=${Value}"
            $updated = $true
        }
    }
    if (-not $updated) { $lines += "${Key}=${Value}" }
    Set-Content -Path $Path -Value $lines -Encoding UTF8
}

function Normalize-Host([string]$HostValue) {
    $v = ""
    if ($null -ne $HostValue) { $v = [string]$HostValue }
    $v = $v.Trim()
    $v = $v -replace "^https?://", ""
    $v = $v -replace "^//", ""
    $v = $v.TrimEnd("/")
    return $v
}

function Assert-PortBusyOrFree([int]$Port, [string]$Name) {
    try {
        $inUse = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue | Where-Object { $_.LocalPort -eq $Port }
        if ($inUse) { Write-Warn "Port $Port dang duoc su dung ($Name). Neu do stack hien tai thi co the bo qua." }
    } catch { }
}

function Is-HeadlessHost() {
    if ($IsWindows) { return $false }
    return [string]::IsNullOrWhiteSpace($env:DISPLAY) -and [string]::IsNullOrWhiteSpace($env:WAYLAND_DISPLAY)
}

function Ensure-CloudflaredExists() {
    $cmd = Get-Command cloudflared -ErrorAction SilentlyContinue
    if (-not $cmd) { throw "Khong tim thay cloudflared trong PATH." }
}

function Ensure-TunnelExists([string]$Name) {
    $json = cloudflared tunnel list --output json
    if ($LASTEXITCODE -ne 0) { throw "Khong the doc danh sach tunnel." }
    $items = $json | ConvertFrom-Json
    $existing = $items | Where-Object { $_.name -eq $Name } | Select-Object -First 1
    if ($existing) {
        Write-Ok "Tunnel da ton tai: $Name"
        return
    }
    cloudflared tunnel create $Name | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Tao tunnel that bai: $Name" }
    Write-Ok "Da tao tunnel: $Name"
}

function Ensure-DnsRoute([string]$Name, [string]$HostName) {
    $tmp = Join-Path $env:TEMP ("vshield_dns_" + [guid]::NewGuid().ToString("N") + ".log")
    try {
        cloudflared tunnel route dns $Name $HostName *> $tmp
        if ($LASTEXITCODE -eq 0) {
            Write-Ok "DNS route san sang: $HostName"
            return
        }
        $outText = if (Test-Path $tmp) { Get-Content $tmp -Raw } else { "" }
        if ($outText -match "already exists") {
            Write-Warn "DNS route da ton tai: $HostName"
            return
        }
        throw "Khong the tao DNS route. Chi tiet: $outText"
    } finally {
        if (Test-Path $tmp) { Remove-Item $tmp -Force -ErrorAction SilentlyContinue }
    }
}

function Get-TunnelToken([string]$Name) {
    $token = (cloudflared tunnel token $Name | Out-String).Trim()
    if ([string]::IsNullOrWhiteSpace($token)) { throw "Khong lay duoc tunnel token cho $Name" }
    return $token
}

function Wait-ApiHealthy([int]$TimeoutSeconds = 60) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $resp = Invoke-RestMethod -Uri "http://localhost:5107/health" -TimeoutSec 3
            if ($resp.status -eq "ok") { return $true }
        } catch { }
        Start-Sleep -Seconds 2
    }
    return $false
}

function Test-CloudflaredStable() {
    try {
        $state = docker inspect vshield-cloudflared --format "{{.State.Status}}|{{.RestartCount}}"
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($state)) { return @{ ok = $false; reason = "Khong inspect duoc container cloudflared." } }
        $parts = $state.Trim().Split("|")
        $status = $parts[0]
        $restartCount = 0
        if ($parts.Count -gt 1) { [int]::TryParse($parts[1], [ref]$restartCount) | Out-Null }
        if ($status -ne "running") { return @{ ok = $false; reason = "cloudflared status=$status" } }
        if ($restartCount -gt 0) { return @{ ok = $false; reason = "cloudflared dang restart (RestartCount=$restartCount)" } }
    } catch {
        return @{ ok = $false; reason = $_.Exception.Message }
    }

    $logs = ""
    try {
        if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -ErrorAction SilentlyContinue) {
            $prev = $PSNativeCommandUseErrorActionPreference
            $PSNativeCommandUseErrorActionPreference = $false
            try {
                $logs = (docker logs --tail 120 vshield-cloudflared 2>&1 | Out-String)
            } finally {
                $PSNativeCommandUseErrorActionPreference = $prev
            }
        } else {
            $logs = (cmd /c "docker logs --tail 120 vshield-cloudflared 2>&1" | Out-String)
        }
    } catch {
        $logs = ""
    }
    if ($logs -match "flag needs an argument: -token") { return @{ ok = $false; reason = "Token cloudflared dang rong." } }
    if ($logs -match "(authentication failed|invalid token|token is invalid)") { return @{ ok = $false; reason = "Token cloudflared khong hop le." } }
    return @{ ok = $true; reason = "OK" }
}

function Select-Mode([string]$InputMode) {
    if ($InputMode -ne "prompt") { return $InputMode }
    Write-Host ""
    Write-Host "Chon che do:"
    Write-Host "  1) Manual token (paste token)"
    Write-Host "  2) Auto tunnel (login/create/route/token)"
    $choice = Read-Host "Nhap 1 hoac 2"
    if ($choice -eq "2") { return "auto" }
    return "manual"
}

$resolvedMode = Select-Mode $Mode

Write-Step "Buoc 0: Chuan bi env + input"
Ensure-EnvFile -Path $EnvFile

$publicHost = Normalize-Host (Read-Host "Nhap domain public (vi du cam.example.com)")
if ([string]::IsNullOrWhiteSpace($publicHost)) { throw "Domain public khong duoc de trong." }

$token = ""
if ($resolvedMode -eq "auto") {
    if (Is-HeadlessHost) {
        Write-Warn "Moi truong headless khong mo duoc browser. Tu dong chuyen sang manual token."
        $resolvedMode = "manual"
    }
}

if ($resolvedMode -eq "auto") {
    Write-Step "Buoc 1: Auto tunnel flow"
    Ensure-CloudflaredExists
    cloudflared tunnel login
    if ($LASTEXITCODE -ne 0) {
        Write-Warn "cloudflared login tra ve non-zero (co the do cert.pem da ton tai). Van tiep tuc."
    }
    Ensure-TunnelExists -Name $TunnelName
    Ensure-DnsRoute -Name $TunnelName -HostName $publicHost
    $token = Get-TunnelToken -Name $TunnelName
    Write-Ok "Lay token tunnel thanh cong."
} else {
    Write-Step "Buoc 1: Manual token flow"
    $token = (Read-Host "Nhap CLOUDFLARED_TUNNEL_TOKEN (dang eyJh...)").Trim()
    if ([string]::IsNullOrWhiteSpace($token)) { throw "Token khong duoc de trong." }
}

Write-Step "Buoc 2: Preflight"
Assert-PortBusyOrFree -Port 1433 -Name "db"
Assert-PortBusyOrFree -Port 5107 -Name "api"
Assert-PortBusyOrFree -Port 5173 -Name "frontend"

Write-Step "Buoc 3: Ghi cau hinh vao .env"
Set-Or-AddEnv -Path $EnvFile -Key "CLOUDFLARED_TUNNEL_TOKEN" -Value $token
Set-Or-AddEnv -Path $EnvFile -Key "APP_FRONTEND_URL" -Value ("https://" + $publicHost)
Set-Or-AddEnv -Path $EnvFile -Key "APP_GO2RTC_PUBLIC_BASE_URL" -Value ("https://" + $publicHost)
Set-Or-AddEnv -Path $EnvFile -Key "APP_ALLOW_CROSS_ORIGIN_GO2RTC_FRAME" -Value "false"
Set-Or-AddEnv -Path $EnvFile -Key "GO2RTC_WEBRTC_CANDIDATES" -Value ""
Set-Or-AddEnv -Path $EnvFile -Key "GO2RTC_WEBRTC_PORT" -Value "8555"
Set-Or-AddEnv -Path $EnvFile -Key "GO2RTC_STREAM_MODE" -Value "webrtc"
Write-Ok "Da cap nhat .env"

Write-Step "Buoc 4: Patch appsettings.json"
$root = Split-Path -Parent $PSScriptRoot
$appsettingsPath = Join-Path $root "API\\API\\API\\appsettings.json"
$updateScript = Join-Path $root "scripts\\update-public-domain-appsettings.ps1"
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $updateScript `
    -AppsettingsPath $appsettingsPath `
    -TunnelName $TunnelName `
    -PublicHostname $publicHost `
    -TargetService "http://localhost:5173"
if ($LASTEXITCODE -ne 0) { throw "Patch appsettings that bai." }
Write-Ok "Patch appsettings thanh cong."

Write-Step "Buoc 5: Khoi dong core stack"
& docker compose up -d --build db go2rtc api frontend
if ($LASTEXITCODE -ne 0) { throw "Khoi dong core stack that bai." }

Write-Step "Buoc 6: Cho API healthy"
if (-not (Wait-ApiHealthy -TimeoutSeconds 90)) { throw "API khong healthy sau khi khoi dong." }
Write-Ok "API healthy."

Write-Step "Buoc 7: Bat cloudflared profile"
& docker compose --profile tunnel up -d --force-recreate cloudflared
if ($LASTEXITCODE -ne 0) { throw "Khoi dong cloudflared that bai." }
Start-Sleep -Seconds 4
$cf = Test-CloudflaredStable
if (-not $cf.ok) { throw "Cloudflared chua on dinh: $($cf.reason)" }
Write-Ok "Cloudflared running on dinh."

Write-Step "Buoc 8: Reload go2rtc"
Invoke-RestMethod -Method Post -Uri "http://localhost:5107/api/camera-runtime/reload-go2rtc" -TimeoutSec 20 | Out-Null
Write-Ok "reload-go2rtc done."

Write-Step "Buoc 9: Verify nhanh stream endpoint"
try {
    $streamCheck = Invoke-WebRequest -Uri ("https://" + $publicHost + "/stream.html?src=cam1&mode=webrtc") -TimeoutSec 15
    Write-Ok ("stream.html status: " + $streamCheck.StatusCode)
} catch {
    Write-Warn "stream.html chua san sang (co the DNS/tunnel can propagation them)."
}

Write-Host ""
Write-Host "================ SUMMARY ================" -ForegroundColor Cyan
Write-Host ("Mode           : " + $resolvedMode)
Write-Host ("Public domain  : https://" + $publicHost)
Write-Host ("App URL        : https://" + $publicHost)
Write-Host ("Stream sample  : https://" + $publicHost + "/stream.html?src=cam1&mode=webrtc")
Write-Host ""
Write-Host "Lan sau (run nhanh): docker compose up -d" -ForegroundColor Yellow
