using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using WinGetUI.Models;
using WinGetUI.Services;

namespace WinGetUI.Views
{
    public enum UnifiedMode
    {
        Install,
        Update,
        Browse,
        Search
    }

    public partial class UnifiedView : UserControl
    {
        private WingetService _wingetService;
        public ObservableCollection<Package> Packages { get; }
        private UnifiedMode _currentMode = UnifiedMode.Install;
        private bool _isShowingAllInstalled = false;

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(UnifiedView), new PropertyMetadata(false));

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public UnifiedView()
        {
            try
            {
                this.InitializeComponent();
                _wingetService = new WingetService();
                Packages = new ObservableCollection<Package>();
                this.DataContext = this;
                UpdateUIForMode();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing UnifiedView: {ex.Message}\n\nStackTrace: {ex.StackTrace}", "UnifiedView Error");
                throw;
            }
        }

        private void UpdateUIForMode()
        {
            try
            {
                // Update search panel visibility
                SearchPanel.Visibility = (_currentMode == UnifiedMode.Install || _currentMode == UnifiedMode.Search) 
                    ? Visibility.Visible : Visibility.Collapsed;
                
                // Update search all button visibility (only for Search mode)
                SearchAllButton.Visibility = (_currentMode == UnifiedMode.Search) 
                    ? Visibility.Visible : Visibility.Collapsed;

                // Update update view panel visibility
                UpdateViewPanel.Visibility = (_currentMode == UnifiedMode.Update) 
                    ? Visibility.Visible : Visibility.Collapsed;

                // Update action buttons
                InstallButton.Visibility = (_currentMode == UnifiedMode.Install || _currentMode == UnifiedMode.Search) 
                    ? Visibility.Visible : Visibility.Collapsed;
                UpdateButton.Visibility = (_currentMode == UnifiedMode.Update) 
                    ? Visibility.Visible : Visibility.Collapsed;
                UninstallButton.Visibility = (_currentMode == UnifiedMode.Browse || _currentMode == UnifiedMode.Update || _currentMode == UnifiedMode.Search) 
                    ? Visibility.Visible : Visibility.Collapsed;

                // Update column visibility
                if (PackagesGrid != null && PackagesGrid.Columns != null)
                {
                    var availableCol = PackagesGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "Available Version");
                    var sourceCol = PackagesGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "Source");
                    var currentVersionCol = PackagesGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "Current Version");

                    if (availableCol != null)
                    {
                        availableCol.Visibility = (_currentMode == UnifiedMode.Update && !_isShowingAllInstalled) 
                            ? Visibility.Visible : Visibility.Collapsed;
                    }

                    if (sourceCol != null)
                    {
                        sourceCol.Visibility = (_currentMode == UnifiedMode.Install || _currentMode == UnifiedMode.Browse || _currentMode == UnifiedMode.Search) 
                            ? Visibility.Visible : Visibility.Collapsed;
                    }

                    if (currentVersionCol != null)
                    {
                        // In Update mode showing updatable packages, show as "Current Version"
                        // Otherwise show as "Version"
                        if (_currentMode == UnifiedMode.Update && !_isShowingAllInstalled)
                        {
                            currentVersionCol.Header = "Current Version";
                            currentVersionCol.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            currentVersionCol.Header = "Version";
                            currentVersionCol.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating UI for mode: {ex.Message}");
            }
        }

        private async void ModeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (InstallModeRadio.IsChecked == true)
            {
                _currentMode = UnifiedMode.Install;
                UpdateUIForMode();
                await LoadInstallMode();
            }
            else if (UpdateModeRadio.IsChecked == true)
            {
                _currentMode = UnifiedMode.Update;
                UpdateUIForMode();
                await LoadUpdateMode();
            }
            else if (BrowseModeRadio.IsChecked == true)
            {
                _currentMode = UnifiedMode.Browse;
                UpdateUIForMode();
                await LoadBrowseMode();
            }
            else if (SearchModeRadio.IsChecked == true)
            {
                _currentMode = UnifiedMode.Search;
                UpdateUIForMode();
                if (Packages != null)
                    Packages.Clear();
                if (StatusText != null)
                    StatusText.Text = "Enter a search term and click Search";
            }
        }

        private async void UpdateViewModeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (UpdatableViewRadio.IsChecked == true)
            {
                _isShowingAllInstalled = false;
                await LoadUpdatablePackages();
            }
            else if (AllInstalledViewRadio.IsChecked == true)
            {
                _isShowingAllInstalled = true;
                await LoadAllInstalledPackages();
            }
            UpdateUIForMode();
        }

        // Install Mode
        private async Task LoadInstallMode()
        {
            if (StatusText != null)
                StatusText.Text = "Ready - Enter a search term to find packages";
            if (Packages != null)
                Packages.Clear();
            UpdateSelectionStatus();
        }

        // Update Mode
        private async Task LoadUpdateMode()
        {
            if (_isShowingAllInstalled)
            {
                await LoadAllInstalledPackages();
            }
            else
            {
                await LoadUpdatablePackages();
            }
        }

        private async Task LoadUpdatablePackages()
        {
            if (_wingetService == null)
            {
                if (StatusText != null)
                    StatusText.Text = "Service not initialized. Please wait...";
                return;
            }

            if (StatusText != null)
                StatusText.Text = "Loading updatable packages...";
            if (Packages != null)
                Packages.Clear();
            UpdateSelectionStatus();

            try
            {
                IsLoading = true;
                var packages = await _wingetService.GetUpgradablePackagesAsync();
                
                if (Packages != null)
                {
                    foreach (var pkg in packages.OrderBy(p => p.Name))
                    {
                        Packages.Add(pkg);
                    }

                    if (StatusText != null)
                        StatusText.Text = $"Found {Packages.Count} packages with updates available";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadAllInstalledPackages()
        {
            if (_wingetService == null)
            {
                if (StatusText != null)
                    StatusText.Text = "Service not initialized. Please wait...";
                return;
            }

            if (StatusText != null)
                StatusText.Text = "Loading all installed packages...";
            if (Packages != null)
                Packages.Clear();
            UpdateSelectionStatus();

            try
            {
                IsLoading = true;
                var packages = await _wingetService.GetInstalledPackagesAsync();
                
                if (Packages != null)
                {
                    foreach (var pkg in packages.OrderBy(p => p.Name))
                    {
                        Packages.Add(pkg);
                    }

                    if (StatusText != null)
                        StatusText.Text = $"Found {Packages.Count} installed packages";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Browse Mode
        private async Task LoadBrowseMode()
        {
            await LoadInstalledPackages();
        }

        private async Task LoadInstalledPackages()
        {
            if (_wingetService == null)
            {
                if (StatusText != null)
                    StatusText.Text = "Service not initialized. Please wait...";
                return;
            }

            if (StatusText != null)
                StatusText.Text = "Loading installed packages...";
            if (Packages != null)
                Packages.Clear();
            UpdateSelectionStatus();

            var packages = await _wingetService.GetInstalledPackagesAsync();
            
            if (Packages != null)
            {
                foreach (var pkg in packages.OrderBy(p => p.Name))
                {
                    Packages.Add(pkg);
                }

                if (StatusText != null)
                    StatusText.Text = $"Found {Packages.Count} installed packages";
            }
        }

        // Search functionality
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMode == UnifiedMode.Install || _currentMode == UnifiedMode.Search)
            {
                await PerformSearch();
            }
        }

        private async void SearchAllButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformSearchAll();
        }

        private async Task PerformSearch()
        {
            if (_wingetService == null)
            {
                if (StatusText != null)
                    StatusText.Text = "Service not initialized. Please wait...";
                return;
            }

            string searchTerm = SearchBox.Text?.Trim() ?? "";
            
            if (_currentMode == UnifiedMode.Install && string.IsNullOrEmpty(searchTerm))
            {
                if (StatusText != null)
                    StatusText.Text = "Enter a search term to find packages";
                return;
            }

            if (_currentMode == UnifiedMode.Search && string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("Please enter a search term", "Search");
                return;
            }

            if (StatusText != null)
                StatusText.Text = $"Searching for '{searchTerm}'...";
            if (Packages != null)
                Packages.Clear();
            UpdateSelectionStatus();

            try
            {
                IsLoading = true;
                SearchFilterType filterType = FilterCombo.SelectedIndex switch
                {
                    0 => SearchFilterType.Name,
                    1 => SearchFilterType.Id,
                    _ => SearchFilterType.Both
                };

                bool exactMatch = MatchCombo.SelectedIndex == 1;

                var packages = await _wingetService.SearchPackagesAdvancedAsync(searchTerm ?? "", filterType, exactMatch);
                
                if (Packages != null)
                {
                    foreach (var pkg in packages)
                    {
                        Packages.Add(pkg);
                    }

                    string matchType = exactMatch ? "exactly" : "containing";
                    string filterText = filterType switch
                    {
                        SearchFilterType.Name => "by Name",
                        SearchFilterType.Id => "by ID",
                        _ => "by Name and ID"
                    };

                    if (StatusText != null)
                        StatusText.Text = $"Found {Packages.Count} packages {filterText} {matchType} '{searchTerm}'";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PerformSearchAll()
        {
            if (_wingetService == null)
            {
                if (StatusText != null)
                    StatusText.Text = "Service not initialized. Please wait...";
                return;
            }

            if (StatusText != null)
                StatusText.Text = "Searching all available packages...";
            if (Packages != null)
                Packages.Clear();
            UpdateSelectionStatus();

            try
            {
                IsLoading = true;
                var packages = await _wingetService.SearchPackagesAsync("");
                
                if (Packages != null)
                {
                    foreach (var pkg in packages)
                    {
                        Packages.Add(pkg);
                    }

                    if (StatusText != null)
                        StatusText.Text = $"Found {Packages.Count} available packages in winget";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && (_currentMode == UnifiedMode.Install || _currentMode == UnifiedMode.Search))
            {
                await PerformSearch();
            }
        }

        // Install functionality
        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (Packages == null) return;
            var selectedPackages = Packages.Where(p => p.IsSelected).ToList();
            
            if (selectedPackages.Count == 0)
            {
                MessageBox.Show("Please select at least one package to install", "Install");
                return;
            }

            var packageNames = string.Join(", ", selectedPackages.Select(p => $"{p.Name} ({p.Id})"));
            var result = MessageBox.Show(
                $"Install {selectedPackages.Count} package(s)?\n\n{packageNames}",
                "Confirm Installation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                bool silent = SilentModeRadio.IsChecked == true;
                if (silent)
                {
                    await InstallSelectedPackagesSilent(selectedPackages);
                }
                else
                {
                    await InstallSelectedPackagesWithProgress(selectedPackages);
                }
            }
        }

        private async Task InstallSelectedPackagesWithProgress(List<Package> packages)
        {
            int successCount = 0;
            int failureCount = 0;
            var mainWindow = Window.GetWindow(this) as MainWindow;

            mainWindow?.UpdateOperationStatus($"Installing {packages.Count} package(s)...");
            
            for (int i = 0; i < packages.Count; i++)
            {
                var pkg = packages[i];
                try
                {
                    int progressPercent = (i * 100) / packages.Count;
                    mainWindow?.UpdateOperationProgress(progressPercent, $"Installing {pkg.Name}...");
                    if (StatusText != null)
                        StatusText.Text = $"Installing {i + 1}/{packages.Count}: {pkg.Name}";

                    var opResult = await _wingetService.InstallPackageAsync(pkg.Id, false);
                    if (opResult.Success)
                    {
                        successCount++;
                        pkg.IsSelected = false;
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch
                {
                    failureCount++;
                }
            }

            mainWindow?.UpdateOperationProgress(100, "Installation complete");

            string summary = $"✓ {successCount} successful";
            if (failureCount > 0)
                summary += $", ✗ {failureCount} failed";

            if (StatusText != null)
                StatusText.Text = summary;
            MessageBox.Show(summary, "Installation Complete", MessageBoxButton.OK, 
                           failureCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            
            mainWindow?.ResetProgress();
            UpdateSelectionStatus();
        }

        private async Task InstallSelectedPackagesSilent(List<Package> packages)
        {
            int successCount = 0;
            int failureCount = 0;
            var mainWindow = Window.GetWindow(this) as MainWindow;

            mainWindow?.UpdateOperationStatus($"Installing {packages.Count} package(s) silently...");

            for (int i = 0; i < packages.Count; i++)
            {
                var pkg = packages[i];
                try
                {
                    int progressPercent = (i * 100) / packages.Count;
                    mainWindow?.UpdateOperationProgress(progressPercent, $"Installing {pkg.Name}...");
                    if (StatusText != null)
                        StatusText.Text = $"Installing {i + 1}/{packages.Count}: {pkg.Name} (Silent)";

                    var opResult = await _wingetService.InstallPackageAsync(pkg.Id, true);
                    if (opResult.Success)
                    {
                        successCount++;
                        pkg.IsSelected = false;
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch
                {
                    failureCount++;
                }
            }

            mainWindow?.UpdateOperationProgress(100, "Installation complete");

            string summary = $"✓ {successCount} successful";
            if (failureCount > 0)
                summary += $", ✗ {failureCount} failed";

            if (StatusText != null)
                StatusText.Text = summary;
            MessageBox.Show(summary, "Installation Complete", MessageBoxButton.OK, 
                           failureCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            
            mainWindow?.ResetProgress();
            UpdateSelectionStatus();
        }

        // Update functionality
        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (Packages == null) return;
            var selectedPackages = Packages.Where(p => p.IsSelected).ToList();
            
            if (selectedPackages.Count == 0)
            {
                MessageBox.Show("Please select at least one package to update", "Update");
                return;
            }

            var packageNames = string.Join(", ", selectedPackages.Select(p => $"{p.Name} ({p.Id})"));
            var result = MessageBox.Show(
                $"Update {selectedPackages.Count} package(s)?\n\n{packageNames}",
                "Confirm Update",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                bool silent = SilentModeRadio.IsChecked == true;
                if (silent)
                {
                    await UpdateSelectedPackagesSilent(selectedPackages);
                }
                else
                {
                    await UpdateSelectedPackagesWithProgress(selectedPackages);
                }
            }
        }

        private async Task UpdateSelectedPackagesWithProgress(List<Package> packages)
        {
            int successCount = 0;
            int failureCount = 0;
            var mainWindow = Window.GetWindow(this) as MainWindow;

            mainWindow?.UpdateOperationStatus($"Updating {packages.Count} package(s)...");
            
            for (int i = 0; i < packages.Count; i++)
            {
                var pkg = packages[i];
                try
                {
                    int progressPercent = (i * 100) / packages.Count;
                    mainWindow?.UpdateOperationProgress(progressPercent, $"Updating {pkg.Name}...");
                    if (StatusText != null)
                        StatusText.Text = $"Updating {i + 1}/{packages.Count}: {pkg.Name}";

                    var opResult = await _wingetService.UpdatePackageAsync(pkg.Id, false);
                    if (opResult.Success)
                    {
                        successCount++;
                        pkg.IsSelected = false;
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch
                {
                    failureCount++;
                }
            }

            mainWindow?.UpdateOperationProgress(100, "Update complete");

            string summary = $"✓ {successCount} successful";
            if (failureCount > 0)
                summary += $", ✗ {failureCount} failed";

            if (StatusText != null)
                StatusText.Text = summary;
            MessageBox.Show(summary, "Update Complete", MessageBoxButton.OK, 
                           failureCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            
            await LoadUpdateMode();
            mainWindow?.ResetProgress();
            UpdateSelectionStatus();
        }

        private async Task UpdateSelectedPackagesSilent(List<Package> packages)
        {
            int successCount = 0;
            int failureCount = 0;
            var mainWindow = Window.GetWindow(this) as MainWindow;

            mainWindow?.UpdateOperationStatus($"Updating {packages.Count} package(s) silently...");

            for (int i = 0; i < packages.Count; i++)
            {
                var pkg = packages[i];
                try
                {
                    int progressPercent = (i * 100) / packages.Count;
                    mainWindow?.UpdateOperationProgress(progressPercent, $"Updating {pkg.Name}...");
                    if (StatusText != null)
                        StatusText.Text = $"Updating {i + 1}/{packages.Count}: {pkg.Name} (Silent)";

                    var opResult = await _wingetService.UpdatePackageAsync(pkg.Id, true);
                    if (opResult.Success)
                    {
                        successCount++;
                        pkg.IsSelected = false;
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch
                {
                    failureCount++;
                }
            }

            mainWindow?.UpdateOperationProgress(100, "Update complete");

            string summary = $"✓ {successCount} successful";
            if (failureCount > 0)
                summary += $", ✗ {failureCount} failed";

            if (StatusText != null)
                StatusText.Text = summary;
            MessageBox.Show(summary, "Update Complete", MessageBoxButton.OK, 
                           failureCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            
            await LoadUpdateMode();
            mainWindow?.ResetProgress();
            UpdateSelectionStatus();
        }

        // Uninstall functionality
        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (Packages == null) return;
            var selectedPackages = Packages.Where(p => p.IsSelected).ToList();
            
            if (selectedPackages.Count == 0)
            {
                MessageBox.Show("Please select at least one package to uninstall", "Uninstall");
                return;
            }

            var packageNames = string.Join(", ", selectedPackages.Select(p => $"{p.Name} ({p.Id})"));
            var result = MessageBox.Show(
                $"Uninstall {selectedPackages.Count} package(s)?\n\n{packageNames}",
                "Confirm Uninstall",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                bool silent = SilentModeRadio.IsChecked == true;
                if (silent)
                {
                    await UninstallSelectedPackagesSilent(selectedPackages);
                }
                else
                {
                    await UninstallSelectedPackagesWithProgress(selectedPackages);
                }
            }
        }

        private async Task UninstallSelectedPackagesWithProgress(List<Package> packages)
        {
            int successCount = 0;
            int failureCount = 0;
            var mainWindow = Window.GetWindow(this) as MainWindow;

            mainWindow?.UpdateOperationStatus($"Uninstalling {packages.Count} package(s)...");
            
            for (int i = 0; i < packages.Count; i++)
            {
                var pkg = packages[i];
                try
                {
                    int progressPercent = (i * 100) / packages.Count;
                    mainWindow?.UpdateOperationProgress(progressPercent, $"Uninstalling {pkg.Name}...");
                    if (StatusText != null)
                        StatusText.Text = $"Uninstalling {i + 1}/{packages.Count}: {pkg.Name}";

                    var opResult = await _wingetService.UninstallPackageAsync(pkg.Id, false);
                    if (opResult.Success)
                    {
                        successCount++;
                        if (Packages != null)
                            Packages.Remove(pkg);
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch
                {
                    failureCount++;
                }
            }

            mainWindow?.UpdateOperationProgress(100, "Uninstall complete");

            string summary = $"✓ {successCount} successful";
            if (failureCount > 0)
                summary += $", ✗ {failureCount} failed";

            if (StatusText != null)
                StatusText.Text = summary;
            MessageBox.Show(summary, "Uninstall Complete", MessageBoxButton.OK, 
                           failureCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            
            if (_currentMode == UnifiedMode.Update)
            {
                await LoadUpdateMode();
            }
            else if (_currentMode == UnifiedMode.Browse)
            {
                await LoadInstalledPackages();
            }
            
            mainWindow?.ResetProgress();
            UpdateSelectionStatus();
        }

        private async Task UninstallSelectedPackagesSilent(List<Package> packages)
        {
            int successCount = 0;
            int failureCount = 0;
            var mainWindow = Window.GetWindow(this) as MainWindow;

            mainWindow?.UpdateOperationStatus($"Uninstalling {packages.Count} package(s) silently...");

            for (int i = 0; i < packages.Count; i++)
            {
                var pkg = packages[i];
                try
                {
                    int progressPercent = (i * 100) / packages.Count;
                    mainWindow?.UpdateOperationProgress(progressPercent, $"Uninstalling {pkg.Name}...");
                    if (StatusText != null)
                        StatusText.Text = $"Uninstalling {i + 1}/{packages.Count}: {pkg.Name} (Silent)";

                    var opResult = await _wingetService.UninstallPackageAsync(pkg.Id, true);
                    if (opResult.Success)
                    {
                        successCount++;
                        if (Packages != null)
                            Packages.Remove(pkg);
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch
                {
                    failureCount++;
                }
            }

            mainWindow?.UpdateOperationProgress(100, "Uninstall complete");

            string summary = $"✓ {successCount} successful";
            if (failureCount > 0)
                summary += $", ✗ {failureCount} failed";

            if (StatusText != null)
                StatusText.Text = summary;
            MessageBox.Show(summary, "Uninstall Complete", MessageBoxButton.OK, 
                           failureCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            
            if (_currentMode == UnifiedMode.Update)
            {
                await LoadUpdateMode();
            }
            else if (_currentMode == UnifiedMode.Browse)
            {
                await LoadInstalledPackages();
            }
            
            mainWindow?.ResetProgress();
            UpdateSelectionStatus();
        }

        // Common functionality
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_currentMode)
            {
                case UnifiedMode.Install:
                    await LoadInstallMode();
                    break;
                case UnifiedMode.Update:
                    await LoadUpdateMode();
                    break;
                case UnifiedMode.Browse:
                    await LoadBrowseMode();
                    break;
                case UnifiedMode.Search:
                    if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                    {
                        await PerformSearch();
                    }
                    break;
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (Packages != null)
            {
                foreach (var pkg in Packages)
                    pkg.IsSelected = true;
            }
            UpdateSelectionStatus();
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (Packages != null)
            {
                foreach (var pkg in Packages)
                    pkg.IsSelected = false;
            }
            UpdateSelectionStatus();
        }

        private void UpdateSelectionStatus()
        {
            if (Packages != null && SelectionStatus != null)
            {
                int selected = Packages.Count(p => p.IsSelected);
                SelectionStatus.Text = $"Selected: {selected}";
            }
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadInstallMode();
        }

        private void LocalIdFilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filterText = LocalIdFilterBox.Text;
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(Packages);
            if (view != null)
            {
                if (string.IsNullOrWhiteSpace(filterText))
                {
                    view.Filter = null;
                }
                else
                {
                    view.Filter = (obj) =>
                    {
                        if (obj is Package pkg)
                        {
                            return pkg.Id?.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0;
                        }
                        return false;
                    };
                }
            }
        }
    }
}

