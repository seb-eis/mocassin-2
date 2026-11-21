// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Persisted shell layout.
/// </summary>
internal sealed record LayoutSettings
{
    /// <summary>
    /// Gets the left panel width.
    /// </summary>
    public int LeftWidth { get; init; } = SettingsDefaults.DefaultLeftWidth;

    /// <summary>
    /// Gets the right panel width.
    /// </summary>
    public int RightWidth { get; init; } = SettingsDefaults.DefaultRightWidth;

    /// <summary>
    /// Gets the console height.
    /// </summary>
    public int ConsoleHeight { get; init; } = SettingsDefaults.DefaultConsoleHeight;

    /// <summary>
    /// Gets a value indicating whether the projects region is visible.
    /// </summary>
    public bool LeftVisible { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the inspector region is visible.
    /// </summary>
    public bool RightVisible { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the console region is visible.
    /// </summary>
    public bool ConsoleVisible { get; init; } = true;

    /// <summary>
    /// Gets the active console channel id.
    /// </summary>
    public string ActiveConsoleChannel { get; init; } = SettingsDefaults.DefaultConsoleChannel;
}
