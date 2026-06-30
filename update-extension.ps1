# PowerToys extension local install: build, register, restart PowerToys.
#
# Route: unsigned MSIX + Developer Mode (no certificate handling at all).
#   - Dev Mode lets Windows register a package from a loose file layout.
#   - Add-AppxPackage -Path of an UNSIGNED .msix is rejected even in Dev Mode,
#     so we unpack the built .msix to a layout and register that instead.
#
# Prereqs: Windows Developer Mode ON (Settings > System > For developers)
#          and the Windows 11 SDK (10.0.26100) installed.

param(
    [switch]$SkipBuild = $false,
    [switch]$SkipRestart = $false
)

$ErrorActionPreference = "Stop"
$PackageName = "caolib.ProjectOpenerExtension"   # <Identity Name> in Package.appxmanifest
$ProjectPath = "$PSScriptRoot\ProjectOpenerExtension\ProjectOpenerExtension.csproj"
$AppPackages = "$PSScriptRoot\ProjectOpenerExtension\AppPackages"
$LayoutDir   = "$PSScriptRoot\ProjectOpenerExtension\bin\x64\Debug\layout"

Write-Host "=== PowerToys extension update ===" -ForegroundColor Cyan

# 0. Developer Mode must be on (it's what allows registering an unsigned layout).
$dm = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" -Name AllowDevelopmentWithoutDevLicense -ErrorAction SilentlyContinue).AllowDevelopmentWithoutDevLicense
if ($dm -ne 1) {
    Write-Host "Developer Mode is OFF. Turn it on (Settings > System > For developers), then retry." -ForegroundColor Red
    exit 1
}
Write-Host "[0/5] Developer Mode ON" -ForegroundColor Gray

# 1. Stop PowerToys.
Write-Host "[1/5] Stopping PowerToys..." -ForegroundColor Yellow
if (Get-Process -Name "PowerToys" -ErrorAction SilentlyContinue) {
    Stop-Process -Name "PowerToys" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

# 2. Uninstall any installed build of this extension.
Write-Host "[2/5] Removing old package..." -ForegroundColor Yellow
Get-AppxPackage -Name $PackageName -ErrorAction SilentlyContinue | ForEach-Object {
    Remove-AppxPackage -Package $_.PackageFullName -ErrorAction SilentlyContinue
}

# 3. Build the (unsigned) MSIX. Empty cert overrides defeat the csproj's pfx signing,
#    which otherwise fails (MSBuild can't read the password-protected key -> APPX0105).
if (-not $SkipBuild) {
    Write-Host "[3/5] Building (Debug, unsigned)..." -ForegroundColor Yellow
    if (Test-Path $AppPackages) { Remove-Item $AppPackages -Recurse -Force -ErrorAction SilentlyContinue }
    dotnet build $ProjectPath -c Debug -r win-x64 /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true /v:minimal `
        /p:AppxPackageSigningEnabled=false "/p:PackageCertificateKeyFile=" "/p:PackageCertificatePassword="
    if ($LASTEXITCODE -ne 0) { Write-Host "Build failed" -ForegroundColor Red; exit 1 }
} else {
    Write-Host "[3/5] Skipping build" -ForegroundColor Gray
}

# 4. Unpack the freshest .msix to a layout and register it (works unsigned in Dev Mode).
Write-Host "[4/5] Installing..." -ForegroundColor Yellow
$msix = Get-ChildItem -Path $AppPackages -Recurse -Filter "*_Debug.msix" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notlike "*\obj\*" } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $msix) { Write-Host "No Debug .msix found under AppPackages" -ForegroundColor Red; exit 1 }

$makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\makeappx.exe" -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending | Select-Object -First 1
if (-not $makeappx) { Write-Host "makeappx.exe not found (install the Windows 11 SDK)" -ForegroundColor Red; exit 1 }

Remove-Item $LayoutDir -Recurse -Force -ErrorAction SilentlyContinue
& $makeappx.FullName unpack /p $msix.FullName /d $LayoutDir /o | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Host "Unpack failed" -ForegroundColor Red; exit 1 }
Add-AppxPackage -Register "$LayoutDir\AppxManifest.xml"
Write-Host "  Registered: $($msix.Name)" -ForegroundColor Green

# 5. Restart PowerToys.
if (-not $SkipRestart) {
    Write-Host "[5/5] Restarting PowerToys..." -ForegroundColor Yellow
    $pt = @("$env:ProgramFiles\PowerToys\PowerToys.exe",
            "$env:LOCALAPPDATA\PowerToys\PowerToys.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($pt) { Start-Process $pt } else { Write-Host "  PowerToys not found, start it manually" -ForegroundColor Yellow }
} else {
    Write-Host "[5/5] Skipping restart" -ForegroundColor Gray
}

Write-Host "=== Done. Press Alt+Space to test. ===" -ForegroundColor Cyan
