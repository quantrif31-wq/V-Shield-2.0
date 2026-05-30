param(
    [string]$EnvFile = ".env"
)

$ErrorActionPreference = "Stop"

function Ensure-EnvFile([string]$Path) {
    if (Test-Path $Path) { return }

    $candidates = @(".env.example", ".env.docker.example")
    $source = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $source) {
        throw "Khong tim thay file .env.example hoac .env.docker.example"
    }
    Copy-Item $source $Path
}

Write-Host "==> Buoc 1: Tao env neu chua co" -ForegroundColor Cyan
Ensure-EnvFile -Path $EnvFile

Write-Host "==> Buoc 2: Khoi dong core stack" -ForegroundColor Cyan
& docker compose up -d --build db go2rtc api frontend
if ($LASTEXITCODE -ne 0) {
    throw "Khoi dong core stack that bai."
}

Write-Host "==> Buoc 3: Kiem tra nhanh" -ForegroundColor Cyan
docker compose ps

Write-Host ""
Write-Host "Hoan tat. Neu dung domain public, cap nhat APP_FRONTEND_URL / APP_GO2RTC_PUBLIC_BASE_URL trong .env." -ForegroundColor Yellow
Write-Host "Mac dinh GIU logic cu: GO2RTC_WEBRTC_CANDIDATES de trong, GO2RTC_STREAM_MODE=webrtc." -ForegroundColor Yellow
