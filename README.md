# OpenWeb Download Manager

![OpenWeb DM Logo](https://openweb.live/openwebdm.png)

**Professional Windows Download Manager that rivals IDM with advanced features**

OpenWeb Download Manager is an advanced, feature-rich download manager for Windows that rivals Internet Download Manager (IDM) with additional modern capabilities and superior performance.

## 🚀 Features

### Core Download Engine
- **Multi-connection downloads** - Up to 16 simultaneous connections per file for maximum speed
- **Dynamic file segmentation** - Intelligent splitting of files for optimal performance
- **Resume interrupted downloads** - Automatic recovery from network interruptions
- **Speed acceleration** - Download speeds up to 10x faster than browser downloads

### Browser Integration
- **Universal browser support** - Chrome, Firefox, Edge, Opera, and more
- **Automatic download detection** - Seamlessly intercepts downloads from web pages
- **One-click downloads** - Download buttons appear next to downloadable links
- **Video/media detection** - Automatically detects streaming media for download

### Advanced Media Downloads
- **YouTube & streaming sites** - Download videos from 1000+ supported sites
- **Multiple format support** - MP4, AVI, MP3, FLAC, and many more
- **Quality selection** - Choose your preferred video/audio quality
- **Batch downloads** - Download entire playlists with one click

### Smart Organization
- **Automatic categorization** - Files organized by type (Videos, Music, Documents, etc.)
- **Custom categories** - Create your own categories with specific rules
- **Flexible save locations** - Different folders for different file types
- **Search and filter** - Quickly find downloads with powerful search

### Scheduling & Automation
- **Download scheduler** - Queue downloads for specific times
- **Bandwidth management** - Limit speeds during specific hours
- **Auto-start/pause** - Automatic download management based on schedule
- **Queue management** - Prioritize downloads with drag-and-drop

### Modern Interface
- **Clean, modern UI** - Intuitive design with dark/light themes
- **Real-time progress** - Live updates on download progress and speeds
- **Detailed statistics** - Track download history and performance
- **Customizable layout** - Arrange interface to your preference

## 📥 Installation

### System Requirements
- Windows 10 or later (64-bit)
- .NET 8.0 Runtime (included in installer)
- 50 MB free disk space
- Administrator privileges (for browser integration)

### Download & Install
1. **[📥 Download OWDM v1.0.0](https://lovemedia.org.za/downloads/OWDM1_0_0.exe)** (Primary)
2. **Alternative**: https://openweb.live/software/OWDM1_0_0.exe
3. Run `OWDM1_0_0.exe` as Administrator
4. Follow the installation wizard
5. Launch OpenWeb Download Manager

### Browser Extensions
Browser extensions are automatically installed during setup. To install manually:

#### Chrome/Edge
1. Open `chrome://extensions/` or `edge://extensions/`
2. Enable "Developer mode"
3. Click "Load unpacked" and select `Extensions/Chrome` folder

#### Firefox
1. Open `about:debugging`
2. Click "This Firefox"
3. Click "Load Temporary Add-on"
4. Select `manifest.json` from `Extensions/Firefox` folder

## 🎯 Quick Start

### Adding Downloads
1. **Copy URL method**: Copy any download URL and OpenWebDM will automatically detect it
2. **Browser integration**: Click the download button that appears next to links
3. **Manual add**: Paste URL in the main window and click Download
4. **Drag & drop**: Drag download links directly into the application

### Video Downloads
1. Navigate to any supported video site (YouTube, Vimeo, etc.)
2. Copy the video URL
3. OpenWebDM will automatically detect available formats
4. Choose your preferred quality and format
5. Click Download

### Scheduling Downloads
1. Go to Downloads → Scheduler
2. Create a new schedule with specific times
3. Set bandwidth limits and download priorities
4. Downloads will start/pause automatically

## ⚙️ Configuration

### Settings Location
- Settings: `%APPDATA%\OpenWebDM\settings.json`
- Downloads: `%USERPROFILE%\Downloads` (default)
- Categories: Automatically created based on file types

### Advanced Settings
```json
{
  "maxConnections": 10,
  "defaultSavePath": "%USERPROFILE%\\Downloads",
  "autoStartDownloads": true,
  "browserIntegration": true,
  "downloadHistory": true,
  "notifications": true
}
```

## 🔧 Building from Source

### Prerequisites
- Visual Studio 2022 or later
- .NET 8.0 SDK
- Windows 10 SDK (latest)
- Inno Setup (for installer creation)

### Build Steps
```powershell
# Clone the repository
git clone https://github.com/openwebdm/openwebdm.git
cd openwebdm

# Build the application
.\build.ps1 -CreateInstaller

# Or build manually
dotnet build OpenWebDM.sln -c Release
dotnet publish src/OpenWebDM/OpenWebDM.csproj -c Release -r win-x64
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Setup
1. Fork the repository
2. Create a feature branch: `git checkout -b feature-name`
3. Make your changes and test thoroughly
4. Submit a pull request with a clear description

### Reporting Issues
Please use our [Issue Tracker](https://github.com/openwebdm/openwebdm/issues) to report bugs or request features.

## 📚 Documentation

- [User Manual](docs/user-manual.md)
- [API Documentation](docs/api.md)
- [Plugin Development](docs/plugins.md)
- [FAQ](docs/faq.md)

## 🛡️ Security & Privacy

OpenWeb Download Manager respects your privacy:
- No data collection or telemetry
- No ads or bundled software
- Open source for transparency
- Local storage only

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by Internet Download Manager
- Uses yt-dlp for video downloads
- Material Design icons
- Modern WPF UI framework

## 📞 Support

- **Documentation**: [OpenWeb Support](https://openweb.co.za/support)
- **Download Updates**: [Software Downloads](https://openweb.live/software)
- **Technical Support**: [support@openweb.co.za](mailto:support@openweb.co.za)
- **Website**: [https://openweb.co.za](https://openweb.co.za)

---

**OpenWeb Download Manager** - Making downloads faster, smarter, and more reliable.

---

**© 2025 [OpenWeb.co.za](https://openweb.co.za) - Advanced Download Solutions**

- **Website**: [https://openweb.co.za](https://openweb.co.za)
- **Download**: [https://openweb.live/software](https://openweb.live/software)
- **Support**: [support@openweb.co.za](mailto:support@openweb.co.za)