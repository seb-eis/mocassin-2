// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// The current settings and the persisted update path.
/// </summary>
internal interface ISettingsService
{
    /// <summary>
    /// Gets the current normalized settings.
    /// </summary>
    public AppSettings Current { get; }

    /// <summary>
    /// Gets the startup load result.
    /// </summary>
    public AppSettingsLoadResult LoadResult { get; }

    /// <summary>
    /// Occurs after a successful settings update.
    /// </summary>
    public event EventHandler<AppSettings>? Changed;

    /// <summary>
    /// Validates, persists, and applies settings.
    /// </summary>
    /// <param name="settings">The candidate settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The save result.</returns>
    public Task<AppSettingsSaveResult> UpdateAsync(AppSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successful project open in the recent-file list.
    /// </summary>
    /// <param name="path">The project path.</param>
    /// <param name="openedUtc">The UTC open time.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The save result.</returns>
    public Task<AppSettingsSaveResult> AddRecentFileAsync(
        string path,
        DateTimeOffset openedUtc,
        CancellationToken cancellationToken = default);
}
