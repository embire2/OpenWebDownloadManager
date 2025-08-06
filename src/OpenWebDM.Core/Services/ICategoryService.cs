using OpenWebDM.Core.Models;

namespace OpenWebDM.Core.Services
{
    public interface ICategoryService
    {
        Task<List<DownloadCategory>> GetCategoriesAsync();
        Task<DownloadCategory> GetCategoryAsync(string name);
        Task AddCategoryAsync(DownloadCategory category);
        Task UpdateCategoryAsync(DownloadCategory category);
        Task RemoveCategoryAsync(string name);
        Task<DownloadCategory> DetermineCategoryAsync(string fileName, string url);
        Task InitializeDefaultCategoriesAsync();
        Task<bool> CategoryExistsAsync(string name);
    }
}