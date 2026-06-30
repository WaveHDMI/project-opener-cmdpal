// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProjectOpenerExtension.Pages;
using ProjectOpenerExtension.Services;

namespace ProjectOpenerExtension;

public partial class ProjectOpenerExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly DynamicSettingsManager _settingsManager;

    public ProjectOpenerExtensionCommandsProvider()
    {
        DisplayName = "Projects";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");

        // Use the dynamic settings manager
        _settingsManager = DynamicSettingsManager.Instance;
        Settings = _settingsManager.Settings;

        _commands = [
            new CommandItem(new ProjectOpenerExtensionPage())
            {
                Title = "Recent Projects",
                Subtitle = "Browse and open recent projects from VS Code and JetBrains IDEs"
            }
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
