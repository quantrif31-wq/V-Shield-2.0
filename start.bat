@echo off
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0manage.ps1" -Action start
if errorlevel 1 (
  echo.
  echo Khoi dong that bai. Vui long xem thong bao loi o tren.
  pause
  exit /b 1
)
echo.
set "VSHIELD_URL=http://127.0.0.1:5173/"
if exist "%~dp0.runtime\view.url" set /p VSHIELD_URL=<"%~dp0.runtime\view.url"
echo V-Shield da san sang: %VSHIELD_URL%
ping 127.0.0.1 -n 6 >nul
