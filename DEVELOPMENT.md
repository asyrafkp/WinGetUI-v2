# WinGet UI Manager - Development Setup Guide

## Quick Start

### 1. Prerequisites Installation

#### Windows 10/11
Ensure you have:
- **Windows 10 Build 19041** or later, or **Windows 11**
- **Administrator account** (required for package operations)

#### Install Required Tools

1. **Install .NET 8.0 SDK**
   - Download from: https://dotnet.microsoft.com/download
   - Run the installer and follow prompts
   - Verify installation:
     ```powershell
     dotnet --version
     ```

2. **Install Windows Package Manager (winget)**
   - Download from Microsoft Store: https://apps.microsoft.com/detail/9NBLGGH4NNS1
   - Or install from GitHub: https://github.com/microsoft/winget-cli/releases
   - Verify installation:
     ```powershell
     winget --version
     ```

3. **Install Visual Studio 2022 (Optional but Recommended)**
   - Download from: https://visualstudio.microsoft.com/
   - Install "Desktop Development with C++" workload
   - Select ".NET Desktop Development" workload
   - Install Windows 10 SDK (Build 19041 or later)

4. **Install Git (Optional)**
   - Download from: https://git-scm.com/
   - Needed if cloning from repository

### 2. Project Setup

#### Clone or Extract Project
```powershell
# If using git
git clone <repository-url>
cd WinGetUI

# Or if extracted from zip
cd path\to\WinGetUI
```

#### Verify Project Structure
```powershell
# List key directories
dir WinGetUI\
dir WinGetUI\Models
dir WinGetUI\Services
dir WinGetUI\Views
```

### 3. Building the Project

#### Option A: Using PowerShell Script (Recommended)
```powershell
# Run with default settings (Release build)
.\build.ps1

# Run with Debug configuration
.\build.ps1 -Configuration Debug

# Build and immediately run the app
.\build.ps1 -Run

# Clean and rebuild
.\build.ps1 -Clean
```

#### Option B: Using Command Prompt
```batch
# Run the batch script
build.bat

# Or build with debug configuration
build.bat debug

# Or build and run
build.bat run
```

#### Option C: Using dotnet CLI
```powershell
# Restore dependencies
dotnet restore WinGetUI\WinGetUI.csproj

# Build the project
dotnet build WinGetUI\WinGetUI.csproj --configuration Release

# Run the application
dotnet run --project WinGetUI\WinGetUI.csproj
```

#### Option D: Using Visual Studio 2022
1. Open `WinGetUI.sln` in Visual Studio
2. Select Solution Configuration: "Release" or "Debug"
3. Press `Ctrl+Shift+B` to build
4. Press `F5` to run with debugging or `Ctrl+F5` without debugging

### 4. Running the Application

#### After Successful Build
The executable will be located at:
```
WinGetUI\bin\Release\net8.0-windows10.0.19041.0\WinGetUI.exe
```

#### Run from Command Line
```powershell
cd WinGetUI\bin\Release\net8.0-windows10.0.19041.0\
.\WinGetUI.exe
```

#### Run from Visual Studio
- Press `F5` to run with debugging
- Press `Ctrl+F5` to run without debugging

#### Run from File Explorer
1. Navigate to `WinGetUI\bin\Release\net8.0-windows10.0.19041.0\`
2. Double-click `WinGetUI.exe`

### 5. Troubleshooting Build Issues

#### Issue: "dotnet: command not found"
**Solution**: 
- Restart your terminal after installing .NET SDK
- Or add .NET to PATH manually
- Verify with: `dotnet --version`

#### Issue: "The SDK 'Microsoft.NET.Sdk.WindowsDesktop' cannot be found"
**Solution**:
- Ensure .NET 8.0 SDK is installed (not just runtime)
- Run: `dotnet workload install desktop`
- Restore packages: `dotnet restore WinGetUI\WinGetUI.csproj`

#### Issue: "Package Microsoft.WindowsAppSDK not found"
**Solution**:
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Restore packages: `dotnet restore WinGetUI\WinGetUI.csproj`
- Try offline if network is slow

#### Issue: Build succeeds but app crashes on launch
**Solution**:
- Ensure Windows 10 Build 19041 or later
- Check Windows App SDK installation
- Run as Administrator
- Verify winget is installed

#### Issue: "Access denied" errors during build
**Solution**:
- Close any running instances of WinGetUI
- Clear bin/obj folders: `dotnet clean`
- Run terminal as Administrator
- Try building again

## Development Workflow

### 1. Making Code Changes

#### Edit C# Code
```
WinGetUI/
├── Models/          → Data models (Package.cs, OperationResult.cs)
├── Services/        → Business logic (WingetService.cs)
├── Views/           → UI pages (Install, Update, Browse, Search)
├── App.xaml(.cs)    → Application startup
└── MainWindow.xaml  → Main window
```

#### Edit XAML UI
- Each view has corresponding `.xaml` and `.xaml.cs` files
- Modify layout, colors, and controls in `.xaml` files
- Add event handlers and logic in `.xaml.cs` files

### 2. Running with Changes

#### Hot Reload (Visual Studio)
1. Make changes to XAML or C#
2. Press `Alt+Shift+F5` to hot reload
3. Changes appear instantly

#### Manual Rebuild and Run
1. Save your changes
2. Press `Ctrl+Shift+B` to build
3. Press `F5` to run

### 3. Debugging

#### Set Breakpoints
1. Click on line number in code editor
2. Press `F9` on the line
3. Red dot appears indicating breakpoint

#### Debug Execution
1. Press `F5` to start debugging
2. Application stops at breakpoints
3. Use Debug toolbar or `F10` (step), `F11` (step into), `Shift+F11` (step out)

#### Debug Console
- Output messages with: `Debug.WriteLine("message");`
- View in Debug → Windows → Output

#### View Variables
- Hover over variables to see values
- Use Debug → Windows → Locals to view all variables
- Use Debug → Windows → Watch to monitor specific values

## Project Structure Details

### Models (`WinGetUI/Models/`)
- **Package.cs** - Represents a software package
  - Properties: Id, Name, Version, Publisher, Status, etc.
  - Implements IComparable for sorting
  
- **OperationResult.cs** - Result of package operations
  - Success/Error status
  - Messages and error details
  - Exit codes from winget

### Services (`WinGetUI/Services/`)
- **WingetService.cs** - Core winget integration
  - Methods: GetInstalledPackagesAsync(), SearchPackagesAsync(), InstallPackageAsync(), etc.
  - Executes winget commands via Process
  - Parses console output into Package objects
  - Handles errors gracefully

### Views (`WinGetUI/Views/`)
- **InstallView.xaml(.cs)** - Install new programs
  - Search functionality
  - Package list display
  - Install button with confirmation
  
- **UpdateView.xaml(.cs)** - Update existing programs
  - List of upgradable packages
  - Single and batch update
  - Refresh button
  
- **BrowseView.xaml(.cs)** - Browse installed programs
  - View all installed packages
  - Sort by Name, ID, Version
  - Uninstall functionality
  
- **SearchView.xaml(.cs)** - Search programs
  - Real-time search
  - Search by name or ID
  - Quick install from results

## Common Development Tasks

### Add a New Feature to Install View
1. Edit `Views/InstallView.xaml` to add UI elements
2. Add event handlers to `Views/InstallView.xaml.cs`
3. Add methods to `Services/WingetService.cs` if needed
4. Test the feature

### Add Sorting Option to Browse View
1. Create new `IComparer<Package>` in `Views/BrowseView.xaml.cs`
2. Add sort button to XAML
3. Implement click handler that applies sorting
4. Test the sorting

### Modify Package Model
1. Add property to `Models/Package.cs`
2. Update parsing in `WingetService.ParsePackageList()`
3. Update XAML templates to display new property
4. Test with actual packages

### Add Error Handling
1. Wrap code in try-catch blocks
2. Log errors with `Debug.WriteLine()`
3. Show user-friendly messages in dialogs
4. Test error scenarios

## Performance Tips

### Async Operations
- All winget calls use async/await
- Never block UI thread
- Use `LoadingProgress` indicators during operations

### Caching
- Store search results to avoid repeated queries
- Implement caching in `WingetService` for frequently accessed data

### UI Virtualization
- ListView uses data virtualization by default
- Large lists render efficiently

## Testing

### Manual Testing Checklist
- [ ] Install a package
- [ ] Update a package
- [ ] Update all packages
- [ ] Browse and sort packages
- [ ] Search for packages by name
- [ ] Search for packages by ID
- [ ] Uninstall a package
- [ ] Confirm dialogs work correctly
- [ ] Error handling displays properly

### Edge Cases to Test
- No packages available
- Search with no results
- Install/update fails
- winget not installed
- Invalid package IDs
- Network interruptions

## Publishing

### Create Release Build
```powershell
dotnet publish WinGetUI\WinGetUI.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true
```

### Package for Distribution
1. Build release version
2. Create installer (MSI/MSIX) using WiX Toolset
3. Sign with code signing certificate
4. Distribute via GitHub Releases or Microsoft Store

## Additional Resources

- [Windows App SDK Documentation](https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- [WinUI 3 Documentation](https://docs.microsoft.com/en-us/windows/apps/winui/)
- [Windows Package Manager Documentation](https://docs.microsoft.com/en-us/windows/package-manager/)
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [XAML Basics](https://docs.microsoft.com/en-us/learn/modules/write-your-first-xaml-app/)

## Getting Help

### Common Issues and Solutions

**Q: Build fails with "Microsoft.WindowsAppSDK not found"**
A: Run `dotnet workload install windowsdesktop` then `dotnet restore`

**Q: App crashes immediately on startup**
A: Ensure Windows 10 Build 19041+, winget installed, and running as admin

**Q: Search/Install/Update buttons don't work**
A: Verify winget is installed: `winget --version`

**Q: Slow performance when listing packages**
A: Normal behavior with many packages. Add caching for improvements.

---

**Happy developing!** 🚀 For issues, check the main [README.md](README.md)
