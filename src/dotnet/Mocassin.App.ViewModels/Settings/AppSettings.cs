// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Mocassin.App.ViewModels.Settings.Blocks;

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// The versioned <c>mocassin.settings</c> document. It is application-local and is not part of the
/// <c>contracts/manifest.json</c> format contract.
/// </summary>
internal sealed record AppSettings
{
    /// <summary>
    /// Gets the format marker. A document without this property is invalid.
    /// </summary>
    [JsonRequired]
    public string Format { get; init; } = SettingsDefaults.Format;

    /// <summary>
    /// Gets the schema version.
    /// </summary>
    public int SchemaVersion { get; init; } = SettingsDefaults.SchemaVersion;

    /// <summary>
    /// Gets the theme selection.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ThemeSelection>))]
    public ThemeSelection Theme { get; init; } = ThemeSelection.System;

    /// <summary>
    /// Gets the optional accent palette key.
    /// </summary>
    public string? Accent { get; init; }

    /// <summary>
    /// Gets the UI density.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<Density>))]
    public Density Density { get; init; } = Density.Comfortable;

    /// <summary>
    /// Gets the font scale.
    /// </summary>
    public double FontScale { get; init; } = SettingsDefaults.DefaultFontScale;

    /// <summary>
    /// Gets a value indicating whether the high-contrast theme is selected.
    /// </summary>
    public bool HighContrast { get; init; }

    /// <summary>
    /// Gets a value indicating whether non-essential motion is disabled.
    /// </summary>
    public bool ReduceMotion { get; init; }

    /// <summary>
    /// Gets the culture tag.
    /// </summary>
    public string Culture { get; init; } = "en";

    /// <summary>
    /// Gets the logging settings.
    /// </summary>
    public LoggingSettings Logging { get; init; } = new();

    /// <summary>
    /// Gets the UI behavior settings.
    /// </summary>
    public UiSettings Ui { get; init; } = new();

    /// <summary>
    /// Gets the last-used directory settings.
    /// </summary>
    public PathSettings Paths { get; init; } = new();

    /// <summary>
    /// Gets the recent files, newest first.
    /// </summary>
    public ImmutableArray<RecentFile> RecentFiles { get; init; } = [];

    /// <summary>
    /// Gets the persisted layout.
    /// </summary>
    public LayoutSettings Layout { get; init; } = new();

    /// <summary>
    /// Gets the additive autosave settings block.
    /// </summary>
    public AutosaveSettings Autosave { get; init; } = new();

    /// <summary>
    /// Gets the additive session settings block.
    /// </summary>
    public SessionSettings Session { get; init; } = new();

    /// <summary>
    /// Gets the documented default settings.
    /// </summary>
    public static AppSettings Default { get; } = new();
}
