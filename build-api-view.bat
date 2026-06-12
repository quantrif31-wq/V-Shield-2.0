@echo off
setlocal EnableExtensions EnableDelayedExpansion
title Build V-Shield API & View
color 0A

set "BASEDIR=%~dp0"
set "APIROOT=%BASEDIR%API\API\API"
set "VIEWROOT=%BASEDIR%View"
set "LOGFILE=%BASEDIR%build_api_view.log"
set "NO_COLOR=1"
set "PIP_NO_COLOR=1"
set "PIP_DISABLE_PIP_VERSION_CHECK=1"
break > "%LOGFILE%"

goto :main

:keepGreen
color 0A >nul
exit /b 0

:main
echo =========================================
echo   BUILD API (.NET) & VIEW (VUE)
echo =========================================
echo.
echo Log chi tiet: %LOGFILE%
echo.

call :requireDir "%APIROOT%" "API" || goto end
call :requireDir "%VIEWROOT%" "View" || goto end

call :checkCmd "dotnet" ".NET SDK" || goto end
call :checkCmd "node" "NodeJS" || goto end

echo [OK] Tat ca cong cu da san sang
echo.

call :setupApi || goto end
call :setupView || goto end

echo.
echo =========================================
echo   BUILD API & VIEW HOAN TAT
echo =========================================
goto end

:checkCmd
set "_cmd=%~1"
set "_name=%~2"
call :keepGreen
echo Kiem tra %_name%...
%_cmd% --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERR] %_name% chua duoc cai hoac chua co trong PATH.
    echo [ERR] Thieu command: %_cmd%>> "%LOGFILE%"
    exit /b 1
)
exit /b 0

:requireDir
set "_dir=%~1"
set "_name=%~2"
if not exist "%_dir%" (
    echo [ERR] Khong tim thay thu muc %_name%: "%_dir%"
    echo [ERR] Missing dir %_name%: "%_dir%">> "%LOGFILE%"
    exit /b 1
)
exit /b 0

:setupApi
call :keepGreen
echo.
echo =========================================
echo RESET DATABASE ASP.NET
echo =========================================

pushd "%APIROOT%" >nul

call :keepGreen
echo Restore NuGet...
dotnet restore
if !errorlevel! neq 0 (
    echo [ERR] Loi dotnet restore
    echo [ERR] dotnet restore that bai>> "%LOGFILE%"
    popd >nul
    exit /b 1
)

call :keepGreen
echo Build project...
dotnet build
if !errorlevel! neq 0 (
    echo [ERR] Loi build project
    echo [ERR] dotnet build that bai>> "%LOGFILE%"
    popd >nul
    exit /b 1
)

dotnet ef --version >nul 2>&1
if !errorlevel! neq 0 (
    echo [ERR] Khong tim thay dotnet-ef.
    echo       Chay lenh nay roi mo lai terminal:
    echo       dotnet tool install --global dotnet-ef
    popd >nul
    exit /b 1
)

call :keepGreen
echo Drop database...
dotnet ef database drop -f
if !errorlevel! neq 0 (
    echo [ERR] Loi drop database
    echo [ERR] dotnet ef database drop that bai>> "%LOGFILE%"
    popd >nul
    exit /b 1
)

call :keepGreen
echo Xoa migrations cu...
if exist "Migrations" (
    rmdir /s /q "Migrations"
)

call :keepGreen
echo Tao migration moi...
dotnet ef migrations add InitialCreate
if !errorlevel! neq 0 (
    echo [ERR] Loi tao migration
    echo [ERR] dotnet ef migrations add that bai>> "%LOGFILE%"
    popd >nul
    exit /b 1
)

call :keepGreen
echo Update database...
dotnet ef database update
if !errorlevel! neq 0 (
    echo [ERR] Loi update database
    echo [ERR] dotnet ef database update that bai>> "%LOGFILE%"
    popd >nul
    exit /b 1
)

popd >nul
call :keepGreen
echo [OK] DATABASE OK
exit /b 0

:setupView
call :keepGreen
echo.
echo =========================================
echo CAI DAT VUE PACKAGE
echo =========================================

pushd "%VIEWROOT%" >nul

call :keepGreen
echo Dang chay npm install...
call npm install
if !errorlevel! neq 0 (
    echo [ERR] Loi khi cai dat NPM package.
    echo       Kiem tra NodeJS, mang internet, package-lock.json.
    echo [ERR] npm install that bai>> "%LOGFILE%"
    popd >nul
    exit /b 1
)

popd >nul
call :keepGreen
echo [OK] VUE PACKAGE CAI DAT THANH CONG
exit /b 0

:end
call :keepGreen
echo.
echo Nhan phim bat ky de thoat...
pause >nul
endlocal