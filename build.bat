@echo off
setlocal enabledelayedexpansion

echo ========================================
echo WinGet UI Manager v2.0 - Build Script
echo ========================================
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 8.0 or later from https://dotnet.microsoft.com
    exit /b 1
)

echo .NET SDK found:
dotnet --version
echo.

REM Determine build configuration
set BUILD_CONFIG=Release
if "%1"=="debug" set BUILD_CONFIG=Debug

echo Building WinGetUI in %BUILD_CONFIG% mode...
echo.

REM Change to WinGetUI directory
cd WinGetUI

REM Clean previous builds
echo Cleaning previous builds...
dotnet clean --configuration %BUILD_CONFIG% >nul 2>&1

REM Restore dependencies
echo Restoring NuGet packages...
dotnet restore
if errorlevel 1 (
    echo ERROR: Failed to restore packages
    cd ..
    exit /b 1
)

echo.

REM Build the project
echo Building project...
dotnet build --configuration %BUILD_CONFIG% --no-restore
if errorlevel 1 (
    echo ERROR: Build failed
    cd ..
    exit /b 1
)

cd ..

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo.

REM Find the built executable
for /r "WinGetUI\bin\%BUILD_CONFIG%" %%F in (WinGetUI.exe) do (
    set EXECUTABLE=%%F
)

if defined EXECUTABLE (
    echo Output: !EXECUTABLE!
    echo.
    echo To run the application, execute:
    echo !EXECUTABLE!
) else (
    echo Build output location: WinGetUI\bin\%BUILD_CONFIG%
)

echo.
if "%1"=="run" (
    echo Starting application...
    start "" "!EXECUTABLE!"
)

endlocal
