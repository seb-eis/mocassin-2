// SPDX-License-Identifier: MIT

using Mocassin.App.Composition;
using Mocassin.App.Settings;
using Mocassin.App.Tests.TestSupport;

namespace Mocassin.App.Tests.Settings;

/// <summary>
/// Verifies the settings environment's log and cache cleanup.
/// </summary>
public sealed class SettingsEnvironmentTests
{
    [Fact]
    public async Task ClearLogsKeepsTheActiveFileAndRemovesOldFiles()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        paths.EnsureDirectories();
        var active = Path.Combine(paths.LogsDirectory, "mocassin-20261120.log");
        var old = Path.Combine(paths.LogsDirectory, "mocassin-20260101.log");
        var cache = Path.Combine(paths.CacheDirectory, "cache.bin");
        await File.WriteAllTextAsync(active, "active");
        await File.WriteAllTextAsync(old, "old");
        await File.WriteAllTextAsync(cache, "cache");
        var launcher = new RecordingShellLauncher();
        var environment = new SettingsEnvironment(paths, launcher, new ManualTimeProvider());

        var freed = await environment.ClearLogsAndCacheAsync();

        Assert.True(freed > 0);
        Assert.True(File.Exists(active));
        Assert.False(File.Exists(old));
        Assert.False(File.Exists(cache));
    }

    [Fact]
    public async Task OpenConfigFolderUsesTheShellLauncher()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        var launcher = new RecordingShellLauncher();
        var environment = new SettingsEnvironment(paths, launcher, new ManualTimeProvider());

        await environment.OpenConfigFolderAsync();

        Assert.Equal([paths.RootDirectory], launcher.Folders);
    }
}
