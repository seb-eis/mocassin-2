// SPDX-License-Identifier: MIT

using Mocassin.App.Composition;
using Mocassin.App.Settings;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.Tests.Settings;

/// <summary>
/// Verifies the real atomic settings store.
/// </summary>
public sealed class AppSettingsStoreTests
{
    [Fact]
    public async Task SettingsRoundTripThroughTheRealFile()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        paths.EnsureDirectories();
        var store = new AppSettingsStore(paths);
        var settings = new AppSettings
        {
            Theme = ThemeSelection.Dark,
            Density = Density.Compact,
            FontScale = 1.2,
            HighContrast = true,
            ReduceMotion = true,
            Logging = new LoggingSettings { Level = SettingsLogLevel.Warning },
            Paths = new PathSettings { LastOpenDirectory = "/tmp/open" },
            RecentFiles = [new RecentFile("/tmp/ceria.mocprj", DateTimeOffset.UnixEpoch)],
            Layout = new LayoutSettings { LeftWidth = 300, LeftVisible = false },
        };

        var saved = await store.SaveAsync(settings);
        var loaded = store.Load();

        Assert.True(saved.IsSuccess);
        Assert.False(loaded.IsFatal);
        Assert.Equal(settings.Theme, loaded.Settings.Theme);
        Assert.Equal(settings.Density, loaded.Settings.Density);
        Assert.Equal(settings.FontScale, loaded.Settings.FontScale);
        Assert.Equal(settings.HighContrast, loaded.Settings.HighContrast);
        Assert.Equal(settings.ReduceMotion, loaded.Settings.ReduceMotion);
        Assert.Equal(settings.Logging.Level, loaded.Settings.Logging.Level);
        Assert.Equal(settings.Paths.LastOpenDirectory, loaded.Settings.Paths.LastOpenDirectory);
        Assert.Equal(settings.Layout.LeftWidth, loaded.Settings.Layout.LeftWidth);
        Assert.False(loaded.Settings.Layout.LeftVisible);
        Assert.Single(loaded.Settings.RecentFiles);
        Assert.Empty(Directory.GetFiles(workspace.Path, "*.tmp", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public void AMissingFileLoadsDefaults()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        var store = new AppSettingsStore(paths);

        var loaded = store.Load();

        Assert.False(loaded.IsFatal);
        Assert.False(loaded.LoadedFromFile);
        Assert.Equal(AppSettings.Default, loaded.Settings);
    }

    [Fact]
    public void AnOversizedFileIsRejectedWithoutBeingOverwritten()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        Directory.CreateDirectory(paths.RootDirectory);
        File.WriteAllText(paths.SettingsFile, new string('x', (int)SettingsDefaults.MaxSettingsBytes + 1));
        var original = File.ReadAllBytes(paths.SettingsFile);
        var store = new AppSettingsStore(paths);

        var loaded = store.Load();

        Assert.True(loaded.IsFatal);
        Assert.Equal(original, File.ReadAllBytes(paths.SettingsFile));
    }

    [Fact]
    public async Task AFailedWriteLeavesThePreviousFileIntact()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        paths.EnsureDirectories();
        var store = new AppSettingsStore(paths);
        var original = new AppSettings { Theme = ThemeSelection.Light };
        Assert.True((await store.SaveAsync(original)).IsSuccess);
        var originalBytes = File.ReadAllBytes(paths.SettingsFile);

        var oversized = original with
        {
            RecentFiles = [new RecentFile(new string('x', (int)SettingsDefaults.MaxSettingsBytes + 1), DateTimeOffset.UnixEpoch)],
        };
        var failed = await store.SaveAsync(oversized);

        Assert.False(failed.IsSuccess);
        Assert.Equal(originalBytes, File.ReadAllBytes(paths.SettingsFile));
        Assert.Empty(Directory.GetFiles(workspace.Path, "*.tmp", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public void AFutureMajorIsRejectedWithoutBeingOverwritten()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        Directory.CreateDirectory(paths.RootDirectory);
        File.WriteAllText(paths.SettingsFile, """{"format":"mocassin.settings","schemaVersion":9}""");
        var original = File.ReadAllText(paths.SettingsFile);
        var store = new AppSettingsStore(paths);

        var loaded = store.Load();

        Assert.True(loaded.IsFatal);
        Assert.Equal(original, File.ReadAllText(paths.SettingsFile));
    }

    [Fact]
    public async Task SettingsServicePersistsUpdatesAndRecentFiles()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        paths.EnsureDirectories();
        var store = new AppSettingsStore(paths);
        var service = new SettingsService(store, store.Load());
        var changes = 0;
        service.Changed += (_, _) => changes++;

        var update = service.Current with
        {
            Theme = ThemeSelection.Light,
            Paths = new PathSettings { LastOpenDirectory = "/tmp/projects" },
            Layout = new LayoutSettings { LeftWidth = 280 },
        };
        Assert.True((await service.UpdateAsync(update)).IsSuccess);
        Assert.Equal(ThemeSelection.Light, service.Current.Theme);
        Assert.Equal("/tmp/projects", service.Current.Paths.LastOpenDirectory);
        Assert.Equal(280, service.Current.Layout.LeftWidth);
        Assert.Equal(1, changes);

        Assert.True((await service.AddRecentFileAsync("/tmp/a.mocprj", DateTimeOffset.UnixEpoch)).IsSuccess);
        Assert.True((await service.AddRecentFileAsync("/tmp/b.mocprj", DateTimeOffset.UnixEpoch.AddDays(1))).IsSuccess);
        Assert.Equal("/tmp/b.mocprj", service.Current.RecentFiles[0].Path);
        Assert.Equal(2, service.Current.RecentFiles.Length);

        var reloaded = store.Load();
        Assert.Equal(ThemeSelection.Light, reloaded.Settings.Theme);
        Assert.Equal(2, reloaded.Settings.RecentFiles.Length);
    }

    [Fact]
    public async Task AFailedServiceUpdateDoesNotChangeTheCurrentSettings()
    {
        var store = new InMemorySettingsStore { FailSave = true };
        var service = new SettingsService(store, AppSettingsLoadResult.Missing());

        var result = await service.UpdateAsync(service.Current with { Theme = ThemeSelection.Dark });

        Assert.False(result.IsSuccess);
        Assert.Equal(ThemeSelection.System, service.Current.Theme);
    }
}
