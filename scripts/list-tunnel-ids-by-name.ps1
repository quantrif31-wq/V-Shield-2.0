param(
  [Parameter(Mandatory=$true)][string]$TunnelName
)

# Return newline-delimited UUID list for non-deleted tunnels matching name.
$items = cloudflared tunnel list --output json 2>$null | ConvertFrom-Json
$matches = @($items | Where-Object { $_.name -eq $TunnelName -and -not $_.deletedAt })
foreach ($m in $matches) {
  if ($m.id) { Write-Output $m.id }
}
