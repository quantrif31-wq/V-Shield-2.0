@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

echo ===========================================================
echo            GET CLOUDFLARE TUNNEL TOKEN
echo ===========================================================
echo.

where cloudflared >nul 2>nul
if errorlevel 1 (
  echo [ERR] Khong tim thay cloudflared trong PATH.
  echo      Cai cloudflared roi chay lai file nay.
  pause
  exit /b 1
)
echo [ OK ] cloudflared found.
echo.

set /p TUNNEL_NAME=Enter tunnel name [cam-tunnel]:
if "%TUNNEL_NAME%"=="" set TUNNEL_NAME=cam-tunnel

set /p PUBLIC_HOST=Enter public hostname (example: app.example.com):
if "%PUBLIC_HOST%"=="" (
  echo [ERR] Hostname khong duoc de trong.
  pause
  exit /b 1
)

echo.
echo [STEP] Login Cloudflare (browser will open, can rerun safely)...
cloudflared tunnel login
if errorlevel 1 (
  echo [WARN] Login command tra ve warning/error.
  echo       Neu da co cert.pem thi co the bo qua buoc login.
)
echo [ OK ] Login step done.

echo.
echo [STEP] Ensure tunnel exists...
for /f "delims=" %%i in ('cloudflared tunnel list --output json ^| findstr /i "\"name\":\"%TUNNEL_NAME%\""') do set HAS_TUNNEL=1
if not defined HAS_TUNNEL (
  cloudflared tunnel create %TUNNEL_NAME%
  if errorlevel 1 (
    echo [ERR] Tao tunnel that bai.
    pause
    exit /b 1
  )
)
echo [ OK ] Tunnel ready.

echo.
echo [STEP] Ensure DNS route...
cloudflared tunnel route dns %TUNNEL_NAME% %PUBLIC_HOST%
if errorlevel 1 (
  echo [WARN] Route DNS co the da ton tai hoac gap warning.
)
echo [ OK ] DNS route ready.

echo.
echo [STEP] Tunnel token (always try):
echo -----------------------------------------------------------
for /f "delims=" %%a in ('cloudflared tunnel token %TUNNEL_NAME%') do (
  set TOKEN=%%a
  echo %%a
)
echo -----------------------------------------------------------
if not defined TOKEN (
  echo [ERR] Khong lay duoc token.
  pause
  exit /b 1
)

echo Copy token above and paste into script:
echo   .\scripts\setup-docker-cloudflare-tunnel.ps1
echo.
echo ===========================================================
echo                          SUMMARY
echo ===========================================================
echo   Tunnel   : %TUNNEL_NAME%
echo   Hostname : %PUBLIC_HOST%
echo.
echo Completed. You can rerun this file safely any time.
echo.
pause
exit /b 0
