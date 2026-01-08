using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WinGetUI.Models;
using WinGetUI.Services;

namespace WinGetUI.Views
{
    public partial class SourcesView : UserControl
    {
        private WingetService _wingetService;
        public ObservableCollection<PackageSource> PackageSources { get; }

        public SourcesView()
        {
            try
            {
                this.InitializeComponent();
                _wingetService = new WingetService();
                PackageSources = new ObservableCollection<PackageSource>();
                this.DataContext = this;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing SourcesView: {ex.Message}\n\nStackTrace: {ex.StackTrace}", "SourcesView Error");
                throw;
            }
        }

        private async Task LoadPackageSources()
        {
            StatusText.Text = "Loading package sources...";
            PackageSources.Clear();

            var sources = await _wingetService.GetPackageSourcesAsync();
            
            foreach (var source in sources.OrderBy(s => s.Name))
            {
                PackageSources.Add(source);
            }

            StatusText.Text = $"Found {PackageSources.Count} package source(s)";
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadPackageSources();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadPackageSources();
        }
    }
}

