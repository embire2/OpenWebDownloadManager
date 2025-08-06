using System.ComponentModel;

namespace OpenWebDM.Core.Models
{
    public class DownloadItem : INotifyPropertyChanged
    {
        private string _url = string.Empty;
        private string _fileName = string.Empty;
        private string _savePath = string.Empty;
        private long _totalSize;
        private long _downloadedSize;
        private DownloadStatus _status;
        private double _progress;
        private string _speed = string.Empty;
        private TimeSpan _estimatedTime;
        private int _connections = 10;
        private DateTime _startTime;
        private DateTime _endTime;
        private string _category = "General";

        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public string Url
        {
            get => _url;
            set { _url = value; OnPropertyChanged(); }
        }

        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }

        public string SavePath
        {
            get => _savePath;
            set { _savePath = value; OnPropertyChanged(); }
        }

        public long TotalSize
        {
            get => _totalSize;
            set { _totalSize = value; OnPropertyChanged(); UpdateProgress(); }
        }

        public long DownloadedSize
        {
            get => _downloadedSize;
            set { _downloadedSize = value; OnPropertyChanged(); UpdateProgress(); }
        }

        public DownloadStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public double Progress
        {
            get => _progress;
            private set { _progress = value; OnPropertyChanged(); }
        }

        public string Speed
        {
            get => _speed;
            set { _speed = value; OnPropertyChanged(); }
        }

        public TimeSpan EstimatedTime
        {
            get => _estimatedTime;
            set { _estimatedTime = value; OnPropertyChanged(); }
        }

        public int Connections
        {
            get => _connections;
            set { _connections = value; OnPropertyChanged(); }
        }

        public DateTime StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(); }
        }

        public DateTime EndTime
        {
            get => _endTime;
            set { _endTime = value; OnPropertyChanged(); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        public string FileExtension => Path.GetExtension(FileName);

        public string FormattedTotalSize => FormatBytes(TotalSize);
        public string FormattedDownloadedSize => FormatBytes(DownloadedSize);

        private void UpdateProgress()
        {
            Progress = TotalSize > 0 ? (double)DownloadedSize / TotalSize * 100 : 0;
        }

        private static string FormatBytes(long bytes)
        {
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum DownloadStatus
    {
        Pending,
        Downloading,
        Paused,
        Completed,
        Failed,
        Cancelled
    }
}