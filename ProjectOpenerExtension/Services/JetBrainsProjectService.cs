// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ProjectOpenerExtension.Models;

namespace ProjectOpenerExtension.Services;

/// <summary>
/// Service that reads recent projects from JetBrains IDEs
/// </summary>
public class JetBrainsProjectService
{
    private readonly DynamicSettingsManager _settingsService;

    public JetBrainsProjectService()
    {
        _settingsService = DynamicSettingsManager.Instance;
    }

    public List<ProjectInfo> GetRecentProjects()
    {
        var projects = new List<ProjectInfo>();
        var jetbrainsEditors = _settingsService.GetEditorConfigs()
            .Where(e => e.Type == EditorType.IntelliJIdea && !string.IsNullOrEmpty(e.ConfigFolderPattern))
            .ToList();

        foreach (var editor in jetbrainsEditors)
        {
            try
            {
                var configPath = editor.ConfigFolderPattern;

                // If it is a directory, search all IDEA versions
                if (Directory.Exists(configPath))
                {
                    // Search all IntelliJ IDEA version directories
                    var ideaDirs = Directory.GetDirectories(configPath, "IntelliJIdea*");

                    foreach (var ideaDir in ideaDirs)
                    {
                        var recentProjectsFile = Path.Combine(ideaDir, "options", "recentProjects.xml");
                        if (File.Exists(recentProjectsFile))
                        {
                            Debug.WriteLine($"[JetBrains] Found config file: {recentProjectsFile}");
                            var editorProjects = ParseRecentProjects(recentProjectsFile, editor.Id);

                            foreach (var project in editorProjects)
                            {
                                var existing = projects.FirstOrDefault(p => p.Path == project.Path);
                                if (existing != null)
                                {
                                    if (!existing.AvailableEditorIds.Contains(editor.Id))
                                    {
                                        existing.AvailableEditorIds.Add(editor.Id);
                                    }
                                }
                                else
                                {
                                    projects.Add(project);
                                }
                            }
                        }
                    }
                }
                // If it is a file, parse it directly
                else if (File.Exists(configPath))
                {
                    Debug.WriteLine($"[JetBrains] Parsing file directly: {configPath}");
                    var editorProjects = ParseRecentProjects(configPath, editor.Id);
                    projects.AddRange(editorProjects);
                }
                else
                {
                    Debug.WriteLine($"[JetBrains] Path does not exist: {configPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading {editor.Name} projects: {ex.Message}");
            }
        }

        return projects;
    }

    private List<ProjectInfo> ParseRecentProjects(string xmlPath, string editorId)
    {
        var projects = new List<ProjectInfo>();

        try
        {
            var doc = XDocument.Load(xmlPath);
            var entries = doc.Descendants("entry");

            foreach (var entry in entries)
            {
                var keyAttr = entry.Attribute("key");
                if (keyAttr != null)
                {
                    var path = keyAttr.Value.Replace("$USER_HOME$", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    path = path.Replace('/', Path.DirectorySeparatorChar);

                    if (Directory.Exists(path))
                    {
                        projects.Add(new ProjectInfo
                        {
                            Name = Path.GetFileName(path),
                            Path = path,
                            AvailableEditorIds = [editorId],
                            LastOpened = Directory.GetLastWriteTime(path),
                            SourceEditorId = editorId
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error parsing recent projects XML: {ex.Message}");
        }

        return projects;
    }

    public static void OpenInJetBrainsIDE(string projectPath, string editorId)
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
            Debug.WriteLine($"Error opening JetBrains IDE: {ex.Message}");
        }
    }
}
