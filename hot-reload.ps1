# PowerToys extension hot-reload script
# Uses x-cmdpal://reload for a quick reload without restarting PowerToys

param(
    [switch]$SkipBuild = $false
)

$ErrorActionPreference = "Stop"
$PackageName = "ProjectOpenerExtension_0.0.1.1_x64__8wekyb3d8bbwe"
$ProjectPath = "$PSScriptRoot\ProjectOpenerExtension\ProjectOpenerExtension.csproj"
$MsixPath = "$PSScriptRoot\ProjectOpenerExtension\AppPackages\ProjectOpenerExtension_0.0.1.1_x64_Debug_Test\ProjectOpenerExtension_0.0.1.1_x64_Debug.msix"

Write-Host "=== 🔥 Hot-reload mode ===" -ForegroundColor Cyan
Write-Host ""

# 1. Uninstall the old version
Write-Host "[1/4] Uninstalling the old version..." -ForegroundColor Yellow
$package = Get-AppxPackage | Where-Object { $_.Name -like "*ProjectOpener*" }
if ($package) {
    $package | Remove-AppxPackage -ErrorAction SilentlyContinue
    Write-Host "✓ Old version uninstalled" -ForegroundColor Green
} else {
    Write-Host "✓ No old version found" -ForegroundColor Green
}

# 2. Build the new version
if (-not $SkipBuild) {
    Write-Host "[2/4] Building the project..." -ForegroundColor Yellow
    dotnet build $ProjectPath -c Debug -r win-x64 /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true /v:minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ Build succeeded" -ForegroundColor Green
} else {
    Write-Host "[2/4] Skipping build" -ForegroundColor Gray
}

# 3. Install the new version
Write-Host "[3/4] Installing the new version..." -ForegroundColor Yellow
if (Test-Path $MsixPath) {
    Add-AppxPackage -Path $MsixPath
    Write-Host "✓ New version installed" -ForegroundColor Green
} else {
    Write-Host "✗ MSIX package not found: $MsixPath" -ForegroundColor Red
    exit 1
}

# 4. Trigger the hot reload
Write-Host "[4/4] Triggering the hot reload..." -ForegroundColor Yellow
Start-Sleep -Milliseconds 500
Start-Process "x-cmdpal://reload"
Write-Host "✓ Reload command sent" -ForegroundColor Green

Write-Host ""
Write-Host "=== 🎉 Hot reload complete! ===" -ForegroundColor Cyan
Write-Host "Extension updated, no need to restart PowerToys!" -ForegroundColor Green
Write-Host "Press Alt+Space to open the Command Palette and test your changes" -ForegroundColor Yellow
