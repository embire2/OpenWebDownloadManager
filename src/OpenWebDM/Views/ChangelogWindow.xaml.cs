using System.Windows;
using OpenWebDM.Services;

namespace OpenWebDM.Views
{
    public partial class ChangelogWindow : Window
    {
        private readonly IUpdateService _updateService;

        public ChangelogWindow()
        {
            InitializeComponent();
            _updateService = new UpdateService();
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
                        Close(); // Close changelog before starting update
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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}