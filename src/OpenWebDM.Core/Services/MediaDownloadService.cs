using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenWebDM.Core.Models;
using HtmlAgilityPack;

namespace OpenWebDM.Core.Services
{
    public class MediaDownloadService : IMediaDownloadService
    {
        private readonly ILogger<MediaDownloadService> _logger;
        private readonly IDownloadEngine _downloadEngine;
        private readonly HttpClient _httpClient;

        public event EventHandler<MediaExtractionCompletedEventArgs>? MediaExtractionCompleted;
        public event EventHandler<MediaExtractionErrorEventArgs>? MediaExtractionError;

        // Supported media sites patterns
        private readonly Dictionary<string, string> _supportedSites = new()
        {
            { "youtube", @"(?:https?://)?(?:www\.)?(?:youtube\.com/watch\?v=|youtu\.be/)([a-zA-Z0-9_-]{11})" },
            { "vimeo", @"(?:https?://)?(?:www\.)?vimeo\.com/(\d+)" },
            { "dailymotion", @"(?:https?://)?(?:www\.)?dailymotion\.com/video/([a-zA-Z0-9]+)" },
            { "twitch", @"(?:https?://)?(?:www\.)?twitch\.tv/videos/(\d+)" },
            { "tiktok", @"(?:https?://)?(?:www\.)?tiktok\.com/@[^/]+/video/(\d+)" },
            { "facebook", @"(?:https?://)?(?:www\.)?facebook\.com/.*/videos/(\d+)" },
            { "instagram", @"(?:https?://)?(?:www\.)?instagram\.com/(?:p|tv|reel)/([a-zA-Z0-9_-]+)" },
            { "twitter", @"(?:https?://)?(?:www\.)?twitter\.com/[^/]+/status/(\d+)" },
            { "soundcloud", @"(?:https?://)?(?:www\.)?soundcloud\.com/[^/]+/[^/]+" }
        };

        public MediaDownloadService(ILogger<MediaDownloadService> logger, IDownloadEngine downloadEngine)
        {
            _logger = logger;
            _downloadEngine = downloadEngine;
            _httpClient = new HttpClient();
        }

        public async Task<bool> IsMediaUrlAsync(string url)
        {
            try
            {
                foreach (var site in _supportedSites)
                {
                    if (Regex.IsMatch(url, site.Value, RegexOptions.IgnoreCase))
                    {
                        return true;
                    }
                }

                // Check for direct media file URLs
                var uri = new Uri(url);
                var extension = Path.GetExtension(uri.LocalPath).ToLowerInvariant();
                var mediaExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", 
                                            ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a" };
                
                return mediaExtensions.Contains(extension);
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<MediaInfo>> ExtractMediaInfoAsync(string url)
        {
            try
            {
                _logger.LogInformation("Extracting media info from: {Url}", url);

                // Try yt-dlp first (most comprehensive)
                var mediaInfos = await ExtractWithYtDlpAsync(url);
                if (mediaInfos.Any())
                {
                    MediaExtractionCompleted?.Invoke(this, new MediaExtractionCompletedEventArgs
                    {
                        Url = url,
                        MediaInfos = mediaInfos
                    });
                    return mediaInfos;
                }

                // Fallback to manual extraction for supported sites
                var fallbackInfo = await ExtractManuallyAsync(url);
                if (fallbackInfo != null)
                {
                    var result = new List<MediaInfo> { fallbackInfo };
                    MediaExtractionCompleted?.Invoke(this, new MediaExtractionCompletedEventArgs
                    {
                        Url = url,
                        MediaInfos = result
                    });
                    return result;
                }

                // If it's a direct media URL, create basic info
                if (await IsDirectMediaUrlAsync(url))
                {
                    var directInfo = await CreateDirectMediaInfoAsync(url);
                    var result = new List<MediaInfo> { directInfo };
                    MediaExtractionCompleted?.Invoke(this, new MediaExtractionCompletedEventArgs
                    {
                        Url = url,
                        MediaInfos = result
                    });
                    return result;
                }

                return new List<MediaInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract media info from: {Url}", url);
                MediaExtractionError?.Invoke(this, new MediaExtractionErrorEventArgs
                {
                    Url = url,
                    Exception = ex,
                    Message = ex.Message
                });
                return new List<MediaInfo>();
            }
        }

        private async Task<List<MediaInfo>> ExtractWithYtDlpAsync(string url)
        {
            try
            {
                // Check if yt-dlp is available
                var ytDlpPath = FindYtDlpExecutable();
                if (string.IsNullOrEmpty(ytDlpPath))
                {
                    _logger.LogWarning("yt-dlp not found, using fallback extraction methods");
                    return new List<MediaInfo>();
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ytDlpPath,
                        Arguments = $"-J \"{url}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("yt-dlp extraction failed: {Error}", error);
                    return new List<MediaInfo>();
                }

                return ParseYtDlpOutput(output);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "yt-dlp extraction failed");
                return new List<MediaInfo>();
            }
        }

        private string? FindYtDlpExecutable()
        {
            var possiblePaths = new[]
            {
                "yt-dlp",
                "yt-dlp.exe",
                Path.Combine(Environment.CurrentDirectory, "yt-dlp.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "yt-dlp", "yt-dlp.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "yt-dlp", "yt-dlp.exe")
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--version",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    });

                    if (process != null)
                    {
                        process.WaitForExit();
                        if (process.ExitCode == 0)
                        {
                            return path;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }

        private List<MediaInfo> ParseYtDlpOutput(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                var mediaInfo = new MediaInfo
                {
                    Id = root.GetProperty("id").GetString() ?? "",
                    Title = root.GetProperty("title").GetString() ?? "",
                    Description = root.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                    ThumbnailUrl = root.TryGetProperty("thumbnail", out var thumb) ? thumb.GetString() ?? "" : "",
                    Uploader = root.TryGetProperty("uploader", out var uploader) ? uploader.GetString() ?? "" : "",
                    Duration = TimeSpan.FromSeconds(root.TryGetProperty("duration", out var duration) ? duration.GetDouble() : 0),
                    ViewCount = root.TryGetProperty("view_count", out var views) ? views.GetInt64() : 0,
                    LikeCount = root.TryGetProperty("like_count", out var likes) ? likes.GetInt64() : 0,
                    WebpageUrl = root.GetProperty("webpage_url").GetString() ?? "",
                    ExtractorName = root.TryGetProperty("extractor", out var extractor) ? extractor.GetString() ?? "" : ""
                };

                if (root.TryGetProperty("upload_date", out var uploadDate))
                {
                    if (DateTime.TryParseExact(uploadDate.GetString(), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
                    {
                        mediaInfo.UploadDate = date;
                    }
                }

                // Parse formats
                if (root.TryGetProperty("formats", out var formatsArray))
                {
                    foreach (var formatElement in formatsArray.EnumerateArray())
                    {
                        var format = new MediaFormat
                        {
                            FormatId = formatElement.TryGetProperty("format_id", out var formatId) ? formatId.GetString() ?? "" : "",
                            Extension = formatElement.TryGetProperty("ext", out var ext) ? ext.GetString() ?? "" : "",
                            Resolution = formatElement.TryGetProperty("resolution", out var res) ? res.GetString() ?? "" : "",
                            FileSize = formatElement.TryGetProperty("filesize", out var size) ? size.GetInt64() : 0,
                            Width = formatElement.TryGetProperty("width", out var width) ? width.GetInt32() : null,
                            Height = formatElement.TryGetProperty("height", out var height) ? height.GetInt32() : null,
                            Fps = formatElement.TryGetProperty("fps", out var fps) ? fps.GetInt32() : null,
                            VideoCodec = formatElement.TryGetProperty("vcodec", out var vcodec) ? vcodec.GetString() ?? "" : "",
                            AudioCodec = formatElement.TryGetProperty("acodec", out var acodec) ? acodec.GetString() ?? "" : "",
                            AudioBitrate = formatElement.TryGetProperty("abr", out var abr) ? abr.GetInt32() : null,
                            VideoBitrate = formatElement.TryGetProperty("vbr", out var vbr) ? vbr.GetInt32() : null,
                            Quality = formatElement.TryGetProperty("quality", out var quality) ? quality.GetString() ?? "" : "",
                            Url = formatElement.TryGetProperty("url", out var url) ? url.GetString() ?? "" : "",
                            Container = formatElement.TryGetProperty("container", out var container) ? container.GetString() ?? "" : ""
                        };

                        // Determine format type
                        if (format.VideoCodec != "none" && format.AudioCodec != "none")
                            format.Type = MediaFormatType.VideoAudio;
                        else if (format.VideoCodec != "none")
                            format.Type = MediaFormatType.VideoOnly;
                        else if (format.AudioCodec != "none")
                            format.Type = MediaFormatType.AudioOnly;

                        mediaInfo.Formats.Add(format);
                    }
                }

                return new List<MediaInfo> { mediaInfo };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse yt-dlp output");
                return new List<MediaInfo>();
            }
        }

        public async Task<List<MediaFormat>> GetAvailableFormatsAsync(string url)
        {
            var mediaInfos = await ExtractMediaInfoAsync(url);
            return mediaInfos.SelectMany(m => m.Formats).ToList();
        }

        public async Task<MediaInfo?> GetMediaInfoAsync(string url)
        {
            var mediaInfos = await ExtractMediaInfoAsync(url);
            return mediaInfos.FirstOrDefault();
        }

        public async Task<DownloadItem> DownloadMediaAsync(string url, MediaFormat format, string savePath)
        {
            try
            {
                var fileName = $"{format.FormatId}_{DateTime.Now:yyyyMMdd_HHmmss}.{format.Extension}";
                return await _downloadEngine.StartDownloadAsync(format.Url, savePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start media download");
                throw;
            }
        }

        private async Task<MediaInfo?> ExtractManuallyAsync(string url)
        {
            // Basic extraction for known sites when yt-dlp is not available
            try
            {
                var response = await _httpClient.GetAsync(url);
                var html = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var mediaInfo = new MediaInfo
                {
                    WebpageUrl = url,
                    Title = ExtractTitle(doc),
                    Description = ExtractDescription(doc),
                    ThumbnailUrl = ExtractThumbnail(doc),
                    Formats = await ExtractBasicFormats(url, doc)
                };

                return mediaInfo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Manual extraction failed for: {Url}", url);
                return null;
            }
        }

        private async Task<bool> IsDirectMediaUrlAsync(string url)
        {
            try
            {
                var uri = new Uri(url);
                var extension = Path.GetExtension(uri.LocalPath).ToLowerInvariant();
                var mediaExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", 
                                            ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a" };
                
                return mediaExtensions.Contains(extension);
            }
            catch
            {
                return false;
            }
        }

        private async Task<MediaInfo> CreateDirectMediaInfoAsync(string url)
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            var mediaInfo = new MediaInfo
            {
                Id = Guid.NewGuid().ToString(),
                Title = fileName,
                WebpageUrl = url,
                Type = IsVideoExtension(extension) ? MediaType.Video : MediaType.Audio,
                Formats = new List<MediaFormat>
                {
                    new MediaFormat
                    {
                        FormatId = "direct",
                        Extension = extension.TrimStart('.'),
                        Url = url,
                        Type = IsVideoExtension(extension) ? MediaFormatType.VideoAudio : MediaFormatType.AudioOnly,
                        Quality = "Original"
                    }
                }
            };

            return mediaInfo;
        }

        private bool IsVideoExtension(string extension)
        {
            var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v" };
            return videoExtensions.Contains(extension);
        }

        private string ExtractTitle(HtmlDocument doc)
        {
            var titleNode = doc.DocumentNode.SelectSingleNode("//title") ??
                           doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']") ??
                           doc.DocumentNode.SelectSingleNode("//meta[@name='title']");
            
            return titleNode?.GetAttributeValue("content", titleNode.InnerText) ?? "Unknown Title";
        }

        private string ExtractDescription(HtmlDocument doc)
        {
            var descNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']") ??
                          doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
            
            return descNode?.GetAttributeValue("content", "") ?? "";
        }

        private string ExtractThumbnail(HtmlDocument doc)
        {
            var thumbNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']") ??
                           doc.DocumentNode.SelectSingleNode("//meta[@name='twitter:image']");
            
            return thumbNode?.GetAttributeValue("content", "") ?? "";
        }

        private async Task<List<MediaFormat>> ExtractBasicFormats(string url, HtmlDocument doc)
        {
            // Basic format extraction - would need site-specific implementations
            var formats = new List<MediaFormat>();

            // Look for video/audio elements
            var videoNodes = doc.DocumentNode.SelectNodes("//video//source") ?? new HtmlNodeCollection(null);
            var audioNodes = doc.DocumentNode.SelectNodes("//audio//source") ?? new HtmlNodeCollection(null);

            foreach (var node in videoNodes.Concat(audioNodes))
            {
                var src = node.GetAttributeValue("src", "");
                var type = node.GetAttributeValue("type", "");
                
                if (!string.IsNullOrEmpty(src))
                {
                    formats.Add(new MediaFormat
                    {
                        FormatId = Guid.NewGuid().ToString(),
                        Url = src,
                        Extension = Path.GetExtension(src).TrimStart('.'),
                        Type = type.StartsWith("video") ? MediaFormatType.VideoAudio : MediaFormatType.AudioOnly,
                        Quality = "Web"
                    });
                }
            }

            return formats;
        }
    }
}