@echo off
set "EXE=%~dp0bin\Release\UnityHubCustom.exe"
if not exist "%EXE%" (
    echo Executable not found. Building first...
    call "%~dp0build.bat"
)
if exist "%EXE%" (
    start "" "%EXE%"
)
