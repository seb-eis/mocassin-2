// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.App.Composition;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Settings;

/// <summary>
/// Settings-dialog operations against the application-data layout.
/// </summary>
internal sealed class SettingsEnvironment : ISettingsEnvironment
{
    private readonly AppPaths paths;
    private readonly IShellLauncher launcher;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Initializes the settings environment.
    /// </summary>
    /// <param name="paths">The application-data paths.</param>
    /// <param name="launcher">The shell launcher.</param>
    /// <param name="timeProvider">The time provider used to identify the active log file.</param>
    public SettingsEnvironment(AppPaths paths, IShellLauncher launcher, TimeProvider timeProvider)
    {
        this.paths = paths;
        this.launcher = launcher;
        this.timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public string SettingsPath => paths.SettingsFile;

    /// <inheritdoc />
    public string DataDirectory => paths.RootDirectory;

    /// <inheritdoc />
    public Task OpenConfigFolderAsync(CancellationToken cancellationToken = default) =>
        launcher.OpenFolderAsync(paths.RootDirectory, cancellationToken);

    /// <inheritdoc />
    public Task<long> ClearLogsAndCacheAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var activeLogName = string.Create(
            CultureInfo.InvariantCulture,
            $"mocassin-{timeProvider.GetLocalNow():yyyyMMdd}.log");
        var freed = DeleteFiles(paths.LogsDirectory, activeLogName, cancellationToken)
            + DeleteFiles(paths.CacheDirectory, activeFileName: null, cancellationToken);
        return Task.FromResult(freed);
    }

    private static long DeleteFiles(string directory, string? activeFileName, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(directory))
        {
            return 0;
        }

        long freed = 0;
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (activeFileName is not null
                && string.Equals(Path.GetFileName(file), activeFileName, StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                var length = new FileInfo(file).Length;
                File.Delete(file);
                freed += length;
            }
            catch (IOException)
            {
                // The active log or a locked cache file is left in place.
            }
            catch (UnauthorizedAccessException)
            {
                // Leave files that the process cannot remove.
            }
        }

        return freed;
    }
}
