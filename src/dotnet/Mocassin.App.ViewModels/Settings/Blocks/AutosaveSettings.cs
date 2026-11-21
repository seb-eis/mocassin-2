// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings.Blocks;

/// <summary>
/// The additive <c>autosave</c> v1 settings block.
/// </summary>
internal sealed record AutosaveSettings
{
    /// <summary>
    /// Gets a value indicating whether periodic autosave is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the autosave interval in minutes, from 1 to 60.
    /// </summary>
    public int IntervalMinutes { get; init; } = SettingsDefaults.DefaultAutosaveIntervalMinutes;
}
