using OpenWebDM.Core.Models;

namespace OpenWebDM.Core.Services
{
    public interface ISchedulerService
    {
        Task StartAsync();
        Task StopAsync();
        Task AddScheduleAsync(DownloadSchedule schedule);
        Task UpdateScheduleAsync(DownloadSchedule schedule);
        Task RemoveScheduleAsync(string scheduleId);
        Task<List<DownloadSchedule>> GetSchedulesAsync();
        Task<DownloadSchedule?> GetActiveScheduleAsync();
        event EventHandler<ScheduleActivatedEventArgs>? ScheduleActivated;
        event EventHandler<ScheduleDeactivatedEventArgs>? ScheduleDeactivated;
    }

    public class ScheduleActivatedEventArgs : EventArgs
    {
        public DownloadSchedule Schedule { get; set; } = new();
        public DateTime ActivationTime { get; set; } = DateTime.Now;
    }

    public class ScheduleDeactivatedEventArgs : EventArgs
    {
        public DownloadSchedule Schedule { get; set; } = new();
        public DateTime DeactivationTime { get; set; } = DateTime.Now;
    }
}