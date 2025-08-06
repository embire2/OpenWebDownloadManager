using OpenWebDM.Core.Models;

namespace OpenWebDM.Core.Services
{
    public interface IMediaDownloadService
    {
        Task<List<MediaInfo>> ExtractMediaInfoAsync(string url);
        Task<DownloadItem> DownloadMediaAsync(string url, MediaFormat format, string savePath);
        Task<bool> IsMediaUrlAsync(string url);
        Task<List<MediaFormat>> GetAvailableFormatsAsync(string url);
        Task<MediaInfo?> GetMediaInfoAsync(string url);
        event EventHandler<MediaExtractionCompletedEventArgs>? MediaExtractionCompleted;
        event EventHandler<MediaExtractionErrorEventArgs>? MediaExtractionError;
    }

    public class MediaExtractionCompletedEventArgs : EventArgs
    {
        public string Url { get; set; } = string.Empty;
        public List<MediaInfo> MediaInfos { get; set; } = new();
    }

    public class MediaExtractionErrorEventArgs : EventArgs
    {
        public string Url { get; set; } = string.Empty;
        public Exception Exception { get; set; } = new Exception();
        public string Message { get; set; } = string.Empty;
    }
}