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

namespace ProjectOpenerExtension;

internal sealed partial class ProjectOpenerExtensionPage : ListPage
{
    private readonly VSCodeProjectService _vscodeService;
    private readonly JetBrainsProjectService _jetbrainsService;
    private readonly VisualStudioProjectService _visualStudioService;
    private readonly DynamicSettingsManager _settingsService;

    public ProjectOpenerExtensionPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Recent Projects";
        PlaceholderText = "Search projects...";

        _vscodeService = new VSCodeProjectService();
        _jetbrainsService = new JetBrainsProjectService();
        _visualStudioService = new VisualStudioProjectService();
        _settingsService = DynamicSettingsManager.Instance;
    }

    public override IListItem[] GetItems()
    {
        var items = new List<IListItem>();
        var editors = _settingsService.GetEditorConfigs();

        // Get VS Code projects
        var vscodeProjects = _vscodeService.GetRecentProjects();
        if (vscodeProjects.Count > 0)
        {
            foreach (var project in vscodeProjects.OrderByDescending(p => p.LastOpened))
            {
                var editor = editors.Find(e => e.Id == project.SourceEditorId);
                var sectionLabel = editor != null ? $"{editor.Name} Projects" : "VS Code Projects";
                items.Add(CreateProjectListItem(project, sectionLabel, editors));
            }
        }

        // Get JetBrains projects
        var jetbrainsProjects = _jetbrainsService.GetRecentProjects();
        if (jetbrainsProjects.Count > 0)
        {
            foreach (var project in jetbrainsProjects.OrderByDescending(p => p.LastOpened))
            {
                var editor = editors.Find(e => e.Id == project.SourceEditorId);
                var sectionLabel = editor != null ? $"{editor.Name} Projects" : "Other Projects";
                items.Add(CreateProjectListItem(project, sectionLabel, editors));
            }
        }

        // Get Visual Studio projects
        var visualStudioProjects = _visualStudioService.GetRecentProjects();
        if (visualStudioProjects.Count > 0)
        {
            foreach (var project in visualStudioProjects.OrderByDescending(p => p.LastOpened))
            {
                var editor = editors.Find(e => e.Id == project.SourceEditorId);
                var sectionLabel = editor != null ? $"{editor.Name} Projects" : "Visual Studio Projects";
                items.Add(CreateProjectListItem(project, sectionLabel, editors));
            }
        }

        if (items.Count == 0)
        {
            // Check if editors are configured
            if (editors.Count == 0)
            {
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = "First time using? Configure editors first",
                    Subtitle = "Specify the configuration file path in settings and create editors.json file"
                });
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = "Configuration Steps",
                    Subtitle = "1. Command Palette → Settings (bottom left) → Extensions → Project Opener Extension"
                });
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = "",
                    Subtitle = "2. Enter full path in 'Configuration File Path' (e.g., C:\\config\\editors.json)"
                });
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = "",
                    Subtitle = "3. Create and edit the file, refer to configuration examples in settings"
                });
            }
            else
            {
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = "No recent projects found",
                    Subtitle = "Open some projects in VS Code or JetBrains IDEs to see them here"
                });
            }
        }

        return items.ToArray();
    }

    private ListItem CreateProjectListItem(ProjectInfo project, string sectionLabel, List<EditorConfig> editors)
    {
        var defaultEditorId = project.AvailableEditorIds.FirstOrDefault() ?? "vscode";
        var defaultCommand = new OpenProjectCommand(project, defaultEditorId);

        var contextCommands = new List<IContextItem>();

        // Add an open command for every enabled editor
        var enabledEditors = _settingsService.GetEditorConfigs().Where(e => e.IsEnabled).ToList();
        foreach (var editor in enabledEditors)
        {
            var openCommand = new OpenProjectCommand(project, editor.Id);
            contextCommands.Add(new CommandContextItem(openCommand));
        }

        // Add a "show in File Explorer" command
        contextCommands.Add(new CommandContextItem(new OpenFolderCommand(project.Path)));

        // Get the editor icon and load it with the icon service
        var sourceEditor = editors.Find(e => e.Id == project.SourceEditorId);
        IconData icon;

        if (sourceEditor != null)
        {
            // Use the configured icon if set
            if (!string.IsNullOrEmpty(sourceEditor.Icon))
            {
                icon = IconService.LoadIcon(sourceEditor.Icon);
            }
            // Otherwise try to extract the icon from the executable
            else if (!string.IsNullOrEmpty(sourceEditor.ExecutablePath) && System.IO.File.Exists(sourceEditor.ExecutablePath))
            {
                icon = IconService.LoadIcon(sourceEditor.ExecutablePath);
            }
            else
            {
                icon = IconService.GetDefaultIcon();
            }
        }
        else
        {
            icon = IconService.GetDefaultIcon();
        }

        // Build the subtitle showing the number of available editors
        var subtitle = project.Path;
        if (project.AvailableEditorIds.Count > 1)
        {
            var editorNames = project.AvailableEditorIds
                .Select(id => editors.Find(e => e.Id == id)?.Name ?? id)
                .Take(3);
            subtitle = $"{project.Path} • {string.Join(", ", editorNames)}";
            if (project.AvailableEditorIds.Count > 3)
            {
                subtitle += $" +{project.AvailableEditorIds.Count - 3}";
            }
        }

        return new ListItem(defaultCommand)
        {
            Title = project.Name,
            Subtitle = subtitle,
            Icon = new IconInfo(icon),
            Section = sectionLabel,
            Tags = [ new Tag { Text = FormatLastOpened(project.LastOpened) } ],
            MoreCommands = [.. contextCommands]
		};
    }

    private static string FormatLastOpened(DateTime lastOpened)
    {
        var diff = DateTime.Now - lastOpened;

        if (diff.TotalMinutes < 1)
            return "Just now";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";
        if (diff.TotalDays < 30)
            return $"{(int)(diff.TotalDays / 7)}w ago";

        return lastOpened.ToString("MMM dd", System.Globalization.CultureInfo.InvariantCulture);
    }
}
