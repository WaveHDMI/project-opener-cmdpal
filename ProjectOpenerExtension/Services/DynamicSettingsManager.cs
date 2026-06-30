// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProjectOpenerExtension.Models;

namespace ProjectOpenerExtension.Services;

/// <summary>
/// Settings manager - fully based on a JSON config file, no hard-coded editors
/// </summary>
public class DynamicSettingsManager : JsonSettingsManager
{
    private static DynamicSettingsManager _instance;
    private static readonly string _namespace = "projectopener";
    private string _configFilePath;

    private List<EditorDefinition> _editors;
    private TextSetting _configPathSetting;

    public static DynamicSettingsManager Instance => _instance ??= new DynamicSettingsManager();

    public List<EditorDefinition> Editors => _editors;

    private DynamicSettingsManager()
    {
        FilePath = GetSettingsFilePath();

        // Get the user-defined config file path (read from the persisted file)
        _configFilePath = GetUserConfigFilePath();

        // No custom path set: use the default location and auto-create editors.json from detected editors.
        if (string.IsNullOrEmpty(_configFilePath))
        {
            _configFilePath = GetDefaultConfigFilePath();
            BootstrapConfigIfMissing(_configFilePath);
        }
        System.Diagnostics.Debug.WriteLine($"[ProjectOpener] Config file path: {_configFilePath}");

        // Load editor configuration
        _editors = LoadEditors();

        // Config file path setting. Empty = use the auto-created default; set a path to override.
        _configPathSetting = new TextSetting(
            Namespaced("config_file_path"),
            "Configuration File Path (optional)",
            "editors.json is created automatically from your installed editors. Leave empty to use the default location, or enter a full path to a custom editors.json to override it.\n\nFor configuration format and examples, visit:\nhttps://github.com/caolib/ProjectOpenerExtension#configuration",
            string.Empty  // Empty = auto/default
        );
        Settings.Add(_configPathSetting);

        // Load settings to display the current values
        LoadSettings();

        // Listen for settings changes
        Settings.SettingsChanged += (s, e) =>
        {
            SaveSettings(); // Save settings
            var newPath = _configPathSetting.Value;
            if (!string.IsNullOrEmpty(newPath) && newPath != _configFilePath)
            {
                _configFilePath = newPath;
                _editors = LoadEditors();
                System.Diagnostics.Debug.WriteLine($"[ProjectOpener] Config file path updated: {newPath}");
            }
        };
    }

    /// <summary>
    /// Get the user-defined config file path (read from the persisted file)
    /// </summary>
    private static string GetUserConfigFilePath()
    {
        try
        {
            var settingsFile = GetSettingsFilePath();
            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
				using JsonDocument doc = JsonDocument.Parse(json);
				var root = doc.RootElement;
				var key = Namespaced("config_file_path");
				if (root.TryGetProperty(key, out JsonElement value))
				{
					var path = value.GetString();
					if (!string.IsNullOrEmpty(path))
					{
						return path;
					}
				}
			}
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProjectOpener] Failed to read config path: {ex.Message}");
        }

        // On first use return an empty string, with no default path
        return string.Empty;
    }

    /// <summary>
    /// Get the default config file path
    /// </summary>
    private static string GetDefaultConfigFilePath()
    {
        bool isMsix = IsMsixPackage();

        if (isMsix)
        {
            try
            {
                var packageFamilyName = GetPackageFamilyName();
                if (!string.IsNullOrEmpty(packageFamilyName))
                {
                    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    return Path.Combine(
                        localAppData,
                        "Packages",
                        packageFamilyName,
                        "LocalCache",
                        "Local",
                        "ProjectOpenerExtension",
                        "editors.json"
                    );
                }
            }
            catch { }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ProjectOpenerExtension",
                "editors.json"
            );
        }
        else
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config",
                "ProjectOpenerExtension",
                "editors.json"
            );
        }
    }

    /// <summary>
    /// Get the PackageFamilyName of the current MSIX package
    /// </summary>
    private static string GetPackageFamilyName()
    {
        try
        {
            // Method 1: environment variable
            var pfn = Environment.GetEnvironmentVariable("PACKAGE_FAMILY_NAME");
            if (!string.IsNullOrEmpty(pfn))
            {
                return pfn;
            }

            // Method 2: parse from the process path
            var processPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(processPath) && processPath.Contains(@"\WindowsApps\"))
            {
                // Path format: C:\Program Files\WindowsApps\{PackageFamilyName}\...
                var parts = processPath.Split([ @"\WindowsApps\" ], StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    var packagePath = parts[1];
                    var packageName = packagePath.Split('\\')[0];
                    return packageName;
                }
            }

            // Method 3: check whether a matching package exists under the Packages directory
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var packagesDir = Path.Combine(localAppData, "Packages");

            if (Directory.Exists(packagesDir))
            {
                var packageDirs = Directory.GetDirectories(packagesDir, "*ProjectOpenerExtension*");
                if (packageDirs.Length > 0)
                {
                    return Path.GetFileName(packageDirs[0]);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProjectOpener] GetPackageFamilyName failed: {ex.Message}");
        }

        return string.Empty;
    }

    /// <summary>
    /// Create editors.json at the given path (from auto-detected editors) if it does not yet exist.
    /// Writes an empty array when nothing is detected so the file exists and can be edited by hand.
    /// </summary>
    private static void BootstrapConfigIfMissing(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return;
            }

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var detected = EditorDetectionService.DetectInstalledEditors();
            var json = JsonSerializer.Serialize(detected, DynamicSettingsContext.Default.ListEditorDefinition);
            File.WriteAllText(path, json);
            System.Diagnostics.Debug.WriteLine($"[ProjectOpener] Auto-generated editors.json with {detected.Count} editor(s) at {path}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProjectOpener] Failed to auto-generate editors.json: {ex.Message}");
        }
    }

    /// <summary>
    /// Load the editor list from the config file
    /// </summary>
    private List<EditorDefinition> LoadEditors()
    {
        try
        {
            if (string.IsNullOrEmpty(_configFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectOpener] ⚠ No config file path set");
                return [];
            }

            System.Diagnostics.Debug.WriteLine($"[ProjectOpener] LoadEditors: loading config file {_configFilePath}");

            if (!File.Exists(_configFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectOpener] ⚠ Config file does not exist: {_configFilePath}");
                return [];
            }

            var json = File.ReadAllText(_configFilePath);
            System.Diagnostics.Debug.WriteLine($"[ProjectOpener] Config file content: {json}");

            var editors = JsonSerializer.Deserialize(json, DynamicSettingsContext.Default.ListEditorDefinition) ?? [];
            System.Diagnostics.Debug.WriteLine($"[ProjectOpener] Successfully loaded {editors.Count} editor(s)");

            foreach (var editor in editors)
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectOpener] - {editor.Name}: Enabled={editor.Enabled}, Type={editor.EditorType}, Path={editor.ProjectPath}");
            }

            return editors;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProjectOpener] Failed to load config: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ProjectOpener] Stack trace: {ex.StackTrace}");
            return [];
        }
    }

    /// <summary>
    /// Get the editor configuration list (backward compatible)
    /// Reloads the config file on every call to ensure it is up to date
    /// </summary>
    public List<EditorConfig> GetEditorConfigs()
    {
        // Reload the config file
        _editors = LoadEditors();

        var configs = new List<EditorConfig>();

        System.Diagnostics.Debug.WriteLine($"[ProjectOpener] GetEditorConfigs: {_editors.Count} editor(s) total");

        foreach (var editor in _editors.Where(e => e.Enabled))
        {
            System.Diagnostics.Debug.WriteLine($"[ProjectOpener] Processing editor: {editor.Name}, Type: {editor.EditorType}, Path: {editor.ProjectPath}");

            var config = new EditorConfig
            {
                Id = editor.Name.ToLowerInvariant().Replace(" ", "_"),
                Name = editor.Name,
                ExecutablePath = editor.ExecutablePath,
                Icon = editor.Icon,
                IsEnabled = editor.Enabled
            };

            if (editor.EditorType.Equals("vscode", StringComparison.OrdinalIgnoreCase))
            {
                config.Type = EditorType.VSCode;
                config.StorageFilePath = editor.ProjectPath;
                System.Diagnostics.Debug.WriteLine($"[ProjectOpener] VS Code editor - StorageFilePath: {config.StorageFilePath}");
            }
            else if (editor.EditorType.Equals("jetbrains", StringComparison.OrdinalIgnoreCase))
            {
                config.Type = EditorType.IntelliJIdea;
                config.ConfigFolderPattern = editor.ProjectPath;
                System.Diagnostics.Debug.WriteLine($"[ProjectOpener] JetBrains editor - ConfigFolderPattern: {config.ConfigFolderPattern}");
            }
            else if (editor.EditorType.Equals("visualstudio", StringComparison.OrdinalIgnoreCase))
            {
                config.Type = EditorType.VisualStudio;
                config.ConfigFolderPattern = editor.ProjectPath; // %LOCALAPPDATA%\Microsoft\VisualStudio
                System.Diagnostics.Debug.WriteLine($"[ProjectOpener] Visual Studio editor - ConfigFolderPattern: {config.ConfigFolderPattern}");
            }

            configs.Add(config);
        }

        System.Diagnostics.Debug.WriteLine($"[ProjectOpener] GetEditorConfigs returning {configs.Count} config(s)");
        return configs;
    }

    /// <summary>
    /// Detect whether running inside an MSIX package
    /// </summary>
    private static bool IsMsixPackage()
    {
        try
        {
            // Method 1: check the PACKAGE_FAMILY_NAME environment variable
            var packageFamilyName = Environment.GetEnvironmentVariable("PACKAGE_FAMILY_NAME");
            if (!string.IsNullOrEmpty(packageFamilyName))
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectOpener] MSIX detection: PACKAGE_FAMILY_NAME = {packageFamilyName}");
                return true;
            }

            // Method 2: check whether the process runs under the WindowsApps directory
            var processPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(processPath) && processPath.Contains(@"\WindowsApps\"))
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectOpener] MSIX detection: process in WindowsApps directory - {processPath}");
                return true;
            }

            // Method 3: check whether AppxManifest.xml exists
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var manifestPath = Path.Combine(appDirectory, "AppxManifest.xml");
            if (File.Exists(manifestPath))
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectOpener] MSIX detection: found AppxManifest.xml");
                return true;
            }

            System.Diagnostics.Debug.WriteLine($"[ProjectOpener] MSIX detection: not an MSIX environment");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProjectOpener] MSIX detection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get the PowerToys settings file path
    /// </summary>
    private static string GetSettingsFilePath()
    {
        bool isMsix = IsMsixPackage();

        string settingsFolder;
        if (isMsix)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            settingsFolder = Path.Combine(localAppData, "ProjectOpenerExtension");
        }
        else
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            settingsFolder = Path.Combine(userProfile, ".config", "ProjectOpenerExtension");
        }

        Directory.CreateDirectory(settingsFolder);
        return Path.Combine(settingsFolder, "powertoys-settings.json");
    }

    private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";
}

/// <summary>
/// Editor definition - matches the user config file format
/// </summary>
public class EditorDefinition
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Icon { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public string EditorType { get; set; } = string.Empty; // "vscode" or "jetbrains"
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<EditorDefinition>))]
internal partial class DynamicSettingsContext : JsonSerializerContext { }