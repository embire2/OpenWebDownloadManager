using OpenWebDM.Core.Models;

namespace OpenWebDM.Core.Services
{
    public interface IDownloadEngine
    {
        Task<DownloadItem> StartDownloadAsync(string url, string savePath, int connections = 10);
        Task PauseDownloadAsync(string downloadId);
        Task ResumeDownloadAsync(string downloadId);
        Task CancelDownloadAsync(string downloadId);
        Task<bool> SupportsRangeRequests(string url);
        Task<long> GetContentLengthAsync(string url);
        Task<string> GetFileNameFromUrl(string url);
        event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
        event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;
        event EventHandler<DownloadErrorEventArgs>? DownloadError;
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public string DownloadId { get; set; } = string.Empty;
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
        public double Progress { get; set; }
        public string Speed { get; set; } = string.Empty;
        public TimeSpan EstimatedTime { get; set; }
    }

    public class DownloadCompletedEventArgs : EventArgs
    {
        public string DownloadId { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string FilePath { get; set; } = string.Empty;
    }

    public class DownloadErrorEventArgs : EventArgs
    {
        public string DownloadId { get; set; } = string.Empty;
        public Exception Exception { get; set; } = new Exception();
        public string Message { get; set; } = string.Empty;
    }
}