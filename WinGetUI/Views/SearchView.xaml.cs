using System;using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WinGetUI.Models;
using WinGetUI.Services;

namespace WinGetUI.Views
{
    public partial class SearchView : UserControl
    {
        private WingetService _wingetService;
        public ObservableCollection<Package> SearchResults { get; }

        public SearchView()
        {
            try
            {
                this.InitializeComponent();
                _wingetService = new WingetService();
                SearchResults = new ObservableCollection<Package>();
                this.DataContext = this;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing SearchView: {ex.Message}\n\nStackTrace: {ex.StackTrace}", "SearchView Error");
                throw;
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformSearch();
        }

        private async void SearchAllButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformSearchAll();
        }

        private async Task PerformSearch()
        {
            string searchTerm = SearchBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("Please enter a search term", "Search");
                return;
            }

            StatusText.Text = $"Searching for '{searchTerm}'...";
            SearchResults.Clear();
            UpdateSelectionStatus();

            // Get filter type
            SearchFilterType filterType = FilterCombo.SelectedIndex switch
            {
                0 => SearchFilterType.Name,
                1 => SearchFilterType.Id,
                _ => SearchFilterType.Both
            };

            // Get match type (exact or partial)
            bool exactMatch = MatchCombo.SelectedIndex == 1;

            var packages = await _wingetService.SearchPackagesAdvancedAsync(searchTerm, filterType, exactMatch);
            
            foreach (var pkg in packages)
            {
                SearchResults.Add(pkg);
            }

            string matchType = exactMatch ? "exactly" : "containing";
            string filterText = filterType switch
            {
                SearchFilterType.Name => "by Name",
                SearchFilterType.Id => "by ID",
                _ => "by Name and ID"
            };

            StatusText.Text = $"Found {SearchResults.Count} packages {filterText} {matchType} '{searchTerm}'";
        }

        private async Task PerformSearchAll()
        {
            StatusText.Text = "Searching all available packages...";
            SearchResults.Clear();
            UpdateSelectionStatus();

            // Search with empty term to get all packages
            var packages = await _wingetService.SearchPackagesAsync("");
            
            foreach (var pkg in packages)
            {
                SearchResults.Add(pkg);
            }

            StatusText.Text = $"Found {SearchResults.Count} available packages in winget";
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPackages = SearchResults.Where(p => p.IsSelected).ToList();
            
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
                    // Update progress
                    int progressPercent = (i * 100) / packages.Count;
                    mainWindow?.UpdateOperationProgress(progressPercent, $"Installing {pkg.Name}...");
                    StatusText.Text = $"Installing {i + 1}/{packages.Count}: {pkg.Name}";

                    var opResult = await _wingetService.InstallPackageAsync(pkg.Id);
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
            mainWindow?.UpdateOperationProgress(100, "Installation complete");

            string summary = $"✓ {successCount} successful";
            if (failureCount > 0)
                summary += $", ✗ {failureCount} failed";

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

            // Silent mode - no progress updates, just process
            for (int i = 0; i < packages.Count; i++)
            {
                var pkg = packages[i];
                try
                {
                    var opResult = await _wingetService.InstallPackageAsync(pkg.Id);
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
            MessageBox.Show(summary, "Installation Complete", MessageBoxButton.OK, 
                           failureCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            
            UpdateSelectionStatus();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var pkg in SearchResults)
                pkg.IsSelected = true;
            UpdateSelectionStatus();
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var pkg in SearchResults)
                pkg.IsSelected = false;
            UpdateSelectionStatus();
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPackages = SearchResults.Where(p => p.IsSelected).ToList();
            
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
                        pkg.IsSelected = false;
                        SearchResults.Remove(pkg);
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
                        pkg.IsSelected = false;
                        SearchResults.Remove(pkg);
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

        private void UpdateSelectionStatus()
        {
            int selected = SearchResults.Count(p => p.IsSelected);
            SelectionStatus.Text = $"Selected: {selected}";
        }

        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                await PerformSearch();
            }
        }
    }
}
