# ProjectOpenerExtension diagnostic script
# Diagnoses config file and editor detection issues

$ErrorActionPreference = "Continue"

Write-Host "====== ProjectOpenerExtension diagnostic tool ======" -ForegroundColor Cyan
Write-Host ""

# 1. System information
Write-Host "[1] System information" -ForegroundColor Yellow
Write-Host "  Windows version: $([Environment]::OSVersion.Version)"
Write-Host "  PowerShell version: $($PSVersionTable.PSVersion)"
Write-Host "  Current user: $env:USERNAME"
Write-Host ""

# 2. Environment variables check
Write-Host "[2] Environment variables" -ForegroundColor Yellow
Write-Host "  USERPROFILE: $env:USERPROFILE"
Write-Host "  LOCALAPPDATA: $env:LOCALAPPDATA"
Write-Host "  APPDATA: $env:APPDATA"
Write-Host "  PACKAGE_FAMILY_NAME: $env:PACKAGE_FAMILY_NAME"
Write-Host ""

# 3. Config file check
Write-Host "[3] Config file location" -ForegroundColor Yellow

# Check the MSIX virtualized path
$package = Get-AppxPackage -Name "*ProjectOpenerExtension*" -ErrorAction SilentlyContinue
if ($package) {
    $msixConfig = "$env:LOCALAPPDATA\Packages\$($package.PackageFamilyName)\LocalCache\Local\ProjectOpenerExtension\editors.json"
    Write-Host "  MSIX config (virtualized): $msixConfig"
} else {
    $msixConfig = "$env:LOCALAPPDATA\ProjectOpenerExtension\editors.json"
    Write-Host "  MSIX config (standard): $msixConfig"
}

if (Test-Path $msixConfig) {
    $fileInfo = Get-Item $msixConfig
    Write-Host "    ✓ File exists" -ForegroundColor Green
    Write-Host "    Size: $($fileInfo.Length) bytes"
    Write-Host "    Created: $($fileInfo.CreationTime)"
    Write-Host "    Modified: $($fileInfo.LastWriteTime)"

    try {
        $content = Get-Content $msixConfig -Raw | ConvertFrom-Json
        Write-Host "    Editor count: $($content.Count)" -ForegroundColor Green
        foreach ($editor in $content) {
            Write-Host "      - $($editor.Name) ($($editor.EditorType))" -ForegroundColor Cyan
        }
    } catch {
        Write-Host "    ✗ JSON parse failed: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "    ✗ File does not exist" -ForegroundColor Red
    $dir = Split-Path $msixConfig
    if (Test-Path $dir) {
        Write-Host "    Directory exists, but the file is missing" -ForegroundColor Yellow
        Write-Host "    Directory contents:"
        Get-ChildItem $dir | ForEach-Object {
            Write-Host "      - $($_.Name)"
        }
    } else {
        Write-Host "    Directory does not exist either" -ForegroundColor Red
    }
}

Write-Host ""

$standaloneConfig = "$env:USERPROFILE\.config\ProjectOpenerExtension\editors.json"
Write-Host "  Standalone config: $standaloneConfig"
if (Test-Path $standaloneConfig) {
    $fileInfo = Get-Item $standaloneConfig
    Write-Host "    ✓ File exists" -ForegroundColor Green
    Write-Host "    Size: $($fileInfo.Length) bytes"
    Write-Host "    Created: $($fileInfo.CreationTime)"
    Write-Host "    Modified: $($fileInfo.LastWriteTime)"

    try {
        $content = Get-Content $standaloneConfig -Raw | ConvertFrom-Json
        Write-Host "    Editor count: $($content.Count)" -ForegroundColor Green
        foreach ($editor in $content) {
            Write-Host "      - $($editor.Name) ($($editor.EditorType))" -ForegroundColor Cyan
        }
    } catch {
        Write-Host "    ✗ JSON parse failed: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "    ✗ File does not exist" -ForegroundColor Red
}

Write-Host ""

# 4. Editor detection
Write-Host "[4] Editor detection" -ForegroundColor Yellow

# VS Code detection
$vscodePaths = @(
    "$env:LOCALAPPDATA\Programs\Microsoft VS Code\Code.exe",
    "$env:ProgramFiles\Microsoft VS Code\Code.exe",
    "${env:ProgramFiles(x86)}\Microsoft VS Code\Code.exe"
)

Write-Host "  VS Code:"
$vscodeFound = $false
foreach ($path in $vscodePaths) {
    if (Test-Path $path) {
        Write-Host "    ✓ Found: $path" -ForegroundColor Green
        $vscodeFound = $true

        # Check the version
        try {
            $version = (Get-Item $path).VersionInfo.FileVersion
            Write-Host "      Version: $version"
        } catch {}

        break
    }
}
if (-not $vscodeFound) {
    Write-Host "    ✗ VS Code executable not found" -ForegroundColor Red
    Write-Host "      Paths checked:"
    foreach ($path in $vscodePaths) {
        Write-Host "        - $path"
    }
}

# VS Code Storage detection
$vscodeStorage = "$env:APPDATA\Code\User\globalStorage\storage.json"
Write-Host "  VS Code Storage: $vscodeStorage"
if (Test-Path $vscodeStorage) {
    Write-Host "    ✓ File exists" -ForegroundColor Green
    $fileInfo = Get-Item $vscodeStorage
    Write-Host "    Size: $($fileInfo.Length) bytes"
    Write-Host "    Modified: $($fileInfo.LastWriteTime)"

    # Try to read the project count
    try {
        $storage = Get-Content $vscodeStorage -Raw | ConvertFrom-Json
        if ($storage.profileAssociations.workspaces) {
            $projectCount = ($storage.profileAssociations.workspaces | Get-Member -MemberType NoteProperty).Count
            Write-Host "    Project count: $projectCount" -ForegroundColor Green
        }
    } catch {
        Write-Host "    Warning: unable to parse storage.json" -ForegroundColor Yellow
    }
} else {
    Write-Host "    ✗ File does not exist" -ForegroundColor Red
}

Write-Host ""

# IntelliJ IDEA detection
$jetbrainsPath = "$env:LOCALAPPDATA\JetBrains"
Write-Host "  JetBrains IDE:"
if (Test-Path $jetbrainsPath) {
    Write-Host "    ✓ Config directory exists: $jetbrainsPath" -ForegroundColor Green

    $ideaDirs = Get-ChildItem $jetbrainsPath -Directory -Filter "IntelliJIdea*" -ErrorAction SilentlyContinue
    if ($ideaDirs) {
        Write-Host "    Found IntelliJ IDEA config:"
        foreach ($dir in $ideaDirs) {
            Write-Host "      - $($dir.Name)" -ForegroundColor Cyan

            $recentProjects = Join-Path $dir.FullName "options\recentProjects.xml"
            if (Test-Path $recentProjects) {
                Write-Host "        ✓ recentProjects.xml exists" -ForegroundColor Green
            } else {
                Write-Host "        ✗ recentProjects.xml does not exist" -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "    No IntelliJ IDEA config directory found" -ForegroundColor Yellow
    }
} else {
    Write-Host "    ✗ JetBrains config directory does not exist" -ForegroundColor Red
}

Write-Host ""

# 5. MSIX package check
Write-Host "[5] MSIX package status" -ForegroundColor Yellow
$package = Get-AppxPackage -Name "*ProjectOpenerExtension*" -ErrorAction SilentlyContinue
if ($package) {
    Write-Host "  ✓ MSIX package installed" -ForegroundColor Green
    Write-Host "    Package name: $($package.Name)"
    Write-Host "    Version: $($package.Version)"
    Write-Host "    PackageFamilyName: $($package.PackageFamilyName)"
    Write-Host "    Install location: $($package.InstallLocation)"
    Write-Host "    Architecture: $($package.Architecture)"
} else {
    Write-Host "  ✗ MSIX package not installed" -ForegroundColor Red
    Write-Host "    This may be a development environment or an EXE install"
}

Write-Host ""

# 6. PowerToys check
Write-Host "[6] PowerToys status" -ForegroundColor Yellow
$powertoysProcess = Get-Process -Name "PowerToys" -ErrorAction SilentlyContinue
if ($powertoysProcess) {
    Write-Host "  ✓ PowerToys is running" -ForegroundColor Green
    Write-Host "    Process ID: $($powertoysProcess.Id)"
    Write-Host "    Path: $($powertoysProcess.Path)"
} else {
    Write-Host "  ✗ PowerToys is not running" -ForegroundColor Yellow
}

Write-Host ""

# 7. Suggested actions
Write-Host "[7] Suggested actions" -ForegroundColor Yellow

$issues = @()

if (-not (Test-Path $msixConfig) -and -not (Test-Path $standaloneConfig)) {
    $issues += "Config file does not exist"
    Write-Host "  ⚠ Config file missing - run 'Fix-Config' to repair" -ForegroundColor Yellow
}

if (-not $vscodeFound -and -not (Test-Path $jetbrainsPath)) {
    $issues += "No editors detected"
    Write-Host "  ⚠ No editors detected - make sure VS Code or a JetBrains IDE is installed" -ForegroundColor Yellow
}

if ($issues.Count -eq 0) {
    Write-Host "  ✓ No obvious problems found" -ForegroundColor Green
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Offer quick-fix options
Write-Host "Quick-fix options:" -ForegroundColor Cyan
Write-Host "  1. Create a sample config file"
Write-Host "  2. Re-detect editors"
Write-Host "  3. Save the diagnostic report"
Write-Host "  0. Exit"
Write-Host ""

$choice = Read-Host "Select an action (0-3)"

switch ($choice) {
    "1" {
        Write-Host "`nCreating a sample config file..." -ForegroundColor Yellow

        # Decide which path to use
        $targetPath = if ($package) { $msixConfig } else { $standaloneConfig }
        $targetDir = Split-Path $targetPath

        # Create the directory
        if (-not (Test-Path $targetDir)) {
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        }

        # Create the sample config
        $sampleConfig = @()

        # If VS Code was found, add its config
        if ($vscodeFound) {
            $vscodePath = $vscodePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
            $sampleConfig += @{
                Name = "VS Code"
                Enabled = $true
                Icon = ""
                ExecutablePath = $vscodePath
                ProjectPath = $vscodeStorage
                EditorType = "vscode"
            }
        }
        
        # If JetBrains was found, add its config
        if (Test-Path $jetbrainsPath) {
            $sampleConfig += @{
                Name = "IntelliJ IDEA"
                Enabled = $true
                Icon = ""
                ExecutablePath = "C:\Program Files\JetBrains\IntelliJ IDEA\bin\idea64.exe"
                ProjectPath = $jetbrainsPath
                EditorType = "jetbrains"
            }
        }
        
        # If no editors were detected, create an empty config
        if ($sampleConfig.Count -eq 0) {
            Write-Host "No editors detected, creating an empty config" -ForegroundColor Yellow
        }

        # Save the config
        $json = $sampleConfig | ConvertTo-Json -Depth 10
        $json | Out-File $targetPath -Encoding UTF8

        if (Test-Path $targetPath) {
            Write-Host "✓ Config file created: $targetPath" -ForegroundColor Green
        } else {
            Write-Host "✗ Failed to create the config file" -ForegroundColor Red
        }
    }

    "2" {
        Write-Host "`nRe-running editor detection..." -ForegroundColor Yellow
        Write-Host "Please restart PowerToys to apply the changes" -ForegroundColor Cyan
    }

    "3" {
        Write-Host "`nSaving the diagnostic report..." -ForegroundColor Yellow
        $reportPath = "$env:TEMP\ProjectOpener-Diagnostic-$(Get-Date -Format 'yyyyMMdd-HHmmss').txt"

        # Re-run the diagnostic and save it
        & $PSCommandPath *> $reportPath

        Write-Host "✓ Diagnostic report saved to: $reportPath" -ForegroundColor Green
        Start-Process notepad.exe $reportPath
    }

    default {
        Write-Host "Exiting the diagnostic tool" -ForegroundColor Gray
    }
}
