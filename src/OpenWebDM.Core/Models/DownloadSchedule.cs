namespace OpenWebDM.Core.Models
{
    public class DownloadSchedule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public ScheduleType Type { get; set; } = ScheduleType.Once;
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
        public List<DayOfWeek> DaysOfWeek { get; set; } = new();
        public int? DayOfMonth { get; set; }
        public TimeSpan? Duration { get; set; }
        public int MaxConcurrentDownloads { get; set; } = 5;
        public int MaxBandwidthKbps { get; set; } = 0; // 0 = unlimited
        public bool AutoStartPaused { get; set; } = false;
        public List<string> IncludedCategories { get; set; } = new();
        
        public bool IsActiveNow()
        {
            if (!IsEnabled) return false;
            
            var now = DateTime.Now;
            
            return Type switch
            {
                ScheduleType.Once => now >= StartTime && (EndTime == null || now <= EndTime),
                ScheduleType.Daily => IsTimeInRange(now) && (EndTime == null || now.Date <= EndTime.Value.Date),
                ScheduleType.Weekly => DaysOfWeek.Contains(now.DayOfWeek) && IsTimeInRange(now) && (EndTime == null || now <= EndTime),
                ScheduleType.Monthly => now.Day == DayOfMonth && IsTimeInRange(now) && (EndTime == null || now <= EndTime),
                _ => false
            };
        }
        
        private bool IsTimeInRange(DateTime now)
        {
            var timeNow = now.TimeOfDay;
            var startTimeOfDay = StartTime.TimeOfDay;
            var endTimeOfDay = Duration.HasValue ? startTimeOfDay.Add(Duration.Value) : TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59));
            
            return timeNow >= startTimeOfDay && timeNow <= endTimeOfDay;
        }
        
        public DateTime? GetNextScheduledTime()
        {
            if (!IsEnabled) return null;
            
            var now = DateTime.Now;
            
            return Type switch
            {
                ScheduleType.Once => StartTime > now ? StartTime : null,
                ScheduleType.Daily => GetNextDailyTime(now),
                ScheduleType.Weekly => GetNextWeeklyTime(now),
                ScheduleType.Monthly => GetNextMonthlyTime(now),
                _ => null
            };
        }
        
        private DateTime? GetNextDailyTime(DateTime now)
        {
            var today = now.Date.Add(StartTime.TimeOfDay);
            return today > now ? today : today.AddDays(1);
        }
        
        private DateTime? GetNextWeeklyTime(DateTime now)
        {
            if (!DaysOfWeek.Any()) return null;
            
            var currentDayOfWeek = (int)now.DayOfWeek;
            var sortedDays = DaysOfWeek.Select(d => (int)d).OrderBy(d => d).ToList();
            
            foreach (var day in sortedDays)
            {
                var daysToAdd = (day - currentDayOfWeek + 7) % 7;
                var nextDate = now.Date.AddDays(daysToAdd).Add(StartTime.TimeOfDay);
                
                if (nextDate > now)
                    return nextDate;
            }
            
            // Next week
            var firstDay = sortedDays.First();
            var daysToNextWeek = (firstDay - currentDayOfWeek + 7) % 7;
            return now.Date.AddDays(daysToNextWeek + 7).Add(StartTime.TimeOfDay);
        }
        
        private DateTime? GetNextMonthlyTime(DateTime now)
        {
            if (!DayOfMonth.HasValue) return null;
            
            var thisMonth = new DateTime(now.Year, now.Month, Math.Min(DayOfMonth.Value, DateTime.DaysInMonth(now.Year, now.Month))).Add(StartTime.TimeOfDay);
            
            if (thisMonth > now)
                return thisMonth;
                
            // Next month
            var nextMonth = now.AddMonths(1);
            return new DateTime(nextMonth.Year, nextMonth.Month, Math.Min(DayOfMonth.Value, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month))).Add(StartTime.TimeOfDay);
        }
    }
    
    public enum ScheduleType
    {
        Once,
        Daily,
        Weekly,
        Monthly
    }
}