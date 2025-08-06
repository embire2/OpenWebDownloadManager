using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace OpenWebDM.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UpdateService>? _logger;
        private const string UPDATE_CHECK_URL = "https://openweb.live/software";
        private const string CURRENT_VERSION = "1.0.0";

        public string CurrentVersion => CURRENT_VERSION;

        public UpdateService(ILogger<UpdateService>? logger = null)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "OpenWebDM/1.0.0 UpdateChecker");
            _logger = logger;
        }

        public async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                _logger?.LogInformation("Checking for updates from {Url}", UPDATE_CHECK_URL);

                var response = await _httpClient.GetAsync(UPDATE_CHECK_URL);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return await ParseUpdateInfoFromHtml(content);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to check for updates");
                throw new Exception($"Update check failed: {ex.Message}", ex);
            }
        }

        private async Task<UpdateInfo> ParseUpdateInfoFromHtml(string htmlContent)
        {
            var updateInfo = new UpdateInfo();

            try
            {
                // Look for OpenWeb Download Manager files with pattern OWDM{version}.exe
                var filePattern = @"OWDM(\d+)_(\d+)_(\d+)(?:_(\d+))?\.exe";
                var matches = Regex.Matches(htmlContent, filePattern, RegexOptions.IgnoreCase);

                if (matches.Count == 0)
                {
                    _logger?.LogInformation("No OpenWeb Download Manager installer files found");
                    return updateInfo;
                }

                // Find the highest version
                Version? latestVersion = null;
                string? latestFileName = null;

                foreach (Match match in matches)
                {
                    var major = int.Parse(match.Groups[1].Value);
                    var minor = int.Parse(match.Groups[2].Value);
                    var patch = int.Parse(match.Groups[3].Value);
                    var build = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;

                    var version = build > 0 
                        ? new Version(major, minor, patch, build)
                        : new Version(major, minor, patch);

                    if (latestVersion == null || version > latestVersion)
                    {
                        latestVersion = version;
                        latestFileName = match.Value;
                    }
                }

                if (latestVersion != null && latestFileName != null)
                {
                    var currentVersion = new Version(CURRENT_VERSION);
                    
                    updateInfo.HasUpdate = latestVersion > currentVersion;
                    updateInfo.LatestVersion = latestVersion.ToString();
                    updateInfo.FileName = latestFileName;
                    updateInfo.DownloadUrl = $"https://openweb.live/software/{latestFileName}";
                    updateInfo.IsRequired = true; // All updates are required for this app
                    
                    _logger?.LogInformation("Latest version found: {Version}, Current: {Current}, Update needed: {UpdateNeeded}", 
                        latestVersion, currentVersion, updateInfo.HasUpdate);
                }

                return updateInfo;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to parse update information");
                throw;
            }
        }

        public async Task<bool> IsUpdateRequiredAsync()
        {
            try
            {
                var updateInfo = await CheckForUpdatesAsync();
                return updateInfo.HasUpdate && updateInfo.IsRequired;
            }
            catch
            {
                // If we can't check for updates, don't force an update
                return false;
            }
        }

        public async Task<bool> ValidateCurrentVersionAsync()
        {
            try
            {
                var updateInfo = await CheckForUpdatesAsync();
                
                if (updateInfo.HasUpdate && updateInfo.IsRequired)
                {
                    _logger?.LogWarning("Forced update required. Current: {Current}, Latest: {Latest}", 
                        CurrentVersion, updateInfo.LatestVersion);
                    
                    await ShowForceUpdateDialog(updateInfo);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to validate current version");
                // If validation fails, allow the app to continue
                return true;
            }
        }

        private async Task ShowForceUpdateDialog(UpdateInfo updateInfo)
        {
            var result = System.Windows.MessageBox.Show(
                $"A required update to version {updateInfo.LatestVersion} is available.\n\n" +
                "You must update to continue using OpenWeb Download Manager.\n\n" +
                "Would you like to download and install the update now?",
                "Required Update",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    await DownloadAndInstallUpdateAsync(updateInfo);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Failed to download update: {ex.Message}\n\n" +
                        "Please visit https://openweb.live/software to download manually.",
                        "Update Failed",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show(
                    "The application will now close. Please visit https://openweb.live/software to download the latest version.",
                    "Update Required",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }

            // Force close the application
            System.Windows.Application.Current.Shutdown();
        }

        public async Task DownloadAndInstallUpdateAsync(UpdateInfo updateInfo)
        {
            try
            {
                _logger?.LogInformation("Starting download of update: {FileName}", updateInfo.FileName);

                // Download the update
                var tempPath = Path.Combine(Path.GetTempPath(), updateInfo.FileName);
                
                using var response = await _httpClient.GetAsync(updateInfo.DownloadUrl);
                response.EnsureSuccessStatusCode();

                using var fileStream = new FileStream(tempPath, FileMode.Create);
                await response.Content.CopyToAsync(fileStream);

                _logger?.LogInformation("Update downloaded to: {Path}", tempPath);

                // Verify file exists and has content
                var fileInfo = new FileInfo(tempPath);
                if (!fileInfo.Exists || fileInfo.Length == 0)
                {
                    throw new Exception("Downloaded file is invalid or empty");
                }

                // Launch the installer
                var processInfo = new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true,
                    Verb = "runas" // Request admin privileges
                };

                Process.Start(processInfo);

                // Close current application
                await Task.Delay(2000); // Give the installer time to start
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to download and install update");
                throw;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}