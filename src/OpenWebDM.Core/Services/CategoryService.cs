using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OpenWebDM.Core.Models;

namespace OpenWebDM.Core.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ILogger<CategoryService> _logger;
        private readonly ConcurrentDictionary<string, DownloadCategory> _categories = new();
        private readonly object _lock = new object();

        public CategoryService(ILogger<CategoryService> logger)
        {
            _logger = logger;
        }

        public async Task<List<DownloadCategory>> GetCategoriesAsync()
        {
            return await Task.FromResult(_categories.Values.ToList());
        }

        public async Task<DownloadCategory> GetCategoryAsync(string name)
        {
            _categories.TryGetValue(name, out var category);
            return await Task.FromResult(category ?? GetDefaultCategory());
        }

        public async Task AddCategoryAsync(DownloadCategory category)
        {
            _categories[category.Name] = category;
            
            // Ensure save path exists
            if (!Directory.Exists(category.SavePath))
            {
                Directory.CreateDirectory(category.SavePath);
            }
            
            _logger.LogInformation("Added download category: {CategoryName}", category.Name);
            await Task.CompletedTask;
        }

        public async Task UpdateCategoryAsync(DownloadCategory category)
        {
            _categories[category.Name] = category;
            
            // Ensure save path exists
            if (!Directory.Exists(category.SavePath))
            {
                Directory.CreateDirectory(category.SavePath);
            }
            
            _logger.LogInformation("Updated download category: {CategoryName}", category.Name);
            await Task.CompletedTask;
        }

        public async Task RemoveCategoryAsync(string name)
        {
            if (_categories.TryRemove(name, out var category))
            {
                _logger.LogInformation("Removed download category: {CategoryName}", name);
            }
            
            await Task.CompletedTask;
        }

        public async Task<DownloadCategory> DetermineCategoryAsync(string fileName, string url)
        {
            // First, try to match by file extension
            foreach (var category in _categories.Values.Where(c => c.AutoCategorize))
            {
                if (category.MatchesFileExtension(fileName))
                {
                    _logger.LogDebug("Categorized '{FileName}' as '{CategoryName}' by extension", fileName, category.Name);
                    return await Task.FromResult(category);
                }
            }

            // Try to categorize by URL patterns
            var urlCategory = await CategorizeByUrlPattern(url);
            if (urlCategory != null)
            {
                _logger.LogDebug("Categorized '{Url}' as '{CategoryName}' by URL pattern", url, urlCategory.Name);
                return urlCategory;
            }

            // Try to categorize by MIME type (would need to be determined from headers)
            var mimeCategory = await CategorizeByMimeType(fileName);
            if (mimeCategory != null)
            {
                _logger.LogDebug("Categorized '{FileName}' as '{CategoryName}' by MIME type", fileName, mimeCategory.Name);
                return mimeCategory;
            }

            // Return default category
            var defaultCategory = GetDefaultCategory();
            _logger.LogDebug("Using default category '{CategoryName}' for '{FileName}'", defaultCategory.Name, fileName);
            return await Task.FromResult(defaultCategory);
        }

        public async Task InitializeDefaultCategoriesAsync()
        {
            if (_categories.IsEmpty)
            {
                var defaultCategories = DownloadCategory.GetDefaultCategories();
                
                foreach (var category in defaultCategories)
                {
                    await AddCategoryAsync(category);
                }
                
                _logger.LogInformation("Initialized {Count} default download categories", defaultCategories.Count);
            }
        }

        public async Task<bool> CategoryExistsAsync(string name)
        {
            return await Task.FromResult(_categories.ContainsKey(name));
        }

        private async Task<DownloadCategory?> CategorizeByUrlPattern(string url)
        {
            try
            {
                var uri = new Uri(url);
                var host = uri.Host.ToLowerInvariant();

                // Video sites
                if (host.Contains("youtube") || host.Contains("vimeo") || host.Contains("dailymotion") || 
                    host.Contains("twitch") || host.Contains("tiktok"))
                {
                    return await GetCategoryAsync("Videos");
                }

                // Music sites
                if (host.Contains("soundcloud") || host.Contains("spotify") || host.Contains("bandcamp"))
                {
                    return await GetCategoryAsync("Music");
                }

                // Software/Program sites
                if (host.Contains("github") || host.Contains("sourceforge") || host.Contains("download") ||
                    url.ToLowerInvariant().Contains("/software/") || url.ToLowerInvariant().Contains("/programs/"))
                {
                    return await GetCategoryAsync("Programs");
                }

                // Image sites
                if (host.Contains("imgur") || host.Contains("flickr") || host.Contains("instagram") ||
                    url.ToLowerInvariant().Contains("/images/") || url.ToLowerInvariant().Contains("/photos/"))
                {
                    return await GetCategoryAsync("Images");
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<DownloadCategory?> CategorizeByMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            // Video formats
            if (new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".3gp", ".ogv" }.Contains(extension))
            {
                return await GetCategoryAsync("Videos");
            }

            // Audio formats
            if (new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".opus", ".aiff" }.Contains(extension))
            {
                return await GetCategoryAsync("Music");
            }

            // Document formats
            if (new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".odt", ".ods", ".odp" }.Contains(extension))
            {
                return await GetCategoryAsync("Documents");
            }

            // Program/executable formats
            if (new[] { ".exe", ".msi", ".dmg", ".pkg", ".deb", ".rpm", ".apk", ".ipa", ".app", ".run" }.Contains(extension))
            {
                return await GetCategoryAsync("Programs");
            }

            // Archive formats
            if (new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".cab", ".iso", ".img" }.Contains(extension))
            {
                return await GetCategoryAsync("Archives");
            }

            // Image formats
            if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".svg", ".ico", ".psd" }.Contains(extension))
            {
                return await GetCategoryAsync("Images");
            }

            return null;
        }

        private DownloadCategory GetDefaultCategory()
        {
            return _categories.Values.FirstOrDefault(c => c.IsDefault) ?? 
                   _categories.Values.FirstOrDefault(c => c.Name == "General") ??
                   new DownloadCategory
                   {
                       Name = "General",
                       SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Downloads"),
                       Color = "#2196F3",
                       MaxConnections = 10,
                       IsDefault = true
                   };
        }
    }
}