// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Applies and persists settings changes.
/// </summary>
internal sealed class SettingsService : ISettingsService
{
    private readonly IAppSettingsStore store;
    private readonly bool ignoreCasePaths;

    /// <summary>
    /// Initializes a settings service from a startup load result.
    /// </summary>
    /// <param name="store">The settings store.</param>
    /// <param name="loadResult">The startup load result.</param>
    /// <param name="ignoreCasePaths">Whether recents are de-duplicated case-insensitively.</param>
    public SettingsService(
        IAppSettingsStore store,
        AppSettingsLoadResult loadResult,
        bool ignoreCasePaths = false)
    {
        this.store = store;
        this.ignoreCasePaths = ignoreCasePaths;
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
        var (normalized, _) = AppSettingsValidator.Normalize(
            SettingsMigration.Migrate(settings),
            ignoreCasePaths);
        if (LoadResult.IsFatal)
        {
            Current = normalized;
            Changed?.Invoke(this, Current);
            return new AppSettingsSaveResult(
                false,
                "The existing settings file was rejected on load and is preserved; changes were not written.");
        }

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
        var pathComparison = ignoreCasePaths ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var files = Current.RecentFiles
            .Where(file => !string.Equals(file.Path, path, pathComparison))
            .ToList();
        files.Insert(0, new RecentFile(path, openedUtc.ToUniversalTime()));
        var updated = Current with
        {
            RecentFiles = [.. files.Take(SettingsDefaults.MaxRecentFiles)],
        };
        return await UpdateAsync(updated, cancellationToken);
    }
}
