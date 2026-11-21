// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Defaults and bounds of the application settings.
/// </summary>
internal static class SettingsDefaults
{
    /// <summary>
    /// The settings format marker.
    /// </summary>
    public const string Format = "mocassin.settings";

    /// <summary>
    /// The current major schema version.
    /// </summary>
    public const int SchemaVersion = 1;

    /// <summary>
    /// The maximum settings document size.
    /// </summary>
    public const long MaxSettingsBytes = 4L * 1024L * 1024L;

    /// <summary>
    /// The maximum number of recent files.
    /// </summary>
    public const int MaxRecentFiles = 10;

    /// <summary>
    /// The default font scale.
    /// </summary>
    public const double DefaultFontScale = 1.0;

    /// <summary>
    /// The minimum font scale.
    /// </summary>
    public const double MinFontScale = 0.85;

    /// <summary>
    /// The maximum font scale.
    /// </summary>
    public const double MaxFontScale = 1.5;

    /// <summary>
    /// The default left panel width.
    /// </summary>
    public const int DefaultLeftWidth = 260;

    /// <summary>
    /// The default right panel width.
    /// </summary>
    public const int DefaultRightWidth = 320;

    /// <summary>
    /// The default console height.
    /// </summary>
    public const int DefaultConsoleHeight = 180;

    /// <summary>
    /// The minimum left panel width.
    /// </summary>
    public const int MinLeftWidth = 160;

    /// <summary>
    /// The maximum left panel width.
    /// </summary>
    public const int MaxLeftWidth = 600;

    /// <summary>
    /// The minimum right panel width.
    /// </summary>
    public const int MinRightWidth = 160;

    /// <summary>
    /// The maximum right panel width.
    /// </summary>
    public const int MaxRightWidth = 800;

    /// <summary>
    /// The minimum console height.
    /// </summary>
    public const int MinConsoleHeight = 80;

    /// <summary>
    /// The maximum console height.
    /// </summary>
    public const int MaxConsoleHeight = 600;

    /// <summary>
    /// The default active console channel.
    /// </summary>
    public const string DefaultConsoleChannel = "application";
}
