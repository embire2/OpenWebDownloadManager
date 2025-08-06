# OpenWeb Download Manager - Build Instructions

## Prerequisites

Before building the installer, ensure you have the following installed:

1. **Visual Studio 2022** or **.NET 8.0 SDK**
2. **Inno Setup 6.0+** (for creating the .exe installer)
3. **Windows 10/11** (required for WPF application)

## Build Steps

### 1. Restore Dependencies
```powershell
dotnet restore OpenWebDM.sln
```

### 2. Build Solution
```powershell
dotnet build OpenWebDM.sln -c Release
```

### 3. Publish Application
```powershell
dotnet publish src\OpenWebDM\OpenWebDM.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=true
```

### 4. Copy Additional Files
The build script will automatically copy:
- Browser extensions
- Native messaging manifests
- Installation scripts
- Documentation

### 5. Create Installer
```powershell
# Using Inno Setup Compiler
ISCC.exe installer\setup.iss
```

This will create: `installer\Output\OWDM1_0_0.exe`

## Automated Build

Run the automated build script:
```powershell
.\build.ps1 -CreateInstaller
```

## Output

The final installer will be:
- **Filename**: `OWDM1_0_0.exe`
- **Location**: `installer\Output\`
- **Size**: ~50-80 MB (self-contained)

## Installation Features

The installer will:
- Install the main application
- Deploy browser extensions automatically
- Configure Windows Defender exclusions
- Set up native messaging for browsers
- Create desktop and start menu shortcuts
- Configure security settings

## Digital Signature (Recommended)

For production deployment, sign the executable:
```powershell
signtool sign /a /t http://timestamp.digicert.com "installer\Output\OWDM1_0_0.exe"
```

## Deployment

Upload the final `OWDM1_0_0.exe` to:
- `https://lovemedia.org.za/downloads/OWDM1_0_0.exe`
- `https://openweb.live/software/OWDM1_0_0.exe`

The application's auto-updater will check these locations for new versions.