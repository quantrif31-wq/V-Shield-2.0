[CmdletBinding()]
param(
    [ValidateSet("all", "auth", "access", "enterprise", "chaos")]
    [string]$Suite = "all",

    [string]$BaseUrl = "http://localhost:5107",

    [string]$AdminToken = "",

    [string]$RefreshToken = "",

    [int]$DurationSeconds = 30,

    [int]$Concurrency = 10,

    [int]$WarmUpSeconds = 3
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repoRoot "API\API\API.Tests\API.Tests.csproj"

$filters = @{
    all = "Category=LoadTest"
    auth = "Category=Auth"
    access = "Category=AccessGateway"
    enterprise = "Category=Enterprise"
    chaos = "Category=StressSoakChaos"
}

$filter = $filters[$Suite]

Write-Host "Load test suite: $Suite"
Write-Host "Target API: $BaseUrl"
Write-Host "Filter: $filter"

try {
    $health = Invoke-WebRequest -Uri "$BaseUrl/health/live" -UseBasicParsing -TimeoutSec 10
    Write-Host "Health check OK: $($health.StatusCode)"
}
catch {
    throw "Khong the ket noi toi $BaseUrl. Hay khoi dong API va nap seed data truoc khi chay load test."
}

$env:ENABLE_LOAD_TESTS = "true"
$env:LOAD_TEST_URL = $BaseUrl
$env:LOAD_TEST_DURATION_SECONDS = $DurationSeconds.ToString()
$env:LOAD_TEST_CONCURRENCY = $Concurrency.ToString()
$env:LOAD_TEST_WARMUP_SECONDS = $WarmUpSeconds.ToString()

if ($AdminToken) {
    $env:LOAD_TEST_ADMIN_TOKEN = $AdminToken
}

if ($RefreshToken) {
    $env:LOAD_TEST_REFRESH_TOKEN = $RefreshToken
}

dotnet test $testProject --filter $filter --nologo
