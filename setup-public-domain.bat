@echo off
setlocal EnableExtensions EnableDelayedExpansion
title V-Shield Public Domain Setup Wizard
color 0A

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"
set "API_DIR=%ROOT%\API\API\API"
set "APPSETTINGS=%API_DIR%\appsettings.json"
set "PS_CMD=powershell -NoProfile -ExecutionPolicy Bypass -Command"
set "FAIL_COUNT=0"
set "WARN_COUNT=0"

echo ===========================================================
echo        V-SHIELD PUBLIC DOMAIN SETUP (GO2RTC + CLOUDFLARED)
echo ===========================================================
echo.

call :section "0) Validate project structure"
if not exist "%API_DIR%" (
  call :fail "Missing API directory: %API_DIR%"
  goto :summary
)
if not exist "%APPSETTINGS%" (
  call :fail "Missing file: %APPSETTINGS%"
  goto :summary
)
call :ok "Project structure looks good."

call :section "1) Read appsettings"
for /f "usebackq delims=" %%A in (`%PS_CMD% "(Get-Content -Raw '%APPSETTINGS%' | ConvertFrom-Json).Cloudflared.TunnelName"`) do set "TUNNEL_NAME=%%A"
for /f "usebackq delims=" %%A in (`%PS_CMD% "(Get-Content -Raw '%APPSETTINGS%' | ConvertFrom-Json).Cloudflared.PublicHostname"`) do set "PUBLIC_HOSTNAME=%%A"
for /f "usebackq delims=" %%A in (`%PS_CMD% "(Get-Content -Raw '%APPSETTINGS%' | ConvertFrom-Json).Cloudflared.TargetService"`) do set "TARGET_SERVICE=%%A"
for /f "usebackq delims=" %%A in (`%PS_CMD% "(Get-Content -Raw '%APPSETTINGS%' | ConvertFrom-Json).AppSettings.Go2RtcPublicBaseUrl"`) do set "GO2RTC_PUBLIC_BASE=%%A"
for /f "usebackq delims=" %%A in (`%PS_CMD% "(Get-Content -Raw '%APPSETTINGS%' | ConvertFrom-Json).RuntimePaths.AiRootFolderName"`) do set "AI_ROOT_NAME=%%A"

if not defined TUNNEL_NAME set "TUNNEL_NAME=cam-tunnel"
if not defined TARGET_SERVICE set "TARGET_SERVICE=http://localhost:1984"
if not defined AI_ROOT_NAME set "AI_ROOT_NAME=AI_Runtime"

if not defined PUBLIC_HOSTNAME (
  call :fail "Cloudflared.PublicHostname is empty in appsettings.json"
  echo   Fix it first in: %APPSETTINGS%
  goto :summary
)

if not defined GO2RTC_PUBLIC_BASE (
  set "GO2RTC_PUBLIC_BASE=https://%PUBLIC_HOSTNAME%"
  call :warn "AppSettings.Go2RtcPublicBaseUrl is empty. Suggested: %GO2RTC_PUBLIC_BASE%"
) else (
  call :ok "Go2RtcPublicBaseUrl = %GO2RTC_PUBLIC_BASE%"
)

call :ok "TunnelName = %TUNNEL_NAME%"
call :ok "PublicHostname = %PUBLIC_HOSTNAME%"
call :ok "TargetService = %TARGET_SERVICE%"
call :ok "AiRootFolderName = %AI_ROOT_NAME%"

call :section "2) Validate binaries"
where cloudflared >nul 2>&1
if errorlevel 1 (
  call :fail "cloudflared not found in PATH."
  echo   Install: winget install Cloudflare.cloudflared
  goto :summary
) else (
  call :ok "cloudflared found."
)

set "GO2RTC_PATH=%ROOT%\%AI_ROOT_NAME%\cam\go2rtc_win64\go2rtc.exe"
if not exist "%GO2RTC_PATH%" (
  set "GO2RTC_PATH=%ROOT%\AI_Runtime\cam\go2rtc_win64\go2rtc.exe"
)
if not exist "%GO2RTC_PATH%" (
  set "GO2RTC_PATH=%ROOT%\AI_Project\cam\go2rtc_win64\go2rtc.exe"
)

if not exist "%GO2RTC_PATH%" (
  call :warn "go2rtc.exe not found. Skip auto-start go2rtc."
) else (
  call :ok "go2rtc.exe found: %GO2RTC_PATH%"
)

call :section "3) Check cloudflared credentials"
set "CLOUDFLARED_DIR=%USERPROFILE%\.cloudflared"
if not exist "%CLOUDFLARED_DIR%" mkdir "%CLOUDFLARED_DIR%" >nul 2>&1

set "CRED_FILE="
for %%F in ("%CLOUDFLARED_DIR%\*.json") do (
  set "CRED_FILE=%%~fF"
  goto :cred_found
)

:cred_found
if not defined CRED_FILE (
  call :warn "No tunnel credentials JSON in %CLOUDFLARED_DIR%"
  echo   Need initial tunnel setup.
  choice /C YN /N /M "Run required cloudflared commands now? [Y/N]: "
  if errorlevel 2 (
    call :fail "User chose not to run required commands."
    echo   You can run later:
    echo   1^) cloudflared tunnel login
    echo   2^) cloudflared tunnel create %TUNNEL_NAME%
    echo   3^) cloudflared tunnel route dns %TUNNEL_NAME% %PUBLIC_HOSTNAME%
    goto :summary
  )

  echo.
  echo [INFO] Running: cloudflared tunnel login
  cloudflared tunnel login
  if errorlevel 1 (
    call :fail "cloudflared tunnel login failed."
    goto :summary
  )

  echo.
  echo [INFO] Running: cloudflared tunnel create %TUNNEL_NAME%
  cloudflared tunnel create %TUNNEL_NAME%
  if errorlevel 1 (
    call :warn "tunnel create returned non-zero. It may already exist, continuing..."
  )

  set "CRED_FILE="
  for %%F in ("%CLOUDFLARED_DIR%\*.json") do (
    set "CRED_FILE=%%~fF"
    goto :cred_refound
  )
  :cred_refound
  if not defined CRED_FILE (
    call :fail "Still cannot find credentials JSON after setup."
    goto :summary
  )
  call :ok "Found credentials after setup: %CRED_FILE%"
) else (
  call :ok "Found credentials: %CRED_FILE%"
)

call :section "4) Ensure DNS route exists"
set "DNS_OUT=%TEMP%\vshield_dns_route_%RANDOM%.log"
cloudflared tunnel route dns %TUNNEL_NAME% %PUBLIC_HOSTNAME% > "%DNS_OUT%" 2>&1
set "DNS_RC=%ERRORLEVEL%"
findstr /I /C:"already exists" "%DNS_OUT%" >nul 2>&1
if not errorlevel 1 set "DNS_RC=0"

if "%DNS_RC%"=="0" (
  call :ok "DNS route ready: %PUBLIC_HOSTNAME% -> tunnel %TUNNEL_NAME%"
) else (
  type "%DNS_OUT%"
  call :warn "Cannot auto-create DNS route."
  choice /C YN /N /M "Retry DNS route command now? [Y/N]: "
  if errorlevel 2 (
    call :warn "Skipped DNS retry by user."
  ) else (
    cloudflared tunnel route dns %TUNNEL_NAME% %PUBLIC_HOSTNAME%
    if errorlevel 1 (
      call :warn "DNS route retry still failed. Check Cloudflare permission/zone."
    ) else (
      call :ok "DNS route created successfully on retry."
    )
  )
)
del /q "%DNS_OUT%" >nul 2>&1

call :section "5) Generate cloudflared config.yml"
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

if exist "%CONFIG_YML%" (
  call :ok "Generated: %CONFIG_YML%"
) else (
  call :fail "Failed to write %CONFIG_YML%"
  goto :summary
)

call :section "6) Restart go2rtc"
taskkill /F /IM go2rtc.exe >nul 2>&1
if exist "%GO2RTC_PATH%" (
  start "" "%GO2RTC_PATH%"
  timeout /t 2 /nobreak >nul
  tasklist /FI "IMAGENAME eq go2rtc.exe" | find /I "go2rtc.exe" >nul
  if errorlevel 1 (
    call :warn "go2rtc may not be running."
  ) else (
    call :ok "go2rtc is running."
  )
) else (
  call :warn "Skipped go2rtc start (binary not found)."
)

call :section "7) Restart cloudflared tunnel"
taskkill /F /IM cloudflared.exe >nul 2>&1
start "" /MIN cloudflared tunnel --config "%CONFIG_YML%" run
timeout /t 3 /nobreak >nul
tasklist /FI "IMAGENAME eq cloudflared.exe" | find /I "cloudflared.exe" >nul
if errorlevel 1 (
  call :fail "cloudflared is not running."
  echo   Check:
  echo   cloudflared tunnel --config "%CONFIG_YML%" run
) else (
  call :ok "cloudflared is running."
)

call :section "8) Public URL smoke test"
set "PUBLIC_TEST_URL=https://%PUBLIC_HOSTNAME%/"
for /f "usebackq delims=" %%A in (`%PS_CMD% "try { $r = Invoke-WebRequest -Uri '%PUBLIC_TEST_URL%' -UseBasicParsing -TimeoutSec 10; if($r.StatusCode -ge 200 -and $r.StatusCode -lt 500){ 'OK' } else { 'BAD ' + $r.StatusCode } } catch { 'ERR ' + $_.Exception.Message }"`) do set "SMOKE_RESULT=%%A"

echo   Test URL: %PUBLIC_TEST_URL%
if /I "%SMOKE_RESULT:~0,2%"=="OK" (
  call :ok "Public domain is reachable."
) else (
  call :warn "Smoke test failed: %SMOKE_RESULT%"
  echo   It can take 30-120s for DNS/tunnel to propagate.
)

:summary
echo.
echo ===========================================================
echo                        SUMMARY
echo ===========================================================
echo   Failures : %FAIL_COUNT%
echo   Warnings : %WARN_COUNT%
echo.
if %FAIL_COUNT% gtr 0 (
  echo Setup incomplete. Resolve failures above, then run again.
  echo.
  pause
  exit /b 1
) else (
  echo Setup completed.
  echo Recommended:
  echo   1^) Open Settings and click reload go2rtc once.
  echo   2^) Verify camera UrlView opens from external network.
  echo.
  pause
  exit /b 0
)

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
