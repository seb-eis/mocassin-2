// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// App-data operations used by the settings dialog.
/// </summary>
internal interface ISettingsEnvironment
{
    /// <summary>
    /// Gets the settings file path.
    /// </summary>
    public string SettingsPath { get; }

    /// <summary>
    /// Gets the data-directory path.
    /// </summary>
    public string DataDirectory { get; }

    /// <summary>
    /// Opens the configuration folder.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the platform request is made.</returns>
    public Task OpenConfigFolderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes closed log and cache files.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of bytes freed.</returns>
    public Task<long> ClearLogsAndCacheAsync(CancellationToken cancellationToken = default);
}
