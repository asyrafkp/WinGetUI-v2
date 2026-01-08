using System.Windows;
using WinGetUI.Models;

namespace WinGetUI.Views
{
    public partial class AddEditSourceDialog : Window
    {
        public string SourceName { get; private set; } = string.Empty;
        public string SourceUrl { get; private set; } = string.Empty;
        public string SourceType { get; private set; } = string.Empty;

        private bool _isEditMode;

        public AddEditSourceDialog(PackageSource? existingSource)
        {
            InitializeComponent();

            _isEditMode = existingSource != null;
            Title = _isEditMode ? "Edit Package Source" : "Add Package Source";

            if (_isEditMode && existingSource != null)
            {
                NameTextBox.Text = existingSource.Name;
                UrlTextBox.Text = existingSource.Argument;
                
                // Try to select the type if it matches
                foreach (var item in TypeComboBox.Items)
                {
                    if (item is System.Windows.Controls.ComboBoxItem comboItem)
                    {
                        if (comboItem.Content?.ToString() == existingSource.Type)
                        {
                            TypeComboBox.SelectedItem = comboItem;
                            break;
                        }
                    }
                }
            }

            NameTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Please enter a source name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(UrlTextBox.Text))
            {
                MessageBox.Show("Please enter a source URL/argument.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                UrlTextBox.Focus();
                return;
            }

            // Set properties
            SourceName = NameTextBox.Text.Trim();
            SourceUrl = UrlTextBox.Text.Trim();
            
            if (TypeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                SourceType = selectedItem.Content?.ToString() ?? "";
            }
            else
            {
                SourceType = "";
            }

            DialogResult = true;
            Close();
        }

        private void ChocolateyButton_Click(object sender, RoutedEventArgs e)
        {
            NameTextBox.Text = "chocolatey";
            UrlTextBox.Text = "https://community.chocolatey.org/api/v2/";
            
            // Select Microsoft.Rest type
            foreach (var item in TypeComboBox.Items)
            {
                if (item is System.Windows.Controls.ComboBoxItem comboItem)
                {
                    if (comboItem.Content?.ToString() == "Microsoft.Rest")
                    {
                        TypeComboBox.SelectedItem = comboItem;
                        break;
                    }
                }
            }
            
            MessageBox.Show(
                "Chocolatey source details have been pre-filled.\n\n" +
                "Note: Chocolatey packages may require different installation methods than winget packages. " +
                "This adds the Chocolatey repository as a source, but compatibility may vary.",
                "Chocolatey Source",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void WingetCommunityButton_Click(object sender, RoutedEventArgs e)
        {
            NameTextBox.Text = "winget-pkgs";
            UrlTextBox.Text = "https://cdn.winget.microsoft.com/cache";
            
            // Select Microsoft.PreIndexed.Package type
            foreach (var item in TypeComboBox.Items)
            {
                if (item is System.Windows.Controls.ComboBoxItem comboItem)
                {
                    if (comboItem.Content?.ToString() == "Microsoft.PreIndexed.Package")
                    {
                        TypeComboBox.SelectedItem = comboItem;
                        break;
                    }
                }
            }
            
            MessageBox.Show(
                "Winget Community source details have been pre-filled.\n\n" +
                "This is the official winget package repository.",
                "Winget Community Source",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ChocolateyGitHubButton_Click(object sender, RoutedEventArgs e)
        {
            NameTextBox.Text = "chocolatey-github";
            UrlTextBox.Text = "https://github.com/chocolatey-community/chocolatey-packages";
            
            // This is likely a REST or custom type but we'll leave it empty for user to decide or default
            TypeComboBox.SelectedIndex = 0; 
            
            MessageBox.Show(
                "Chocolatey GitHub repository link has been pre-filled.\n\n" +
                "Note: A GitHub URL may not be a direct winget source. This is added for your convenience if you are using shims or custom source scripts that point to this repository.",
                "Chocolatey GitHub",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
