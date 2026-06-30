// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProjectOpenerExtension.Models;

namespace ProjectOpenerExtension.Services;

/// <summary>
/// Settings manager - uses PowerToys' JsonSettingsManager
/// </summary>
public class SettingsManager : JsonSettingsManager
{
    private static SettingsManager _instance;
    private static readonly string _namespace = "projectopener";

    // VS Code family
    private readonly ToggleSetting _enableVSCode;
    private readonly TextSetting _vscodeExecutable;
    private readonly TextSetting _vscodeIcon;
    private readonly TextSetting _vscodeStoragePath;

    private readonly ToggleSetting _enableVSCodium;
    private readonly TextSetting _vscodiumExecutable;
    private readonly TextSetting _vscodiumIcon;
    private readonly TextSetting _vscodiumStoragePath;

    private readonly ToggleSetting _enableCursor;
    private readonly TextSetting _cursorExecutable;
    private readonly TextSetting _cursorIcon;
    private readonly TextSetting _cursorStoragePath;

    private readonly ToggleSetting _enableWindsurf;
    private readonly TextSetting _windsurfExecutable;
    private readonly TextSetting _windsurfIcon;
    private readonly TextSetting _windsurfStoragePath;

    // JetBrains family
    private readonly ToggleSetting _enableIntelliJ;
    private readonly TextSetting _intellijExecutable;
    private readonly TextSetting _intellijIcon;
    private readonly TextSetting _intellijConfigPattern;

    private readonly ToggleSetting _enableWebStorm;
    private readonly TextSetting _webstormExecutable;
    private readonly TextSetting _webstormIcon;
    private readonly TextSetting _webstormConfigPattern;

    private readonly ToggleSetting _enablePyCharm;
    private readonly TextSetting _pycharmExecutable;
    private readonly TextSetting _pycharmIcon;
    private readonly TextSetting _pycharmConfigPattern;

    private readonly ToggleSetting _enableGoLand;
    private readonly TextSetting _golandExecutable;
    private readonly TextSetting _golandIcon;
    private readonly TextSetting _golandConfigPattern;

    private readonly ToggleSetting _enablePhpStorm;
    private readonly TextSetting _phpstormExecutable;
    private readonly TextSetting _phpstormIcon;
    private readonly TextSetting _phpstormConfigPattern;

    private readonly ToggleSetting _enableRider;
    private readonly TextSetting _riderExecutable;
    private readonly TextSetting _riderIcon;
    private readonly TextSetting _riderConfigPattern;

    private readonly ToggleSetting _enableCLion;
    private readonly TextSetting _clionExecutable;
    private readonly TextSetting _clionIcon;
    private readonly TextSetting _clionConfigPattern;

    private readonly ToggleSetting _enableRubyMine;
    private readonly TextSetting _rubymineExecutable;
    private readonly TextSetting _rubymineIcon;
    private readonly TextSetting _rubymineConfigPattern;

    private readonly ToggleSetting _enableDataGrip;
    private readonly TextSetting _datagripExecutable;
    private readonly TextSetting _datagripIcon;
    private readonly TextSetting _datagripConfigPattern;

    public static SettingsManager Instance => _instance ??= new SettingsManager();

    private SettingsManager()
    {
        FilePath = GetSettingsFilePath();

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // VS Code
        _enableVSCode = new ToggleSetting(Namespaced("enableVSCode"), "Enable VS Code", "Show projects from Visual Studio Code", true);
        _vscodeExecutable = new TextSetting(Namespaced("vscodeExecutable"), "VS Code Executable", "Path to code.exe or command name", "code");
        _vscodeIcon = new TextSetting(Namespaced("vscodeIcon"), "VS Code Icon", "Icon emoji", "📝");
        _vscodeStoragePath = new TextSetting(Namespaced("vscodeStoragePath"), "VS Code Storage Path", "Path to storage.json",
            Path.Combine(appData, "Code", "User", "globalStorage", "storage.json"));

        // VSCodium
        _enableVSCodium = new ToggleSetting(Namespaced("enableVSCodium"), "Enable VSCodium", "Show projects from VSCodium", true);
        _vscodiumExecutable = new TextSetting(Namespaced("vscodiumExecutable"), "VSCodium Executable", "Path to codium.exe or command name", "codium");
        _vscodiumIcon = new TextSetting(Namespaced("vscodiumIcon"), "VSCodium Icon", "Icon emoji", "📘");
        _vscodiumStoragePath = new TextSetting(Namespaced("vscodiumStoragePath"), "VSCodium Storage Path", "Path to storage.json",
            Path.Combine(appData, "VSCodium", "User", "globalStorage", "storage.json"));

        // Cursor
        _enableCursor = new ToggleSetting(Namespaced("enableCursor"), "Enable Cursor", "Show projects from Cursor editor", true);
        _cursorExecutable = new TextSetting(Namespaced("cursorExecutable"), "Cursor Executable", "Path to cursor.exe or command name", "cursor");
        _cursorIcon = new TextSetting(Namespaced("cursorIcon"), "Cursor Icon", "Icon emoji", "🖱️");
        _cursorStoragePath = new TextSetting(Namespaced("cursorStoragePath"), "Cursor Storage Path", "Path to storage.json",
            Path.Combine(appData, "Cursor", "User", "globalStorage", "storage.json"));

        // Windsurf
        _enableWindsurf = new ToggleSetting(Namespaced("enableWindsurf"), "Enable Windsurf", "Show projects from Windsurf editor", true);
        _windsurfExecutable = new TextSetting(Namespaced("windsurfExecutable"), "Windsurf Executable", "Path to windsurf.exe or command name", "windsurf");
        _windsurfIcon = new TextSetting(Namespaced("windsurfIcon"), "Windsurf Icon", "Icon emoji", "🏄");
        _windsurfStoragePath = new TextSetting(Namespaced("windsurfStoragePath"), "Windsurf Storage Path", "Path to storage.json",
            Path.Combine(appData, "Windsurf", "User", "globalStorage", "storage.json"));

        // IntelliJ IDEA
        _enableIntelliJ = new ToggleSetting(Namespaced("enableIntelliJ"), "Enable IntelliJ IDEA", "Show projects from IntelliJ IDEA", true);
        _intellijExecutable = new TextSetting(Namespaced("intellijExecutable"), "IntelliJ Executable", "Path to idea64.exe", "idea64.exe");
        _intellijIcon = new TextSetting(Namespaced("intellijIcon"), "IntelliJ Icon", "Icon emoji", "💡");
        _intellijConfigPattern = new TextSetting(Namespaced("intellijConfigPattern"), "IntelliJ Config Pattern", "Config folder pattern (e.g., IntelliJIdea*)", "IntelliJIdea*");

        // WebStorm
        _enableWebStorm = new ToggleSetting(Namespaced("enableWebStorm"), "Enable WebStorm", "Show projects from WebStorm", true);
        _webstormExecutable = new TextSetting(Namespaced("webstormExecutable"), "WebStorm Executable", "Path to webstorm64.exe", "webstorm64.exe");
        _webstormIcon = new TextSetting(Namespaced("webstormIcon"), "WebStorm Icon", "Icon emoji", "🌊");
        _webstormConfigPattern = new TextSetting(Namespaced("webstormConfigPattern"), "WebStorm Config Pattern", "Config folder pattern", "WebStorm*");

        // PyCharm
        _enablePyCharm = new ToggleSetting(Namespaced("enablePyCharm"), "Enable PyCharm", "Show projects from PyCharm", true);
        _pycharmExecutable = new TextSetting(Namespaced("pycharmExecutable"), "PyCharm Executable", "Path to pycharm64.exe", "pycharm64.exe");
        _pycharmIcon = new TextSetting(Namespaced("pycharmIcon"), "PyCharm Icon", "Icon emoji", "🐍");
        _pycharmConfigPattern = new TextSetting(Namespaced("pycharmConfigPattern"), "PyCharm Config Pattern", "Config folder pattern", "PyCharm*");

        // GoLand
        _enableGoLand = new ToggleSetting(Namespaced("enableGoLand"), "Enable GoLand", "Show projects from GoLand", true);
        _golandExecutable = new TextSetting(Namespaced("golandExecutable"), "GoLand Executable", "Path to goland64.exe", "goland64.exe");
        _golandIcon = new TextSetting(Namespaced("golandIcon"), "GoLand Icon", "Icon emoji", "🦫");
        _golandConfigPattern = new TextSetting(Namespaced("golandConfigPattern"), "GoLand Config Pattern", "Config folder pattern", "GoLand*");

        // PhpStorm
        _enablePhpStorm = new ToggleSetting(Namespaced("enablePhpStorm"), "Enable PhpStorm", "Show projects from PhpStorm", true);
        _phpstormExecutable = new TextSetting(Namespaced("phpstormExecutable"), "PhpStorm Executable", "Path to phpstorm64.exe", "phpstorm64.exe");
        _phpstormIcon = new TextSetting(Namespaced("phpstormIcon"), "PhpStorm Icon", "Icon emoji", "🐘");
        _phpstormConfigPattern = new TextSetting(Namespaced("phpstormConfigPattern"), "PhpStorm Config Pattern", "Config folder pattern", "PhpStorm*");

        // Rider
        _enableRider = new ToggleSetting(Namespaced("enableRider"), "Enable Rider", "Show projects from Rider", true);
        _riderExecutable = new TextSetting(Namespaced("riderExecutable"), "Rider Executable", "Path to rider64.exe", "rider64.exe");
        _riderIcon = new TextSetting(Namespaced("riderIcon"), "Rider Icon", "Icon emoji", "🎯");
        _riderConfigPattern = new TextSetting(Namespaced("riderConfigPattern"), "Rider Config Pattern", "Config folder pattern", "Rider*");

        // CLion
        _enableCLion = new ToggleSetting(Namespaced("enableCLion"), "Enable CLion", "Show projects from CLion", true);
        _clionExecutable = new TextSetting(Namespaced("clionExecutable"), "CLion Executable", "Path to clion64.exe", "clion64.exe");
        _clionIcon = new TextSetting(Namespaced("clionIcon"), "CLion Icon", "Icon emoji", "⚙️");
        _clionConfigPattern = new TextSetting(Namespaced("clionConfigPattern"), "CLion Config Pattern", "Config folder pattern", "CLion*");

        // RubyMine
        _enableRubyMine = new ToggleSetting(Namespaced("enableRubyMine"), "Enable RubyMine", "Show projects from RubyMine", true);
        _rubymineExecutable = new TextSetting(Namespaced("rubymineExecutable"), "RubyMine Executable", "Path to rubymine64.exe", "rubymine64.exe");
        _rubymineIcon = new TextSetting(Namespaced("rubymineIcon"), "RubyMine Icon", "Icon emoji", "💎");
        _rubymineConfigPattern = new TextSetting(Namespaced("rubymineConfigPattern"), "RubyMine Config Pattern", "Config folder pattern", "RubyMine*");

        // DataGrip
        _enableDataGrip = new ToggleSetting(Namespaced("enableDataGrip"), "Enable DataGrip", "Show projects from DataGrip", true);
        _datagripExecutable = new TextSetting(Namespaced("datagripExecutable"), "DataGrip Executable", "Path to datagrip64.exe", "datagrip64.exe");
        _datagripIcon = new TextSetting(Namespaced("datagripIcon"), "DataGrip Icon", "Icon emoji", "🗄️");
        _datagripConfigPattern = new TextSetting(Namespaced("datagripConfigPattern"), "DataGrip Config Pattern", "Config folder pattern", "DataGrip*");

        // Add all settings to the Settings collection
        AddVSCodeSettings();
        AddJetBrainsSettings();

        // Load settings
        LoadSettings();

        // Listen for settings changes and save automatically
        Settings.SettingsChanged += (s, e) => SaveSettings();
    }

    private void AddVSCodeSettings()
    {
        // VS Code
        Settings.Add(_enableVSCode);
        Settings.Add(_vscodeExecutable);
        Settings.Add(_vscodeIcon);
        Settings.Add(_vscodeStoragePath);

        // VSCodium
        Settings.Add(_enableVSCodium);
        Settings.Add(_vscodiumExecutable);
        Settings.Add(_vscodiumIcon);
        Settings.Add(_vscodiumStoragePath);

        // Cursor
        Settings.Add(_enableCursor);
        Settings.Add(_cursorExecutable);
        Settings.Add(_cursorIcon);
        Settings.Add(_cursorStoragePath);

        // Windsurf
        Settings.Add(_enableWindsurf);
        Settings.Add(_windsurfExecutable);
        Settings.Add(_windsurfIcon);
        Settings.Add(_windsurfStoragePath);
    }

    private void AddJetBrainsSettings()
    {
        // IntelliJ
        Settings.Add(_enableIntelliJ);
        Settings.Add(_intellijExecutable);
        Settings.Add(_intellijIcon);
        Settings.Add(_intellijConfigPattern);

        // WebStorm
        Settings.Add(_enableWebStorm);
        Settings.Add(_webstormExecutable);
        Settings.Add(_webstormIcon);
        Settings.Add(_webstormConfigPattern);

        // PyCharm
        Settings.Add(_enablePyCharm);
        Settings.Add(_pycharmExecutable);
        Settings.Add(_pycharmIcon);
        Settings.Add(_pycharmConfigPattern);

        // GoLand
        Settings.Add(_enableGoLand);
        Settings.Add(_golandExecutable);
        Settings.Add(_golandIcon);
        Settings.Add(_golandConfigPattern);

        // PhpStorm
        Settings.Add(_enablePhpStorm);
        Settings.Add(_phpstormExecutable);
        Settings.Add(_phpstormIcon);
        Settings.Add(_phpstormConfigPattern);

        // Rider
        Settings.Add(_enableRider);
        Settings.Add(_riderExecutable);
        Settings.Add(_riderIcon);
        Settings.Add(_riderConfigPattern);

        // CLion
        Settings.Add(_enableCLion);
        Settings.Add(_clionExecutable);
        Settings.Add(_clionIcon);
        Settings.Add(_clionConfigPattern);

        // RubyMine
        Settings.Add(_enableRubyMine);
        Settings.Add(_rubymineExecutable);
        Settings.Add(_rubymineIcon);
        Settings.Add(_rubymineConfigPattern);

        // DataGrip
        Settings.Add(_enableDataGrip);
        Settings.Add(_datagripExecutable);
        Settings.Add(_datagripIcon);
        Settings.Add(_datagripConfigPattern);
    }

    private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

    private static string GetSettingsFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsFolder = Path.Combine(localAppData, "ProjectOpenerExtension");
        Directory.CreateDirectory(settingsFolder);
        return Path.Combine(settingsFolder, "powertoys-settings.json");
    }

    // Build the editor configuration list
    public List<EditorConfig> GetEditorConfigs()
    {
        var configs = new List<EditorConfig>();

        // VS Code family
        if (_enableVSCode.Value)
        {
            configs.Add(new EditorConfig
            {
                Id = "vscode",
                Name = "Visual Studio Code",
                ExecutablePath = _vscodeExecutable.Value,
                Icon = _vscodeIcon.Value,
                Type = EditorType.VSCode,
                IsEnabled = true,
                StorageFilePath = _vscodeStoragePath.Value
            });
        }

        if (_enableVSCodium.Value)
        {
            configs.Add(new EditorConfig
            {
                Id = "vscodium",
                Name = "VSCodium",
                ExecutablePath = _vscodiumExecutable.Value,
                Icon = _vscodiumIcon.Value,
                Type = EditorType.VSCode,
                IsEnabled = true,
                StorageFilePath = _vscodiumStoragePath.Value
            });
        }

        if (_enableCursor.Value)
        {
            configs.Add(new EditorConfig
            {
                Id = "cursor",
                Name = "Cursor",
                ExecutablePath = _cursorExecutable.Value,
                Icon = _cursorIcon.Value,
                Type = EditorType.VSCode,
                IsEnabled = true,
                StorageFilePath = _cursorStoragePath.Value
            });
        }

        if (_enableWindsurf.Value)
        {
            configs.Add(new EditorConfig
            {
                Id = "windsurf",
                Name = "Windsurf",
                ExecutablePath = _windsurfExecutable.Value,
                Icon = _windsurfIcon.Value,
                Type = EditorType.VSCode,
                IsEnabled = true,
                StorageFilePath = _windsurfStoragePath.Value
            });
        }

        // JetBrains family
        if (_enableIntelliJ.Value)
        {
            configs.Add(new EditorConfig
            {
                Id = "idea",
                Name = "IntelliJ IDEA",
                ExecutablePath = _intellijExecutable.Value,
                Icon = _intellijIcon.Value,
                Type = EditorType.IntelliJIdea,
                IsEnabled = true,
                ConfigFolderPattern = _intellijConfigPattern.Value
            });
        }

        if (_enableWebStorm.Value)
        {
            configs.Add(new EditorConfig
            {
                Id = "webstorm",
                Name = "WebStorm",
                ExecutablePath = _webstormExecutable.Value,
                Icon = _webstormIcon.Value,
                Type = EditorType.WebStorm,
                IsEnabled = true,
                ConfigFolderPattern = _webstormConfigPattern.Value
            });
        }

        if (_enablePyCharm.Value)
        {
            configs.Add(new EditorConfig
            {
                Id = "pycharm",
                Name = "PyCharm",
                ExecutablePath = _pycharmExecutable.Value,
                Icon = _pycharmIcon.Value,
                Type = EditorType.PyCharm,
                IsEnabled = true,
                ConfigFolderPattern = _pycharmConfigPattern.Value
            });
        }

        if (_enableGoLand.Value)
        {
            configs.Add(new EditorConfig
            {
                Id = "goland",
                Name = "GoLand",
                ExecutablePath = _golandExecutable.Value,
                Icon = _golandIcon.Value,
                Type = EditorType.GoLand,
                IsEnabled = true,
                ConfigFolderPattern = _golandConfigPattern.Value
            });
        }

        if (_enablePhpStorm.Value)
        {
            configs.Add(new EditorConfig
            {
                Id = "phpstorm",
                Name = "PhpStorm",
                ExecutablePath = _phpstormExecutable.Value,
                Icon = _phpstormIcon.Value,
                Type = EditorType.PhpStorm,
                IsEnabled = true,
                ConfigFolderPattern = _phpstormConfigPattern.Value
            });
        }

        if (_enableRider.Value)
        {
            configs.Add(new EditorConfig
            {
                Id = "rider",
                Name = "Rider",
                ExecutablePath = _riderExecutable.Value,
                Icon = _riderIcon.Value,
                Type = EditorType.Rider,
                IsEnabled = true,
                ConfigFolderPattern = _riderConfigPattern.Value
            });
        }

        if (_enableCLion.Value)
        {
            configs.Add(new EditorConfig
            {
                Id = "clion",
                Name = "CLion",
                ExecutablePath = _clionExecutable.Value,
                Icon = _clionIcon.Value,
                Type = EditorType.CLion,
                IsEnabled = true,
                ConfigFolderPattern = _clionConfigPattern.Value
            });
        }

        if (_enableRubyMine.Value)
        {
            configs.Add(new EditorConfig
            {
                Id = "rubymine",
                Name = "RubyMine",
                ExecutablePath = _rubymineExecutable.Value,
                Icon = _rubymineIcon.Value,
                Type = EditorType.RubyMine,
                IsEnabled = true,
                ConfigFolderPattern = _rubymineConfigPattern.Value
            });
        }

        if (_enableDataGrip.Value)
        {
            configs.Add(new EditorConfig
            {
                Id = "datagrip",
                Name = "DataGrip",
                ExecutablePath = _datagripExecutable.Value,
                Icon = _datagripIcon.Value,
                Type = EditorType.DataGrip,
                IsEnabled = true,
                ConfigFolderPattern = _datagripConfigPattern.Value
            });
        }

        return configs;
    }

    public bool IsEditorEnabled(string editorId)
    {
        return editorId switch
        {
            "vscode" => _enableVSCode.Value,
            "vscodium" => _enableVSCodium.Value,
            "cursor" => _enableCursor.Value,
            "windsurf" => _enableWindsurf.Value,
            "idea" => _enableIntelliJ.Value,
            "webstorm" => _enableWebStorm.Value,
            "pycharm" => _enablePyCharm.Value,
            "goland" => _enableGoLand.Value,
            "phpstorm" => _enablePhpStorm.Value,
            "rider" => _enableRider.Value,
            "clion" => _enableCLion.Value,
            "rubymine" => _enableRubyMine.Value,
            "datagrip" => _enableDataGrip.Value,
            _ => false
        };
    }
}
