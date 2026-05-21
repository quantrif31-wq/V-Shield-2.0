@echo off
setlocal
cd /d "%~dp0"

echo [INFO] Stopping V-Shield production services...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$names = @('vshield-api','vshield-go2rtc','vshield-cloudflared','vshield-python-qr','vshield-python-plate','vshield-python-cam-gia-lap');" ^
  "$existing = Get-Service -Name $names -ErrorAction SilentlyContinue;" ^
  "if ($existing -and $existing.Count -gt 0) { $existing | Stop-Service -ErrorAction SilentlyContinue; Write-Host '[OK ] Windows services stopped.' } else { Write-Host '[WARN] Windows services not found. Fallback to dev stop.'; & '.\\manage.ps1' -Action stop }"

echo [INFO] Done.
endlocal
