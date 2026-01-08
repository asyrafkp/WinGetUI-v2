using System;
using System.IO;
using System.Windows;

namespace WinGetUI.Services
{
    /// <summary>
    /// Manages application themes (Light/Dark mode)
    /// </summary>
    public class ThemeManager
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WinGetUI",
            "config.txt"
        );

        public enum AppTheme
        {
            Light,
            Dark
        }

        private static AppTheme _currentTheme = AppTheme.Light;

        public static AppTheme CurrentTheme
        {
            get => _currentTheme;
            private set => _currentTheme = value;
        }

        public static event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        /// <summary>
        /// Initialize theme from saved configuration
        /// </summary>
        public static void Initialize()
        {
            LoadTheme();
            ApplyTheme(CurrentTheme);
        }

        /// <summary>
        /// Toggle between light and dark theme
        /// </summary>
        public static void ToggleTheme()
        {
            AppTheme newTheme = CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
            SetTheme(newTheme);
        }

        /// <summary>
        /// Set specific theme
        /// </summary>
        public static void SetTheme(AppTheme theme)
        {
            CurrentTheme = theme;
            ApplyTheme(theme);
            SaveTheme();
            ThemeChanged?.Invoke(null, new ThemeChangedEventArgs(theme));
        }

        /// <summary>
        /// Apply theme to application
        /// </summary>
        private static void ApplyTheme(AppTheme theme)
        {
            var resourceDictionaries = Application.Current.Resources.MergedDictionaries;

            // Remove existing theme dictionary if present
            if (resourceDictionaries.Count > 0)
            {
                resourceDictionaries.RemoveAt(resourceDictionaries.Count - 1);
            }

            // Add new theme dictionary
            var themeUri = theme == AppTheme.Light
                ? new Uri("pack://application:,,,/Themes/LightTheme.xaml")
                : new Uri("pack://application:,,,/Themes/DarkTheme.xaml");

            var themeDictionary = new ResourceDictionary { Source = themeUri };
            resourceDictionaries.Add(themeDictionary);
        }

        /// <summary>
        /// Save theme preference to config file
        /// </summary>
        private static void SaveTheme()
        {
            try
            {
                var configDir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);

                File.WriteAllText(ConfigPath, CurrentTheme.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving theme: {ex.Message}");
            }
        }

        /// <summary>
        /// Load theme preference from config file
        /// </summary>
        private static void LoadTheme()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string content = File.ReadAllText(ConfigPath).Trim();
                    if (Enum.TryParse<AppTheme>(content, out var theme))
                    {
                        CurrentTheme = theme;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading theme: {ex.Message}");
            }
        }
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public ThemeManager.AppTheme NewTheme { get; }

        public ThemeChangedEventArgs(ThemeManager.AppTheme newTheme)
        {
            NewTheme = newTheme;
        }
    }
}
