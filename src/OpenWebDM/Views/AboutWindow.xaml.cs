using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using OpenWebDM.Services;

namespace OpenWebDM.Views
{
    public partial class AboutWindow : Window
    {
        private readonly IUpdateService _updateService;

        public AboutWindow(IUpdateService? updateService = null)
        {
            InitializeComponent();
            _updateService = updateService ?? new UpdateService();
        }

        private void Website_Click(object sender, MouseButtonEventArgs e)
        {
            var textBlock = sender as System.Windows.Controls.TextBlock;
            if (textBlock?.Tag is string url)
            {
                OpenUrl(url);
            }
        }

        private void Email_Click(object sender, MouseButtonEventArgs e)
        {
            var textBlock = sender as System.Windows.Controls.TextBlock;
            if (textBlock?.Tag is string email)
            {
                OpenUrl(email);
            }
        }

        private void GitHub_Click(object sender, MouseButtonEventArgs e)
        {
            var textBlock = sender as System.Windows.Controls.TextBlock;
            if (textBlock?.Tag is string url)
            {
                OpenUrl(url);
            }
        }

        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                button.IsEnabled = false;
                button.Content = "Checking...";
            }

            try
            {
                var updateInfo = await _updateService.CheckForUpdatesAsync();
                
                if (updateInfo.HasUpdate)
                {
                    var result = MessageBox.Show(
                        $"A new version {updateInfo.LatestVersion} is available!\n\n" +
                        "Would you like to download and install it now?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        await _updateService.DownloadAndInstallUpdateAsync(updateInfo);
                    }
                }
                else
                {
                    MessageBox.Show(
                        "You are using the latest version of OpenWeb Download Manager.",
                        "No Updates Available",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to check for updates: {ex.Message}",
                    "Update Check Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "Check for Updates";
                }
            }
        }

        private void ViewChangelog_Click(object sender, RoutedEventArgs e)
        {
            var changelogWindow = new ChangelogWindow();
            changelogWindow.Owner = this;
            changelogWindow.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}