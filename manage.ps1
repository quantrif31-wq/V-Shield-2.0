param(
  [ValidateSet('install','start','stop','uninstall','status')]
  [string]$Action = 'status'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiDir = Join-Path $root 'API\API\API'
$viewDir = Join-Path $root 'View'
$aiRoot = Join-Path $root 'AI_Project'
$logsDir = Join-Path $root '.runtime\logs'
$pidsDir = Join-Path $root '.runtime\pids'

$services = @(
  @{ Name = 'api';  WorkDir = $apiDir; Command = 'cmd.exe'; Args = '/c set ASPNETCORE_ENVIRONMENT=Development&& set DOTNET_ENVIRONMENT=Development&& dotnet run --no-launch-profile --urls http://0.0.0.0:5107'; Health = 'http://127.0.0.1:5107/health'; Port = 5107 },
  @{ Name = 'view'; WorkDir = $viewDir; Command = 'cmd.exe'; Args = '/c npm run dev -- --host 0.0.0.0 --port 5173 --strictPort'; Health = 'http://127.0.0.1:5173/'; Port = 5173 }
)

function Ensure-Dir([string]$path) {
  if (-not (Test-Path -LiteralPath $path)) { New-Item -Path $path -ItemType Directory -Force | Out-Null }
}

function Write-Info([string]$msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Ok([string]$msg) { Write-Host "[ OK ] $msg" -ForegroundColor Green }
function Write-WarnMsg([string]$msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }

function Test-CommandExists([string]$name) {
  return [bool](Get-Command $name -ErrorAction SilentlyContinue)
}

function Resolve-Executable([string]$name) {
  $cmd = Get-Command $name -ErrorAction SilentlyContinue | Select-Object -First 1
  if (-not $cmd) {
    throw "Khong tim thay command: $name"
  }

  if ($cmd.Source) {
    return $cmd.Source
  }

  return $cmd.Name
}

function Install-WithWinget([string]$id, [string]$name) {
  if (-not (Test-CommandExists 'winget')) {
    throw "Khong tim thay winget de cai $name."
  }

  Write-Info "Dang cai $name ($id) bang winget..."
  & winget install --id $id --exact --accept-package-agreements --accept-source-agreements --silent
}

function Ensure-Dependency([string]$cmd, [string]$wingetId, [string]$displayName) {
  if (Test-CommandExists $cmd) {
    Write-Ok "$displayName da san sang"
    return
  }

  Write-WarnMsg "$displayName chua co. Thu cai tu dong..."
  Install-WithWinget -id $wingetId -name $displayName

  if (-not (Test-CommandExists $cmd)) {
    throw "Cai $displayName xong nhung shell chua nhan command '$cmd'. Hay mo lai terminal va chay lai script."
  }
}

function Setup-PythonVenv([string]$projectDir) {
  $req = Join-Path $projectDir 'requirements.txt'
  if (-not (Test-Path -LiteralPath $req)) { return }

  $venv = Join-Path $projectDir 'venv'
  $py = Join-Path $venv 'Scripts\python.exe'

  if (-not (Test-Path -LiteralPath $py)) {
    Write-Info "Tao venv: $projectDir"
    & python -m venv $venv
  }

  Write-Info "Cai dependency Python: $projectDir"
  & $py -m pip install --upgrade pip setuptools wheel
  & $py -m pip install -r $req
}

function Save-Pid([string]$name, [int]$processId) {
  Ensure-Dir $pidsDir
  Set-Content -LiteralPath (Join-Path $pidsDir "$name.pid") -Value $processId -Encoding ascii
}

function Read-Pid([string]$name) {
  $file = Join-Path $pidsDir "$name.pid"
  if (-not (Test-Path -LiteralPath $file)) { return $null }
  $text = (Get-Content -LiteralPath $file -Raw).Trim()
  if (-not $text) { return $null }
  return [int]$text
}

function Remove-Pid([string]$name) {
  $file = Join-Path $pidsDir "$name.pid"
  if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file -Force }
}

function Get-ListeningProcessId([int]$port) {
  $connection = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
    Select-Object -First 1
  if (-not $connection) { return $null }
  return [int]$connection.OwningProcess
}

function Test-ServiceProcessOwnership($svc, [int]$processId) {
  $workDir = ([System.IO.Path]::GetFullPath($svc.WorkDir)).TrimEnd('\')
  $pending = [System.Collections.Generic.Queue[int]]::new()
  $seen = [System.Collections.Generic.HashSet[int]]::new()
  $pending.Enqueue($processId)

  while ($pending.Count -gt 0) {
    $currentId = $pending.Dequeue()
    if (-not $seen.Add($currentId)) { continue }

    $process = Get-CimInstance Win32_Process -Filter "ProcessId=$currentId" -ErrorAction SilentlyContinue
    if ($process) {
      $identity = "$($process.ExecutablePath) $($process.CommandLine)".Replace('/', '\')
      if ($identity.IndexOf($workDir, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) { return $true }
    }

    Get-CimInstance Win32_Process -Filter "ParentProcessId=$currentId" -ErrorAction SilentlyContinue |
      ForEach-Object { $pending.Enqueue([int]$_.ProcessId) }
  }

  return $false
}

function Stop-ProcessTree([int]$processId) {
  $children = Get-CimInstance Win32_Process -Filter "ParentProcessId=$processId" -ErrorAction SilentlyContinue
  foreach ($child in $children) { Stop-ProcessTree -processId ([int]$child.ProcessId) }
  Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
}

function Resolve-ServicePort($svc) {
  if (-not $svc.Port) { return }

  $portPid = Get-ListeningProcessId -port $svc.Port
  if (-not $portPid -or (Test-ServiceProcessOwnership -svc $svc -processId $portPid)) { return }

  if ($svc.Name -ne 'view') {
    throw "Cong $($svc.Port) dang bi ung dung khac su dung. Hay dong ung dung do roi chay lai."
  }

  foreach ($candidate in 5174, 5175) {
    $candidatePid = Get-ListeningProcessId -port $candidate
    if (-not $candidatePid -or (Test-ServiceProcessOwnership -svc $svc -processId $candidatePid)) {
      Write-WarnMsg "Cong $($svc.Port) dang bi ung dung khac su dung. V-Shield se dung cong $candidate."
      $svc.Port = $candidate
      $svc.Health = "http://127.0.0.1:$candidate/"
      $svc.Args = "/c npm run dev -- --host 0.0.0.0 --port $candidate --strictPort"
      return
    }
  }

  throw 'Khong con cong frontend du phong 5174/5175. Hay dong mot ung dung dang chiem cac cong nay.'
}

function Save-PortPidIfRunning($svc) {
  if (-not $svc.Port) { return $false }

  $portPid = Get-ListeningProcessId -port $svc.Port
  if (-not $portPid) { return $false }

  $proc = Get-Process -Id $portPid -ErrorAction SilentlyContinue
  if (-not $proc -or -not (Test-ServiceProcessOwnership -svc $svc -processId $portPid)) { return $false }

  Save-Pid -name $svc.Name -processId $portPid
  Write-WarnMsg "$($svc.Name) da dang lang nghe cong $($svc.Port) (PID $portPid)"
  return $true
}

function Start-ServiceItem($svc) {
  $name = $svc.Name
  Resolve-ServicePort $svc
  $existingPid = Read-Pid $name
  if ($existingPid) {
    $proc = Get-Process -Id $existingPid -ErrorAction SilentlyContinue
    if ($proc -and (Test-ServiceProcessOwnership -svc $svc -processId $existingPid)) {
      Write-WarnMsg "$name da dang chay (PID $existingPid)"
      return
    }
    Remove-Pid $name
  }

  if (Save-PortPidIfRunning $svc) {
    return
  }

  Ensure-Dir $logsDir
  $stdout = Join-Path $logsDir "$name.out.log"
  $stderr = Join-Path $logsDir "$name.err.log"
  $exe = Resolve-Executable $svc.Command

  Write-Info "Khoi dong $name..."
  $proc = Start-Process -FilePath $exe -ArgumentList $svc.Args -WorkingDirectory $svc.WorkDir -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru -WindowStyle Hidden
  Save-Pid -name $name -processId $proc.Id
  if ($svc.Port) {
    $deadline = (Get-Date).AddSeconds(20)
    while ((Get-Date) -lt $deadline) {
      $portPid = Get-ListeningProcessId -port $svc.Port
      if ($portPid) {
        Save-Pid -name $name -processId $portPid
        Write-Ok "$name da khoi dong (PID $portPid)"
        return
      }
      Start-Sleep -Milliseconds 500
    }
  }
  Write-Ok "$name da khoi dong (PID $($proc.Id))"
}

function Stop-ServiceItem($svc) {
  $name = $svc.Name
  $processId = Read-Pid $name
  if (-not $processId) {
    if ($svc.Port) {
      $processId = Get-ListeningProcessId -port $svc.Port
    }
    if (-not $processId) {
      Write-WarnMsg "$name chua chay"
      return
    }
  }

  $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
  if (-not $proc) {
    Write-WarnMsg "$name khong con chay"
    Remove-Pid $name
    return
  }

  if (-not (Test-ServiceProcessOwnership -svc $svc -processId $processId)) {
    Write-WarnMsg "$name PID $processId khong thuoc V-Shield; khong dung tien trinh nay"
    Remove-Pid $name
    return
  }

  Write-Info "Dung $name (PID $processId)..."
  Stop-ProcessTree -processId $processId
  Remove-Pid $name
  Write-Ok "$name da dung"
}

function Wait-Health([string]$url, [int]$timeoutSeconds = 45) {
  $deadline = (Get-Date).AddSeconds($timeoutSeconds)
  while ((Get-Date) -lt $deadline) {
    try {
      $res = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2
      if ($res.StatusCode -ge 200 -and $res.StatusCode -lt 300) { return $true }
    } catch {}
    Start-Sleep -Seconds 1
  }
  return $false
}

function Install-All {
  Ensure-Dependency -cmd 'dotnet' -wingetId 'Microsoft.DotNet.SDK.8' -displayName '.NET SDK 8'
  Ensure-Dependency -cmd 'node' -wingetId 'OpenJS.NodeJS.LTS' -displayName 'NodeJS LTS'
  Ensure-Dependency -cmd 'npm' -wingetId 'OpenJS.NodeJS.LTS' -displayName 'npm'
  Ensure-Dependency -cmd 'python' -wingetId 'Python.Python.3.11' -displayName 'Python 3.11'

  Write-Info 'Restore API...'
  Push-Location $apiDir
  & dotnet restore
  Pop-Location

  Write-Info 'Cai dependency frontend...'
  Push-Location $viewDir
  & npm ci
  Pop-Location

  $pyProjects = @(
    (Join-Path $aiRoot 'face_recognition'),
    (Join-Path $aiRoot 'doc_bien_gpu')
  )

  foreach ($proj in $pyProjects) {
    if (Test-Path -LiteralPath $proj) {
      Setup-PythonVenv -projectDir $proj
    }
  }

  Ensure-Dir (Join-Path $root '.runtime')
  Ensure-Dir $logsDir
  Ensure-Dir $pidsDir

  Write-Ok 'Install hoan tat.'
  Write-Info 'Buoc tiep theo: .\\manage.ps1 -Action start'
}

function Start-All {
  foreach ($svc in $services) { Start-ServiceItem $svc }

  foreach ($svc in $services) {
    if ($svc.Health) {
      if (Wait-Health -url $svc.Health -timeoutSeconds 60) {
        Write-Ok "$($svc.Name) health check OK"
      } else {
        Write-WarnMsg "$($svc.Name) chua healthy trong 60s. Xem log trong .runtime\\logs\\$($svc.Name).err.log"
      }
    }
  }

  $viewService = $services | Where-Object { $_.Name -eq 'view' } | Select-Object -First 1
  $webUrl = $viewService.Health
  Set-Content -LiteralPath (Join-Path $root '.runtime\view.url') -Value $webUrl -Encoding ascii
  try {
    Start-Process $webUrl | Out-Null
    Write-Ok "Da mo web: $webUrl"
  } catch {
    Write-WarnMsg "Khong the tu mo trinh duyet. Hay mo: $webUrl"
  }

  Write-Info 'API: http://127.0.0.1:5107'
  Write-Info "VIEW: $webUrl"
}

function Stop-All {
  foreach ($svc in $services) { Stop-ServiceItem $svc }
}

function Show-Status {
  foreach ($svc in $services) {
    Resolve-ServicePort $svc
    $processId = Read-Pid $svc.Name
    if ($processId -and
        (Get-Process -Id $processId -ErrorAction SilentlyContinue) -and
        (Test-ServiceProcessOwnership -svc $svc -processId $processId)) {
      Write-Ok "$($svc.Name): running (PID $processId)"
    } else {
      if ($svc.Port) {
        $portPid = Get-ListeningProcessId -port $svc.Port
        if ($portPid -and
            (Get-Process -Id $portPid -ErrorAction SilentlyContinue) -and
            (Test-ServiceProcessOwnership -svc $svc -processId $portPid)) {
          Save-Pid -name $svc.Name -processId $portPid
          Write-Ok "$($svc.Name): running on port $($svc.Port) (PID $portPid)"
          continue
        }
      }
      Write-WarnMsg "$($svc.Name): stopped"
    }
  }
}

function Uninstall-All {
  Stop-All

  $pathsToRemove = @(
    (Join-Path $viewDir 'node_modules'),
    (Join-Path $aiRoot 'face_recognition\\venv'),
    (Join-Path $aiRoot 'doc_bien_gpu\\venv'),
    (Join-Path $aiRoot 'QR_Dong\\venv'),
    (Join-Path $aiRoot 'AI_An_Ninh\\venv'),
    (Join-Path $apiDir 'bin'),
    (Join-Path $apiDir 'obj'),
    (Join-Path $root '.runtime')
  )

  if (Test-CommandExists 'dotnet') {
    try {
      Push-Location $apiDir
      & dotnet ef --version *> $null
      if ($LASTEXITCODE -eq 0) {
        Write-Info 'Drop database...'
        & dotnet restore *> $null
        if ($LASTEXITCODE -ne 0) {
          Write-WarnMsg 'Khong the restore project de drop database, bo qua.'
        } else {
          $prevNativeErrPref = $PSNativeCommandUseErrorActionPreference
          $PSNativeCommandUseErrorActionPreference = $false
          & dotnet ef database drop -f
          if ($LASTEXITCODE -ne 0) {
            Write-WarnMsg 'Khong the xoa database, bo qua.'
          }
          $PSNativeCommandUseErrorActionPreference = $prevNativeErrPref
        }
      } else {
        Write-WarnMsg 'Khong tim thay dotnet-ef, bo qua buoc xoa database.'
      }
      Pop-Location
    } catch {
      Write-WarnMsg 'Khong the xoa database, bo qua.'
      if ((Get-Location).Path -ne $root) { Pop-Location }
    }
  } else {
    Write-WarnMsg 'Khong tim thay dotnet, bo qua buoc xoa database.'
  }

  foreach ($path in $pathsToRemove) {
    if (Test-Path -LiteralPath $path) {
      Write-Info "Xoa: $path"
      Remove-Item -LiteralPath $path -Recurse -Force
    }
  }

  try {
    Push-Location $apiDir
    & dotnet clean
    Pop-Location
  } catch {
    Write-WarnMsg 'dotnet clean that bai, bo qua.'
  }

  Write-Ok 'Da go sach moi truong da cai (venv, node_modules, runtime, build output, database).'
}

try {
  switch ($Action) {
    'install' { Install-All; break }
    'start' { Start-All; break }
    'stop' { Stop-All; break }
    'uninstall' { Uninstall-All; break }
    'status' { Show-Status; break }
  }
  exit 0
} catch {
  Write-Host "[ERR ] $($_.Exception.Message)" -ForegroundColor Red
  exit 1
}
