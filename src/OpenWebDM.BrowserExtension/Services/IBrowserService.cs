namespace OpenWebDM.BrowserExtension.Services
{
    public interface IBrowserService
    {
        Task RegisterBrowserExtensionsAsync();
        Task<bool> IsExtensionInstalledAsync(string browserName);
        Task InstallExtensionAsync(string browserName);
        Task UninstallExtensionAsync(string browserName);
        Task StartNativeMessagingHostAsync();
        Task StopNativeMessagingHostAsync();
        event EventHandler<DownloadRequestEventArgs>? DownloadRequested;
    }

    public class DownloadRequestEventArgs : EventArgs
    {
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Referrer { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
    }
}