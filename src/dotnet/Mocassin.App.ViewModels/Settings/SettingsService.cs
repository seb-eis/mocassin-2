// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Applies and persists settings changes.
/// </summary>
internal sealed class SettingsService : ISettingsService
{
    private readonly IAppSettingsStore store;

    /// <summary>
    /// Initializes a settings service from a startup load result.
    /// </summary>
    /// <param name="store">The settings store.</param>
    /// <param name="loadResult">The startup load result.</param>
    public SettingsService(IAppSettingsStore store, AppSettingsLoadResult loadResult)
    {
        this.store = store;
        LoadResult = loadResult;
        Current = SettingsMigration.Migrate(loadResult.Settings);
    }

    /// <inheritdoc />
    public AppSettings Current { get; private set; }

    /// <inheritdoc />
    public AppSettingsLoadResult LoadResult { get; }

    /// <inheritdoc />
    public event EventHandler<AppSettings>? Changed;

    /// <inheritdoc />
    public async Task<AppSettingsSaveResult> UpdateAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        var (normalized, _) = AppSettingsValidator.Normalize(SettingsMigration.Migrate(settings));
        var saved = await store.SaveAsync(normalized, cancellationToken);
        if (!saved.IsSuccess)
        {
            return saved;
        }

        Current = normalized;
        Changed?.Invoke(this, Current);
        return saved;
    }

    /// <inheritdoc />
    public async Task<AppSettingsSaveResult> AddRecentFileAsync(
        string path,
        DateTimeOffset openedUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var files = Current.RecentFiles
            .Where(file => !string.Equals(file.Path, path, StringComparison.OrdinalIgnoreCase))
            .ToList();
        files.Insert(0, new RecentFile(path, openedUtc.ToUniversalTime()));
        var updated = Current with
        {
            RecentFiles = [.. files.Take(SettingsDefaults.MaxRecentFiles)],
        };
        return await UpdateAsync(updated, cancellationToken);
    }
}
