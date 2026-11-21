// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Result of loading the application settings.
/// </summary>
/// <param name="Settings">The normalized settings.</param>
/// <param name="Warnings">Non-fatal load warnings.</param>
/// <param name="IsFatal">Whether the stored document was rejected and defaults were used.</param>
/// <param name="LoadedFromFile">Whether a settings file was read.</param>
/// <param name="FatalMessage">The fatal rejection reason, if any.</param>
internal sealed record AppSettingsLoadResult(
    AppSettings Settings,
    IReadOnlyList<string> Warnings,
    bool IsFatal,
    bool LoadedFromFile,
    string? FatalMessage)
{
    /// <summary>
    /// Creates a defaults result for a missing file.
    /// </summary>
    /// <returns>The missing-file result.</returns>
    public static AppSettingsLoadResult Missing() =>
        new(AppSettings.Default, [], IsFatal: false, LoadedFromFile: false, FatalMessage: null);

    /// <summary>
    /// Creates a fatal defaults result.
    /// </summary>
    /// <param name="message">The rejection reason.</param>
    /// <returns>The fatal result.</returns>
    public static AppSettingsLoadResult Fatal(string message) =>
        new(AppSettings.Default, [], IsFatal: true, LoadedFromFile: true, FatalMessage: message);
}
