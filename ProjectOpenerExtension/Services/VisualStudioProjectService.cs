// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using ProjectOpenerExtension.Models;

namespace ProjectOpenerExtension.Services;

/// <summary>
/// Reads recent Visual Studio solutions/folders from ApplicationPrivateSettings.xml.
/// </summary>
public class VisualStudioProjectService
{
    private readonly DynamicSettingsManager _settingsService;

    public VisualStudioProjectService()
    {
        _settingsService = DynamicSettingsManager.Instance;
    }

    public List<ProjectInfo> GetRecentProjects()
    {
        var projects = new List<ProjectInfo>();
        var vsEditors = _settingsService.GetEditorConfigs()
            .Where(e => e.Type == EditorType.VisualStudio && !string.IsNullOrEmpty(e.ConfigFolderPattern))
            .ToList();

        foreach (var editor in vsEditors)
        {
            try
            {
                var configPath = editor.ConfigFolderPattern;

                // ConfigFolderPattern is normally %LOCALAPPDATA%\Microsoft\VisualStudio, which holds one
                // subfolder per installed instance (e.g. 17.0_xxxx, 18.0_yyyy). Scan each instance's settings file.
                if (Directory.Exists(configPath))
                {
                    foreach (var instanceDir in Directory.GetDirectories(configPath))
                    {
                        var settingsFile = Path.Combine(instanceDir, "ApplicationPrivateSettings.xml");
                        if (File.Exists(settingsFile))
                        {
                            Debug.WriteLine($"[VisualStudio] Found settings file: {settingsFile}");
                            foreach (var project in ParseRecentProjects(settingsFile, editor.Id))
                            {
                                AddOrUpdateProject(projects, project, editor.Id);
                            }
                        }
                    }
                }
                // Allow pointing ConfigFolderPattern directly at an ApplicationPrivateSettings.xml file.
                else if (File.Exists(configPath))
                {
                    Debug.WriteLine($"[VisualStudio] Parsing file directly: {configPath}");
                    foreach (var project in ParseRecentProjects(configPath, editor.Id))
                    {
                        AddOrUpdateProject(projects, project, editor.Id);
                    }
                }
                else
                {
                    Debug.WriteLine($"[VisualStudio] Path does not exist: {configPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading {editor.Name} projects: {ex.Message}");
            }
        }

        return projects;
    }

    private static void AddOrUpdateProject(List<ProjectInfo> projects, ProjectInfo project, string editorId)
    {
        var existing = projects.FirstOrDefault(p => p.Path == project.Path);
        if (existing != null)
        {
            if (!existing.AvailableEditorIds.Contains(editorId))
            {
                existing.AvailableEditorIds.Add(editorId);
            }

            // Keep the most recent access time across instances.
            if (project.LastOpened > existing.LastOpened)
            {
                existing.LastOpened = project.LastOpened;
            }
        }
        else
        {
            projects.Add(project);
        }
    }

    private static List<ProjectInfo> ParseRecentProjects(string xmlPath, string editorId)
    {
        var projects = new List<ProjectInfo>();

        try
        {
            var doc = XDocument.Load(xmlPath);

            // <collection name="CodeContainers.Offline"><value name="value">[ JSON ]</value></collection>
            var json = doc.Descendants("collection")
                .FirstOrDefault(c => (string?)c.Attribute("name") == "CodeContainers.Offline")?
                .Elements("value")
                .FirstOrDefault(v => (string?)v.Attribute("name") == "value")?
                .Value;

            if (string.IsNullOrWhiteSpace(json))
            {
                return projects;
            }

            using var entries = JsonDocument.Parse(json);
            foreach (var entry in entries.RootElement.EnumerateArray())
            {
                if (!entry.TryGetProperty("Key", out var keyEl))
                {
                    continue;
                }

                var path = keyEl.GetString();
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var isFile = File.Exists(path);
                var isDir = Directory.Exists(path);
                if (!isFile && !isDir)
                {
                    continue; // solution/folder no longer present on disk
                }

                var lastOpened = isFile ? File.GetLastWriteTime(path) : Directory.GetLastWriteTime(path);
                if (entry.TryGetProperty("Value", out var valueEl) &&
                    valueEl.TryGetProperty("LastAccessed", out var lastAccessedEl) &&
                    lastAccessedEl.ValueKind == JsonValueKind.String &&
                    DateTimeOffset.TryParse(lastAccessedEl.GetString(), out var parsed))
                {
                    lastOpened = parsed.LocalDateTime;
                }

                projects.Add(new ProjectInfo
                {
                    Name = isFile ? Path.GetFileNameWithoutExtension(path) : Path.GetFileName(path),
                    Path = path,
                    AvailableEditorIds = new List<string> { editorId },
                    LastOpened = lastOpened,
                    SourceEditorId = editorId
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error parsing Visual Studio settings XML: {ex.Message}");
        }

        return projects;
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

            var args = string.Format(System.Globalization.CultureInfo.InvariantCulture, editor.CommandLineArgs, projectPath);
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
            Debug.WriteLine($"Error opening Visual Studio: {ex.Message}");
        }
    }
}
