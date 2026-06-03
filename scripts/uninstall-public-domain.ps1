param(
    [string]$PublicHostname = "",
    [string]$TunnelName = "cam-tunnel",
    [switch]$DeleteTunnel,
    [switch]$DeleteCredentials,
    [switch]$DeleteCloudflaredFolder,
    [switch]$UninstallCloudflared,
    [switch]$SkipReset,
    [switch]$SkipDbCleanup,
    [switch]$SkipStop,
    [switch]$SkipLocalCleanup,
    [switch]$DryRun,
    [switch]$NonInteractive
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$RuntimeDir = Join-Path $Root ".runtime\public-domain-setup"
$LogPath = Join-Path $RuntimeDir ("uninstall-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log")
$PidPath = Join-Path $RuntimeDir "cloudflared.pid"
$EnvFile = Join-Path $Root "customer.env"
$AppsettingsPath = Join-Path $Root "API\API\API\appsettings.json"
$ReadScript = Join-Path $Root "scripts\read-public-domain-appsettings.ps1"
$ResetScript = Join-Path $Root "scripts\reset-public-domain-appsettings.ps1"
$ListIdsScript = Join-Path $Root "scripts\list-tunnel-ids-by-name.ps1"
$ClearUrlViewScript = Join-Path $Root "scripts\clear-camera-urlview.ps1"
$CloudflaredDir = Join-Path $env:USERPROFILE ".cloudflared"
$CloudflaredConfig = Join-Path $CloudflaredDir "config.yml"

$script:Failures = 0
$script:Warnings = 0

New-Item -ItemType Directory -Path $RuntimeDir -Force | Out-Null

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

function Confirm-Action([string]$Question, [bool]$Default = $false) {
    if ($NonInteractive) { return $Default }
    $suffix = if ($Default) { "[Y/n]" } else { "[y/N]" }
    $answer = Read-Host "$Question $suffix"
    if ([string]::IsNullOrWhiteSpace($answer)) { return $Default }
    return $answer -match "^(y|yes)$"
}

function Read-EnvFile([string]$Path) {
    $result = @{}
    if (-not (Test-Path -LiteralPath $Path)) { return $result }
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line.TrimStart().StartsWith("#")) { continue }
        $idx = $line.IndexOf("=")
        if ($idx -le 0) { continue }
        $result[$line.Substring(0, $idx).Trim()] = $line.Substring($idx + 1).Trim()
    }
    return $result
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

function Stop-ManagedCloudflared {
    if ($SkipStop -or $DryRun) {
        Write-Warn "SkipStop/DryRun enabled; managed cloudflared was not stopped."
        return
    }
    if (-not (Test-Path -LiteralPath $PidPath)) {
        Write-Warn "No managed cloudflared PID file found. Other cloudflared processes are kept."
        return
    }
    $pidText = (Get-Content -LiteralPath $PidPath -Raw).Trim()
    $pidValue = 0
    if (-not [int]::TryParse($pidText, [ref]$pidValue)) {
        Remove-Item -LiteralPath $PidPath -Force -ErrorAction SilentlyContinue
        Write-Warn "Invalid PID file removed."
        return
    }
    $proc = Get-Process -Id $pidValue -ErrorAction SilentlyContinue
    if (-not $proc) {
        Remove-Item -LiteralPath $PidPath -Force -ErrorAction SilentlyContinue
        Write-Warn "Managed cloudflared process is not running."
        return
    }
    if ($proc.ProcessName -notlike "cloudflared*") {
        Write-Warn "PID file points to a non-cloudflared process. Nothing stopped."
        return
    }
    Stop-Process -Id $pidValue -Force
    Remove-Item -LiteralPath $PidPath -Force -ErrorAction SilentlyContinue
    Write-Ok "Stopped managed cloudflared process."
}

function Stop-Go2Rtc {
    if ($SkipStop -or $DryRun) {
        Write-Warn "SkipStop/DryRun enabled; go2rtc was not stopped."
        return
    }
    $procs = @(Get-Process -Name "go2rtc" -ErrorAction SilentlyContinue)
    if ($procs.Count -eq 0) {
        Write-Warn "go2rtc is not running."
        return
    }
    foreach ($proc in $procs) {
        try { Stop-Process -Id $proc.Id -Force -ErrorAction Stop } catch { Write-Warn ("Cannot stop go2rtc: " + $_.Exception.Message) }
    }
    Write-Ok "Stopped go2rtc process(es)."
}

function Resolve-Values {
    if ((Test-Path -LiteralPath $AppsettingsPath) -and (Test-Path -LiteralPath $ReadScript)) {
        try {
            $lines = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ReadScript -AppsettingsPath $AppsettingsPath
            foreach ($line in $lines) {
                $lineText = [string]$line
                $idx = $lineText.IndexOf("=")
                if ($idx -le 0) { continue }
                $key = $lineText.Substring(0, $idx)
                $value = $lineText.Substring($idx + 1)
                if ($key -eq "PUBLIC_HOSTNAME" -and [string]::IsNullOrWhiteSpace($PublicHostname)) { $script:PublicHostname = $value }
                if ($key -eq "TUNNEL_NAME" -and $TunnelName -eq "cam-tunnel") { $script:TunnelName = $value }
            }
        } catch {
            Write-Warn ("Could not read appsettings values: " + $_.Exception.Message)
        }
    }

    $envValues = Read-EnvFile -Path $EnvFile
    if ($envValues.ContainsKey("PUBLIC_HOSTNAME") -and [string]::IsNullOrWhiteSpace($script:PublicHostname)) { $script:PublicHostname = $envValues["PUBLIC_HOSTNAME"] }
    if ($envValues.ContainsKey("TUNNEL_NAME") -and $script:TunnelName -eq "cam-tunnel") { $script:TunnelName = $envValues["TUNNEL_NAME"] }

    if ([string]::IsNullOrWhiteSpace($script:PublicHostname)) { $script:PublicHostname = $PublicHostname }
    if ([string]::IsNullOrWhiteSpace($script:TunnelName)) { $script:TunnelName = $TunnelName }
    $script:PublicHostname = Normalize-Host $script:PublicHostname
}

function Delete-TunnelIfRequested {
    if ($DryRun) {
        Write-Warn "DryRun enabled; tunnel was not deleted."
        return
    }
    $shouldDelete = $DeleteTunnel -or (Confirm-Action -Question "Delete Cloudflare tunnel '$script:TunnelName'?" -Default $false)
    if (-not $shouldDelete) {
        Write-Warn "Skipped tunnel deletion."
        return
    }
    $cloudflared = Find-Cloudflared
    if (-not $cloudflared) {
        Write-Warn "cloudflared not found. Cannot delete tunnel."
        return
    }
    if (-not (Test-Path -LiteralPath $ListIdsScript)) {
        Write-Warn "Missing tunnel id helper. Trying delete by name."
        & $cloudflared tunnel delete $script:TunnelName
        if ($LASTEXITCODE -eq 0) { Write-Ok "Tunnel delete requested." } else { Write-Warn "Tunnel delete failed or tunnel not found." }
        return
    }
    $ids = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ListIdsScript -TunnelName $script:TunnelName)
    if ($ids.Count -eq 0) {
        Write-Warn "No active tunnel found with that name."
        return
    }
    foreach ($id in $ids) {
        & $cloudflared tunnel delete $id
        if ($LASTEXITCODE -eq 0) { Write-Ok "Deleted tunnel id $id." } else { Write-Warn "Delete failed for tunnel id $id." }
    }
}

function Reset-Appsettings {
    if ($SkipReset -or $DryRun) {
        Write-Warn "SkipReset enabled; appsettings was not changed."
        return
    }
    if (-not (Test-Path -LiteralPath $AppsettingsPath)) {
        Write-Warn "Missing appsettings.json; skip reset."
        return
    }
    if (-not (Test-Path -LiteralPath $ResetScript)) {
        Write-Warn "Missing reset helper; skip appsettings reset."
        return
    }
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ResetScript -AppsettingsPath $AppsettingsPath
    if ($LASTEXITCODE -eq 0) { Write-Ok "appsettings public-domain values reset." } else { Write-Warn "appsettings reset failed." }
}

function Clear-CameraUrlView {
    if ($SkipDbCleanup -or $DryRun) {
        Write-Warn "SkipDbCleanup enabled; Camera.UrlView was not changed."
        return
    }
    if (-not (Test-Path -LiteralPath $ClearUrlViewScript)) {
        Write-Warn "Missing DB cleanup helper; skip Camera.UrlView cleanup."
        return
    }
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ClearUrlViewScript -AppsettingsPath $AppsettingsPath
    if ($LASTEXITCODE -eq 0) { Write-Ok "Camera.UrlView cleared in DB." } else { Write-Warn "Could not clear Camera.UrlView automatically." }
}

try {
    Write-Host "==========================================================="
    Write-Host "           V-SHIELD PUBLIC DOMAIN UNINSTALL"
    Write-Host "==========================================================="
    Write-Host ""
    Write-LogLine "Uninstall started. Root=$Root"

    Resolve-Values

    Write-Step "0) Resolved values"
    if ([string]::IsNullOrWhiteSpace($script:PublicHostname)) { Write-Warn "Hostname not found automatically." } else { Write-Ok ("Hostname = " + $script:PublicHostname) }
    Write-Ok ("Tunnel   = " + $script:TunnelName)

    Write-Step "1) Stop managed processes"
    Stop-ManagedCloudflared
    Stop-Go2Rtc

    Write-Step "2) DNS route note"
    if (-not [string]::IsNullOrWhiteSpace($script:PublicHostname)) {
        Write-Warn "DNS route deletion is not reliable via cloudflared CLI. Delete CNAME manually if needed: $script:PublicHostname"
    }

    Write-Step "3) Remove tunnel"
    Delete-TunnelIfRequested

    Write-Step "4) Cleanup local cloudflared files"
    if ($SkipLocalCleanup -or $DryRun) {
        Write-Warn "SkipLocalCleanup/DryRun enabled; local cloudflared files were not changed."
    } elseif (Test-Path -LiteralPath $CloudflaredConfig) {
        Remove-Item -LiteralPath $CloudflaredConfig -Force -ErrorAction SilentlyContinue
        Write-Ok "Deleted cloudflared config.yml."
    }
    $removeCreds = $DeleteCredentials -or (Confirm-Action -Question "Delete local Cloudflare credentials (*.json, cert.pem)?" -Default $false)
    if (($SkipLocalCleanup -or $DryRun) -and $removeCreds) {
        Write-Warn "Skipped credential deletion because SkipLocalCleanup/DryRun is enabled."
    } elseif ($removeCreds) {
        Remove-Item -Path (Join-Path $CloudflaredDir "*.json") -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath (Join-Path $CloudflaredDir "cert.pem") -Force -ErrorAction SilentlyContinue
        Write-Ok "Local credentials deleted."
    } else {
        Write-Warn "Kept local credentials."
    }
    $removeFolder = $DeleteCloudflaredFolder -or (Confirm-Action -Question "Delete entire .cloudflared folder?" -Default $false)
    if (($SkipLocalCleanup -or $DryRun) -and $removeFolder) {
        Write-Warn "Skipped .cloudflared folder deletion because SkipLocalCleanup/DryRun is enabled."
    } elseif ($removeFolder -and (Test-Path -LiteralPath $CloudflaredDir)) {
        Remove-Item -LiteralPath $CloudflaredDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Ok "Deleted .cloudflared folder."
    }

    Write-Step "5) Reset appsettings and DB"
    Reset-Appsettings
    Clear-CameraUrlView

    Write-Step "6) Uninstall cloudflared app"
    if ($DryRun) {
        Write-Warn "DryRun enabled; cloudflared app was not uninstalled."
    } else {
        $removeApp = $UninstallCloudflared -or (Confirm-Action -Question "Uninstall cloudflared from Windows?" -Default $false)
        if ($removeApp) {
            $winget = Get-Command winget -ErrorAction SilentlyContinue
            if (-not $winget) {
                Write-Warn "winget not found. Cannot auto-uninstall cloudflared."
            } else {
                & $winget.Source uninstall --id Cloudflare.cloudflared --exact --silent
                if ($LASTEXITCODE -eq 0) { Write-Ok "cloudflared uninstall requested." } else { Write-Warn "cloudflared uninstall returned non-zero." }
            }
        } else {
            Write-Warn "Kept cloudflared app installed."
        }
    }
}
catch {
    Write-Fail $_.Exception.Message
}
finally {
    Write-Host ""
    Write-Host "==========================================================="
    Write-Host "                         SUMMARY"
    Write-Host "==========================================================="
    Write-Host ("  Hostname : " + $script:PublicHostname)
    Write-Host ("  Tunnel   : " + $script:TunnelName)
    Write-Host ("  Failures : " + $script:Failures)
    Write-Host ("  Warnings : " + $script:Warnings)
    Write-Host ("  Log      : " + $LogPath)
    Write-Host ""
    if ($script:Failures -gt 0) {
        Write-Host "Uninstall incomplete." -ForegroundColor Red
        exit 1
    }
    Write-Host "Uninstall completed." -ForegroundColor Green
    exit 0
}
