// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Persisted shell layout. The resizable regions store fractions rather than pixels so that a window resize
/// keeps the ratio and the splitter's temporary pixel-valued stars are not durable.
/// </summary>
internal sealed record LayoutSettings
{
    /// <summary>
    /// Gets the left panel share of the workspace width.
    /// </summary>
    public double LeftFraction { get; init; } = SettingsDefaults.DefaultLeftFraction;

    /// <summary>
    /// Gets the right panel share of the workspace width.
    /// </summary>
    public double RightFraction { get; init; } = SettingsDefaults.DefaultRightFraction;

    /// <summary>
    /// Gets the console share of the documents plus console height.
    /// </summary>
    public double ConsoleFraction { get; init; } = SettingsDefaults.DefaultConsoleFraction;

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
