@echo off
setlocal EnableExtensions EnableDelayedExpansion
title V-Shield Public Domain Setup
color 0A

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"
set "ENV_FILE=%ROOT%\customer.env"
set "API_DIR=%ROOT%\API\API\API"
set "APPSETTINGS=%API_DIR%\appsettings.json"
set "UPDATE_PS1=%ROOT%\scripts\update-public-domain-appsettings.ps1"
set "FAIL_COUNT=0"
set "WARN_COUNT=0"
set "CLOUDFLARED_DIR=%USERPROFILE%\.cloudflared"
set "TARGET_SERVICE=http://localhost:1984"
set "TUNNEL_NAME=cam-tunnel"
set "AI_ROOT_NAME=AI_Runtime"

echo ===========================================================
echo              V-SHIELD PUBLIC DOMAIN SETUP
echo ===========================================================
echo.

if not exist "%CLOUDFLARED_DIR%" mkdir "%CLOUDFLARED_DIR%" >nul 2>&1
call :load_from_env
goto :ask_values
:after_ask_values
call :normalize_public_hostname
if not defined PUBLIC_HOSTNAME (
  call :fail "PUBLIC_HOSTNAME is empty after normalization."
  goto :summary
)

call :section "1) Validate binaries"
where cloudflared >nul 2>&1
if errorlevel 1 (
  call :warn "cloudflared not found in PATH."
  choice /C YN /N /M "Install cloudflared now via winget? [Y/N]: "
  if errorlevel 2 (
    call :fail "cloudflared is required. User skipped install."
    goto :summary
  )
  where winget >nul 2>&1
  if errorlevel 1 (
    call :fail "winget not found. Install cloudflared manually."
    goto :summary
  )
  winget install --id Cloudflare.cloudflared --exact --accept-package-agreements --accept-source-agreements --silent
  where cloudflared >nul 2>&1
  if errorlevel 1 (
    call :fail "cloudflared install did not complete in current shell. Re-open terminal and run again."
    goto :summary
  )
)
call :ok "cloudflared found."

set "GO2RTC_PATH=%ROOT%\%AI_ROOT_NAME%\cam\go2rtc_win64\go2rtc.exe"
if not exist "%GO2RTC_PATH%" set "GO2RTC_PATH=%ROOT%\AI_Runtime\cam\go2rtc_win64\go2rtc.exe"
if not exist "%GO2RTC_PATH%" set "GO2RTC_PATH=%ROOT%\AI_Project\cam\go2rtc_win64\go2rtc.exe"
if exist "%GO2RTC_PATH%" (call :ok "go2rtc.exe found.") else call :warn "go2rtc.exe not found. Skip go2rtc start."

call :section "2) Ensure tunnel credentials"
set "CRED_FILE="
for %%F in ("%CLOUDFLARED_DIR%\*.json") do set "CRED_FILE=%%~fF"
if not defined CRED_FILE (
  choice /C YN /N /M "Run cloudflared login/create now? [Y/N]: "
  if errorlevel 2 (
    call :fail "User skipped required Cloudflare auth."
    goto :summary
  )
  cloudflared tunnel login
  if errorlevel 1 (
    call :fail "cloudflared tunnel login failed."
    goto :summary
  )
  cloudflared tunnel create %TUNNEL_NAME%
  if errorlevel 1 call :warn "tunnel create returned non-zero (may already exist)."
  for %%F in ("%CLOUDFLARED_DIR%\*.json") do set "CRED_FILE=%%~fF"
)
if not defined CRED_FILE (
  call :fail "No credentials JSON found in %CLOUDFLARED_DIR%"
  goto :summary
)
call :ok "Credentials file ready."

call :section "3) Ensure DNS route"
set "DNS_OUT=%TEMP%\vshield_dns_%RANDOM%.log"
cloudflared tunnel route dns %TUNNEL_NAME% %PUBLIC_HOSTNAME% > "%DNS_OUT%" 2>&1
set "DNS_RC=%ERRORLEVEL%"
findstr /I /C:"already exists" "%DNS_OUT%" >nul 2>&1
if not errorlevel 1 set "DNS_RC=0"
if "%DNS_RC%"=="0" (
  call :ok "DNS route ready."
) else (
  type "%DNS_OUT%"
  call :warn "DNS route command failed."
)
del /q "%DNS_OUT%" >nul 2>&1

call :section "4) Write cloudflared config"
set "CONFIG_YML=%CLOUDFLARED_DIR%\config.yml"
(
  echo tunnel: %TUNNEL_NAME%
  echo credentials-file: %CRED_FILE:\=/%
  echo.
  echo ingress:
  echo   - hostname: %PUBLIC_HOSTNAME%
  echo     service: %TARGET_SERVICE%
  echo   - service: http_status:404
) > "%CONFIG_YML%"
if exist "%CONFIG_YML%" (call :ok "config.yml generated.") else call :fail "Failed writing config.yml"

call :section "5) Restart processes"
taskkill /F /IM cloudflared.exe >nul 2>&1
start "" /MIN cloudflared tunnel --config "%CONFIG_YML%" run
timeout /t 2 /nobreak >nul
tasklist /FI "IMAGENAME eq cloudflared.exe" | find /I "cloudflared.exe" >nul
if errorlevel 1 (call :fail "cloudflared not running.") else call :ok "cloudflared running."

taskkill /F /IM go2rtc.exe >nul 2>&1
if exist "%GO2RTC_PATH%" (
  start "" "%GO2RTC_PATH%"
  timeout /t 2 /nobreak >nul
  tasklist /FI "IMAGENAME eq go2rtc.exe" | find /I "go2rtc.exe" >nul
  if errorlevel 1 (call :warn "go2rtc not running.") else call :ok "go2rtc running."
)

call :section "6) Appsettings reminder"
if not exist "%APPSETTINGS%" (
  call :fail "Missing appsettings.json. Cannot continue safely."
  goto :summary
) else if not exist "%UPDATE_PS1%" (
  call :fail "Missing helper script. Cannot continue safely."
  goto :summary
) else (
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%UPDATE_PS1%" -AppsettingsPath "%APPSETTINGS%" -TunnelName "%TUNNEL_NAME%" -PublicHostname "%PUBLIC_HOSTNAME%" -TargetService "%TARGET_SERVICE%"
  if errorlevel 1 (
    call :fail "Auto update appsettings failed."
    goto :summary
  ) else (
    call :ok "appsettings.json updated automatically."
  )
)

call :section "7) Regenerate UrlView from backend"
set "RELOAD_TMP=%TEMP%\vshield_reload_%RANDOM%.txt"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "try { Invoke-RestMethod -Method Post -Uri 'http://127.0.0.1:5107/api/camera-runtime/reload-go2rtc' -TimeoutSec 15 | Out-Null; Write-Host 'OK' } catch { Write-Host 'FAIL' }" > "%RELOAD_TMP%" 2>nul
findstr /I "OK" "%RELOAD_TMP%" >nul 2>&1
if errorlevel 1 (
  call :warn "Could not auto-call reload-go2rtc (API may be down). Run reload manually in app when API is up."
) else (
  call :ok "reload-go2rtc called successfully."
)
del /q "%RELOAD_TMP%" >nul 2>&1

goto :summary

:load_from_env
if not exist "%ENV_FILE%" exit /b 0
for /f "usebackq tokens=1,* delims==" %%K in ("%ENV_FILE%") do (
  if /I "%%K"=="PUBLIC_HOSTNAME" set "PUBLIC_HOSTNAME=%%L"
  if /I "%%K"=="TUNNEL_NAME" set "TUNNEL_NAME=%%L"
  if /I "%%K"=="TARGET_SERVICE" set "TARGET_SERVICE=%%L"
)
call :ok "Loaded customer.env"
exit /b 0

:ask_values
if not defined PUBLIC_HOSTNAME set /p PUBLIC_HOSTNAME=Enter PUBLIC_HOSTNAME, example cam.customer.com: 
set "TMP_INPUT="
set /p TMP_INPUT=Enter TUNNEL_NAME [%TUNNEL_NAME%]: 
if defined TMP_INPUT set "TUNNEL_NAME=%TMP_INPUT%"
set "TMP_INPUT="
set /p TMP_INPUT=Enter TARGET_SERVICE [%TARGET_SERVICE%]: 
if defined TMP_INPUT set "TARGET_SERVICE=%TMP_INPUT%"
goto :after_ask_values

:normalize_public_hostname
if not defined PUBLIC_HOSTNAME exit /b 0
set "PUBLIC_HOSTNAME=%PUBLIC_HOSTNAME: =%"
set "PUBLIC_HOSTNAME=%PUBLIC_HOSTNAME:https://=%"
set "PUBLIC_HOSTNAME=%PUBLIC_HOSTNAME:http://=%"
if "%PUBLIC_HOSTNAME:~0,2%"=="//" set "PUBLIC_HOSTNAME=%PUBLIC_HOSTNAME:~2%"
if "%PUBLIC_HOSTNAME:~-1%"=="/" set "PUBLIC_HOSTNAME=%PUBLIC_HOSTNAME:~0,-1%"
exit /b 0

:summary
echo.
echo ===========================================================
echo                          SUMMARY
echo ===========================================================
echo   Hostname : %PUBLIC_HOSTNAME%
echo   Tunnel   : %TUNNEL_NAME%
echo   Failures : %FAIL_COUNT%
echo   Warnings : %WARN_COUNT%
echo.
if %FAIL_COUNT% gtr 0 (echo Setup incomplete.) else (echo Setup completed.)
echo.
pause
if %FAIL_COUNT% gtr 0 (exit /b 1) else (exit /b 0)

:section
echo.
echo ---------- %~1 ----------
exit /b 0

:ok
echo [ OK ] %~1
exit /b 0

:warn
set /a WARN_COUNT+=1
echo [WARN] %~1
exit /b 0

:fail
set /a FAIL_COUNT+=1
echo [FAIL] %~1
exit /b 0
