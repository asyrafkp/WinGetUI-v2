using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinGetUI.Models
{
    /// <summary>
    /// Represents a Windows package that can be managed via winget
    /// </summary>
    public class Package : IComparable<Package>, INotifyPropertyChanged
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string AvailableVersion { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public PackageStatus Status { get; set; }
        public DateTime LastUpdated { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public Package()
        {
            Status = PackageStatus.Unknown;
            LastUpdated = DateTime.Now;
            Source = "winget";
            _isSelected = false;
        }

        public int CompareTo(Package? other)
        {
            if (other == null) return 1;
            return string.Compare(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return $"{Name} ({Id}) - v{Version}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum PackageStatus
    {
        Unknown,
        Installed,
        Available,
        UpdateAvailable,
        Installing,
        Updating,
        Removing
    }
}
