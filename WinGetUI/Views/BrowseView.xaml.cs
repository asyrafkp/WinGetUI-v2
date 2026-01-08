using System;
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
    public partial class BrowseView : UserControl
    {
        private WingetService _wingetService;
        public ObservableCollection<Package> InstalledPackages { get; }

        public BrowseView()
        {
            try
            {
                this.InitializeComponent();
                _wingetService = new WingetService();
                InstalledPackages = new ObservableCollection<Package>();
                this.DataContext = this;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing BrowseView: {ex.Message}\n\nStackTrace: {ex.StackTrace}", "BrowseView Error");
                throw;
            }
        }

        private async Task LoadInstalledPackages()
        {
            StatusText.Text = "Loading installed packages...";
            InstalledPackages.Clear();
            UpdateSelectionStatus();

            var packages = await _wingetService.GetInstalledPackagesAsync();
            
            foreach (var pkg in packages.OrderBy(p => p.Name))
            {
                InstalledPackages.Add(pkg);
            }

            StatusText.Text = $"Found {InstalledPackages.Count} installed packages";
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadInstalledPackages();
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPackages = InstalledPackages.Where(p => p.IsSelected).ToList();
            
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
                        InstalledPackages.Remove(pkg);
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
                        InstalledPackages.Remove(pkg);
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
            
            UpdateSelectionStatus();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var pkg in InstalledPackages)
                pkg.IsSelected = true;
            UpdateSelectionStatus();
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var pkg in InstalledPackages)
                pkg.IsSelected = false;
            UpdateSelectionStatus();
        }

        private void UpdateSelectionStatus()
        {
            int selected = InstalledPackages.Count(p => p.IsSelected);
            SelectionStatus.Text = $"Selected: {selected}";
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadInstalledPackages();
        }
    }
}
