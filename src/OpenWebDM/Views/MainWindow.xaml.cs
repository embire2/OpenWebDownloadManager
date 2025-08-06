using System.Windows;
using OpenWebDM.ViewModels;
using OpenWebDM.Services;

namespace OpenWebDM.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void NewDownload_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open new download dialog
        }

        private void ImportUrls_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open import URLs dialog
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open settings window
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void StartAll_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Start all paused downloads
        }

        private void PauseAll_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Pause all active downloads
        }

        private void Categories_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open categories management
        }

        private void Scheduler_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open download scheduler
        }

        private void BrowserIntegration_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open browser integration settings
        }

        private void SiteExplorer_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open site explorer
        }

        private void BatchDownload_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open batch download dialog
        }

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open options dialog
        }

        private void UserManual_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://openweb.co.za/support",
                UseShellExecute = true
            });
        }

        private void KeyboardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Keyboard Shortcuts:\n\n" +
                "Ctrl+N - New Download\n" +
                "Ctrl+O - Open File\n" +
                "Ctrl+D - Delete Download\n" +
                "Space - Pause/Resume\n" +
                "Delete - Remove from list\n" +
                "F5 - Refresh\n" +
                "Ctrl+A - Select All\n" +
                "Ctrl+S - Settings",
                "Keyboard Shortcuts",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("You are using the latest version of OpenWeb Download Manager.", 
                          "Check for Updates", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void AddDownload_Click(object sender, RoutedEventArgs e)
        {
            // Focus URL textbox for quick download addition
            var urlTextBox = FindName("DownloadUrl") as System.Windows.Controls.TextBox;
            urlTextBox?.Focus();
        }

        private void PasteUrl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var viewModel = DataContext as MainViewModel;
                    if (viewModel != null)
                    {
                        viewModel.DownloadUrl = Clipboard.GetText();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to paste URL: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}