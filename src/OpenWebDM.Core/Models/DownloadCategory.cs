namespace OpenWebDM.Core.Models
{
    public class DownloadCategory
    {
        public string Name { get; set; } = string.Empty;
        public string SavePath { get; set; } = string.Empty;
        public List<string> FileExtensions { get; set; } = new();
        public string Color { get; set; } = "#2196F3";
        public bool AutoCategorize { get; set; } = true;
        public int MaxConnections { get; set; } = 10;
        public bool IsDefault { get; set; } = false;

        public static List<DownloadCategory> GetDefaultCategories()
        {
            return new List<DownloadCategory>
            {
                new()
                {
                    Name = "General",
                    SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Downloads"),
                    FileExtensions = new(),
                    Color = "#2196F3",
                    AutoCategorize = true,
                    MaxConnections = 10,
                    IsDefault = true
                },
                new()
                {
                    Name = "Videos",
                    SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Downloads"),
                    FileExtensions = new() { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v" },
                    Color = "#E91E63",
                    AutoCategorize = true,
                    MaxConnections = 16
                },
                new()
                {
                    Name = "Music",
                    SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Downloads"),
                    FileExtensions = new() { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a" },
                    Color = "#9C27B0",
                    AutoCategorize = true,
                    MaxConnections = 8
                },
                new()
                {
                    Name = "Documents",
                    SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Downloads", "Documents"),
                    FileExtensions = new() { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf" },
                    Color = "#4CAF50",
                    AutoCategorize = true,
                    MaxConnections = 6
                },
                new()
                {
                    Name = "Programs",
                    SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Downloads", "Programs"),
                    FileExtensions = new() { ".exe", ".msi", ".dmg", ".pkg", ".deb", ".rpm", ".apk", ".ipa" },
                    Color = "#FF5722",
                    AutoCategorize = true,
                    MaxConnections = 4
                },
                new()
                {
                    Name = "Archives",
                    SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Downloads", "Archives"),
                    FileExtensions = new() { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".cab" },
                    Color = "#FF9800",
                    AutoCategorize = true,
                    MaxConnections = 8
                },
                new()
                {
                    Name = "Images",
                    SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Downloads"),
                    FileExtensions = new() { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".svg" },
                    Color = "#00BCD4",
                    AutoCategorize = true,
                    MaxConnections = 12
                }
            };
        }

        public bool MatchesFileExtension(string fileName)
        {
            if (!AutoCategorize || !FileExtensions.Any()) return false;
            
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return FileExtensions.Contains(extension);
        }
    }
}