param(
  [Parameter(Mandatory=$true)][string]$AppsettingsPath,
  [Parameter(Mandatory=$true)][string]$TunnelName,
  [Parameter(Mandatory=$true)][string]$PublicHostname,
  [Parameter(Mandatory=$true)][string]$TargetService
)

if (-not (Test-Path -LiteralPath $AppsettingsPath)) {
  throw "Missing appsettings: $AppsettingsPath"
}

$backupPath = "$AppsettingsPath.bak.public-domain"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$PublicHostname = [string]$PublicHostname
$PublicHostname = $PublicHostname.Trim()
$PublicHostname = $PublicHostname -replace '^https?://', ''
$PublicHostname = $PublicHostname -replace '^//', ''
$PublicHostname = $PublicHostname.TrimEnd('/')

if ([string]::IsNullOrWhiteSpace($PublicHostname)) {
  throw "PublicHostname is empty after normalization."
}

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
  $updated = Replace-JsonStringValue -InputText $updated -Key "TunnelName" -NewValue $TunnelName
  $updated = Replace-JsonStringValue -InputText $updated -Key "PublicHostname" -NewValue $PublicHostname
  $updated = Replace-JsonStringValue -InputText $updated -Key "TargetService" -NewValue $TargetService
  $updated = Replace-JsonStringValue -InputText $updated -Key "Go2RtcPublicBaseUrl" -NewValue ("https://{0}" -f $PublicHostname)
  $updated = Replace-JsonStringValue -InputText $updated -Key "FrontendUrl" -NewValue ("https://{0}" -f $PublicHostname)

  $null = $updated | ConvertFrom-Json
  [System.IO.File]::WriteAllText($AppsettingsPath, $updated, $utf8NoBom)

  Write-Host "Updated appsettings.json successfully (layout-preserving mode)."
}
catch {
  if (Test-Path -LiteralPath $backupPath) {
    Copy-Item -LiteralPath $backupPath -Destination $AppsettingsPath -Force
  }
  throw
}
