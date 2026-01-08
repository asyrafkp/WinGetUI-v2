using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinGetUI.Models
{
    /// <summary>
    /// Represents a package source in winget
    /// </summary>
    public class PackageSource : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Argument { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
        public string Identifier { get; set; }
        public bool IsDefault { get; set; }

        public PackageSource()
        {
            Name = "";
            Argument = "";
            Type = "";
            Data = "";
            Identifier = "";
            IsDefault = false;
        }

        public override string ToString()
        {
            return $"{Name} ({Type})";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

