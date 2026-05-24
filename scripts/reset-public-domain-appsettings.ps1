param(
  [Parameter(Mandatory=$true)][string]$AppsettingsPath
)

if (-not (Test-Path -LiteralPath $AppsettingsPath)) {
  throw "Missing appsettings: $AppsettingsPath"
}

$backupPath = "$AppsettingsPath.bak.public-domain"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Replace-JsonStringValue {
  param(
    [Parameter(Mandatory=$true)][string]$InputText,
    [Parameter(Mandatory=$true)][string]$Key,
    [Parameter(Mandatory=$true)][AllowEmptyString()][string]$NewValue
  )
  $pattern = "(`"$Key`"\s*:\s*`")([^`"]*)(`")"
  return [Regex]::Replace($InputText, $pattern, { param($m) $m.Groups[1].Value + $NewValue + $m.Groups[3].Value }, 1)
}

try {
  $jsonText = Get-Content -LiteralPath $AppsettingsPath -Raw
  [System.IO.File]::WriteAllText($backupPath, $jsonText, $utf8NoBom)

  $null = $jsonText | ConvertFrom-Json

  $updated = $jsonText
  $updated = Replace-JsonStringValue -InputText $updated -Key "TunnelName" -NewValue "cam-tunnel"
  $updated = Replace-JsonStringValue -InputText $updated -Key "PublicHostname" -NewValue ""
  $updated = Replace-JsonStringValue -InputText $updated -Key "TargetService" -NewValue "http://localhost:1984"
  $updated = Replace-JsonStringValue -InputText $updated -Key "Go2RtcPublicBaseUrl" -NewValue ""
  $updated = Replace-JsonStringValue -InputText $updated -Key "FrontendUrl" -NewValue "http://localhost:5173"

  $null = $updated | ConvertFrom-Json
  [System.IO.File]::WriteAllText($AppsettingsPath, $updated, $utf8NoBom)

  Write-Host "Reset appsettings.json public-domain keys to defaults (layout-preserving mode)."
}
catch {
  if (Test-Path -LiteralPath $backupPath) {
    Copy-Item -LiteralPath $backupPath -Destination $AppsettingsPath -Force
  }
  throw
}
