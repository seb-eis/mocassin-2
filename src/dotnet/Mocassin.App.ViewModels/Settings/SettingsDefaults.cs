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
    /// The design width used by the pre-fix pixel layout and its one-time migration.
    /// </summary>
    public const double LegacyDesignWidth = 1200d;

    /// <summary>
    /// The design height used by the pre-fix pixel layout and its one-time migration.
    /// </summary>
    public const double LegacyDesignHeight = 800d;

    /// <summary>
    /// The default left panel fraction.
    /// </summary>
    public const double DefaultLeftFraction = 260d / LegacyDesignWidth;

    /// <summary>
    /// The default right panel fraction.
    /// </summary>
    public const double DefaultRightFraction = 320d / LegacyDesignWidth;

    /// <summary>
    /// The default console fraction.
    /// </summary>
    public const double DefaultConsoleFraction = 180d / LegacyDesignHeight;

    /// <summary>
    /// The minimum persisted side-panel fraction.
    /// </summary>
    public const double MinSideFraction = 0.08;

    /// <summary>
    /// The maximum persisted side-panel fraction.
    /// </summary>
    public const double MaxSideFraction = 0.45;

    /// <summary>
    /// The minimum persisted console fraction.
    /// </summary>
    public const double MinConsoleFraction = 0.08;

    /// <summary>
    /// The maximum persisted console fraction.
    /// </summary>
    public const double MaxConsoleFraction = 0.60;

    /// <summary>
    /// The default active console channel.
    /// </summary>
    public const string DefaultConsoleChannel = "application";

    /// <summary>
    /// The default autosave interval in minutes.
    /// </summary>
    public const int DefaultAutosaveIntervalMinutes = 5;

    /// <summary>
    /// The minimum autosave interval in minutes.
    /// </summary>
    public const int MinAutosaveIntervalMinutes = 1;

    /// <summary>
    /// The maximum autosave interval in minutes.
    /// </summary>
    public const int MaxAutosaveIntervalMinutes = 60;

    /// <summary>
    /// The maximum number of remembered open-project paths.
    /// </summary>
    public const int MaxOpenProjects = 20;
}
