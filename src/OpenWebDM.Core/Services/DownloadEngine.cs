using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;
using OpenWebDM.Core.Models;

namespace OpenWebDM.Core.Services
{
    public class DownloadEngine : IDownloadEngine
    {
        private readonly ILogger<DownloadEngine> _logger;
        private readonly ConcurrentDictionary<string, DownloadItem> _activeDownloads = new();
        private readonly ConcurrentDictionary<string, List<DownloadSegment>> _downloadSegments = new();
        private readonly HttpClient _httpClient;

        public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
        public event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;
        public event EventHandler<DownloadErrorEventArgs>? DownloadError;

        public DownloadEngine(ILogger<DownloadEngine> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "OpenWebDM/1.0 (Windows NT 10.0; Win64; x64)");
        }

        public async Task<DownloadItem> StartDownloadAsync(string url, string savePath, int connections = 10)
        {
            var downloadItem = new DownloadItem
            {
                Url = url,
                SavePath = savePath,
                Connections = connections,
                Status = DownloadStatus.Pending,
                StartTime = DateTime.Now
            };

            try
            {
                downloadItem.FileName = await GetFileNameFromUrl(url);
                downloadItem.TotalSize = await GetContentLengthAsync(url);
                
                _activeDownloads[downloadItem.Id] = downloadItem;

                var supportsRanges = await SupportsRangeRequests(url);
                
                if (supportsRanges && downloadItem.TotalSize > 0)
                {
                    await StartMultiConnectionDownload(downloadItem);
                }
                else
                {
                    await StartSingleConnectionDownload(downloadItem);
                }

                return downloadItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start download for {Url}", url);
                downloadItem.Status = DownloadStatus.Failed;
                DownloadError?.Invoke(this, new DownloadErrorEventArgs
                {
                    DownloadId = downloadItem.Id,
                    Exception = ex,
                    Message = ex.Message
                });
                return downloadItem;
            }
        }

        private async Task StartMultiConnectionDownload(DownloadItem downloadItem)
        {
            downloadItem.Status = DownloadStatus.Downloading;
            
            var segments = CreateDownloadSegments(downloadItem);
            _downloadSegments[downloadItem.Id] = segments;

            var tasks = segments.Select(segment => DownloadSegmentAsync(downloadItem, segment)).ToArray();

            var progressTimer = new System.Timers.Timer(1000);
            progressTimer.Elapsed += (s, e) => UpdateProgress(downloadItem);
            progressTimer.Start();

            try
            {
                await Task.WhenAll(tasks);
                await MergeSegments(downloadItem, segments);
                
                downloadItem.Status = DownloadStatus.Completed;
                downloadItem.EndTime = DateTime.Now;
                
                DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs
                {
                    DownloadId = downloadItem.Id,
                    IsSuccess = true,
                    FilePath = Path.Combine(downloadItem.SavePath, downloadItem.FileName)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Multi-connection download failed for {Url}", downloadItem.Url);
                downloadItem.Status = DownloadStatus.Failed;
                
                DownloadError?.Invoke(this, new DownloadErrorEventArgs
                {
                    DownloadId = downloadItem.Id,
                    Exception = ex,
                    Message = ex.Message
                });
            }
            finally
            {
                progressTimer.Stop();
                progressTimer.Dispose();
            }
        }

        private async Task StartSingleConnectionDownload(DownloadItem downloadItem)
        {
            downloadItem.Status = DownloadStatus.Downloading;
            
            var progressTimer = new System.Timers.Timer(1000);
            progressTimer.Elapsed += (s, e) => UpdateProgress(downloadItem);
            progressTimer.Start();

            try
            {
                using var response = await _httpClient.GetAsync(downloadItem.Url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var filePath = Path.Combine(downloadItem.SavePath, downloadItem.FileName);
                Directory.CreateDirectory(downloadItem.SavePath);

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadItem.DownloadedSize += bytesRead;
                }

                downloadItem.Status = DownloadStatus.Completed;
                downloadItem.EndTime = DateTime.Now;
                
                DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs
                {
                    DownloadId = downloadItem.Id,
                    IsSuccess = true,
                    FilePath = filePath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Single-connection download failed for {Url}", downloadItem.Url);
                downloadItem.Status = DownloadStatus.Failed;
                
                DownloadError?.Invoke(this, new DownloadErrorEventArgs
                {
                    DownloadId = downloadItem.Id,
                    Exception = ex,
                    Message = ex.Message
                });
            }
            finally
            {
                progressTimer.Stop();
                progressTimer.Dispose();
            }
        }

        private List<DownloadSegment> CreateDownloadSegments(DownloadItem downloadItem)
        {
            var segments = new List<DownloadSegment>();
            var segmentSize = downloadItem.TotalSize / downloadItem.Connections;

            for (int i = 0; i < downloadItem.Connections; i++)
            {
                var startByte = i * segmentSize;
                var endByte = i == downloadItem.Connections - 1 ? downloadItem.TotalSize - 1 : (i + 1) * segmentSize - 1;

                segments.Add(new DownloadSegment
                {
                    Id = i,
                    StartByte = startByte,
                    EndByte = endByte,
                    CurrentByte = startByte,
                    Status = SegmentStatus.Pending,
                    TempFilePath = Path.Combine(Path.GetTempPath(), $"{downloadItem.Id}_segment_{i}.tmp"),
                    CancellationTokenSource = new CancellationTokenSource()
                });
            }

            return segments;
        }

        private async Task DownloadSegmentAsync(DownloadItem downloadItem, DownloadSegment segment)
        {
            segment.Status = SegmentStatus.Downloading;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, downloadItem.Url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(segment.StartByte, segment.EndByte);

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, 
                    segment.CancellationTokenSource!.Token);
                response.EnsureSuccessStatusCode();

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(segment.TempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, segment.CancellationTokenSource.Token)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, segment.CancellationTokenSource.Token);
                    segment.CurrentByte += bytesRead;
                    downloadItem.DownloadedSize += bytesRead;

                    if (segment.CancellationTokenSource.Token.IsCancellationRequested)
                        break;
                }

                segment.Status = segment.IsCompleted ? SegmentStatus.Completed : SegmentStatus.Failed;
            }
            catch (OperationCanceledException)
            {
                segment.Status = SegmentStatus.Paused;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Segment {SegmentId} download failed", segment.Id);
                segment.Status = SegmentStatus.Failed;
            }
        }

        private async Task MergeSegments(DownloadItem downloadItem, List<DownloadSegment> segments)
        {
            var filePath = Path.Combine(downloadItem.SavePath, downloadItem.FileName);
            Directory.CreateDirectory(downloadItem.SavePath);

            using var finalFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);

            foreach (var segment in segments.OrderBy(s => s.Id))
            {
                if (File.Exists(segment.TempFilePath))
                {
                    using var segmentStream = new FileStream(segment.TempFilePath, FileMode.Open, FileAccess.Read);
                    await segmentStream.CopyToAsync(finalFileStream);
                    
                    File.Delete(segment.TempFilePath);
                }
            }
        }

        private void UpdateProgress(DownloadItem downloadItem)
        {
            if (downloadItem.TotalSize <= 0) return;

            var elapsed = DateTime.Now - downloadItem.StartTime;
            var speed = elapsed.TotalSeconds > 0 ? downloadItem.DownloadedSize / elapsed.TotalSeconds : 0;
            var remaining = downloadItem.TotalSize - downloadItem.DownloadedSize;
            var eta = speed > 0 ? TimeSpan.FromSeconds(remaining / speed) : TimeSpan.Zero;

            downloadItem.Speed = FormatSpeed(speed);
            downloadItem.EstimatedTime = eta;

            DownloadProgress?.Invoke(this, new DownloadProgressEventArgs
            {
                DownloadId = downloadItem.Id,
                DownloadedBytes = downloadItem.DownloadedSize,
                TotalBytes = downloadItem.TotalSize,
                Progress = downloadItem.Progress,
                Speed = downloadItem.Speed,
                EstimatedTime = eta
            });
        }

        public async Task PauseDownloadAsync(string downloadId)
        {
            if (_downloadSegments.TryGetValue(downloadId, out var segments))
            {
                foreach (var segment in segments)
                {
                    segment.CancellationTokenSource?.Cancel();
                }
            }

            if (_activeDownloads.TryGetValue(downloadId, out var download))
            {
                download.Status = DownloadStatus.Paused;
            }
        }

        public async Task ResumeDownloadAsync(string downloadId)
        {
            if (_activeDownloads.TryGetValue(downloadId, out var download))
            {
                download.Status = DownloadStatus.Downloading;
                
                if (_downloadSegments.TryGetValue(downloadId, out var segments))
                {
                    foreach (var segment in segments.Where(s => s.Status == SegmentStatus.Paused))
                    {
                        segment.CancellationTokenSource = new CancellationTokenSource();
                        _ = DownloadSegmentAsync(download, segment);
                    }
                }
            }
        }

        public async Task CancelDownloadAsync(string downloadId)
        {
            await PauseDownloadAsync(downloadId);

            if (_activeDownloads.TryGetValue(downloadId, out var download))
            {
                download.Status = DownloadStatus.Cancelled;
            }

            if (_downloadSegments.TryGetValue(downloadId, out var segments))
            {
                foreach (var segment in segments)
                {
                    if (File.Exists(segment.TempFilePath))
                    {
                        try { File.Delete(segment.TempFilePath); } catch { }
                    }
                }
                _downloadSegments.TryRemove(downloadId, out _);
            }

            _activeDownloads.TryRemove(downloadId, out _);
        }

        public async Task<bool> SupportsRangeRequests(string url)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await _httpClient.SendAsync(request);
                
                return response.Headers.AcceptRanges?.Contains("bytes") == true ||
                       response.Content.Headers.ContentRange != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<long> GetContentLengthAsync(string url)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await _httpClient.SendAsync(request);
                
                return response.Content.Headers.ContentLength ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<string> GetFileNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var fileName = Path.GetFileName(uri.LocalPath);
                
                if (string.IsNullOrEmpty(fileName) || !Path.HasExtension(fileName))
                {
                    var request = new HttpRequestMessage(HttpMethod.Head, url);
                    using var response = await _httpClient.SendAsync(request);
                    
                    if (response.Content.Headers.ContentDisposition?.FileName != null)
                    {
                        fileName = response.Content.Headers.ContentDisposition.FileName.Trim('"');
                    }
                    else
                    {
                        fileName = $"download_{DateTime.Now:yyyyMMdd_HHmmss}";
                        
                        var contentType = response.Content.Headers.ContentType?.MediaType;
                        if (!string.IsNullOrEmpty(contentType))
                        {
                            var extension = GetExtensionFromMimeType(contentType);
                            if (!string.IsNullOrEmpty(extension))
                                fileName += extension;
                        }
                    }
                }
                
                return fileName;
            }
            catch
            {
                return $"download_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
        }

        private static string GetExtensionFromMimeType(string mimeType)
        {
            return mimeType.ToLower() switch
            {
                "text/html" => ".html",
                "text/plain" => ".txt",
                "application/pdf" => ".pdf",
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "video/mp4" => ".mp4",
                "video/avi" => ".avi",
                "audio/mp3" => ".mp3",
                "application/zip" => ".zip",
                "application/x-rar-compressed" => ".rar",
                _ => ""
            };
        }

        private static string FormatSpeed(double bytesPerSecond)
        {
            string[] suffixes = { "B/s", "KB/s", "MB/s", "GB/s" };
            int counter = 0;
            double number = bytesPerSecond;
            
            while (number >= 1024 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }
            
            return $"{number:F2} {suffixes[counter]}";
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}