using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using OpenWebDM.Core.Models;
using OpenWebDM.Core.Services;
using OpenWebDM.BrowserExtension.Services;

namespace OpenWebDM.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IDownloadEngine _downloadEngine;
        private readonly NativeMessagingHost _nativeMessagingHost;
        private readonly ILogger<MainViewModel> _logger;
        
        private string _downloadUrl = string.Empty;
        private string _savePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Downloads";
        private bool _isDownloading = false;
        private string _statusText = "Ready";
        private int _activeDownloads = 0;
        private string _totalSpeed = "0 B/s";

        public ObservableCollection<DownloadItem> Downloads { get; } = new();
        
        public string DownloadUrl
        {
            get => _downloadUrl;
            set { _downloadUrl = value; OnPropertyChanged(); }
        }

        public string SavePath
        {
            get => _savePath;
            set { _savePath = value; OnPropertyChanged(); }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set { _isDownloading = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public int ActiveDownloads
        {
            get => _activeDownloads;
            set { _activeDownloads = value; OnPropertyChanged(); }
        }

        public string TotalSpeed
        {
            get => _totalSpeed;
            set { _totalSpeed = value; OnPropertyChanged(); }
        }

        public ICommand StartDownloadCommand { get; }
        public ICommand PauseDownloadCommand { get; }
        public ICommand ResumeDownloadCommand { get; }
        public ICommand CancelDownloadCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand BrowseFolderCommand { get; }
        public ICommand ClearCompletedCommand { get; }

        public MainViewModel(IDownloadEngine downloadEngine, NativeMessagingHost nativeMessagingHost, ILogger<MainViewModel> logger)
        {
            _downloadEngine = downloadEngine;
            _nativeMessagingHost = nativeMessagingHost;
            _logger = logger;

            StartDownloadCommand = new RelayCommand(async () => await StartDownloadAsync(), CanStartDownload);
            PauseDownloadCommand = new RelayCommand<DownloadItem>(async (item) => await PauseDownloadAsync(item));
            ResumeDownloadCommand = new RelayCommand<DownloadItem>(async (item) => await ResumeDownloadAsync(item));
            CancelDownloadCommand = new RelayCommand<DownloadItem>(async (item) => await CancelDownloadAsync(item));
            OpenFileCommand = new RelayCommand<DownloadItem>(OpenFile);
            OpenFolderCommand = new RelayCommand<DownloadItem>(OpenFolder);
            BrowseFolderCommand = new RelayCommand(BrowseFolder);
            ClearCompletedCommand = new RelayCommand(ClearCompleted);

            SubscribeToEvents();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                await _nativeMessagingHost.StartAsync();
                StatusText = "Browser integration active";
                _logger.LogInformation("Native messaging host started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start native messaging host");
                StatusText = "Browser integration unavailable";
            }
        }

        private void SubscribeToEvents()
        {
            _downloadEngine.DownloadProgress += OnDownloadProgress;
            _downloadEngine.DownloadCompleted += OnDownloadCompleted;
            _downloadEngine.DownloadError += OnDownloadError;
            _nativeMessagingHost.DownloadRequested += OnDownloadRequested;
        }

        private async void OnDownloadRequested(object? sender, DownloadRequestEventArgs e)
        {
            try
            {
                var downloadItem = await _downloadEngine.StartDownloadAsync(e.Url, _savePath);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Downloads.Insert(0, downloadItem);
                    UpdateStatus();
                });

                _logger.LogInformation("Download started from browser: {Url}", e.Url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start download from browser request");
            }
        }

        private void OnDownloadProgress(object? sender, DownloadProgressEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateStatus();
                CalculateTotalSpeed();
            });
        }

        private void OnDownloadCompleted(object? sender, DownloadCompletedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = $"Download completed: {Path.GetFileName(e.FilePath)}";
                UpdateStatus();
            });
        }

        private void OnDownloadError(object? sender, DownloadErrorEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = $"Download error: {e.Message}";
                UpdateStatus();
            });
        }

        private async Task StartDownloadAsync()
        {
            if (string.IsNullOrWhiteSpace(DownloadUrl)) return;

            try
            {
                IsDownloading = true;
                StatusText = "Starting download...";

                var downloadItem = await _downloadEngine.StartDownloadAsync(DownloadUrl, SavePath);
                Downloads.Insert(0, downloadItem);

                DownloadUrl = string.Empty;
                UpdateStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start download");
                StatusText = $"Error: {ex.Message}";
            }
            finally
            {
                IsDownloading = false;
            }
        }

        private bool CanStartDownload()
        {
            return !string.IsNullOrWhiteSpace(DownloadUrl) && !IsDownloading;
        }

        private async Task PauseDownloadAsync(DownloadItem? item)
        {
            if (item == null) return;

            try
            {
                await _downloadEngine.PauseDownloadAsync(item.Id);
                StatusText = $"Paused: {item.FileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pause download");
                StatusText = $"Error pausing download: {ex.Message}";
            }
        }

        private async Task ResumeDownloadAsync(DownloadItem? item)
        {
            if (item == null) return;

            try
            {
                await _downloadEngine.ResumeDownloadAsync(item.Id);
                StatusText = $"Resumed: {item.FileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resume download");
                StatusText = $"Error resuming download: {ex.Message}";
            }
        }

        private async Task CancelDownloadAsync(DownloadItem? item)
        {
            if (item == null) return;

            try
            {
                await _downloadEngine.CancelDownloadAsync(item.Id);
                Downloads.Remove(item);
                StatusText = $"Cancelled: {item.FileName}";
                UpdateStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel download");
                StatusText = $"Error cancelling download: {ex.Message}";
            }
        }

        private void OpenFile(DownloadItem? item)
        {
            if (item?.Status != DownloadStatus.Completed) return;

            try
            {
                var filePath = Path.Combine(item.SavePath, item.FileName);
                if (File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open file");
                StatusText = $"Error opening file: {ex.Message}";
            }
        }

        private void OpenFolder(DownloadItem? item)
        {
            if (item == null) return;

            try
            {
                var filePath = Path.Combine(item.SavePath, item.FileName);
                if (File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
                else
                {
                    System.Diagnostics.Process.Start("explorer.exe", item.SavePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open folder");
                StatusText = $"Error opening folder: {ex.Message}";
            }
        }

        private void BrowseFolder()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Download Folder",
                InitialDirectory = SavePath
            };

            if (dialog.ShowDialog() == true)
            {
                SavePath = dialog.FolderName;
            }
        }

        private void ClearCompleted()
        {
            var completedItems = Downloads.Where(d => d.Status == DownloadStatus.Completed).ToList();
            foreach (var item in completedItems)
            {
                Downloads.Remove(item);
            }
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            ActiveDownloads = Downloads.Count(d => d.Status == DownloadStatus.Downloading);
            
            if (ActiveDownloads > 0)
            {
                StatusText = $"{ActiveDownloads} active download(s)";
            }
            else if (Downloads.Any())
            {
                var completed = Downloads.Count(d => d.Status == DownloadStatus.Completed);
                StatusText = $"{completed} download(s) completed";
            }
            else
            {
                StatusText = "Ready";
            }
        }

        private void CalculateTotalSpeed()
        {
            var activeDownloads = Downloads.Where(d => d.Status == DownloadStatus.Downloading);
            if (activeDownloads.Any())
            {
                var totalSpeedText = activeDownloads.FirstOrDefault()?.Speed ?? "0 B/s";
                TotalSpeed = totalSpeedText;
            }
            else
            {
                TotalSpeed = "0 B/s";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            _executeAsync = executeAsync;
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _executeAsync = () => { execute(); return Task.CompletedTask; };
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public async void Execute(object? parameter)
        {
            await _executeAsync();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _executeAsync;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Func<T?, Task> executeAsync, Func<T?, bool>? canExecute = null)
        {
            _executeAsync = executeAsync;
            _canExecute = canExecute;
        }

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _executeAsync = (param) => { execute(param); return Task.CompletedTask; };
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

        public async void Execute(object? parameter)
        {
            await _executeAsync((T?)parameter);
        }
    }
}