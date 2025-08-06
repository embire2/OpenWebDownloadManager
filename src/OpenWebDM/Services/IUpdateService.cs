using System;
using System.Threading.Tasks;

namespace OpenWebDM.Services
{
    public interface IUpdateService
    {
        Task<UpdateInfo> CheckForUpdatesAsync();
        Task<bool> IsUpdateRequiredAsync();
        Task DownloadAndInstallUpdateAsync(UpdateInfo updateInfo);
        Task<bool> ValidateCurrentVersionAsync();
        string CurrentVersion { get; }
    }

    public class UpdateInfo
    {
        public bool HasUpdate { get; set; }
        public string LatestVersion { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public bool IsRequired { get; set; }
        public long FileSize { get; set; }
        public string FileHash { get; set; } = string.Empty;
    }
}