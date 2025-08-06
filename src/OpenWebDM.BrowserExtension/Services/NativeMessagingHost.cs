using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OpenWebDM.BrowserExtension.Services
{
    public class NativeMessagingHost
    {
        private readonly ILogger<NativeMessagingHost> _logger;
        private NamedPipeServerStream? _pipeServer;
        private bool _isRunning;
        private CancellationTokenSource? _cancellationTokenSource;

        public event EventHandler<DownloadRequestEventArgs>? DownloadRequested;

        public NativeMessagingHost(ILogger<NativeMessagingHost> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync()
        {
            if (_isRunning) return;

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await StartPipeServerAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in native messaging host");
                        await Task.Delay(1000, _cancellationTokenSource.Token);
                    }
                }
            });

            await Task.CompletedTask;
        }

        private async Task StartPipeServerAsync()
        {
            _pipeServer = new NamedPipeServerStream("OpenWebDM_NativeHost", PipeDirection.InOut, 10);
            
            _logger.LogInformation("Waiting for browser extension connection...");
            await _pipeServer.WaitForConnectionAsync(_cancellationTokenSource!.Token);
            _logger.LogInformation("Browser extension connected");

            using var reader = new StreamReader(_pipeServer, Encoding.UTF8);
            using var writer = new StreamWriter(_pipeServer, Encoding.UTF8) { AutoFlush = true };

            while (_pipeServer.IsConnected && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var message = await reader.ReadLineAsync();
                    if (message != null)
                    {
                        await ProcessMessageAsync(message, writer);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from browser extension");
                    break;
                }
            }

            _pipeServer.Disconnect();
            _pipeServer.Dispose();
        }

        private async Task ProcessMessageAsync(string message, StreamWriter writer)
        {
            try
            {
                var request = JsonSerializer.Deserialize<BrowserMessage>(message);
                if (request == null) return;

                switch (request.Type)
                {
                    case "download_request":
                        await HandleDownloadRequest(request, writer);
                        break;
                    case "ping":
                        await SendResponse(writer, new { type = "pong", status = "ok" });
                        break;
                    default:
                        _logger.LogWarning("Unknown message type: {Type}", request.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing browser message: {Message}", message);
            }
        }

        private async Task HandleDownloadRequest(BrowserMessage request, StreamWriter writer)
        {
            if (request.Data?.TryGetValue("url", out var urlObj) == true && urlObj is JsonElement urlElement)
            {
                var url = urlElement.GetString();
                if (!string.IsNullOrEmpty(url))
                {
                    var downloadRequest = new DownloadRequestEventArgs
                    {
                        Url = url,
                        FileName = GetStringFromData(request.Data, "filename") ?? "",
                        Referrer = GetStringFromData(request.Data, "referrer") ?? "",
                        UserAgent = GetStringFromData(request.Data, "userAgent") ?? "",
                        Headers = GetHeadersFromData(request.Data)
                    };

                    DownloadRequested?.Invoke(this, downloadRequest);

                    await SendResponse(writer, new 
                    { 
                        type = "download_response", 
                        status = "accepted",
                        message = "Download request received"
                    });

                    _logger.LogInformation("Download request received for: {Url}", url);
                    return;
                }
            }

            await SendResponse(writer, new 
            { 
                type = "download_response", 
                status = "error", 
                message = "Invalid download request" 
            });
        }

        private string? GetStringFromData(Dictionary<string, JsonElement>? data, string key)
        {
            if (data?.TryGetValue(key, out var element) == true && element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }
            return null;
        }

        private Dictionary<string, string> GetHeadersFromData(Dictionary<string, JsonElement>? data)
        {
            var headers = new Dictionary<string, string>();
            
            if (data?.TryGetValue("headers", out var headersElement) == true && 
                headersElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in headersElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        headers[property.Name] = property.Value.GetString() ?? "";
                    }
                }
            }
            
            return headers;
        }

        private async Task SendResponse(StreamWriter writer, object response)
        {
            var json = JsonSerializer.Serialize(response);
            await writer.WriteLineAsync(json);
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            
            _pipeServer?.Disconnect();
            _pipeServer?.Dispose();
            
            _cancellationTokenSource?.Dispose();
            
            await Task.CompletedTask;
        }

        private class BrowserMessage
        {
            public string Type { get; set; } = string.Empty;
            public Dictionary<string, JsonElement>? Data { get; set; }
        }
    }
}