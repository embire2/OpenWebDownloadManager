[Setup]
AppName=OpenWeb Download Manager
AppVersion=1.0.0
AppId={{8B2F1B1A-2D3F-4E5A-9B7C-1F8E9D2A5C3B}
AppPublisher=OpenWeb.co.za
AppPublisherURL=https://openweb.co.za
AppSupportURL=https://openweb.co.za/support
AppUpdatesURL=https://openweb.live/software
DefaultDirName={autopf}\OpenWebDM
DefaultGroupName=OpenWeb Download Manager
AllowNoIcons=yes
LicenseFile=..\LICENSE
OutputDir=.\Output
OutputBaseFilename=OWDM1_0_0
Compression=lzma
SolidCompression=yes
SetupIconFile=..\src\OpenWebDM\Resources\icon.ico
UninstallDisplayIcon={app}\OpenWebDM.exe
UninstallDisplayName=OpenWeb Download Manager
VersionInfoVersion=1.0.0.0
VersionInfoCompany=OpenWeb.co.za
VersionInfoDescription=OpenWeb Download Manager Setup
VersionInfoCopyright=Copyright (C) 2025 OpenWeb.co.za
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1
Name: "browserintegration"; Description: "Install browser extensions"; GroupDescription: "Browser Integration"; Flags: checked
Name: "startmenu"; Description: "Add to Start Menu"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checked

[Files]
; Main application files
Source: "..\src\OpenWebDM\bin\Release\net8.0-windows\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Browser extensions
Source: "..\browser-extensions\chrome\*"; DestDir: "{app}\Extensions\Chrome"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\browser-extensions\firefox\*"; DestDir: "{app}\Extensions\Firefox"; Flags: ignoreversion recursesubdirs createallsubdirs

; Native messaging manifests
Source: "..\native-messaging\chrome\*"; DestDir: "{app}\NativeMessaging\Chrome"; Flags: ignoreversion
Source: "..\native-messaging\firefox\*"; DestDir: "{app}\NativeMessaging\Firefox"; Flags: ignoreversion

; Installation scripts
Source: ".\install-extensions.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\security-config.ps1"; DestDir: "{app}"; Flags: ignoreversion

; Documentation
Source: "..\README.md"; DestDir: "{app}"; DestName: "README.txt"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\OpenWeb Download Manager"; Filename: "{app}\OpenWebDM.exe"
Name: "{group}\{cm:UninstallProgram,OpenWeb Download Manager}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\OpenWeb Download Manager"; Filename: "{app}\OpenWebDM.exe"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\OpenWeb Download Manager"; Filename: "{app}\OpenWebDM.exe"; Tasks: quicklaunchicon

[Registry]
; File associations for common download types
Root: HKCR; Subkey: ".torrent"; ValueType: string; ValueName: ""; ValueData: "OpenWebDM.TorrentFile"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "OpenWebDM.TorrentFile"; ValueType: string; ValueName: ""; ValueData: "OpenWeb DM Torrent File"; Flags: uninsdeletekey
Root: HKCR; Subkey: "OpenWebDM.TorrentFile\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\OpenWebDM.exe,0"
Root: HKCR; Subkey: "OpenWebDM.TorrentFile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\OpenWebDM.exe"" ""%1"""

; URL protocol handlers
Root: HKCR; Subkey: "openwebdm"; ValueType: string; ValueName: ""; ValueData: "URL:OpenWeb DM Protocol"; Flags: uninsdeletekey
Root: HKCR; Subkey: "openwebdm"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""
Root: HKCR; Subkey: "openwebdm\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\OpenWebDM.exe,0"
Root: HKCR; Subkey: "openwebdm\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\OpenWebDM.exe"" ""%1"""

; Native messaging registry entries for Chrome
Root: HKLM; Subkey: "Software\Google\Chrome\NativeMessagingHosts\com.openwebdm.native"; ValueType: string; ValueName: ""; ValueData: "{app}\NativeMessaging\Chrome\com.openwebdm.native.json"; Tasks: browserintegration; Flags: uninsdeletekey

; Native messaging registry entries for Firefox
Root: HKLM; Subkey: "Software\Mozilla\NativeMessagingHosts\com.openwebdm.native"; ValueType: string; ValueName: ""; ValueData: "{app}\NativeMessaging\Firefox\com.openwebdm.native.json"; Tasks: browserintegration; Flags: uninsdeletekey

[Run]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\security-config.ps1"" -InstallPath ""{app}"" -Silent"; WorkingDir: "{app}"; Description: "Configure security settings"; Flags: runascurrentuser
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\install-extensions.ps1"" -InstallPath ""{app}"" -Silent"; WorkingDir: "{app}"; Description: "Install browser extensions"; Flags: runascurrentuser; Check: WizardIsTaskSelected('browserintegration')
Filename: "{app}\OpenWebDM.exe"; Description: "{cm:LaunchProgram,OpenWeb Download Manager}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill"; Parameters: "/f /im OpenWebDM.exe"; Flags: runhidden

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  AppPath: string;
  ManifestPath: string;
  ManifestContent: string;
begin
  if CurStep = ssPostInstall then
  begin
    AppPath := ExpandConstant('{app}');
    
    // Update Chrome native messaging manifest
    if WizardIsTaskSelected('browserintegration') then
    begin
      ManifestPath := AppPath + '\NativeMessaging\Chrome\com.openwebdm.native.json';
      if LoadStringFromFile(ManifestPath, ManifestContent) then
      begin
        StringChangeEx(ManifestContent, '%APP_PATH%', AppPath + '\OpenWebDM.exe', True);
        SaveStringToFile(ManifestPath, ManifestContent, False);
      end;
      
      // Update Firefox native messaging manifest
      ManifestPath := AppPath + '\NativeMessaging\Firefox\com.openwebdm.native.json';
      if LoadStringFromFile(ManifestPath, ManifestContent) then
      begin
        StringChangeEx(ManifestContent, '%APP_PATH%', AppPath + '\OpenWebDM.exe', True);
        SaveStringToFile(ManifestPath, ManifestContent, False);
      end;
    end;
  end;
end;