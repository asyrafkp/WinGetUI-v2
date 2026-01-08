using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WinGetUI.Models;
using WinGetUI.Services;

namespace WinGetUI.Views
{
    public partial class SourceManagementWindow : Window
    {
        private WingetService _wingetService;
        public ObservableCollection<PackageSource> Sources { get; }

        public SourceManagementWindow()
        {
            InitializeComponent();
            _wingetService = new WingetService();
            Sources = new ObservableCollection<PackageSource>();
            SourcesGrid.ItemsSource = Sources;
            
            Loaded += SourceManagementWindow_Loaded;
        }

        private async void SourceManagementWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSources();
        }

        private async System.Threading.Tasks.Task LoadSources()
        {
            StatusText.Text = "Loading sources...";
            Sources.Clear();

            var sources = await _wingetService.GetPackageSourcesAsync();
            foreach (var source in sources)
            {
                Sources.Add(source);
            }

            StatusText.Text = $"Loaded {Sources.Count} source(s)";
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadSources();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditSourceDialog(null);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    StatusText.Text = $"Adding source '{dialog.SourceName}'...";
                    
                    var result = await _wingetService.AddSourceAsync(
                        dialog.SourceName, 
                        dialog.SourceUrl, 
                        dialog.SourceType);

                    if (result.Success)
                    {
                        // Refresh first to show results immediately
                        await LoadSources();
                        MessageBox.Show(result.Message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusText.Text = "Failed to add source";
                        await LoadSources(); // Refresh anyway to be sure
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Error adding source";
                }
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = SourcesGrid.SelectedItem as PackageSource;
            if (selected == null) return;

            var dialog = new AddEditSourceDialog(selected);
            if (dialog.ShowDialog() == true)
            {
                // For editing, we need to remove and re-add
                StatusText.Text = $"Updating source '{selected.Name}'...";
                
                // Remove old source
                var removeResult = await _wingetService.RemoveSourceAsync(selected.Name);
                if (!removeResult.Success)
                {
                    MessageBox.Show($"Failed to remove old source: {removeResult.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Add with new settings
                var addResult = await _wingetService.AddSourceAsync(
                    dialog.SourceName, 
                    dialog.SourceUrl, 
                    dialog.SourceType);

                if (addResult.Success)
                {
                    MessageBox.Show("Source updated successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadSources();
                }
                else
                {
                    MessageBox.Show($"Failed to add updated source: {addResult.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Failed to update source";
                }
            }
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = SourcesGrid.SelectedItem as PackageSource;
            if (selected == null) return;

            if (selected.IsDefault)
            {
                MessageBox.Show("Cannot remove default source. You can reset it instead.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to remove source '{selected.Name}'?",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                StatusText.Text = $"Removing source '{selected.Name}'...";
                
                var opResult = await _wingetService.RemoveSourceAsync(selected.Name);

                if (opResult.Success)
                {
                    MessageBox.Show(opResult.Message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadSources();
                }
                else
                {
                    MessageBox.Show(opResult.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Failed to remove source";
                }
            }
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = SourcesGrid.SelectedItem as PackageSource;
            if (selected == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to reset source '{selected.Name}' to default settings?",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                StatusText.Text = $"Resetting source '{selected.Name}'...";
                
                var opResult = await _wingetService.ResetSourceAsync(selected.Name);

                if (opResult.Success)
                {
                    MessageBox.Show(opResult.Message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadSources();
                }
                else
                {
                    MessageBox.Show(opResult.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Failed to reset source";
                }
            }
        }

        private void SourcesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = SourcesGrid.SelectedItem != null;
            EditButton.IsEnabled = hasSelection;
            RemoveButton.IsEnabled = hasSelection;
            ResetButton.IsEnabled = hasSelection;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
