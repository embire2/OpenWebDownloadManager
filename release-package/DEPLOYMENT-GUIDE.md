# OpenWeb Download Manager - Deployment Guide

## Overview
This guide explains how to build and deploy OpenWeb Download Manager to https://lovemedia.org.za/downloads/

## Prerequisites

### Development Environment
- **Windows 10/11** (required for WPF compilation)
- **Visual Studio 2022** or **.NET 8.0 SDK**
- **Inno Setup 6.0+** (free from https://jrsoftware.org/isdl.php)
- **PowerShell 5.0+**

## Build Process

### 1. Clone/Download Source Code
```bash
# Download the complete OpenWebDM source code
# All files from /root/openwebdm/ directory
```

### 2. Install Dependencies
```powershell
# Navigate to project directory
cd OpenWebDM

# Restore NuGet packages
dotnet restore OpenWebDM.sln
```

### 3. Build Application
```powershell
# Build using the provided script
.\build.ps1 -CreateInstaller

# OR manually:
dotnet build OpenWebDM.sln -c Release
dotnet publish src\OpenWebDM\OpenWebDM.csproj -c Release -r win-x64 --self-contained
ISCC.exe installer\setup.iss
```

### 4. Expected Output
- **File**: `installer\Output\OWDM1_0_0.exe`
- **Size**: ~65 MB
- **Type**: Windows Installer (Self-contained .NET 8.0 application)

## Deployment to Love Media

### 1. Upload Installer
Replace the placeholder with the real installer:
```bash
# On the Love Media server
cd /root/lovemedia/LoveMediaFoundation/public/downloads/
rm OWDM1_0_0.exe.placeholder
# Upload OWDM1_0_0.exe here
```

### 2. File Permissions
```bash
# Ensure proper permissions
chmod 644 OWDM1_0_0.exe
```

### 3. Verify Access
The file will be available at:
- **Primary**: https://lovemedia.org.za/downloads/OWDM1_0_0.exe
- **Page**: https://lovemedia.org.za/downloads/

## Server Configuration

The Love Media server is already configured to serve static files from the `public/downloads/` directory. No additional server configuration is required.

### Express.js Static Serving
```javascript
// Already implemented in server/vite.ts
app.use(express.static(distPath)); // serves public/ directory
```

## Security Considerations

### 1. File Integrity
- Consider adding SHA256 checksums for download verification
- Implement basic malware scanning before deployment

### 2. Download Limits
- Monitor bandwidth usage
- Consider implementing download rate limiting if needed

### 3. HTTPS
- Ensure all downloads are served over HTTPS
- Love Media already has SSL configured

## Update Process

When releasing new versions:

### 1. Version Naming
- Version 1.0.1 → `OWDM1_0_1.exe`
- Version 1.1.0 → `OWDM1_1_0.exe`
- Version 2.0.0 → `OWDM2_0_0.exe`

### 2. Keep Old Versions
```bash
# Keep previous versions for rollback
mv OWDM1_0_0.exe OWDM1_0_0_old.exe
# Upload new version as OWDM1_0_0.exe (latest stable)
# Also upload with version-specific name
```

### 3. Update Forced Updates
The app checks https://openweb.live/software for newer versions. Ensure both locations are updated:
- https://lovemedia.org.za/downloads/OWDM1_0_1.exe
- https://openweb.live/software/OWDM1_0_1.exe

## Monitoring & Analytics

### 1. Download Tracking
The download page includes basic JavaScript tracking. Enhance with:
```javascript
// Add to downloads/index.html
gtag('event', 'download', {
    'event_category': 'Software',
    'event_label': 'OpenWeb DM v1.0.0',
    'value': 1
});
```

### 2. Server Logs
Monitor download frequency and success rates:
```bash
# Check access logs
tail -f /var/log/nginx/access.log | grep "OWDM"
```

## Troubleshooting

### Build Issues
- **Missing .NET**: Install .NET 8.0 SDK
- **Missing Inno Setup**: Download from official website
- **PowerShell Errors**: Run as Administrator

### Deployment Issues
- **Permission Denied**: Check file permissions and ownership
- **404 Errors**: Verify file path and server static serving
- **Large File Issues**: Check nginx/apache max file size limits

## Support Information

### Contact
- **Developer**: OpenWeb.co.za
- **Host**: Love Media Foundation
- **Technical Issues**: Create GitHub issue or contact support

### Documentation
- **User Guide**: Included in installer
- **API Documentation**: Available in source code
- **Changelog**: CHANGELOG.md in project root

## File Structure on Server

```
/root/lovemedia/LoveMediaFoundation/public/
├── downloads/
│   ├── index.html          (Download page)
│   ├── OWDM1_0_0.exe      (Main installer)
│   └── [version archives]  (Previous versions)
└── [other public files]
```

## Success Criteria

✅ **OWDM1_0_0.exe** is accessible at https://lovemedia.org.za/downloads/OWDM1_0_0.exe  
✅ **Download page** is accessible at https://lovemedia.org.za/downloads/  
✅ **File downloads** without corruption (verify with checksums)  
✅ **Installer runs** on Windows 10/11 without security warnings  
✅ **Auto-updater** can detect and download new versions  

## Next Steps

1. **Build the installer** on a Windows development machine
2. **Upload OWDM1_0_0.exe** to replace the placeholder
3. **Test the download** link and installer functionality
4. **Monitor usage** and prepare for version updates

The download infrastructure is now ready - you just need to build and upload the actual installer file!