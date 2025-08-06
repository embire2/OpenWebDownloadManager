# OpenWeb Download Manager Build Script
param(
    [string]$Configuration = "Release",
    [switch]$SkipTests,
    [switch]$CreateInstaller
)

Write-Host "Building OpenWeb Download Manager..." -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

try {
    # Clean previous builds
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path ".\src\OpenWebDM\bin") { Remove-Item ".\src\OpenWebDM\bin" -Recurse -Force }
    if (Test-Path ".\src\OpenWebDM\obj") { Remove-Item ".\src\OpenWebDM\obj" -Recurse -Force }
    if (Test-Path ".\src\OpenWebDM.Core\bin") { Remove-Item ".\src\OpenWebDM.Core\bin" -Recurse -Force }
    if (Test-Path ".\src\OpenWebDM.Core\obj") { Remove-Item ".\src\OpenWebDM.Core\obj" -Recurse -Force }

    # Restore packages
    Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
    dotnet restore OpenWebDM.sln

    # Run tests if not skipped
    if (-not $SkipTests) {
        Write-Host "Running tests..." -ForegroundColor Yellow
        # dotnet test --no-restore --verbosity normal
        Write-Host "No tests defined yet - skipping test execution" -ForegroundColor Gray
    }

    # Build solution
    Write-Host "Building solution..." -ForegroundColor Yellow
    dotnet build OpenWebDM.sln -c $Configuration --no-restore

    # Publish main application
    Write-Host "Publishing application..." -ForegroundColor Yellow
    dotnet publish .\src\OpenWebDM\OpenWebDM.csproj -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=true

    # Copy additional files
    Write-Host "Copying additional files..." -ForegroundColor Yellow
    $publishDir = ".\src\OpenWebDM\bin\$Configuration\net8.0-windows\win-x64\publish"
    
    # Copy browser extensions
    if (-not (Test-Path "$publishDir\Extensions")) { New-Item -ItemType Directory -Path "$publishDir\Extensions" }
    Copy-Item ".\browser-extensions\*" -Destination "$publishDir\Extensions\" -Recurse -Force

    # Copy native messaging manifests
    if (-not (Test-Path "$publishDir\NativeMessaging")) { New-Item -ItemType Directory -Path "$publishDir\NativeMessaging" }
    Copy-Item ".\native-messaging\*" -Destination "$publishDir\NativeMessaging\" -Recurse -Force

    # Copy installer scripts
    Copy-Item ".\installer\install-extensions.ps1" -Destination $publishDir -ErrorAction SilentlyContinue
    Copy-Item ".\installer\security-config.ps1" -Destination $publishDir -ErrorAction SilentlyContinue

    # Copy documentation
    if (Test-Path ".\README.md") { Copy-Item ".\README.md" -Destination $publishDir }
    if (Test-Path ".\LICENSE") { Copy-Item ".\LICENSE" -Destination $publishDir }
    if (Test-Path ".\CHANGELOG.md") { Copy-Item ".\CHANGELOG.md" -Destination $publishDir }

    Write-Host "Build completed successfully!" -ForegroundColor Green
    Write-Host "Output directory: $publishDir" -ForegroundColor Cyan

    # Create installer if requested
    if ($CreateInstaller) {
        Write-Host "Creating installer..." -ForegroundColor Yellow
        
        # Try to find Inno Setup in common installation paths
        $innoSetupPaths = @(
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
            "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe",
            "${env:ProgramFiles}\Inno Setup 5\ISCC.exe"
        )
        
        $innoSetupPath = $null
        foreach ($path in $innoSetupPaths) {
            if (Test-Path $path) {
                $innoSetupPath = $path
                break
            }
        }
        
        # Also check PATH
        if ($null -eq $innoSetupPath) {
            $innoSetupPath = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
        }
        
        if ($null -eq $innoSetupPath) {
            Write-Warning "Inno Setup not found. Checked common installation paths:"
            foreach ($path in $innoSetupPaths) {
                Write-Host "  - $path" -ForegroundColor Gray
            }
            Write-Host "Download from: https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
            Write-Host "Or add ISCC.exe to your PATH environment variable" -ForegroundColor Cyan
        } else {
            Write-Host "Found Inno Setup: $innoSetupPath" -ForegroundColor Green
            & "$innoSetupPath" ".\installer\setup.iss"
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Installer created successfully!" -ForegroundColor Green
                Write-Host "Installer file: .\installer\Output\OWDM1_0_0.exe" -ForegroundColor Cyan
            } else {
                Write-Error "Installer creation failed with exit code: $LASTEXITCODE"
            }
        }
    }

    # Display build summary
    Write-Host "`n=== Build Summary ===" -ForegroundColor Cyan
    Write-Host "Configuration: $Configuration" -ForegroundColor White
    Write-Host "Target Runtime: win-x64" -ForegroundColor White
    Write-Host "Self-contained: Yes" -ForegroundColor White
    Write-Host "Ready to run: Yes" -ForegroundColor White
    if ($CreateInstaller -and $null -ne $innoSetupPath) {
        Write-Host "Installer: Created" -ForegroundColor White
    }

    Write-Host "`nTo run the application:" -ForegroundColor Yellow
    Write-Host "  $publishDir\OpenWebDM.exe" -ForegroundColor White

} catch {
    Write-Host "Build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}