#!/bin/bash

echo "========================================"
echo "WinGet UI Manager v2.0 - Build Script"
echo "========================================"
echo ""

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK is not installed or not in PATH"
    echo "Please install .NET 8.0 or later from https://dotnet.microsoft.com"
    exit 1
fi

echo ".NET SDK found:"
dotnet --version
echo ""

# Determine build configuration
BUILD_CONFIG="Release"
if [ "$1" == "debug" ]; then
    BUILD_CONFIG="Debug"
fi

echo "Building WinGetUI in $BUILD_CONFIG mode..."
echo ""

# Change to WinGetUI directory
cd WinGetUI

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean --configuration "$BUILD_CONFIG" > /dev/null 2>&1

# Restore dependencies
echo "Restoring NuGet packages..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "ERROR: Failed to restore packages"
    cd ..
    exit 1
fi

echo ""

# Build the project
echo "Building project..."
dotnet build --configuration "$BUILD_CONFIG" --no-restore
if [ $? -ne 0 ]; then
    echo "ERROR: Build failed"
    cd ..
    exit 1
fi

cd ..

echo ""
echo "========================================"
echo "Build completed successfully!"
echo "========================================"
echo ""

# Find the built executable
EXECUTABLE=$(find WinGetUI/bin/$BUILD_CONFIG -name "WinGetUI" -type f 2>/dev/null | head -1)

if [ -n "$EXECUTABLE" ]; then
    echo "Output: $EXECUTABLE"
    echo ""
    echo "To run the application, execute:"
    echo "$EXECUTABLE"
else
    echo "Build output location: WinGetUI/bin/$BUILD_CONFIG"
fi

echo ""
if [ "$1" == "run" ]; then
    echo "Starting application..."
    "$EXECUTABLE" &
fi
