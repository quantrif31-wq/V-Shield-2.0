@echo on
setlocal EnableExtensions EnableDelayedExpansion
title Setup V-Shield Environment
color 0A

set "BASEDIR=%~dp0"
set "ROOT=%BASEDIR%AI_Project"
set "APIROOT=%BASEDIR%API\API\API"
set "VIEWROOT=%BASEDIR%View"
set "PY_CMD="

echo =========================================
echo        SETUP HE THONG V-SHIELD
echo =========================================
echo.

call :requireDir "%ROOT%" "AI_Project" || goto end
call :requireDir "%APIROOT%" "API" || goto end
call :requireDir "%VIEWROOT%" "View" || goto end

call :resolvePython || goto end
call :checkCmd "node" "NodeJS" || goto end
call :checkCmd "dotnet" ".NET SDK" || goto end

echo [OK] Tat ca cong cu da san sang
echo.

call :setupPythonProject "%ROOT%\doc_bien_gpu" "DOC_BIEN_GPU" || goto end
call :setupPythonProject "%ROOT%\face_recognition" "FACE_RECOGNITION" || goto end

call :setupApi || goto end
call :setupView || goto end

echo.
echo =========================================
echo   TAT CA MOI TRUONG DA CAI DAT THANH CONG
echo =========================================
goto end

:resolvePython
echo Kiem tra Python...
py -3.11 --version >nul 2>&1
if %errorlevel%==0 (
    set "PY_CMD=py -3.11"
    echo [OK] Su dung Python qua py -3.11
    goto :eof
)

py -3.10 --version >nul 2>&1
if %errorlevel%==0 (
    set "PY_CMD=py -3.10"
    echo [OK] Su dung Python qua py -3.10
    goto :eof
)

py -3.12 --version >nul 2>&1
if %errorlevel%==0 (
    set "PY_CMD=py -3.12"
    echo [OK] Su dung Python qua py -3.12
    goto :eof
)

python --version >nul 2>&1
if %errorlevel%==0 (
    set "PY_CMD=python"
    echo [OK] Su dung Python qua python
    goto :eof
)

echo [ERR] Khong tim thay Python. Hay cai Python 3.10+ va mo lai terminal.
exit /b 1

:checkCmd
set "_cmd=%~1"
set "_name=%~2"
echo Kiem tra %_name%...
%_cmd% --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERR] %_name% chua duoc cai hoac chua co trong PATH.
    exit /b 1
)
exit /b 0

:requireDir
set "_dir=%~1"
set "_name=%~2"
if not exist "%_dir%" (
    echo [ERR] Khong tim thay thu muc %_name%: "%_dir%"
    exit /b 1
)
exit /b 0

:setupPythonProject
set "_projDir=%~1"
set "_projName=%~2"

echo.
echo =========================================
echo TAO MOI TRUONG %_projName%
echo =========================================

call :requireDir "%_projDir%" "%_projName%" || exit /b 1

if not exist "%_projDir%\requirements.txt" (
    echo [ERR] Thieu requirements.txt trong %_projDir%
    exit /b 1
)

pushd "%_projDir%" >nul

if exist "venv\Scripts\python.exe" (
    for /f "tokens=2 delims= " %%v in ('venv\Scripts\python.exe --version 2^>^&1') do (
        set "_venvVer=%%v"
    )
    echo !_venvVer! | findstr /b /c:"3.10." /c:"3.11." /c:"3.12." >nul
    if !errorlevel! neq 0 (
        echo [WARN] Venv hien tai dung Python !_venvVer! khong tuong thich. Dang tao lai...
        rmdir /s /q "venv"
    )
)

if not exist "venv\Scripts\python.exe" (
    echo Dang tao virtual environment...
    %PY_CMD% -m venv venv
    if !errorlevel! neq 0 (
        echo [ERR] Loi tao venv %_projName%
        popd >nul
        exit /b 1
    )
)

echo Cap nhat pip...
venv\Scripts\python.exe -m pip install --upgrade pip setuptools wheel
if !errorlevel! neq 0 (
    echo [ERR] Loi cap nhat pip %_projName%
    popd >nul
    exit /b 1
)

echo Cai dat requirements...
if /I "%_projName%"=="DOC_BIEN_GPU" (
    venv\Scripts\python.exe -m pip install --extra-index-url https://download.pytorch.org/whl/cu118 -r requirements.txt
) else (
    venv\Scripts\python.exe -m pip install -r requirements.txt
)
if !errorlevel! neq 0 (
    if /I "%_projName%"=="FACE_RECOGNITION" (
        echo [WARN] Thu fallback dlib-bin (bo qua build dlib tu source)...
        findstr /V /B /I "dlib==" requirements.txt > requirements_nodlib.tmp
        venv\Scripts\python.exe -m pip install dlib-bin
        if !errorlevel! equ 0 (
            venv\Scripts\python.exe -m pip install -r requirements_nodlib.tmp
        )
        if exist requirements_nodlib.tmp del /q requirements_nodlib.tmp >nul 2>&1
    )
)
if !errorlevel! neq 0 (
    echo [ERR] Loi pip install %_projName%
    popd >nul
    exit /b 1
)

popd >nul
echo [OK] %_projName% OK
exit /b 0

:setupApi
echo.
echo =========================================
echo RESET DATABASE ASP.NET
echo =========================================

pushd "%APIROOT%" >nul

echo Restore NuGet...
dotnet restore
if !errorlevel! neq 0 (
    echo [ERR] Loi dotnet restore
    popd >nul
    exit /b 1
)

echo Build project...
dotnet build
if !errorlevel! neq 0 (
    echo [ERR] Loi build project
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

echo Drop database...
dotnet ef database drop -f
if !errorlevel! neq 0 (
    echo [ERR] Loi drop database
    popd >nul
    exit /b 1
)

echo Xoa migrations cu...
if exist "Migrations" (
    rmdir /s /q "Migrations"
)

echo Tao migration moi...
dotnet ef migrations add InitialCreate
if !errorlevel! neq 0 (
    echo [ERR] Loi tao migration
    popd >nul
    exit /b 1
)

echo Update database...
dotnet ef database update
if !errorlevel! neq 0 (
    echo [ERR] Loi update database
    popd >nul
    exit /b 1
)

popd >nul
echo [OK] DATABASE OK
exit /b 0

:setupView
echo.
echo =========================================
echo CAI DAT VUE PACKAGE
echo =========================================

pushd "%VIEWROOT%" >nul

echo Dang chay npm install...
call npm install
if !errorlevel! neq 0 (
    echo [ERR] Loi khi cai dat NPM package.
    echo       Kiem tra NodeJS, mang internet, package-lock.json.
    popd >nul
    exit /b 1
)

popd >nul
echo [OK] VUE PACKAGE CAI DAT THANH CONG
exit /b 0

:end
echo.
echo Nhan phim bat ky de thoat...
pause >nul
endlocal

