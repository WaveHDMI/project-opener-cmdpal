// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProjectOpenerExtension.Commands;
using ProjectOpenerExtension.Models;
using ProjectOpenerExtension.Services;

namespace ProjectOpenerExtension.Pages;

/// <summary>
/// Settings page - displays and manages editor configuration
/// </summary>
internal sealed partial class SettingsPage : ListPage
{
    private readonly DynamicSettingsManager _settingsService;

    public SettingsPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Settings";
        PlaceholderText = "Search editors...";

        _settingsService = DynamicSettingsManager.Instance;
    }

    public override IListItem[] GetItems()
    {
        var items = new List<IListItem>();
        var editors = _settingsService.GetEditorConfigs();

        // === VS Code editors ===
        items.Add(new ListItem(new NoOpCommand())
        {
            Title = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
            Subtitle = "VS Code Editors",
            Section = "VS Code Editors"
        });

        foreach (var editor in editors.Where(e => e.Type == EditorType.VSCode))
        {
            var subtitle = BuildEditorSubtitle(editor);

            items.Add(new ListItem(new NoOpCommand())
            {
                Title = $"{editor.Name}",
                Subtitle = subtitle,
                Section = "VS Code Editors",
                Tags = new[]
                {
                    new Tag { Text = editor.IsEnabled ? "✓ Enabled" : "Disabled" }
                }
            });
        }

        // === Visual Studio ===
        items.Add(new ListItem(new NoOpCommand())
        {
            Title = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
            Subtitle = "Visual Studio",
            Section = "Visual Studio Editors"
        });

        foreach (var editor in editors.Where(e => e.Type == EditorType.VisualStudio))
        {
            var subtitle = BuildEditorSubtitle(editor);

            items.Add(new ListItem(new NoOpCommand())
            {
                Title = editor.Name,
                Subtitle = subtitle,
                Section = "Visual Studio Editors",
                Tags = [ new Tag { Text = editor.IsEnabled ? "✓ Enabled" : "Disabled" } ]
            });
        }

        // === JetBrains editors ===
        items.Add(new ListItem(new NoOpCommand())
        {
            Title = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
            Subtitle = "JetBrains Editors",
            Section = "JetBrains Editors"
        });

        foreach (var editor in editors.Where(e => e.Type != EditorType.VSCode && e.Type != EditorType.VisualStudio))
        {
            var subtitle = BuildEditorSubtitle(editor);

            items.Add(new ListItem(new NoOpCommand())
            {
                Title = editor.Name,
                Subtitle = subtitle,
                Section = "JetBrains Editors",
                Tags = new[]
                {
                    new Tag { Text = editor.IsEnabled ? "✓ Enabled" : "Disabled" }
                }
            });
        }

        // === Custom editor instructions ===
        items.Add(new ListItem(new NoOpCommand())
        {
            Title = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
            Subtitle = "How to add custom editors",
            Section = "Custom Editors"
        });

        var settingsFolder = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProjectOpenerExtension");

        items.Add(new ListItem(new OpenFolderCommand(settingsFolder))
        {
            Title = "📂 Open Config Folder",
            Subtitle = settingsFolder,
            Section = "Custom Editors"
        });

        items.Add(new ListItem(new NoOpCommand())
        {
            Title = "📝 Edit custom-editors.json",
            Subtitle = "Create or edit the custom-editors.json file in the config folder",
            Section = "Custom Editors"
        });

        items.Add(new ListItem(new NoOpCommand())
        {
            Title = "💡 VS Code Editor Format",
            Subtitle = "{\"Id\":\"myeditor\", \"Name\":\"My Editor\", \"Type\":\"vscode\", \"DefaultExecutable\":\"myeditor\"}",
            Section = "Custom Editors"
        });

        items.Add(new ListItem(new NoOpCommand())
        {
            Title = "💡 JetBrains Editor Format",
            Subtitle = "{\"Id\":\"myide\", \"Name\":\"My IDE\", \"Type\":\"jetbrains\", \"DefaultExecutable\":\"myide64.exe\"}",
            Section = "Custom Editors"
        });

        items.Add(new ListItem(new NoOpCommand())
        {
            Title = "🔄 Restart to apply",
            Subtitle = "After adding custom editors, restart PowerToys for the changes to take effect",
            Section = "Custom Editors"
        });

        return [.. items];
    }

    private string BuildEditorSubtitle(EditorConfig editor)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(editor.ExecutablePath))
        {
            parts.Add($"Executable: {editor.ExecutablePath}");
        }

        if (!string.IsNullOrEmpty(editor.StorageFilePath))
        {
            parts.Add($"Storage: {editor.StorageFilePath}");
        }
        else if (!string.IsNullOrEmpty(editor.ConfigFolderPattern))
        {
            parts.Add($"Config Pattern: {editor.ConfigFolderPattern}");
        }

        return parts.Count > 0 ? string.Join(" • ", parts) : "No configuration";
    }
}
