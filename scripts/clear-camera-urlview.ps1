param(
  [Parameter(Mandatory=$true)][string]$AppsettingsPath
)

if (-not (Test-Path -LiteralPath $AppsettingsPath)) {
  throw "Missing appsettings: $AppsettingsPath"
}

$jsonText = Get-Content -LiteralPath $AppsettingsPath -Raw
$config = $jsonText | ConvertFrom-Json
$connStr = [string]$config.ConnectionStrings.DefaultConnection

if ([string]::IsNullOrWhiteSpace($connStr)) {
  throw "Missing ConnectionStrings:DefaultConnection in appsettings."
}

$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
try {
  $conn.Open()
  $cmd = $conn.CreateCommand()
  $cmd.CommandText = "UPDATE Camera SET UrlView = NULL WHERE UrlView IS NOT NULL;"
  $affected = $cmd.ExecuteNonQuery()
  Write-Host ("Cleared UrlView rows: {0}" -f $affected)
}
finally {
  if ($conn.State -ne [System.Data.ConnectionState]::Closed) {
    $conn.Close()
  }
}
