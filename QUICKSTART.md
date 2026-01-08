# Quick Start Guide - WinGet UI Manager v2.0

## 5-Minute Setup

### Prerequisites Check
```powershell
# Check Windows version
systeminfo | findstr /i "OS Name"

# Check winget installed
winget --version

# Check .NET SDK installed
dotnet --version
```

All three should show version information. If any fail, see "Prerequisites" section below.

### Build & Run

#### Option 1: PowerShell (Simplest)
```powershell
# Navigate to project directory
cd "path\to\WinGetUI"

# Build and run
.\build.ps1 -Run
```

#### Option 2: Command Prompt
```batch
cd "path\to\WinGetUI"
build.bat run
```

#### Option 3: Visual Studio
```
File → Open → Folder → select WinGetUI folder
Build → Build Solution (Ctrl+Shift+B)
Press F5 to run
```

#### Option 4: Direct dotnet
```powershell
cd "path\to\WinGetUI"
dotnet run --project WinGetUI\WinGetUI.csproj
```

## Prerequisites Installation

### If .NET 8.0 Not Installed
```powershell
# Download and install
# https://dotnet.microsoft.com/download

# Verify after installation
dotnet --version
```

### If winget Not Installed
```powershell
# Option 1: Microsoft Store
# Search for "Windows Package Manager" in Microsoft Store

# Option 2: GitHub
# https://github.com/microsoft/winget-cli/releases
# Download latest release and install

# Verify after installation
winget --version
```

### If Visual Studio Not Installed (Optional)
- Download: https://visualstudio.microsoft.com/
- During installation, select ".NET Desktop Development"
- Install Windows SDK (Build 19041 or later)

## Using the Application

### Tab 1: Install
1. Type program name (e.g., "Visual Studio Code")
2. Click **Search**
3. Select from results
4. Click **Install Selected Package**
5. Confirm the dialog

### Tab 2: Update
1. View programs with available updates
2. Select a program
3. Click **Update Selected** OR click **Update All**
4. Confirm when prompted

### Tab 3: Browse
1. View all installed programs
2. Click **Sort by Name**, **Sort by ID**, or **Sort by Version**
3. Select program to uninstall
4. Click **Uninstall Selected**

### Tab 4: Search
1. Type program name or ID
2. Press **Enter** or click **Search**
3. Results appear below
4. Select and click **Install Selected Package**

## Common Commands

```powershell
# Build only
.\build.ps1

# Build Debug version
.\build.ps1 -Configuration Debug

# Clean build
.\build.ps1 -Clean

# Build and run immediately
.\build.ps1 -Run
```

## Troubleshooting

### "App crashes on startup"
1. Ensure Windows 10 Build 19041+ or Windows 11
2. Install winget: `winget --version`
3. Run as Administrator
4. Try rebuilding: `.\build.ps1 -Clean`

### "Cannot find dotnet"
1. Install .NET 8.0 SDK
2. Close and reopen terminal
3. Verify: `dotnet --version`

### "Build fails with package errors"
```powershell
# Clear cache and restore
dotnet nuget locals all --clear
dotnet restore WinGetUI\WinGetUI.csproj
dotnet build WinGetUI\WinGetUI.csproj
```

### "winget: command not found"
1. Install Windows Package Manager from Microsoft Store
2. Restart computer
3. Verify: `winget --version`

## File Locations

| File | Purpose |
|------|---------|
| `WinGetUI.sln` | Solution file (Visual Studio) |
| `WinGetUI/WinGetUI.csproj` | Project file |
| `WinGetUI/MainWindow.xaml` | Main UI window |
| `WinGetUI/Services/WingetService.cs` | Core logic |
| `WinGetUI/Models/` | Data models |
| `WinGetUI/Views/` | UI views |
| `README.md` | User documentation |
| `DEVELOPMENT.md` | Developer guide |
| `ARCHITECTURE.md` | Technical details |
| `build.bat` | Windows batch build script |
| `build.ps1` | PowerShell build script |

## Key Features

✅ **Install** - Search and install new programs
✅ **Update** - Update installed programs (single or batch)
✅ **Browse** - View and sort installed programs
✅ **Search** - Find programs by name or ID
✅ **Sort Options** - Name, ID, Version
✅ **Real-time Feedback** - Status messages and progress
✅ **Error Handling** - Graceful error messages
✅ **Async Operations** - Never blocks UI

## Architecture

```
User Interface (XAML Views)
        ↓
View Code-Behind (Event Handlers)
        ↓
WingetService (Business Logic)
        ↓
Process.Start (Execute winget.exe)
        ↓
Parse Output → Package Objects
        ↓
Display Results
```

## Next Steps

1. **Familiarize yourself** with the four tabs
2. **Read DEVELOPMENT.md** for detailed setup
3. **Read ARCHITECTURE.md** for technical details
4. **Try features** - Install, update, browse, search
5. **Customize** - Modify colors, layout, features

## Support Resources

- **Windows App SDK**: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/
- **WinUI 3**: https://docs.microsoft.com/en-us/windows/apps/winui/
- **winget**: https://github.com/microsoft/winget-cli/
- **Project Repo**: [GitHub repository link]

## Tips & Tricks

💡 In Search tab: Press **Enter** to search (no button click needed)

💡 In Browse tab: Click sort buttons multiple times to toggle sort order

💡 In Update tab: Use **Update All** to keep everything current

💡 Always run as **Administrator** for install/update operations

💡 If packages list is slow, **winget** is communicating with servers

---

**That's it! Enjoy managing your packages.** 🚀

For more detailed information, see README.md or DEVELOPMENT.md
