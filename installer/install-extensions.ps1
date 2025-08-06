# OpenWeb Download Manager Browser Extension Installer
param(
    [switch]$Silent,
    [switch]$ForceInstall,
    [string]$InstallPath = ""
)

$ErrorActionPreference = "Continue"

# Extension configuration
$ExtensionId = "openwebdm-extension"
$ExtensionName = "OpenWeb Download Manager"

function Write-Log {
    param($Message, $Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] [$Level] $Message"
}

function Test-AdminRights {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Install-ChromeExtension {
    param($ExtensionPath)
    
    Write-Log "Installing Chrome extension..."
    
    try {
        # Chrome extension registry path
        $chromeRegPath = "HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist"
        
        # Ensure registry path exists
        if (-not (Test-Path $chromeRegPath)) {
            New-Item -Path $chromeRegPath -Force | Out-Null
        }
        
        # Force install extension
        $extensionEntry = "${ExtensionId};file:///$ExtensionPath"
        Set-ItemProperty -Path $chromeRegPath -Name "1" -Value $extensionEntry -Type String
        
        Write-Log "Chrome extension registry entry created successfully" "SUCCESS"
        return $true
    }
    catch {
        Write-Log "Failed to install Chrome extension: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Install-EdgeExtension {
    param($ExtensionPath)
    
    Write-Log "Installing Edge extension..."
    
    try {
        # Edge extension registry path
        $edgeRegPath = "HKLM:\SOFTWARE\Policies\Microsoft\Edge\ExtensionInstallForcelist"
        
        # Ensure registry path exists
        if (-not (Test-Path $edgeRegPath)) {
            New-Item -Path $edgeRegPath -Force | Out-Null
        }
        
        # Force install extension
        $extensionEntry = "${ExtensionId};file:///$ExtensionPath"
        Set-ItemProperty -Path $edgeRegPath -Name "1" -Value $extensionEntry -Type String
        
        Write-Log "Edge extension registry entry created successfully" "SUCCESS"
        return $true
    }
    catch {
        Write-Log "Failed to install Edge extension: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Install-FirefoxExtension {
    param($ExtensionPath)
    
    Write-Log "Installing Firefox extension..."
    
    try {
        # Firefox extension registry path
        $firefoxRegPath = "HKLM:\SOFTWARE\Mozilla\Firefox\Extensions"
        
        # Ensure registry path exists
        if (-not (Test-Path $firefoxRegPath)) {
            New-Item -Path $firefoxRegPath -Force | Out-Null
        }
        
        # Install extension
        Set-ItemProperty -Path $firefoxRegPath -Name "${ExtensionId}@openwebdm.com" -Value "$ExtensionPath\manifest.json" -Type String
        
        Write-Log "Firefox extension registry entry created successfully" "SUCCESS"
        return $true
    }
    catch {
        Write-Log "Failed to install Firefox extension: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Configure-WindowsDefender {
    param($AppPath)
    
    Write-Log "Configuring Windows Defender exclusions..."
    
    try {
        # Add application exclusion
        Add-MpPreference -ExclusionPath $AppPath -Force -ErrorAction SilentlyContinue
        
        # Add process exclusion
        $exeFile = Join-Path $AppPath "OpenWebDM.exe"
        if (Test-Path $exeFile) {
            Add-MpPreference -ExclusionProcess "OpenWebDM.exe" -Force -ErrorAction SilentlyContinue
        }
        
        Write-Log "Windows Defender exclusions configured successfully" "SUCCESS"
        return $true
    }
    catch {
        Write-Log "Failed to configure Windows Defender: $($_.Exception.Message)" "WARN"
        return $false
    }
}

function Create-UninstallScript {
    param($InstallPath)
    
    $uninstallScript = @"
# OpenWeb Download Manager Extension Uninstaller
`$ErrorActionPreference = "Continue"

Write-Host "Removing OpenWeb Download Manager browser extensions..."

# Remove Chrome extension
try {
    Remove-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist" -Name "1" -ErrorAction SilentlyContinue
    Write-Host "Chrome extension removed"
} catch { }

# Remove Edge extension
try {
    Remove-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\Edge\ExtensionInstallForcelist" -Name "1" -ErrorAction SilentlyContinue
    Write-Host "Edge extension removed"
} catch { }

# Remove Firefox extension
try {
    Remove-ItemProperty -Path "HKLM:\SOFTWARE\Mozilla\Firefox\Extensions" -Name "openwebdm-extension@openwebdm.com" -ErrorAction SilentlyContinue
    Write-Host "Firefox extension removed"
} catch { }

Write-Host "Extension cleanup completed"
"@

    $scriptPath = Join-Path $InstallPath "uninstall-extensions.ps1"
    $uninstallScript | Out-File -FilePath $scriptPath -Encoding UTF8
    Write-Log "Uninstall script created at: $scriptPath"
}

# Main installation logic
Write-Log "Starting OpenWeb Download Manager browser extension installation..."

# Check admin rights
if (-not (Test-AdminRights)) {
    Write-Log "Administrator rights required for browser extension installation" "ERROR"
    exit 1
}

# Determine install path
if ([string]::IsNullOrEmpty($InstallPath)) {
    $InstallPath = "${env:ProgramFiles}\OpenWebDM"
}

if (-not (Test-Path $InstallPath)) {
    Write-Log "Application not found at: $InstallPath" "ERROR"
    exit 1
}

# Extension paths
$chromeExtPath = Join-Path $InstallPath "Extensions\Chrome\extension"
$firefoxExtPath = Join-Path $InstallPath "Extensions\Firefox\extension"

$results = @{
    Chrome = $false
    Edge = $false  
    Firefox = $false
    WindowsDefender = $false
}

# Install extensions
if (Test-Path $chromeExtPath) {
    $results.Chrome = Install-ChromeExtension -ExtensionPath $chromeExtPath
    $results.Edge = Install-EdgeExtension -ExtensionPath $chromeExtPath
} else {
    Write-Log "Chrome extension not found at: $chromeExtPath" "WARN"
}

if (Test-Path $firefoxExtPath) {
    $results.Firefox = Install-FirefoxExtension -ExtensionPath $firefoxExtPath
} else {
    Write-Log "Firefox extension not found at: $firefoxExtPath" "WARN"
}

# Configure security settings
$results.WindowsDefender = Configure-WindowsDefender -AppPath $InstallPath

# Create uninstall script
Create-UninstallScript -InstallPath $InstallPath

# Summary
Write-Log "=== Installation Summary ==="
Write-Log "Chrome Extension: $(if($results.Chrome) {'SUCCESS'} else {'FAILED'})"
Write-Log "Edge Extension: $(if($results.Edge) {'SUCCESS'} else {'FAILED'})"
Write-Log "Firefox Extension: $(if($results.Firefox) {'SUCCESS'} else {'FAILED'})"
Write-Log "Windows Defender: $(if($results.WindowsDefender) {'CONFIGURED'} else {'SKIPPED'})"

$successCount = ($results.Values | Where-Object { $_ -eq $true }).Count
Write-Log "Successfully configured $successCount out of 4 components"

if (-not $Silent) {
    Write-Host "`nPress any key to continue..."
    $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") | Out-Null
}

# Restart browsers notification
if ($results.Chrome -or $results.Edge -or $results.Firefox) {
    Write-Log "Please restart your browsers to activate the extensions" "IMPORTANT"
}

exit 0