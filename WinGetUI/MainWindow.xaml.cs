using System;
using System.Windows;
using System.Threading.Tasks;
using WinGetUI.Services;
using WinGetUI.Views;
using System.Windows.Controls;

namespace WinGetUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                this.InitializeComponent();
                UpdateThemeUI();
                
                // Subscribe to theme changes
                ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
                ConnectionStatusService.StatusChanged += ConnectionStatusService_StatusChanged;
                
                // Initialize connection status check
                InitializeConnectionStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing MainWindow: {ex.Message}\n{ex.StackTrace}", "Initialization Error");
                throw;
            }
        }

        private async void InitializeConnectionStatus()
        {
            await ConnectionStatusService.InitializeAsync();
        }

        private void ConnectionStatusService_StatusChanged(object? sender, ConnectionStatusChangedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                string indicator = ConnectionStatusService.GetStatusIndicator();
                ConnectionStatusIndicator.Text = $"{indicator} {e.Message}";
                
                // Update color based on status
                switch (e.Status)
                {
                    case ConnectionStatus.Connected:
                        ConnectionStatusIndicator.SetResourceReference(TextBlock.ForegroundProperty, "SuccessStatusBrush");
                        break;
                    case ConnectionStatus.Disconnected:
                        ConnectionStatusIndicator.SetResourceReference(TextBlock.ForegroundProperty, "ErrorStatusBrush");
                        break;
                    default:
                        ConnectionStatusIndicator.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
                        break;
                }
            });
        }

        public void UpdateOperationProgress(int percentage, string statusMessage)
        {
            this.Dispatcher.Invoke(() =>
            {
                int value = Math.Min(percentage, 100);
                OperationProgressBar.Value = value;
                OperationStatusText.Text = statusMessage;
                OperationPercentageText.Text = $"{value}%";
                OperationPercentageText.Visibility = Visibility.Visible;
            });
        }

        public void UpdateOperationStatus(string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                StatusInfoText.Text = message;
            });
        }

        public void ResetProgress()
        {
            this.Dispatcher.Invoke(() =>
            {
                OperationProgressBar.Value = 0;
                OperationStatusText.Text = "Ready";
                OperationPercentageText.Text = "0%";
                OperationPercentageText.Visibility = Visibility.Collapsed;
                StatusInfoText.Text = "";
            });
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ToggleTheme();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void ThemeManager_ThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            UpdateThemeUI();
        }

        private void UpdateThemeUI()
        {
            if (ThemeManager.CurrentTheme == ThemeManager.AppTheme.Light)
            {
                ThemeToggleButton.Content = "🌙 Dark Mode";
                ThemeStatusText.Text = "Light Mode";
            }
            else
            {
                ThemeToggleButton.Content = "☀️ Light Mode";
                ThemeStatusText.Text = "Dark Mode";
            }
        }
    }
}
