@echo off
setlocal
cd /d "%~dp0"
echo ===================================================
echo [INFO] Dang dung V-Shield Docker Local Stack...
echo ===================================================
docker compose down
echo.
echo [OK] Da dung toan bo cac container V-Shield.
echo.
pause
