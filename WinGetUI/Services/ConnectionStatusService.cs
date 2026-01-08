using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace WinGetUI.Services
{
    public enum ConnectionStatus
    {
        Connected,
        Disconnected,
        Checking
    }

    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public ConnectionStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public static class ConnectionStatusService
    {
        private static ConnectionStatus _currentStatus = ConnectionStatus.Checking;
        public static ConnectionStatus CurrentStatus
        {
            get => _currentStatus;
            private set
            {
                if (_currentStatus != value)
                {
                    _currentStatus = value;
                    StatusChanged?.Invoke(null, new ConnectionStatusChangedEventArgs
                    {
                        Status = value,
                        Message = GetStatusMessage(value)
                    });
                }
            }
        }

        public static event EventHandler<ConnectionStatusChangedEventArgs>? StatusChanged;

        public static async Task InitializeAsync()
        {
            await CheckConnectionAsync();
        }

        public static async Task CheckConnectionAsync()
        {
            CurrentStatus = ConnectionStatus.Checking;

            try
            {
                // Try to check if winget is available
                bool wingetAvailable = await IsWingetAvailableAsync();

                if (wingetAvailable)
                {
                    CurrentStatus = ConnectionStatus.Connected;
                }
                else
                {
                    CurrentStatus = ConnectionStatus.Disconnected;
                }
            }
            catch
            {
                CurrentStatus = ConnectionStatus.Disconnected;
            }
        }

        private static async Task<bool> IsWingetAvailableAsync()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                bool completed = process.WaitForExit(5000); // 5 second timeout

                if (completed && process.ExitCode == 0)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static string GetStatusMessage(ConnectionStatus status)
        {
            return status switch
            {
                ConnectionStatus.Connected => "✓ Connected - winget available",
                ConnectionStatus.Disconnected => "✗ Disconnected - check winget or internet",
                ConnectionStatus.Checking => "⟳ Checking connection...",
                _ => "Unknown status"
            };
        }

        public static string GetStatusIndicator()
        {
            return CurrentStatus switch
            {
                ConnectionStatus.Connected => "●",
                ConnectionStatus.Disconnected => "○",
                ConnectionStatus.Checking => "⟳",
                _ => "?"
            };
        }
    }
}
