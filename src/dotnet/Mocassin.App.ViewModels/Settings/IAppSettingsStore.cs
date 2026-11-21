// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Loads and saves the application-local settings file. The implementation lives in the application host.
/// </summary>
internal interface IAppSettingsStore
{
    /// <summary>
    /// Reads the settings synchronously for startup.
    /// </summary>
    /// <returns>The load result.</returns>
    public AppSettingsLoadResult Load();

    /// <summary>
    /// Writes the settings atomically.
    /// </summary>
    /// <param name="settings">The normalized settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The save result.</returns>
    public Task<AppSettingsSaveResult> SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
