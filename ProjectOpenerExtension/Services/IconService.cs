// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ProjectOpenerExtension.Services;

/// <summary>
/// Icon service - loads icons from files
/// </summary>
public static class IconService
{
    /// <summary>
    /// Load an icon from a path
    /// Supports: .png, .ico, .jpg files (relative paths are based on the application directory)
    /// Also supports extracting icons from .exe files (format: "path.exe,index")
    /// </summary>
    public static IconData LoadIcon(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath))
        {
            return GetDefaultIcon();
        }

        try
        {
            // If it is a relative path, convert it to an absolute path
            var fullPath = Path.IsPathRooted(iconPath)
                ? iconPath
                : Path.Combine(AppContext.BaseDirectory, iconPath);

            // Check whether the file exists
            if (!File.Exists(fullPath))
            {
                System.Diagnostics.Debug.WriteLine($"Icon file not found: {fullPath}, using default");
                return GetDefaultIcon();
            }

            var extension = Path.GetExtension(fullPath).ToLowerInvariant();

            // For image files, use the path directly
            if (extension == ".png" || extension == ".ico" || extension == ".jpg" || extension == ".jpeg")
            {
                return new IconData(fullPath);
            }

            // For executables, extract the icon from the exe (using index 0)
            if (extension == ".exe" || extension == ".dll")
            {
                return new IconData($"{fullPath},0");
            }

            System.Diagnostics.Debug.WriteLine($"Unsupported icon file format: {extension}, using default");
            return GetDefaultIcon();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading icon from {iconPath}: {ex.Message}");
            return GetDefaultIcon();
        }
    }

    /// <summary>
    /// Get the default icon (used when a custom icon cannot be loaded)
    /// </summary>
    public static IconData GetDefaultIcon()
    {
        // Use the default application icon
        return new IconData("📦");
    }
}
