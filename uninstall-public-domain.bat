@echo off
setlocal EnableExtensions EnableDelayedExpansion
title V-Shield Public Domain Uninstall
color 0C

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"
set "ENV_FILE=%ROOT%\customer.env"
set "API_DIR=%ROOT%\API\API\API"
set "APPSETTINGS=%API_DIR%\appsettings.json"
set "RESET_PS1=%ROOT%\scripts\reset-public-domain-appsettings.ps1"
set "READ_PS1=%ROOT%\scripts\read-public-domain-appsettings.ps1"
set "LIST_IDS_PS1=%ROOT%\scripts\list-tunnel-ids-by-name.ps1"
set "CLEAR_URLVIEW_PS1=%ROOT%\scripts\clear-camera-urlview.ps1"
set "FAIL_COUNT=0"
set "WARN_COUNT=0"
set "CLOUDFLARED_DIR=%USERPROFILE%\.cloudflared"
set "TUNNEL_NAME=cam-tunnel"

echo ===========================================================
echo            V-SHIELD PUBLIC DOMAIN UNINSTALL
echo ===========================================================
echo.

call :load_from_appsettings
call :load_from_env
call :load_from_cloudflared_config
call :normalize_public_hostname
if not defined TUNNEL_NAME set "TUNNEL_NAME=cam-tunnel"

call :section "0) Resolved values"
if defined PUBLIC_HOSTNAME (call :ok "Hostname = %PUBLIC_HOSTNAME%") else call :warn "Hostname not found automatically."
call :ok "Tunnel   = %TUNNEL_NAME%"

call :section "1) Stop processes"
taskkill /F /IM cloudflared.exe >nul 2>&1
taskkill /F /IM go2rtc.exe >nul 2>&1
call :ok "Stopped cloudflared/go2rtc (if running)."

call :section "2) Remove DNS route (optional)"
choice /C YN /N /M "Remove DNS route %PUBLIC_HOSTNAME% ? [Y/N]: "
if errorlevel 2 (
  call :warn "Skipped DNS route deletion."
) else (
  if not defined PUBLIC_HOSTNAME (
    call :warn "Skip DNS delete because hostname is empty."
    goto :skip_dns_delete
  )
  call :warn "DNS delete via CLI is unstable across cloudflared versions."
  echo   Please delete DNS record manually in Cloudflare DNS:
  echo   - Name: %PUBLIC_HOSTNAME%
  echo   - Type: CNAME
)
:skip_dns_delete

call :section "3) Remove tunnel (optional)"
choice /C YN /N /M "Delete tunnel %TUNNEL_NAME% ? [Y/N]: "
if errorlevel 2 (
  call :warn "Skipped tunnel deletion."
) else (
  call :ensure_cloudflare_auth
  if errorlevel 1 (
    call :warn "Skip tunnel delete because Cloudflare auth is unavailable."
    goto :skip_tunnel_delete
  )
  if not exist "%LIST_IDS_PS1%" (
    call :warn "Missing helper for tunnel-id lookup. Trying delete by name."
    cloudflared tunnel delete %TUNNEL_NAME%
    if errorlevel 1 (call :warn "Tunnel delete failed or not found.") else call :ok "Tunnel deleted."
  ) else (
    set "HAS_ID=0"
    for /f "usebackq delims=" %%I in (`powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%LIST_IDS_PS1%" -TunnelName "%TUNNEL_NAME%"`) do (
      set "HAS_ID=1"
      echo   Deleting tunnel id: %%I
      cloudflared tunnel delete %%I
      if errorlevel 1 (call :warn "Delete failed for tunnel id %%I") else call :ok "Deleted tunnel id %%I"
    )
    if "!HAS_ID!"=="0" (
      call :warn "No active tunnel found with name %TUNNEL_NAME%."
    )
  )
)
:skip_tunnel_delete

call :section "4) Cleanup local cloudflared files"
if exist "%CLOUDFLARED_DIR%\config.yml" del /q "%CLOUDFLARED_DIR%\config.yml" >nul 2>&1
choice /C YN /N /M "Delete local credentials (*.json, cert.pem) ? [Y/N]: "
if errorlevel 2 (
  call :warn "Kept local credentials."
) else (
  del /q "%CLOUDFLARED_DIR%\*.json" >nul 2>&1
  if exist "%CLOUDFLARED_DIR%\cert.pem" del /q "%CLOUDFLARED_DIR%\cert.pem" >nul 2>&1
  call :ok "Local credentials deleted."
)
choice /C YN /N /M "Delete entire .cloudflared folder? [Y/N]: "
if errorlevel 2 (
  call :warn "Kept .cloudflared folder."
) else (
  rmdir /S /Q "%CLOUDFLARED_DIR%" >nul 2>&1
  call :ok "Deleted .cloudflared folder."
)

call :section "5) Reset appsettings.json to defaults"
if not exist "%APPSETTINGS%" (
  call :warn "Missing appsettings.json, skip reset."
) else if not exist "%RESET_PS1%" (
  call :warn "Missing helper script, skip reset."
) else (
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%RESET_PS1%" -AppsettingsPath "%APPSETTINGS%"
  if errorlevel 1 (
    call :warn "Auto reset appsettings failed."
  ) else (
    call :ok "appsettings.json reset to default public-domain values."
  )
)

call :section "6) Clean camera UrlView in DB"
if not exist "%APPSETTINGS%" (
  call :warn "Missing appsettings.json, skip DB cleanup."
) else if not exist "%CLEAR_URLVIEW_PS1%" (
  call :warn "Missing DB cleanup helper, skip camera UrlView cleanup."
) else (
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%CLEAR_URLVIEW_PS1%" -AppsettingsPath "%APPSETTINGS%"
  if errorlevel 1 (
    call :warn "Could not clear Camera.UrlView automatically."
  ) else (
    call :ok "Camera.UrlView cleared in DB."
  )
)

call :section "7) Uninstall cloudflared app (optional)"
choice /C YN /N /M "Uninstall cloudflared from Windows? [Y/N]: "
if errorlevel 2 (
  call :warn "Kept cloudflared app installed."
) else (
  where winget >nul 2>&1
  if errorlevel 1 (
    call :warn "winget not found. Cannot auto-uninstall cloudflared."
  ) else (
    winget uninstall --id Cloudflare.cloudflared --exact --silent
    if errorlevel 1 (call :warn "cloudflared uninstall command returned non-zero.") else call :ok "cloudflared app uninstall requested."
  )
)

echo.
echo ===========================================================
echo                          SUMMARY
echo ===========================================================
echo   Hostname : %PUBLIC_HOSTNAME%
echo   Tunnel   : %TUNNEL_NAME%
echo   Failures : %FAIL_COUNT%
echo   Warnings : %WARN_COUNT%
echo.
if %FAIL_COUNT% gtr 0 (echo Uninstall incomplete.) else (echo Uninstall completed.)
echo.
pause
if %FAIL_COUNT% gtr 0 (exit /b 1) else (exit /b 0)

:load_from_appsettings
if not exist "%APPSETTINGS%" exit /b 0
if not exist "%READ_PS1%" exit /b 0
for /f "usebackq tokens=1,* delims==" %%K in (`powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%READ_PS1%" -AppsettingsPath "%APPSETTINGS%"`) do (
  if /I "%%K"=="PUBLIC_HOSTNAME" set "PUBLIC_HOSTNAME=%%L"
  if /I "%%K"=="TUNNEL_NAME" set "TUNNEL_NAME=%%L"
)
exit /b 0

:load_from_cloudflared_config
set "CF_CONFIG=%CLOUDFLARED_DIR%\config.yml"
if not exist "%CF_CONFIG%" exit /b 0
for /f "usebackq tokens=1,* delims=:" %%A in ("%CF_CONFIG%") do (
  set "K=%%A"
  set "V=%%B"
  set "K=!K: =!"
  if /I "!K!"=="tunnel" if not defined TUNNEL_NAME set "TUNNEL_NAME=!V:~1!"
)
for /f "usebackq tokens=1,* delims=:" %%A in ("%CF_CONFIG%") do (
  set "K=%%A"
  set "V=%%B"
  set "K=!K: =!"
  if /I "!K!"=="-hostname" if not defined PUBLIC_HOSTNAME set "PUBLIC_HOSTNAME=!V:~1!"
  if /I "!K!"=="hostname" if not defined PUBLIC_HOSTNAME set "PUBLIC_HOSTNAME=!V:~1!"
)
exit /b 0

:load_from_env
if not exist "%ENV_FILE%" exit /b 0
for /f "usebackq tokens=1,* delims==" %%K in ("%ENV_FILE%") do (
  if /I "%%K"=="PUBLIC_HOSTNAME" set "PUBLIC_HOSTNAME=%%L"
  if /I "%%K"=="TUNNEL_NAME" set "TUNNEL_NAME=%%L"
)
call :ok "Loaded customer.env"
exit /b 0

:normalize_public_hostname
if not defined PUBLIC_HOSTNAME exit /b 0
set "PUBLIC_HOSTNAME=%PUBLIC_HOSTNAME: =%"
set "PUBLIC_HOSTNAME=%PUBLIC_HOSTNAME:https://=%"
set "PUBLIC_HOSTNAME=%PUBLIC_HOSTNAME:http://=%"
if "%PUBLIC_HOSTNAME:~0,2%"=="//" set "PUBLIC_HOSTNAME=%PUBLIC_HOSTNAME:~2%"
if "%PUBLIC_HOSTNAME:~-1%"=="/" set "PUBLIC_HOSTNAME=%PUBLIC_HOSTNAME:~0,-1%"
exit /b 0

:ensure_cloudflare_auth
where cloudflared >nul 2>&1
if errorlevel 1 (
  call :warn "cloudflared not found. Cannot perform Cloudflare delete actions."
  exit /b 1
)
if exist "%CLOUDFLARED_DIR%\cert.pem" exit /b 0
call :warn "Missing cert.pem. Cloudflare delete requires login."
choice /C YN /N /M "Run cloudflared tunnel login now? [Y/N]: "
if errorlevel 2 exit /b 1
cloudflared tunnel login
if errorlevel 1 exit /b 1
if exist "%CLOUDFLARED_DIR%\cert.pem" (exit /b 0) else (exit /b 1)

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
