// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace ProjectOpenerExtension.Models;

/// <summary>
/// Editor type
/// </summary>
public enum EditorType
{
    VSCode,
    VisualStudio,
    Rider,
    WebStorm,
    IntelliJIdea,
    PyCharm,
    GoLand,
    PhpStorm,
    RubyMine,
    CLion,
    DataGrip,
}

/// <summary>
/// Project information
/// </summary>
public class ProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public List<string> AvailableEditorIds { get; set; } = []; // List of editor IDs
    public DateTime LastOpened { get; set; }
    public string SourceEditorId { get; set; } = string.Empty; // Source editor ID
}
