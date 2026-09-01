@echo off
setlocal
cd /d "%~dp0"
echo ===================================================
echo [INFO] Khoi dong V-Shield Docker Local Stack...
echo ===================================================
docker compose up -d --build
if errorlevel 1 (
    echo.
    echo [ERROR] Khoi dong Docker that bai. Vui long kiem tra Docker Desktop da bat chua.
    pause
    exit /b 1
)
echo.
echo [OK] V-Shield Stack da san sang tai: http://localhost:5173
echo.
pause
