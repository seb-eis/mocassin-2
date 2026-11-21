// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// In-memory settings store for service and dialog tests.
/// </summary>
internal sealed class InMemorySettingsStore : IAppSettingsStore
{
    /// <summary>
    /// Gets or sets the load result.
    /// </summary>
    public AppSettingsLoadResult LoadResult { get; set; } = AppSettingsLoadResult.Missing();

    /// <summary>
    /// Gets the last saved settings.
    /// </summary>
    public AppSettings? Saved { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether saves fail.
    /// </summary>
    public bool FailSave { get; set; }

    /// <inheritdoc />
    public AppSettingsLoadResult Load() => LoadResult;

    /// <inheritdoc />
    public Task<AppSettingsSaveResult> SaveAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (FailSave)
        {
            return Task.FromResult(new AppSettingsSaveResult(false, "simulated write failure"));
        }

        Saved = settings;
        return Task.FromResult(AppSettingsSaveResult.Success);
    }
}
