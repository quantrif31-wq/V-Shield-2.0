@echo off
setlocal EnableExtensions
title V-Shield Public Domain Uninstall

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"
set "UNINSTALL_PS1=%ROOT%\scripts\uninstall-public-domain.ps1"

if not exist "%UNINSTALL_PS1%" (
  echo [FAIL] Missing uninstall script: "%UNINSTALL_PS1%"
  pause
  exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%UNINSTALL_PS1%"
set "RC=%ERRORLEVEL%"

echo.
pause
exit /b %RC%
