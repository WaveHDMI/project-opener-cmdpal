// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace ProjectOpenerExtension.Services;

/// <summary>
/// Automatically detects editors installed on the system
/// </summary>
public static class EditorDetectionService
{
    /// <summary>
    /// Detect all installed editors
    /// </summary>
    public static List<EditorDefinition> DetectInstalledEditors()
    {
        var editors = new List<EditorDefinition>();

        // Detect VS Code
        var vscode = DetectVSCode();
        if (vscode != null) editors.Add(vscode);

        // Detect IntelliJ IDEA
        var idea = DetectIntelliJIDEA();
        if (idea != null) editors.Add(idea);

        // Detect Visual Studio
        var visualStudio = DetectVisualStudio();
        if (visualStudio != null) editors.Add(visualStudio);

        return editors;
    }

    /// <summary>
    /// Detect Visual Studio (devenv.exe). Recent solutions live under
    /// %LOCALAPPDATA%\Microsoft\VisualStudio in each instance's ApplicationPrivateSettings.xml.
    /// </summary>
    private static EditorDefinition? DetectVisualStudio()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configPath = Path.Combine(localAppData, "Microsoft", "VisualStudio");

        var devenv = FindDevenvViaVsWhere() ?? FindDevenvByScanning();
        if (string.IsNullOrEmpty(devenv) || !Directory.Exists(configPath))
        {
            System.Diagnostics.Debug.WriteLine("[EditorDetection] ✗ Visual Studio not detected");
            return null;
        }

        System.Diagnostics.Debug.WriteLine($"[EditorDetection] ✓ Found Visual Studio: {devenv}");
        return new EditorDefinition
        {
            Name = "Visual Studio",
            Enabled = true,
            Icon = "",
            ExecutablePath = devenv,
            ProjectPath = configPath,
            EditorType = "visualstudio"
        };
    }

    /// <summary>Ask vswhere.exe for the latest install's devenv.exe path (robust across editions/years).</summary>
    private static string? FindDevenvViaVsWhere()
    {
        try
        {
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var vswhere = Path.Combine(programFilesX86, "Microsoft Visual Studio", "Installer", "vswhere.exe");
            if (!File.Exists(vswhere))
            {
                return null;
            }

            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = vswhere,
                    Arguments = "-latest -prerelease -property productPath",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);

            return File.Exists(output) ? output : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EditorDetection] vswhere error: {ex.Message}");
            return null;
        }
    }

    /// <summary>Fallback: scan the standard install root for any edition's devenv.exe.</summary>
    private static string? FindDevenvByScanning()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var root = Path.Combine(programFiles, "Microsoft Visual Studio");
        if (!Directory.Exists(root))
        {
            return null;
        }

        // Layout: <root>\<year-or-version>\<edition>\Common7\IDE\devenv.exe
        foreach (var yearDir in Directory.GetDirectories(root).OrderByDescending(d => d))
        {
            foreach (var editionDir in Directory.GetDirectories(yearDir))
            {
                var devenv = Path.Combine(editionDir, "Common7", "IDE", "devenv.exe");
                if (File.Exists(devenv))
                {
                    return devenv;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Detect VS Code
    /// </summary>
    private static EditorDefinition? DetectVSCode()
    {
        System.Diagnostics.Debug.WriteLine("[EditorDetection] Detecting VS Code...");

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        System.Diagnostics.Debug.WriteLine($"[EditorDetection] AppData: {appData}");
        System.Diagnostics.Debug.WriteLine($"[EditorDetection] LocalAppData: {localAppData}");
        System.Diagnostics.Debug.WriteLine($"[EditorDetection] ProgramFiles: {programFiles}");
        System.Diagnostics.Debug.WriteLine($"[EditorDetection] UserProfile: {userProfile}");

        // Check common install locations
        var possiblePaths = new[]
        {
            Path.Combine(localAppData, "Programs", "Microsoft VS Code", "Code.exe"),
            Path.Combine(programFiles, "Microsoft VS Code", "Code.exe"),
            Path.Combine(programFilesX86, "Microsoft VS Code", "Code.exe"),
            // User installer paths (several possibilities)
            Path.Combine(userProfile, "AppData", "Local", "Programs", "Microsoft VS Code", "Code.exe"),
            Path.Combine(userProfile, "scoop", "apps", "vscode", "current", "Code.exe"),
            // Portable version
            Path.Combine(userProfile, "Downloads", "VSCode-win32-x64", "Code.exe"),
            Path.Combine(userProfile, "VSCode", "Code.exe")
        };

        foreach (var path in possiblePaths)
        {
            System.Diagnostics.Debug.WriteLine($"[EditorDetection] Checking path: {path}");
            if (File.Exists(path))
            {
                System.Diagnostics.Debug.WriteLine($"[EditorDetection] ✓ Found VS Code: {path}");
                var storagePath = Path.Combine(appData, "Code", "User", "globalStorage", "storage.json");
                System.Diagnostics.Debug.WriteLine($"[EditorDetection] Storage path: {storagePath}");
                System.Diagnostics.Debug.WriteLine($"[EditorDetection] Storage exists: {File.Exists(storagePath)}");

                return new EditorDefinition
                {
                    Name = "VS Code",
                    Enabled = true,
                    Icon = "",
                    ExecutablePath = path,
                    ProjectPath = storagePath,
                    EditorType = "vscode"
                };
            }
        }

        System.Diagnostics.Debug.WriteLine("[EditorDetection] VS Code not found in standard paths, trying the registry...");

        // Try to find it in the registry
        try
        {
            System.Diagnostics.Debug.WriteLine("[EditorDetection] Searching the registry...");
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
            if (key != null)
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    var displayName = subKey?.GetValue("DisplayName") as string;

                    if (displayName != null && displayName.Contains("Visual Studio Code"))
                    {
                        System.Diagnostics.Debug.WriteLine($"[EditorDetection] Found registry entry: {displayName}");
                        var installLocation = subKey?.GetValue("InstallLocation") as string;
                        System.Diagnostics.Debug.WriteLine($"[EditorDetection] Install location: {installLocation}");

                        if (!string.IsNullOrEmpty(installLocation))
                        {
                            var exePath = Path.Combine(installLocation, "Code.exe");
                            if (File.Exists(exePath))
                            {
                                System.Diagnostics.Debug.WriteLine($"[EditorDetection] ✓ Found VS Code in the registry: {exePath}");
                                var storagePath = Path.Combine(appData, "Code", "User", "globalStorage", "storage.json");

                                return new EditorDefinition
                                {
                                    Name = "VS Code",
                                    Enabled = true,
                                    Icon = "",
                                    ExecutablePath = exePath,
                                    ProjectPath = storagePath,
                                    EditorType = "vscode"
                                };
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EditorDetection] Registry access error: {ex.Message}");
        }

        System.Diagnostics.Debug.WriteLine("[EditorDetection] ✗ VS Code not detected");
        return null;
    }

    /// <summary>
    /// Detect IntelliJ IDEA
    /// </summary>
    private static EditorDefinition? DetectIntelliJIDEA()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        // Check common install locations
        var possibleBasePaths = new[]
        {
            Path.Combine(programFiles, "JetBrains"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "JetBrains")
        };

        foreach (var basePath in possibleBasePaths)
        {
            if (Directory.Exists(basePath))
            {
                // Find the IDEA directory
                var ideaDirs = Directory.GetDirectories(basePath, "IntelliJ IDEA*");
                if (ideaDirs.Length > 0)
                {
                    // Take the latest version (sorted by directory name, the latest is usually last)
                    Array.Sort(ideaDirs);
                    var ideaDir = ideaDirs[^1];

                    var exePath = Path.Combine(ideaDir, "bin", "idea64.exe");
                    if (File.Exists(exePath))
                    {
                        var configPath = Path.Combine(localAppData, "JetBrains");

                        return new EditorDefinition
                        {
                            Name = "IntelliJ IDEA",
                            Enabled = true,
                            Icon = "",
                            ExecutablePath = exePath,
                            ProjectPath = configPath,
                            EditorType = "jetbrains"
                        };
                    }
                }
            }
        }

        // Try to find it in the registry
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\JetBrains\IntelliJ IDEA");
            if (key != null)
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    var installLocation = subKey?.GetValue("") as string;

                    if (!string.IsNullOrEmpty(installLocation))
                    {
                        var exePath = Path.Combine(installLocation, "bin", "idea64.exe");
                        if (File.Exists(exePath))
                        {
                            var configPath = Path.Combine(localAppData, "JetBrains");

                            return new EditorDefinition
                            {
                                Name = "IntelliJ IDEA",
                                Enabled = true,
                                Icon = "",
                                ExecutablePath = exePath,
                                ProjectPath = configPath,
                                EditorType = "jetbrains"
                            };
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore registry access errors
        }

        return null;
    }
}
