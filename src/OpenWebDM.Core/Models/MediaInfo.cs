namespace OpenWebDM.Core.Models
{
    public class MediaInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string Uploader { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public TimeSpan Duration { get; set; }
        public long ViewCount { get; set; }
        public long LikeCount { get; set; }
        public List<MediaFormat> Formats { get; set; } = new();
        public string WebpageUrl { get; set; } = string.Empty;
        public string ExtractorName { get; set; } = string.Empty;
        public MediaType Type { get; set; } = MediaType.Video;
    }

    public class MediaFormat
    {
        public string FormatId { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string Resolution { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? Fps { get; set; }
        public string VideoCodec { get; set; } = string.Empty;
        public string AudioCodec { get; set; } = string.Empty;
        public int? AudioBitrate { get; set; }
        public int? VideoBitrate { get; set; }
        public string Quality { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public MediaFormatType Type { get; set; } = MediaFormatType.VideoAudio;
        public string Container { get; set; } = string.Empty;
        public bool IsLive { get; set; } = false;

        public string FormattedFileSize => FormatBytes(FileSize);
        public string DisplayName => GetDisplayName();

        private string GetDisplayName()
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(Quality))
                parts.Add(Quality);
            else if (!string.IsNullOrEmpty(Resolution))
                parts.Add(Resolution);

            if (!string.IsNullOrEmpty(Extension))
                parts.Add(Extension.ToUpperInvariant());

            if (Type == MediaFormatType.AudioOnly && AudioBitrate.HasValue)
                parts.Add($"{AudioBitrate}kbps");
            else if (Type == MediaFormatType.VideoOnly && VideoBitrate.HasValue)
                parts.Add($"{VideoBitrate}kbps");

            if (FileSize > 0)
                parts.Add(FormattedFileSize);

            return string.Join(" - ", parts);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes == 0) return "Unknown size";
            
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }
    }

    public enum MediaType
    {
        Video,
        Audio,
        Playlist
    }

    public enum MediaFormatType
    {
        VideoAudio,
        VideoOnly,
        AudioOnly
    }
}