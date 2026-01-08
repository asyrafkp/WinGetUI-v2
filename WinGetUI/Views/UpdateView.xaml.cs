using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using WinGetUI.Models;
using WinGetUI.Services;

namespace WinGetUI.Views
{
    public partial class UpdateView : UserControl
    {
        private WingetService _wingetService;
        public ObservableCollection<Package> UpdatablePackages { get; }
        private bool _isShowingAllInstalled = false;

        public UpdateView()
        {
            try
            {
                this.InitializeComponent();
                _wingetService = new WingetService();
                UpdatablePackages = new ObservableCollection<Package>();
                this.DataContext = this;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing UpdateView: {ex.Message}\n\nStackTrace: {ex.StackTrace}", "UpdateView Error");
                throw;
            }
        }

        private async Task LoadUpdatablePackages()
        {
            StatusText.Text = "Loading updatable packages...";
            UpdatablePackages.Clear();
            UpdateSelectionStatus();

            var packages = await _wingetService.GetUpgradablePackagesAsync();
            
            foreach (var pkg in packages.OrderBy(p => p.Name))
            {
                UpdatablePackages.Add(pkg);
            }

            StatusText.Text = $"Found {UpdatablePackages.Count} packages with updates available";
        }

        private async Task LoadAllInstalledPackages()
        {
            StatusText.Text = "Loading all installed packages...";
            UpdatablePackages.Clear();
            UpdateSelectionStatus();

            var packages = await _wingetService.GetInstalledPackagesAsync();
            
            foreach (var pkg in packages.OrderBy(p => p.Name))
            {
                UpdatablePackages.Add(pkg);
            }

            StatusText.Text = $"Found {UpdatablePackages.Count} installed packages";
        }

        private async void ViewModeRadio_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PackagesGrid == null || PackagesGrid.Columns == null)
                    return;

                if (UpdatableViewRadio.IsChecked == true)
                {
                    _isShowingAllInstalled = false;
                    // Show Available Version column
                    var availableCol = PackagesGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "Available Version");
                    if (availableCol != null)
                        availableCol.Visibility = System.Windows.Visibility.Visible;
                    await LoadUpdatablePackages();
                }
                else if (AllInstalledViewRadio.IsChecked == true)
                {
                    _isShowingAllInstalled = true;
                    // Hide Available Version column
                    var availableCol = PackagesGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "Available Version");
                    if (availableCol != null)
                        availableCol.Visibility = System.Windows.Visibility.Collapsed;
                    await LoadAllInstalledPackages();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ViewModeRadio_Checked: {ex.Message}");
                // Continue with loading packages even if column visibility fails
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
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPackages = UpdatablePackages.Where(p => p.IsSelected).ToList();
            
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
                    // Update progress
                    int progressPercent = (i * 100) / packages.Count;
                    mainWindow?.UpdateOperationProgress(progressPercent, $"Updating {pkg.Name}...");
                    StatusText.Text = $"Updating {i + 1}/{packages.Count}: {pkg.Name}";

                    var opResult = await _wingetService.UpdatePackageAsync(pkg.Id);
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

            // Complete progress
            mainWindow?.UpdateOperationProgress(100, "Update complete");

            string summary = $"✓ {successCount} successful";
            if (failureCount > 0)
                summary += $", ✗ {failureCount} failed";

            StatusText.Text = summary;
            MessageBox.Show(summary, "Update Complete", MessageBoxButton.OK, 
                           failureCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            
            if (_isShowingAllInstalled)
            {
                await LoadAllInstalledPackages();
            }
            else
            {
                await LoadUpdatablePackages();
            }
            mainWindow?.ResetProgress();
            UpdateSelectionStatus();
        }

        private async Task UpdateSelectedPackagesSilent(List<Package> packages)
        {
            int successCount = 0;
            int failureCount = 0;

            // Silent mode - no progress updates, just process
            for (int i = 0; i < packages.Count; i++)
            {
                var pkg = packages[i];
                try
                {
                    var opResult = await _wingetService.UpdatePackageAsync(pkg.Id);
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

            // Only show result at the end
            string summary = $"✓ {successCount} successful";
            if (failureCount > 0)
                summary += $", ✗ {failureCount} failed";

            StatusText.Text = summary;
            MessageBox.Show(summary, "Update Complete", MessageBoxButton.OK, 
                           failureCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            
            if (_isShowingAllInstalled)
            {
                await LoadAllInstalledPackages();
            }
            else
            {
                await LoadUpdatablePackages();
            }
            UpdateSelectionStatus();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var pkg in UpdatablePackages)
                pkg.IsSelected = true;
            UpdateSelectionStatus();
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var pkg in UpdatablePackages)
                pkg.IsSelected = false;
            UpdateSelectionStatus();
        }

        private void UpdateSelectionStatus()
        {
            int selected = UpdatablePackages.Count(p => p.IsSelected);
            SelectionStatus.Text = $"Selected: {selected}";
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
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

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPackages = UpdatablePackages.Where(p => p.IsSelected).ToList();
            
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
                    // Update progress
                    int progressPercent = (i * 100) / packages.Count;
                    mainWindow?.UpdateOperationProgress(progressPercent, $"Uninstalling {pkg.Name}...");
                    StatusText.Text = $"Uninstalling {i + 1}/{packages.Count}: {pkg.Name}";

                    var opResult = await _wingetService.UninstallPackageAsync(pkg.Id);
                    if (opResult.Success)
                    {
                        successCount++;
                        UpdatablePackages.Remove(pkg);
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

            // Complete progress
            mainWindow?.UpdateOperationProgress(100, "Uninstall complete");

            string summary = $"✓ {successCount} successful";
            if (failureCount > 0)
                summary += $", ✗ {failureCount} failed";

            StatusText.Text = summary;
            MessageBox.Show(summary, "Uninstall Complete", MessageBoxButton.OK, 
                           failureCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            
            if (_isShowingAllInstalled)
            {
                await LoadAllInstalledPackages();
            }
            else
            {
                await LoadUpdatablePackages();
            }
            mainWindow?.ResetProgress();
            UpdateSelectionStatus();
        }

        private async Task UninstallSelectedPackagesSilent(List<Package> packages)
        {
            int successCount = 0;
            int failureCount = 0;

            // Silent mode - no progress updates, just process
            for (int i = 0; i < packages.Count; i++)
            {
                var pkg = packages[i];
                try
                {
                    var opResult = await _wingetService.UninstallPackageAsync(pkg.Id);
                    if (opResult.Success)
                    {
                        successCount++;
                        UpdatablePackages.Remove(pkg);
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

            // Only show result at the end
            string summary = $"✓ {successCount} successful";
            if (failureCount > 0)
                summary += $", ✗ {failureCount} failed";

            StatusText.Text = summary;
            MessageBox.Show(summary, "Uninstall Complete", MessageBoxButton.OK, 
                           failureCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            
            if (_isShowingAllInstalled)
            {
                await LoadAllInstalledPackages();
            }
            else
            {
                await LoadUpdatablePackages();
            }
            UpdateSelectionStatus();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadUpdatablePackages();
        }
    }
}
