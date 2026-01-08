# WinGet UI Manager - PowerShell Build Script

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [switch]$Run,
    [switch]$Clean,
    [switch]$Restore
)

Write-Host "========================================"
Write-Host "WinGet UI Manager v2.0 - Build Script"
Write-Host "========================================"
Write-Host ""

# Check if .NET SDK is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host ".NET SDK found: $dotnetVersion"
} catch {
    Write-Host "ERROR: .NET SDK is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install .NET 8.0 or later from https://dotnet.microsoft.com"
    exit 1
}

Write-Host ""
Write-Host "Building WinGetUI in $Configuration mode..."
Write-Host ""

# Navigate to project directory
Push-Location "WinGetUI"

try {
    # Clean if requested
    if ($Clean -or $Restore) {
        Write-Host "Cleaning previous builds..."
        & dotnet clean --configuration $Configuration 2>$null
    }

    # Restore dependencies
    Write-Host "Restoring NuGet packages..."
    & dotnet restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to restore packages" -ForegroundColor Red
        exit 1
    }

    Write-Host ""

    # Build the project
    Write-Host "Building project..."
    & dotnet build --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build failed" -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "========================================"
    Write-Host "Build completed successfully!"
    Write-Host "========================================"
    Write-Host ""

    # Find the built executable
    $executable = Get-ChildItem -Path "bin\$Configuration" -Filter "WinGetUI.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

    if ($executable) {
        $fullPath = $executable.FullName
        Write-Host "Output: $fullPath"
        Write-Host ""
        Write-Host "To run the application, execute:"
        Write-Host "$fullPath"
        
        if ($Run) {
            Write-Host ""
            Write-Host "Starting application..."
            & $fullPath
        }
    } else {
        Write-Host "Build output location: bin\$Configuration"
    }

    Write-Host ""
} finally {
    Pop-Location
}
