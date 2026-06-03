param(
    [ValidateSet("prompt", "manual", "auto")]
    [string]$Mode = "prompt",
    [string]$PublicHostname = "",
    [string]$TunnelName = "cam-tunnel",
    [string]$TargetService = "http://localhost:1984",
    [string]$Token = "",
    [switch]$SaveToken,
    [switch]$SkipStart,
    [switch]$SkipReload,
    [switch]$SkipVerify,
    [switch]$NonInteractive
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$RuntimeDir = Join-Path $Root ".runtime\public-domain-setup"
$LogPath = Join-Path $RuntimeDir ("setup-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log")
$PidPath = Join-Path $RuntimeDir "cloudflared.pid"
$EnvFile = Join-Path $Root "customer.env"
$AppsettingsPath = Join-Path $Root "API\API\API\appsettings.json"
$UpdateScript = Join-Path $Root "scripts\update-public-domain-appsettings.ps1"
$CloudflaredDir = Join-Path $env:USERPROFILE ".cloudflared"
$CloudflaredConfig = Join-Path $CloudflaredDir "config.yml"

$script:Failures = 0
$script:Warnings = 0
$script:CloudflaredExe = $null
$script:ResolvedMode = $Mode

New-Item -ItemType Directory -Path $RuntimeDir -Force | Out-Null
New-Item -ItemType Directory -Path $CloudflaredDir -Force | Out-Null

function Write-LogLine([string]$Text) {
    $line = ("[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Text)
    Add-Content -LiteralPath $LogPath -Value $line -Encoding UTF8
}

function Write-Step([string]$Text) {
    Write-Host ""
    Write-Host "---------- $Text ----------" -ForegroundColor Cyan
    Write-LogLine "STEP $Text"
}

function Write-Ok([string]$Text) {
    Write-Host "[ OK ] $Text" -ForegroundColor Green
    Write-LogLine "OK $Text"
}

function Write-Warn([string]$Text) {
    $script:Warnings++
    Write-Host "[WARN] $Text" -ForegroundColor Yellow
    Write-LogLine "WARN $Text"
}

function Write-Fail([string]$Text) {
    $script:Failures++
    Write-Host "[FAIL] $Text" -ForegroundColor Red
    Write-LogLine "FAIL $Text"
}

function Normalize-Host([string]$Value) {
    $v = ""
    if ($null -ne $Value) { $v = [string]$Value }
    $v = $v.Trim()
    $v = $v -replace "^https?://", ""
    $v = $v -replace "^//", ""
    $v = $v.TrimEnd("/")
    return $v
}

function Read-EnvFile([string]$Path) {
    $result = @{}
    if (-not (Test-Path -LiteralPath $Path)) { return $result }
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line.TrimStart().StartsWith("#")) { continue }
        $idx = $line.IndexOf("=")
        if ($idx -le 0) { continue }
        $key = $line.Substring(0, $idx).Trim()
        $value = $line.Substring($idx + 1).Trim()
        $result[$key] = $value
    }
    return $result
}

function Set-EnvValues([string]$Path, [hashtable]$Values) {
    $lines = @()
    if (Test-Path -LiteralPath $Path) { $lines = @(Get-Content -LiteralPath $Path) }
    foreach ($key in $Values.Keys) {
        $updated = $false
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match "^\s*$([regex]::Escape($key))\s*=") {
                $lines[$i] = "$key=$($Values[$key])"
                $updated = $true
            }
        }
        if (-not $updated) { $lines += "$key=$($Values[$key])" }
    }
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllLines($Path, $lines, $utf8NoBom)
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory=$true)][string]$FilePath,
        [Parameter(Mandatory=$true)][string[]]$Arguments,
        [int]$TimeoutSeconds = 120,
        [switch]$IgnoreExitCode
    )
    $stdout = Join-Path $RuntimeDir ("cmd-out-" + [guid]::NewGuid().ToString("N") + ".log")
    $stderr = Join-Path $RuntimeDir ("cmd-err-" + [guid]::NewGuid().ToString("N") + ".log")
    Write-LogLine ("RUN {0} {1}" -f $FilePath, ($Arguments -join " "))
    $process = Start-Process -FilePath $FilePath -ArgumentList $Arguments -NoNewWindow -Wait -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $outText = if (Test-Path $stdout) { Get-Content -LiteralPath $stdout -Raw } else { "" }
    $errText = if (Test-Path $stderr) { Get-Content -LiteralPath $stderr -Raw } else { "" }
    Remove-Item -LiteralPath $stdout, $stderr -Force -ErrorAction SilentlyContinue
    Write-LogLine ("EXIT {0}" -f $process.ExitCode)
    if (-not [string]::IsNullOrWhiteSpace($outText)) { Write-LogLine ("STDOUT " + $outText.Trim()) }
    if (-not [string]::IsNullOrWhiteSpace($errText)) { Write-LogLine ("STDERR " + $errText.Trim()) }
    if ($process.ExitCode -ne 0 -and -not $IgnoreExitCode) {
        throw ("Command failed ({0}): {1}" -f $process.ExitCode, ($errText + $outText).Trim())
    }
    return [pscustomobject]@{
        ExitCode = $process.ExitCode
        StdOut = $outText
        StdErr = $errText
    }
}

function Find-Cloudflared {
    $candidates = @()
    $cmd = Get-Command cloudflared -ErrorAction SilentlyContinue
    if ($cmd) { $candidates += $cmd.Source }
    $candidates += @(
        (Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages\Cloudflare.cloudflared_Microsoft.Winget.Source_8wekyb3d8bbwe\cloudflared.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\cloudflared\cloudflared.exe"),
        (Join-Path $env:ProgramFiles "cloudflared\cloudflared.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "cloudflared\cloudflared.exe")
    )
    foreach ($candidate in $candidates | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique) {
        if (Test-Path -LiteralPath $candidate) { return (Resolve-Path -LiteralPath $candidate).Path }
    }
    return $null
}

function Ensure-Cloudflared {
    $script:CloudflaredExe = Find-Cloudflared
    if ($script:CloudflaredExe) {
        $version = Invoke-NativeCommand -FilePath $script:CloudflaredExe -Arguments @("--version") -TimeoutSeconds 30 -IgnoreExitCode
        Write-Ok ("cloudflared found: " + $script:CloudflaredExe)
        if (-not [string]::IsNullOrWhiteSpace($version.StdOut)) { Write-LogLine ("cloudflared version: " + $version.StdOut.Trim()) }
        return
    }

    if ($NonInteractive) {
        throw "cloudflared not found and NonInteractive mode cannot install it."
    }

    Write-Warn "cloudflared not found."
    $answer = Read-Host "Install cloudflared now via winget? [Y/N]"
    if ($answer -notmatch "^(y|yes)$") { throw "cloudflared is required." }

    $winget = Get-Command winget -ErrorAction SilentlyContinue
    if (-not $winget) { throw "winget not found. Install cloudflared manually." }

    Invoke-NativeCommand -FilePath $winget.Source -Arguments @("install", "--id", "Cloudflare.cloudflared", "--exact", "--accept-package-agreements", "--accept-source-agreements", "--silent") -TimeoutSeconds 600 -IgnoreExitCode | Out-Null
    $script:CloudflaredExe = Find-Cloudflared
    if (-not $script:CloudflaredExe) { throw "cloudflared install finished but executable was not found." }
    Write-Ok ("cloudflared installed: " + $script:CloudflaredExe)
}

function Select-SetupMode([string]$Value) {
    if ($Value -ne "prompt") { return $Value }
    if ($NonInteractive) { return "manual" }
    Write-Host ""
    Write-Host "Select setup mode:"
    Write-Host "  1) Manual token (paste token, no browser login)"
    Write-Host "  2) Auto tunnel (login/create/route via browser)"
    $choice = Read-Host "Enter 1 or 2 [1]"
    if ($choice -eq "2") { return "auto" }
    return "manual"
}

function Ensure-TunnelExists([string]$Name) {
    $result = Invoke-NativeCommand -FilePath $script:CloudflaredExe -Arguments @("tunnel", "list", "--output", "json") -TimeoutSeconds 120 -IgnoreExitCode
    if ($result.ExitCode -ne 0) { throw "Cannot list Cloudflare tunnels. Login may be missing." }
    $items = @()
    if (-not [string]::IsNullOrWhiteSpace($result.StdOut)) { $items = @($result.StdOut | ConvertFrom-Json) }
    $existing = $items | Where-Object { $_.name -eq $Name } | Select-Object -First 1
    if ($existing) {
        Write-Ok "Tunnel already exists: $Name"
        return
    }
    Invoke-NativeCommand -FilePath $script:CloudflaredExe -Arguments @("tunnel", "create", $Name) -TimeoutSeconds 120 | Out-Null
    Write-Ok "Tunnel created: $Name"
}

function Ensure-DnsRoute([string]$Name, [string]$HostName) {
    $result = Invoke-NativeCommand -FilePath $script:CloudflaredExe -Arguments @("tunnel", "route", "dns", $Name, $HostName) -TimeoutSeconds 120 -IgnoreExitCode
    $text = ($result.StdOut + "`n" + $result.StdErr)
    if ($result.ExitCode -eq 0) {
        Write-Ok "DNS route ready: $HostName"
        return
    }
    if ($text -match "already exists") {
        Write-Warn "DNS route already exists: $HostName"
        return
    }
    throw ("DNS route failed: " + $text.Trim())
}

function Get-TunnelToken([string]$Name) {
    $result = Invoke-NativeCommand -FilePath $script:CloudflaredExe -Arguments @("tunnel", "token", $Name) -TimeoutSeconds 120
    $value = ($result.StdOut + "`n" + $result.StdErr).Trim()
    $line = ($value -split "`r?`n" | Where-Object { $_ -match "^[A-Za-z0-9_\-=]+\.[A-Za-z0-9_\-=]+|^eyJ" } | Select-Object -First 1)
    if ([string]::IsNullOrWhiteSpace($line)) { $line = $value }
    if ([string]::IsNullOrWhiteSpace($line)) { throw "Cannot read Cloudflare tunnel token." }
    return $line.Trim()
}

function Write-CloudflaredConfig([string]$Name, [string]$HostName, [string]$Service) {
    $jsonFile = Get-ChildItem -LiteralPath $CloudflaredDir -Filter "*.json" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $jsonFile) {
        Write-Warn "No credentials json found. Config.yml is skipped; token mode can still start cloudflared now."
        return
    }
    $content = @"
tunnel: $Name
credentials-file: $($jsonFile.FullName.Replace("\", "/"))

ingress:
  - hostname: $HostName
    service: $Service
  - service: http_status:404
"@
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($CloudflaredConfig, $content, $utf8NoBom)
    Write-Ok "cloudflared config.yml ready."
}

function Stop-ManagedCloudflared {
    if (-not (Test-Path -LiteralPath $PidPath)) { return }
    $pidText = (Get-Content -LiteralPath $PidPath -Raw).Trim()
    $pidValue = 0
    if (-not [int]::TryParse($pidText, [ref]$pidValue)) {
        Remove-Item -LiteralPath $PidPath -Force -ErrorAction SilentlyContinue
        return
    }
    $proc = Get-Process -Id $pidValue -ErrorAction SilentlyContinue
    if (-not $proc) { Remove-Item -LiteralPath $PidPath -Force -ErrorAction SilentlyContinue; return }
    if ($proc.ProcessName -notlike "cloudflared*") {
        Write-Warn "PID file points to a non-cloudflared process. Keeping it untouched."
        return
    }
    try {
        Stop-Process -Id $pidValue -Force -ErrorAction Stop
        Remove-Item -LiteralPath $PidPath -Force -ErrorAction SilentlyContinue
        Write-Ok "Stopped previous V-Shield cloudflared process."
    } catch {
        Write-Warn ("Cannot stop previous cloudflared process: " + $_.Exception.Message)
    }
}

function Start-Cloudflared([string]$SetupMode, [string]$TunnelToken) {
    if ($SkipStart) {
        Write-Warn "SkipStart enabled; cloudflared process was not started."
        return
    }
    Stop-ManagedCloudflared
    $outLog = Join-Path $RuntimeDir "cloudflared.out.log"
    $errLog = Join-Path $RuntimeDir "cloudflared.err.log"
    $args = if ($SetupMode -eq "manual") {
        @("tunnel", "--no-autoupdate", "run", "--token", $TunnelToken)
    } else {
        @("tunnel", "--config", $CloudflaredConfig, "run")
    }
    $process = Start-Process -FilePath $script:CloudflaredExe -ArgumentList $args -WindowStyle Hidden -PassThru -RedirectStandardOutput $outLog -RedirectStandardError $errLog
    Set-Content -LiteralPath $PidPath -Value $process.Id -Encoding ASCII
    Start-Sleep -Seconds 5
    $stillRunning = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
    $logs = ""
    if (Test-Path $outLog) { $logs += Get-Content -LiteralPath $outLog -Raw -ErrorAction SilentlyContinue }
    if (Test-Path $errLog) { $logs += "`n" + (Get-Content -LiteralPath $errLog -Raw -ErrorAction SilentlyContinue) }
    Write-LogLine ("cloudflared logs: " + $logs.Trim())
    if ($logs -match "flag needs an argument: -token") { throw "Cloudflare token is empty." }
    if ($logs -match "(authentication failed|invalid token|token is invalid|Tunnel token is not valid|Cannot determine default origin certificate)") { throw "Cloudflare authentication failed. Check token/login." }
    if (-not $stillRunning) { throw "cloudflared stopped immediately. Check log: $errLog" }
    Write-Ok ("cloudflared running. PID=" + $process.Id)
}

function Start-Go2Rtc {
    if ($SkipStart) {
        Write-Warn "SkipStart enabled; go2rtc process was not started."
        return
    }
    $go2rtcPath = Join-Path $Root "AI_Runtime\cam\go2rtc_win64\go2rtc.exe"
    if (-not (Test-Path -LiteralPath $go2rtcPath)) {
        Write-Warn "go2rtc.exe not found. Skip go2rtc start."
        return
    }
    $existing = Get-Process -Name "go2rtc" -ErrorAction SilentlyContinue
    foreach ($proc in $existing) {
        try { Stop-Process -Id $proc.Id -Force -ErrorAction Stop } catch { Write-Warn ("Cannot stop go2rtc: " + $_.Exception.Message) }
    }
    Start-Process -FilePath $go2rtcPath -WorkingDirectory (Split-Path -Parent $go2rtcPath) -WindowStyle Hidden | Out-Null
    Start-Sleep -Seconds 2
    if (Get-Process -Name "go2rtc" -ErrorAction SilentlyContinue) { Write-Ok "go2rtc running." } else { Write-Warn "go2rtc did not stay running." }
}

function Patch-Appsettings([string]$HostName, [string]$Service) {
    if (-not (Test-Path -LiteralPath $AppsettingsPath)) { throw "Missing appsettings.json: $AppsettingsPath" }
    if (-not (Test-Path -LiteralPath $UpdateScript)) { throw "Missing helper script: $UpdateScript" }
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $UpdateScript -AppsettingsPath $AppsettingsPath -TunnelName $TunnelName -PublicHostname $HostName -TargetService $Service
    if ($LASTEXITCODE -ne 0) { throw "Auto update appsettings failed." }
    Write-Ok "appsettings.json updated."
}

function Reload-Go2Rtc {
    if ($SkipReload) {
        Write-Warn "SkipReload enabled; go2rtc reload was not called."
        return
    }
    try {
        Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:5107/api/camera-runtime/reload-go2rtc" -TimeoutSec 20 | Out-Null
        Write-Ok "reload-go2rtc called successfully."
    } catch {
        Write-Warn ("Could not call reload-go2rtc. API may be down. " + $_.Exception.Message)
    }
}

function Test-PublicEndpoint([string]$HostName) {
    if ($SkipVerify) {
        Write-Warn "SkipVerify enabled; public endpoint was not checked."
        return
    }

    $url = "https://$HostName/stream.html?src=cam1&mode=webrtc"
    try {
        $response = Invoke-WebRequest -Uri $url -TimeoutSec 20 -UseBasicParsing
        Write-Ok ("Public stream endpoint responded: HTTP " + [int]$response.StatusCode)
    } catch {
        Write-Warn ("Public stream endpoint is not ready yet: $url. " + $_.Exception.Message)
    }
}

function Validate-SetupInputs {
    if ([string]::IsNullOrWhiteSpace($PublicHostname)) {
        if ($NonInteractive) { throw "PublicHostname is required in NonInteractive mode." }
        $PublicHostname = Read-Host "Enter PUBLIC_HOSTNAME, example cam.customer.com"
    }
    $script:PublicHostname = Normalize-Host $PublicHostname
    if ([string]::IsNullOrWhiteSpace($script:PublicHostname)) { throw "PUBLIC_HOSTNAME is empty after normalization." }

    if (-not $NonInteractive) {
        $tmp = Read-Host "Enter TUNNEL_NAME [$TunnelName]"
        if (-not [string]::IsNullOrWhiteSpace($tmp)) { $script:TunnelName = $tmp.Trim() }
        $tmp = Read-Host "Enter TARGET_SERVICE [$TargetService]"
        if (-not [string]::IsNullOrWhiteSpace($tmp)) { $script:TargetService = $tmp.Trim() }
    }
}

try {
    Write-Host "==========================================================="
    Write-Host "             V-SHIELD PUBLIC DOMAIN SETUP"
    Write-Host "==========================================================="
    Write-Host ""
    Write-LogLine "Setup started. Root=$Root"

    $envValues = Read-EnvFile -Path $EnvFile
    if ($envValues.ContainsKey("PUBLIC_HOSTNAME") -and [string]::IsNullOrWhiteSpace($PublicHostname)) { $PublicHostname = $envValues["PUBLIC_HOSTNAME"] }
    if ($envValues.ContainsKey("TUNNEL_NAME") -and $TunnelName -eq "cam-tunnel") { $TunnelName = $envValues["TUNNEL_NAME"] }
    if ($envValues.ContainsKey("TARGET_SERVICE") -and $TargetService -eq "http://localhost:1984") { $TargetService = $envValues["TARGET_SERVICE"] }
    if ($envValues.ContainsKey("SETUP_MODE") -and $Mode -eq "prompt") {
        $savedMode = $envValues["SETUP_MODE"].Trim().ToLowerInvariant()
        if ($savedMode -eq "manual_token") { $Mode = "manual" }
        elseif ($savedMode -eq "auto") { $Mode = "auto" }
    }
    if ($envValues.ContainsKey("CLOUDFLARED_TUNNEL_TOKEN") -and [string]::IsNullOrWhiteSpace($Token)) { $Token = $envValues["CLOUDFLARED_TUNNEL_TOKEN"] }

    Validate-SetupInputs
    $resolvedMode = Select-SetupMode -Value $Mode
    $script:ResolvedMode = $resolvedMode

    Write-Step "1) Validate cloudflared"
    Ensure-Cloudflared

    $tunnelToken = $Token.Trim()
    if ($resolvedMode -eq "auto") {
        Write-Step "2) Auto tunnel"
        $login = Invoke-NativeCommand -FilePath $script:CloudflaredExe -Arguments @("tunnel", "login") -TimeoutSeconds 600 -IgnoreExitCode
        if ($login.ExitCode -ne 0) { Write-Warn "cloudflared login returned non-zero; continuing because an existing cert may already be valid." }
        Ensure-TunnelExists -Name $TunnelName
        Ensure-DnsRoute -Name $TunnelName -HostName $script:PublicHostname
        $tunnelToken = Get-TunnelToken -Name $TunnelName
        Write-Ok "Tunnel token acquired."
    } else {
        Write-Step "2) Manual token"
        if ([string]::IsNullOrWhiteSpace($tunnelToken)) {
            if ($NonInteractive) { throw "Token is required in manual NonInteractive mode." }
            $tunnelToken = (Read-Host "Enter CLOUDFLARED_TUNNEL_TOKEN").Trim()
        }
        if ([string]::IsNullOrWhiteSpace($tunnelToken)) { throw "CLOUDFLARED_TUNNEL_TOKEN is empty." }
        Write-Ok "Token accepted for startup."
    }

    Write-Step "3) Save setup values"
    $values = @{
        PUBLIC_HOSTNAME = $script:PublicHostname
        TUNNEL_NAME = $TunnelName
        TARGET_SERVICE = $TargetService
        SETUP_MODE = if ($resolvedMode -eq "manual") { "MANUAL_TOKEN" } else { "AUTO" }
    }
    if ($SaveToken) {
        $values["CLOUDFLARED_TUNNEL_TOKEN"] = $tunnelToken
    } elseif ($resolvedMode -eq "manual" -and -not $NonInteractive) {
        $saveAnswer = Read-Host "Save token to customer.env for rerun/runtime? [Y/N]"
        if ($saveAnswer -match "^(y|yes)$") {
            $values["CLOUDFLARED_TUNNEL_TOKEN"] = $tunnelToken
        }
    }
    Set-EnvValues -Path $EnvFile -Values $values
    Write-Ok "customer.env updated."

    Write-Step "4) Write config and patch appsettings"
    if ($resolvedMode -eq "auto") {
        Write-CloudflaredConfig -Name $TunnelName -HostName $script:PublicHostname -Service $TargetService
    } else {
        Write-Warn "Manual token mode does not require config.yml for current startup."
    }
    Patch-Appsettings -HostName $script:PublicHostname -Service $TargetService

    Write-Step "5) Start runtimes"
    Start-Cloudflared -SetupMode $resolvedMode -TunnelToken $tunnelToken
    Start-Go2Rtc

    Write-Step "6) Reload camera URLs"
    Reload-Go2Rtc

    Write-Step "7) Verify public endpoint"
    Test-PublicEndpoint -HostName $script:PublicHostname
}
catch {
    Write-Fail $_.Exception.Message
}
finally {
    Write-Host ""
    Write-Host "==========================================================="
    Write-Host "                         SUMMARY"
    Write-Host "==========================================================="
    Write-Host ("  Hostname : " + (Normalize-Host $script:PublicHostname))
    Write-Host ("  Tunnel   : " + $TunnelName)
    Write-Host ("  Mode     : " + $script:ResolvedMode)
    Write-Host ("  Failures : " + $script:Failures)
    Write-Host ("  Warnings : " + $script:Warnings)
    Write-Host ("  Log      : " + $LogPath)
    Write-Host ""
    if ($script:Failures -gt 0) {
        Write-Host "Setup incomplete." -ForegroundColor Red
        exit 1
    }
    Write-Host "Setup completed." -ForegroundColor Green
    exit 0
}
