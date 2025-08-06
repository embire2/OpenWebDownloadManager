using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OpenWebDM.Core.Models;

namespace OpenWebDM.Core.Services
{
    public class SchedulerService : ISchedulerService
    {
        private readonly ILogger<SchedulerService> _logger;
        private readonly ConcurrentBag<DownloadSchedule> _schedules = new();
        private readonly Timer _timer;
        private DownloadSchedule? _activeSchedule;
        private readonly object _lock = new object();

        public event EventHandler<ScheduleActivatedEventArgs>? ScheduleActivated;
        public event EventHandler<ScheduleDeactivatedEventArgs>? ScheduleDeactivated;

        public SchedulerService(ILogger<SchedulerService> logger)
        {
            _logger = logger;
            _timer = new Timer(CheckSchedules, null, Timeout.Infinite, Timeout.Infinite);
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("Starting scheduler service");
            _timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1)); // Check every minute
            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _logger.LogInformation("Stopping scheduler service");
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            
            if (_activeSchedule != null)
            {
                ScheduleDeactivated?.Invoke(this, new ScheduleDeactivatedEventArgs
                {
                    Schedule = _activeSchedule,
                    DeactivationTime = DateTime.Now
                });
                _activeSchedule = null;
            }
            
            await Task.CompletedTask;
        }

        public async Task AddScheduleAsync(DownloadSchedule schedule)
        {
            _schedules.Add(schedule);
            _logger.LogInformation("Added download schedule: {ScheduleName}", schedule.Name);
            await Task.CompletedTask;
        }

        public async Task UpdateScheduleAsync(DownloadSchedule schedule)
        {
            await RemoveScheduleAsync(schedule.Id);
            await AddScheduleAsync(schedule);
        }

        public async Task RemoveScheduleAsync(string scheduleId)
        {
            var schedulesToKeep = _schedules.Where(s => s.Id != scheduleId).ToList();
            
            // Clear and re-add (ConcurrentBag doesn't have Remove)
            while (_schedules.TryTake(out _)) { }
            
            foreach (var schedule in schedulesToKeep)
            {
                _schedules.Add(schedule);
            }
            
            _logger.LogInformation("Removed download schedule: {ScheduleId}", scheduleId);
            await Task.CompletedTask;
        }

        public async Task<List<DownloadSchedule>> GetSchedulesAsync()
        {
            return await Task.FromResult(_schedules.ToList());
        }

        public async Task<DownloadSchedule?> GetActiveScheduleAsync()
        {
            return await Task.FromResult(_activeSchedule);
        }

        private void CheckSchedules(object? state)
        {
            lock (_lock)
            {
                try
                {
                    var now = DateTime.Now;
                    var currentActiveSchedule = _schedules.FirstOrDefault(s => s.IsActiveNow());

                    // Check if active schedule changed
                    if (currentActiveSchedule != _activeSchedule)
                    {
                        // Deactivate previous schedule
                        if (_activeSchedule != null)
                        {
                            _logger.LogInformation("Deactivating schedule: {ScheduleName}", _activeSchedule.Name);
                            ScheduleDeactivated?.Invoke(this, new ScheduleDeactivatedEventArgs
                            {
                                Schedule = _activeSchedule,
                                DeactivationTime = now
                            });
                        }

                        // Activate new schedule
                        if (currentActiveSchedule != null)
                        {
                            _logger.LogInformation("Activating schedule: {ScheduleName}", currentActiveSchedule.Name);
                            ScheduleActivated?.Invoke(this, new ScheduleActivatedEventArgs
                            {
                                Schedule = currentActiveSchedule,
                                ActivationTime = now
                            });
                        }

                        _activeSchedule = currentActiveSchedule;
                    }

                    // Log next scheduled times
                    if (_schedules.Any())
                    {
                        var nextSchedule = _schedules
                            .Where(s => s.IsEnabled)
                            .Select(s => new { Schedule = s, NextTime = s.GetNextScheduledTime() })
                            .Where(x => x.NextTime.HasValue)
                            .OrderBy(x => x.NextTime)
                            .FirstOrDefault();

                        if (nextSchedule != null)
                        {
                            _logger.LogDebug("Next scheduled download: {ScheduleName} at {NextTime}", 
                                nextSchedule.Schedule.Name, nextSchedule.NextTime);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking download schedules");
                }
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}