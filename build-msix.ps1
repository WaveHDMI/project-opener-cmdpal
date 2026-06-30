# ============================================
# MSIX package build script
# Builds the package for Microsoft Store publishing
# ============================================

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [switch]$Clean = $true
)

$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  MSIX package build script" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$ProjectDir = "$PSScriptRoot\ProjectOpenerExtension"
$ProjectFile = "$ProjectDir\ProjectOpenerExtension.csproj"

# Show the build configuration
Write-Host "Build configuration:" -ForegroundColor Yellow
Write-Host "  Configuration: $Configuration" -ForegroundColor White
Write-Host "  Platform: $Platform" -ForegroundColor White
Write-Host "  Project path: $ProjectDir" -ForegroundColor White
Write-Host ""

# Verify the project file exists
if (-not (Test-Path $ProjectFile)) {
    Write-Host "❌ Error: project file not found" -ForegroundColor Red
    Write-Host "   Path: $ProjectFile" -ForegroundColor Gray
    exit 1
}

# Clean previous build output
if ($Clean) {
    Write-Host "Cleaning previous build output..." -ForegroundColor Yellow
    
    $pathsToClean = @(
        "$ProjectDir\bin\$Configuration",
        "$ProjectDir\obj\$Configuration",
        "$ProjectDir\AppPackages"
    )
    
    foreach ($path in $pathsToClean) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  ✓ Cleaned: $path" -ForegroundColor Gray
        }
    }

    Write-Host "✅ Cleanup complete" -ForegroundColor Green
    Write-Host ""
}

# Show package identity information
Write-Host "Package identity:" -ForegroundColor Cyan
Write-Host "  Name: caolib.ProjectOpenerExtension" -ForegroundColor White
Write-Host "  Publisher: CN=1CAC90B8-3709-4D70-847A-683B7D151D03" -ForegroundColor White
Write-Host "  DisplayName: caolib.ProjectOpenerExtension" -ForegroundColor White
Write-Host "  PublisherDisplayName: caolib" -ForegroundColor White
Write-Host ""

# Start building
Write-Host "Building the MSIX package..." -ForegroundColor Yellow
Write-Host ""

$buildStartTime = Get-Date

try {
    dotnet build $ProjectFile `
        --configuration $Configuration `
        -p:Platform=$Platform `
        -p:GenerateAppxPackageOnBuild=true `
        --verbosity minimal
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed, exit code: $LASTEXITCODE"
    }
    
    $buildEndTime = Get-Date
    $buildDuration = ($buildEndTime - $buildStartTime).TotalSeconds
    
    Write-Host ""
    Write-Host "✅ Build succeeded!" -ForegroundColor Green
    Write-Host "   Elapsed: $([math]::Round($buildDuration, 1)) seconds" -ForegroundColor Gray
    Write-Host ""

    # Find the generated MSIX package
    Write-Host "Searching for the MSIX package..." -ForegroundColor Yellow
    
    $msixFiles = Get-ChildItem -Path "$ProjectDir\AppPackages" -Recurse -Filter "*.msix" -ErrorAction SilentlyContinue | 
        Where-Object { $_.Name -notlike "*Debug*" -and $_.FullName -notlike "*\obj\*" }
    
    if ($msixFiles) {
        Write-Host ""
        Write-Host "📦 Found $($msixFiles.Count) MSIX package(s):" -ForegroundColor Green
        Write-Host ""
        
        foreach ($msix in $msixFiles) {
            $sizeMB = [math]::Round($msix.Length / 1MB, 2)
            Write-Host "  File name: $($msix.Name)" -ForegroundColor Cyan
            Write-Host "  Size: $sizeMB MB" -ForegroundColor White
            Write-Host "  Path: $($msix.Directory.FullName)" -ForegroundColor Gray
            Write-Host ""
        }

        # Show the full path of the first package
        $mainMsix = $msixFiles[0]
        Write-Host "============================================" -ForegroundColor Cyan
        Write-Host "  Build complete!" -ForegroundColor Green
        Write-Host "============================================" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "📤 MSIX package location:" -ForegroundColor Yellow
        Write-Host "   $($mainMsix.FullName)" -ForegroundColor White
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "  1. Open https://partner.microsoft.com/dashboard" -ForegroundColor White
        Write-Host "  2. Go to the app submission page" -ForegroundColor White
        Write-Host "  3. Upload this MSIX file in the 'Packages' section" -ForegroundColor White
        Write-Host ""
        Write-Host "⚠️  Note: a local signing failure is normal; the Microsoft Store will re-sign it" -ForegroundColor Yellow
        Write-Host ""

    } else {
        Write-Host "⚠️  Warning: no MSIX package file found" -ForegroundColor Yellow
        Write-Host "   Check the build log for more information" -ForegroundColor Gray
        Write-Host ""
    }
    
} catch {
    Write-Host ""
    Write-Host "❌ Build failed!" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Tips:" -ForegroundColor Yellow
    Write-Host "  1. Make sure the .NET 10 SDK is installed" -ForegroundColor White
    Write-Host "  2. Check that the Package.appxmanifest configuration is correct" -ForegroundColor White
    Write-Host "  3. Review the detailed error information above" -ForegroundColor White
    Write-Host ""
    exit 1
}
