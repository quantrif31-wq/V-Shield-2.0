@echo off
setlocal EnableExtensions EnableDelayedExpansion
title Setup V-Shield Environment
color 0A

set "BASEDIR=%~dp0"
set "ROOT=%BASEDIR%AI_Runtime"
set "APIROOT=%BASEDIR%API\API\API"
set "VIEWROOT=%BASEDIR%View"
set "PY_CMD="
set "LOGFILE=%BASEDIR%setup_verbose.log"
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
echo        SETUP HE THONG V-SHIELD
echo =========================================
echo.
echo Log chi tiet: %LOGFILE%
echo.

call :requireDir "%ROOT%" "AI_Runtime" || goto end
call :requireDir "%APIROOT%" "API" || goto end
call :requireDir "%VIEWROOT%" "View" || goto end

call :resolvePython || goto end
call :checkCmd "node" "NodeJS" || goto end
call :checkCmd "dotnet" ".NET SDK" || goto end

echo [OK] Tat ca cong cu da san sang
echo.

call :setupPythonProject "%ROOT%\doc_bien_gpu" "DOC_BIEN_GPU" || goto end
call :setupPythonProject "%ROOT%\face_recognition" "FACE_RECOGNITION" || goto end
call :setupPythonProject "%ROOT%\QR_Dong" "QR_DONG" || goto end
call :setupPythonProject "%ROOT%\AI_An_Ninh" "AI_AN_NINH" || goto end

call :setupApi || goto end
call :setupView || goto end

echo.
echo =========================================
echo   TAT CA MOI TRUONG DA CAI DAT THANH CONG
echo =========================================
goto end

:resolvePython
call :keepGreen
echo Kiem tra Python...
py -3.10 --version >nul 2>&1
if %errorlevel%==0 (
    set "PY_CMD=py -3.10"
    for /f %%v in ('py -3.10 -c "import sys;print('.'.join(map(str,sys.version_info[:3])))" 2^>^&1') do set "_pyVer=%%v"
    if /I not "!_pyVer!"=="3.10.11" (
        echo [ERR] Yeu cau dung dung Python 3.10.11. Hien tai la !_pyVer!.
        echo       Hay cai/doi sang Python 3.10.11 roi chay lai.
        echo [ERR] Python version khong dung: !_pyVer!>> "%LOGFILE%"
        exit /b 1
    )
    echo [OK] Su dung Python qua py -3.10 ^(3.10.11^)
    goto :eof
)

echo [ERR] Khong tim thay Python 3.10.11. Hay cai Python 3.10.11 va mo lai terminal.
echo [ERR] Khong tim thay py -3.10>> "%LOGFILE%"
exit /b 1

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

:setupPythonProject
set "_projDir=%~1"
set "_projName=%~2"
call :keepGreen

echo.
echo =========================================
echo TAO MOI TRUONG %_projName%
echo =========================================

call :requireDir "%_projDir%" "%_projName%" || exit /b 1

if not exist "%_projDir%\requirements.txt" (
    echo [ERR] Thieu requirements.txt trong %_projDir%
    echo [ERR] Missing requirements.txt in %_projDir%>> "%LOGFILE%"
    exit /b 1
)

pushd "%_projDir%" >nul

if exist "venv\Scripts\python.exe" (
    for /f "tokens=2 delims= " %%v in ('venv\Scripts\python.exe --version 2^>^&1') do (
        set "_venvVer=%%v"
    )
    if /I not "!_venvVer!"=="3.10.11" (
        echo [INFO] Venv hien tai dung Python !_venvVer! khong dung moc 3.10.11. Dang tao lai...
        rmdir /s /q "venv"
    )
)

if exist "venv\Scripts\python.exe" (
    for /f %%v in ('venv\Scripts\python.exe -c "import sys;print('.'.join(map(str,sys.version_info[:3])))"') do (
        set "_venvExact=%%v"
    )
    if /I not "!_venvExact!"=="3.10.11" (
        echo [INFO] Venv hien tai co version !_venvExact! khong dung moc 3.10.11. Dang tao lai...
        rmdir /s /q "venv"
    )
)

if exist "venv\Scripts\python.exe" (
    echo !_venvVer! | findstr /b /c:"3.10." >nul
    if !errorlevel! neq 0 (
        echo [INFO] Venv hien tai khong tuong thich. Dang tao lai...
        rmdir /s /q "venv"
    )
)

if not exist "venv\Scripts\python.exe" (
    call :keepGreen
    echo Dang tao virtual environment...
    %PY_CMD% -m venv venv
    if !errorlevel! neq 0 (
        echo [ERR] Loi tao venv %_projName%
        echo [ERR] Tao venv that bai: %_projName%>> "%LOGFILE%"
        popd >nul
        exit /b 1
    )
)

call :keepGreen
echo Cap nhat pip...
venv\Scripts\python.exe -m pip install --upgrade pip setuptools wheel
if !errorlevel! neq 0 (
    echo [ERR] Loi cap nhat pip %_projName%
    echo [ERR] Pip upgrade that bai: %_projName%>> "%LOGFILE%"
    popd >nul
    exit /b 1
)

call :keepGreen
echo Cai dat requirements...
set "_reqFile=requirements.txt"
if /I "%_projName%"=="DOC_BIEN_GPU" (
    venv\Scripts\python.exe -m pip install --extra-index-url https://download.pytorch.org/whl/cu118 -r "%_reqFile%"
) else (
    venv\Scripts\python.exe -m pip install -r "%_reqFile%"
)
if !errorlevel! neq 0 goto pip_failed
goto pip_ok

:pip_failed
if /I not "%_projName%"=="FACE_RECOGNITION" goto pip_fail_out
call :keepGreen
echo [INFO] Thu fallback dlib-bin (bo qua build dlib tu source)...
findstr /V /B /I "dlib==" requirements.txt > requirements_nodlib.tmp
venv\Scripts\python.exe -m pip install dlib-bin
if !errorlevel! neq 0 goto pip_fail_out
venv\Scripts\python.exe -m pip install -r requirements_nodlib.tmp
if exist requirements_nodlib.tmp del /q requirements_nodlib.tmp >nul 2>&1
if !errorlevel! neq 0 goto pip_fail_out
goto pip_ok

:pip_fail_out
if exist requirements_nodlib.tmp del /q requirements_nodlib.tmp >nul 2>&1
echo [ERR] Loi pip install %_projName%
echo [ERR] Pip install that bai: %_projName%>> "%LOGFILE%"
popd >nul
exit /b 1

:pip_ok
popd >nul
call :keepGreen
echo [OK] %_projName% OK
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
