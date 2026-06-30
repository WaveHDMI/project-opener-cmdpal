// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using ProjectOpenerExtension.Models;

namespace ProjectOpenerExtension.Services;

/// <summary>
/// Service that reads recent projects from VS Code-family editors
/// </summary>
public class VSCodeProjectService
{
    private readonly DynamicSettingsManager _settingsService;

    public VSCodeProjectService()
    {
        _settingsService = DynamicSettingsManager.Instance;
    }

    public List<ProjectInfo> GetRecentProjects()
    {
        var projects = new List<ProjectInfo>();
        var vscodeEditors = _settingsService.GetEditorConfigs()
            .Where(e => e.Type == EditorType.VSCode && !string.IsNullOrEmpty(e.StorageFilePath))
            .ToList();

        foreach (var editor in vscodeEditors)
        {
            try
            {
                if (!File.Exists(editor.StorageFilePath))
                {
                    continue;
                }

                var json = File.ReadAllText(editor.StorageFilePath);
                using var doc = JsonDocument.Parse(json);

                // Try the new format: profileAssociations.workspaces
                if (doc.RootElement.TryGetProperty("profileAssociations", out var profileAssociations))
                {
                    if (profileAssociations.TryGetProperty("workspaces", out var workspaces))
                    {
                        foreach (var workspace in workspaces.EnumerateObject())
                        {
                            var uri = workspace.Name;
                            // Only handle local file paths, skip remote paths (vscode-remote://)
                            if (!string.IsNullOrEmpty(uri) && uri.StartsWith("file:///") && !uri.Contains("vscode-remote://"))
                            {
                                var path = Uri.UnescapeDataString(uri.Replace("file:///", "").Replace("/", "\\"));
                                // Decode URL-encoded paths (e.g. %3A -> :)
                                path = Uri.UnescapeDataString(path);

                                if (Directory.Exists(path))
                                {
                                    AddOrUpdateProject(projects, path, editor.Id);
                                }
                            }
                        }
                    }
                }

                // Fall back to the old format: openedPathsList
                if (doc.RootElement.TryGetProperty("openedPathsList", out var openedPaths))
                {
                    if (openedPaths.TryGetProperty("entries", out var entries))
                    {
                        foreach (var entry in entries.EnumerateArray())
                        {
                            if (entry.TryGetProperty("folderUri", out var folderUri))
                            {
                                var uri = folderUri.GetString();
                                if (!string.IsNullOrEmpty(uri))
                                {
                                    var path = Uri.UnescapeDataString(uri.Replace("file:///", "").Replace("/", "\\"));
                                    if (Directory.Exists(path))
                                    {
                                        AddOrUpdateProject(projects, path, editor.Id);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading {editor.Name} projects: {ex.Message}");
            }
        }

        return projects;
    }

    private static void AddOrUpdateProject(List<ProjectInfo> projects, string path, string editorId)
    {
        var existing = projects.FirstOrDefault(p => p.Path == path);
        if (existing != null)
        {
            if (!existing.AvailableEditorIds.Contains(editorId))
            {
                existing.AvailableEditorIds.Add(editorId);
            }
        }
        else
        {
            projects.Add(new ProjectInfo
            {
                Name = Path.GetFileName(path),
                Path = path,
                AvailableEditorIds = new List<string> { editorId },
                LastOpened = Directory.GetLastWriteTime(path),
                SourceEditorId = editorId
            });
        }
    }

    public static void OpenInEditor(string projectPath, string editorId)
    {
        try
        {
            var editor = DynamicSettingsManager.Instance.GetEditorConfigs().Find(e => e.Id == editorId);
            if (editor == null)
            {
                Debug.WriteLine($"Editor not found: {editorId}");
                return;
            }

            var args = string.Format(editor.CommandLineArgs, projectPath);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = editor.ExecutablePath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening editor: {ex.Message}");
        }
    }
}
