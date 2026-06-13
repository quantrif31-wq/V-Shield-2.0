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
echo V-Shield da san sang: http://127.0.0.1:5173/
ping 127.0.0.1 -n 6 >nul
