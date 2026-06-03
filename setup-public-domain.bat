@echo off
setlocal EnableExtensions
title V-Shield Public Domain Setup

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"
set "SETUP_PS1=%ROOT%\scripts\setup-public-domain.ps1"

if not exist "%SETUP_PS1%" (
  echo [FAIL] Missing setup script: "%SETUP_PS1%"
  pause
  exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SETUP_PS1%"
set "RC=%ERRORLEVEL%"

echo.
pause
exit /b %RC%
