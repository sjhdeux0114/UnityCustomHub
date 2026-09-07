@echo off
setlocal enabledelayedexpansion

echo ========================================================
echo   UnityHubCustom Build Script
echo ========================================================

set "MSBUILD="

if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
)
if "%MSBUILD%"=="" if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
)
if "%MSBUILD%"=="" if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
)

if "%MSBUILD%"=="" (
    echo [ERROR] MSBuild.exe not found!
    pause
    exit /b 1
)

echo [INFO] Found MSBuild: "%MSBUILD%"
echo [INFO] Building Release...

"%MSBUILD%" "%~dp0UnityHubCustom.csproj" /p:Configuration=Release /t:Rebuild /v:m

if errorlevel 1 (
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

echo.
echo ========================================================
echo [SUCCESS] Build completed!
echo Executable: %~dp0bin\Release\UnityHubCustom.exe
echo ========================================================
