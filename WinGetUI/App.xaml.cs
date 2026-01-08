using System;
using System.Windows;
using WinGetUI.Services;

namespace WinGetUI
{
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show($"Error: {e.Exception.Message}\n\n{e.Exception.StackTrace}", "Application Error");
                e.Handled = true;
            };

            this.Startup += (s, e) =>
            {
                try
                {
                    // Initialize theme from saved configuration
                    ThemeManager.Initialize();

                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.MainWindow = mainWindow;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to start application: {ex.Message}\n{ex.StackTrace}", "Startup Error");
                    this.Shutdown();
                }
            };
        }
    }
}
