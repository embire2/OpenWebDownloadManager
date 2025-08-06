using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenWebDM.Core.Services;
using OpenWebDM.BrowserExtension.Services;
using OpenWebDM.ViewModels;
using OpenWebDM.Services;
using System.Windows;

namespace OpenWebDM
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Check for forced updates before showing main window
            var updateService = _serviceProvider.GetRequiredService<IUpdateService>();
            var isValidVersion = await updateService.ValidateCurrentVersionAsync();
            
            if (!isValidVersion)
            {
                // Application will be closed by the update service
                return;
            }

            var mainWindow = _serviceProvider.GetRequiredService<Views.MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Core services
            services.AddSingleton<IDownloadEngine, DownloadEngine>();
            services.AddSingleton<NativeMessagingHost>();
            services.AddSingleton<IUpdateService, UpdateService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<DownloadListViewModel>();
            services.AddTransient<SettingsViewModel>();

            // Views
            services.AddTransient<Views.MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}