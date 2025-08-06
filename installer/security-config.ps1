# Security Configuration Script for OpenWeb Download Manager
param(
    [string]$InstallPath,
    [switch]$Silent
)

$ErrorActionPreference = "Continue"

function Write-Log {
    param($Message, $Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    if (-not $Silent) {
        Write-Host "[$timestamp] [$Level] $Message"
    }
}

function Add-TrustedPublisher {
    param($AppPath)
    
    try {
        # Add to trusted applications in Windows Defender
        $exePath = Join-Path $AppPath "OpenWebDM.exe"
        
        if (Test-Path $exePath) {
            # Add application to Windows Defender exclusions
            Add-MpPreference -ExclusionProcess "OpenWebDM.exe" -ErrorAction SilentlyContinue
            
            # Add installation directory to exclusions
            Add-MpPreference -ExclusionPath $AppPath -ErrorAction SilentlyContinue
            
            Write-Log "Added Windows Defender exclusions" "SUCCESS"
        }
    }
    catch {
        Write-Log "Could not configure Windows Defender: $($_.Exception.Message)" "WARN"
    }
}

function Configure-SmartScreen {
    param($AppPath)
    
    try {
        # Configure SmartScreen to allow the application
        $exePath = Join-Path $AppPath "OpenWebDM.exe"
        
        if (Test-Path $exePath) {
            # Add to SmartScreen cache as trusted
            $regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"
            
            # Create registry entry to mark as trusted
            New-Item -Path "$regPath\SmartScreenEnabled" -Force -ErrorAction SilentlyContinue
            Set-ItemProperty -Path "$regPath\SmartScreenEnabled" -Name "OpenWebDM" -Value "Trusted" -ErrorAction SilentlyContinue
            
            Write-Log "Configured SmartScreen settings" "SUCCESS"
        }
    }
    catch {
        Write-Log "Could not configure SmartScreen: $($_.Exception.Message)" "WARN"
    }
}

function Set-FilePermissions {
    param($AppPath)
    
    try {
        # Set proper file permissions
        $acl = Get-Acl $AppPath
        
        # Allow full control for administrators
        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("Administrators", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
        $acl.SetAccessRule($accessRule)
        
        # Allow read and execute for users
        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("Users", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
        $acl.SetAccessRule($accessRule)
        
        Set-Acl -Path $AppPath -AclObject $acl
        
        Write-Log "Set secure file permissions" "SUCCESS"
    }
    catch {
        Write-Log "Could not set file permissions: $($_.Exception.Message)" "WARN"
    }
}

function Create-TrustedSiteEntries {
    param($AppPath)
    
    try {
        # Add download domains to trusted sites for easier integration
        $trustedSites = @(
            "openweb.co.za",
            "openweb.live",
            "*.openweb.co.za",
            "*.openweb.live"
        )
        
        $regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap\Domains"
        
        foreach ($site in $trustedSites) {
            $sitePath = $regPath + "\" + $site.Replace("*.", "")
            New-Item -Path $sitePath -Force -ErrorAction SilentlyContinue
            Set-ItemProperty -Path $sitePath -Name "https" -Value 2 -Type DWord -ErrorAction SilentlyContinue
        }
        
        Write-Log "Configured trusted site entries" "SUCCESS"
    }
    catch {
        Write-Log "Could not configure trusted sites: $($_.Exception.Message)" "WARN"
    }
}

function Configure-BrowserSecurity {
    param($AppPath)
    
    try {
        # Configure browser policies to allow the extension
        
        # Chrome policies
        $chromeRegPath = "HKLM:\SOFTWARE\Policies\Google\Chrome"
        if (-not (Test-Path $chromeRegPath)) {
            New-Item -Path $chromeRegPath -Force | Out-Null
        }
        
        # Allow extension from local path
        Set-ItemProperty -Path $chromeRegPath -Name "DeveloperToolsAvailability" -Value 1 -Type DWord -ErrorAction SilentlyContinue
        Set-ItemProperty -Path $chromeRegPath -Name "ExtensionInstallWhitelist" -Value @("openwebdm-extension") -Type MultiString -ErrorAction SilentlyContinue
        
        # Edge policies  
        $edgeRegPath = "HKLM:\SOFTWARE\Policies\Microsoft\Edge"
        if (-not (Test-Path $edgeRegPath)) {
            New-Item -Path $edgeRegPath -Force | Out-Null
        }
        
        Set-ItemProperty -Path $edgeRegPath -Name "DeveloperToolsAvailability" -Value 1 -Type DWord -ErrorAction SilentlyContinue
        
        Write-Log "Configured browser security policies" "SUCCESS"
    }
    catch {
        Write-Log "Could not configure browser policies: $($_.Exception.Message)" "WARN"
    }
}

# Main execution
Write-Log "Configuring security settings for OpenWeb Download Manager..."

if ([string]::IsNullOrEmpty($InstallPath)) {
    $InstallPath = "${env:ProgramFiles}\OpenWebDM"
}

if (-not (Test-Path $InstallPath)) {
    Write-Log "Installation path not found: $InstallPath" "ERROR"
    exit 1
}

# Apply security configurations
Add-TrustedPublisher -AppPath $InstallPath
Configure-SmartScreen -AppPath $InstallPath
Set-FilePermissions -AppPath $InstallPath
Create-TrustedSiteEntries -AppPath $InstallPath
Configure-BrowserSecurity -AppPath $InstallPath

Write-Log "Security configuration completed successfully" "SUCCESS"

if (-not $Silent) {
    Write-Host "`nSecurity configuration completed. The application should now be trusted by Windows security systems."
}

exit 0