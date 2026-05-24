param(
  [Parameter(Mandatory=$true)][string]$AppsettingsPath
)

if (-not (Test-Path -LiteralPath $AppsettingsPath)) {
  exit 0
}

$jsonText = Get-Content -LiteralPath $AppsettingsPath -Raw
$config = $jsonText | ConvertFrom-Json

$hostname = ""
$tunnel = ""

if ($config.PSObject.Properties['Cloudflared']) {
  $hostname = [string]$config.Cloudflared.PublicHostname
  $tunnel = [string]$config.Cloudflared.TunnelName
}

Write-Output ("PUBLIC_HOSTNAME={0}" -f $hostname.Trim())
Write-Output ("TUNNEL_NAME={0}" -f $tunnel.Trim())
