@echo off
setlocal
cd /d "%~dp0"
echo Starting UniLauncher build...
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-Windows.ps1"
if errorlevel 1 (
    echo.
    echo BUILD FAILED. Copy the complete error text and send it to ChatGPT.
    echo.
    pause
    exit /b 1
)
echo.
echo Build completed. Check the dist folder.
pause
