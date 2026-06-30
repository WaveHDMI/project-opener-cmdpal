// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions.Toolkit;
using ProjectOpenerExtension.Models;
using ProjectOpenerExtension.Services;

namespace ProjectOpenerExtension.Commands;

/// <summary>
/// Command to open a project in the specified editor
/// </summary>
public partial class OpenProjectCommand : InvokableCommand
{
    private readonly ProjectInfo _project;
    private readonly string _editorId;

    public OpenProjectCommand(ProjectInfo project, string editorId)
    {
        _project = project;
        _editorId = editorId;

        var editor = DynamicSettingsManager.Instance.GetEditorConfigs().Find(e => e.Id == editorId);
        if (editor != null)
        {
            Name = $"Open with {editor.Name}";

            // Use the configured icon if set, otherwise extract it from the exe file
            if (!string.IsNullOrWhiteSpace(editor.Icon))
            {
                Icon = new(editor.Icon);
            }
            else if (!string.IsNullOrWhiteSpace(editor.ExecutablePath) && System.IO.File.Exists(editor.ExecutablePath))
            {
                Icon = new($"{editor.ExecutablePath},0");
            }
            else
            {
                Icon = new("📁");
            }
        }
        else
        {
            Name = "Open";
            Icon = new("📁");
        }
    }

    public override CommandResult Invoke()
    {
        var editor = DynamicSettingsManager.Instance.GetEditorConfigs().Find(e => e.Id == _editorId);
        if (editor == null)
        {
            return CommandResult.Dismiss();
        }

        switch (editor.Type)
        {
            case EditorType.VSCode:
                VSCodeProjectService.OpenInEditor(_project.Path, _editorId);
                break;
            case EditorType.VisualStudio:
                VisualStudioProjectService.OpenInEditor(_project.Path, _editorId);
                break;
            default:
                JetBrainsProjectService.OpenInJetBrainsIDE(_project.Path, _editorId);
                break;
        }

        return CommandResult.Dismiss();
    }
}

/// <summary>
/// Open the project folder in File Explorer
/// </summary>
public partial class OpenFolderCommand : InvokableCommand
{
    private readonly string _path;

    public OpenFolderCommand(string path)
    {
        _path = path;
        Name = "Show in File Explorer";
        Icon = new("📂");
    }

    public override CommandResult Invoke()
    {
        // _path may be a file (e.g. a Visual Studio .sln); open its containing folder in that case.
        var target = System.IO.File.Exists(_path) ? System.IO.Path.GetDirectoryName(_path) ?? _path : _path;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{target}\"",
            UseShellExecute = true
        });
        return CommandResult.Dismiss();
    }
}


