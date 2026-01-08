# WinGet UI Manager v2.0

A modern, feature-rich Windows GUI application for managing packages using the Windows Package Manager (winget) command-line tool.

## Features

### ✅ Install New Programs
- Search for available packages by name or ID
- View detailed package information
- One-click installation with progress feedback
- Automatic acceptance of package and source agreements

### ✅ Update Programs
- View all packages with available updates
- Update individual packages
- Batch update all packages at once
- Real-time status feedback

### ✅ Browse Installed Programs
- View complete list of all installed packages
- Sort by:
  - **Name** (alphabetical)
  - **ID** (package identifier)
  - **Version** (numeric version comparison)
- Uninstall selected programs

### ✅ Search Programs
- Real-time search by program name or ID
- Quick installation from search results
- Press Enter to search or use the Search button

## System Requirements

- **Windows 10** (Build 19041) or later
- **Windows 11** (Recommended)
- **.NET 6.0** or later
- **Windows Package Manager (winget)** installed
- Administrator privileges for install/update operations

## Project Structure

```
WinGetUI/
├── Models/
│   ├── Package.cs              # Package data model
│   └── OperationResult.cs      # Operation result handling
├── Services/
│   └── WingetService.cs        # Winget integration service
├── Views/
│   ├── InstallView.xaml(.cs)   # Install packages UI
│   ├── UpdateView.xaml(.cs)    # Update packages UI
│   ├── BrowseView.xaml(.cs)    # Browse installed UI
│   └── SearchView.xaml(.cs)    # Search packages UI
├── Assets/                      # Application icons and resources
├── App.xaml(.cs)               # Application entry point
├── MainWindow.xaml(.cs)        # Main application window
└── Package.appxmanifest        # UWP application manifest
```

## Building the Application

### Prerequisites
1. Install [Visual Studio 2022](https://visualstudio.microsoft.com/)
2. Install ".NET Desktop Development" workload
3. Install "Windows 10 SDK" (Build 19041 or later)
4. Ensure Windows Package Manager is installed

### Build Steps

1. **Open in Visual Studio**
   ```
   Open WinGetUI.sln in Visual Studio 2022
   ```

2. **Restore NuGet Packages**
   - Visual Studio will automatically restore packages on load

3. **Build the Project**
   ```
   Build → Build Solution (Ctrl+Shift+B)
   ```

4. **Run the Application**
   ```
   Debug → Start Debugging (F5)
   ```

## Running the Application

### From Visual Studio
- Press `F5` to run with debugging
- Press `Ctrl+F5` to run without debugging

### From Command Line
```powershell
dotnet run --project WinGetUI\WinGetUI.csproj
```

### From Release Build
```powershell
.\bin\Release\net6.0-windows10.0.19041.0\WinGetUI.exe
```

## Usage Guide

### 1. Install New Programs
1. Click the **Install** tab
2. Enter a program name or ID in the search box
3. Click **Search** (or leave empty to show all)
4. Select a program from the list
5. Click **Install Selected Package**
6. Confirm in the dialog

### 2. Update Programs
1. Click the **Update** tab
2. View all programs with available updates
3. Option A: Select a program and click **Update Selected Package**
4. Option B: Click **Update All** to update all packages at once
5. Confirm in the dialog

### 3. Browse Installed Programs
1. Click the **Browse** tab
2. View all installed programs
3. Use sort buttons:
   - **Sort by Name** - Alphabetical order
   - **Sort by ID** - Package ID order
   - **Sort by Version** - Numeric version order
4. Select a program and click **Uninstall Selected** to remove it

### 4. Search Programs
1. Click the **Search** tab
2. Enter a program name or ID
3. Press **Enter** or click **Search**
4. Select from results
5. Click **Install Selected Package** to install

## Features in Detail

### Smart Version Sorting
The Browse tab includes intelligent version number sorting that properly handles multi-part version numbers (e.g., 1.2.3.4), not just alphabetical sorting.

### Real-time Feedback
- Status messages show current operation
- Loading indicators provide visual feedback
- Dialog confirmations prevent accidental operations

### Error Handling
- Graceful error messages for failed operations
- Detailed status updates throughout the process
- Automatic handling of winget edge cases

### Responsive UI
- Non-blocking operations using async/await
- Tab-based interface for easy navigation
- Clean, modern Windows 11 design language

## Keyboard Shortcuts

- **Enter** in Search tab - Execute search
- **Ctrl+Shift+B** in Visual Studio - Build solution
- **F5** in Visual Studio - Start debugging

## Troubleshooting

### Issue: "winget is not recognized"
**Solution**: Install Windows Package Manager from Microsoft Store or via GitHub

### Issue: "Administrator privileges required"
**Solution**: Run the application as Administrator for install/update/uninstall operations

### Issue: "Cannot build project"
**Solution**: 
- Verify .NET 6.0 SDK is installed
- Check Windows SDK version (19041 or later)
- Restore NuGet packages manually: `dotnet restore`

### Issue: Application crashes on startup
**Solution**:
- Verify Windows 10 Build 19041 or later
- Check that Windows App SDK is properly installed
- Try rebuilding: `dotnet clean && dotnet build`

## Architecture

### WingetService
Encapsulates all interactions with winget:
- Executes winget commands asynchronously
- Parses command output into Package objects
- Handles errors gracefully
- Provides methods for: install, update, uninstall, search

### Package Model
Represents a software package with properties:
- `Id` - Unique package identifier
- `Name` - Display name
- `Version` - Current/available version
- `Publisher` - Package publisher
- `Status` - Installation status
- `Description` - Package description

### XAML Views
Four main UI views built with XAML and WinUI 3:
- Each view handles specific functionality
- Uses ObservableCollection for data binding
- Implements proper async/await patterns
- Provides user feedback and confirmations

## Contributing

To extend this application:

1. **Add new package sources**: Modify `WingetService` to support additional sources
2. **Custom sorting**: Add new `IComparer<Package>` implementations in views
3. **Advanced search**: Enhance search with filters and advanced options
4. **Statistics**: Track installation history and usage statistics
5. **Themes**: Customize app appearance with custom XAML themes

## License

MIT License - Feel free to use, modify, and distribute

## Support

For issues, feature requests, or contributions, please refer to the project repository.

## Version History

### v2.0.0 (Current)
- Complete rewrite with modern UWP architecture
- Four main features: Install, Update, Browse, Search
- Advanced sorting capabilities
- Improved error handling and user feedback
- WinUI 3 modern design

### v1.0.0
- Initial release
- Basic winget integration

## Credits

Built with:
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK)
- [WinUI 3](https://github.com/microsoft/microsoft-ui-xaml)
- [Windows Package Manager](https://github.com/microsoft/winget-cli)

---

**Enjoy managing your packages with WinGet UI!** 🚀
