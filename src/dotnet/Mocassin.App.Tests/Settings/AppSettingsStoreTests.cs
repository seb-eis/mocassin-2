// SPDX-License-Identifier: MIT

using Mocassin.App.Composition;
using Mocassin.App.Settings;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.Tests.Settings;

/// <summary>
/// Verifies the real atomic settings store.
/// </summary>
[Trait("Tier", "PR-fast")]
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
            Layout = new LayoutSettings { LeftFraction = 0.25, LeftVisible = false },
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
        Assert.Equal(settings.Layout.LeftFraction, loaded.Settings.Layout.LeftFraction);
        Assert.False(loaded.Settings.Layout.LeftVisible);
        var json = await File.ReadAllTextAsync(paths.SettingsFile);
        Assert.DoesNotContain("\"leftWidth\"", json, StringComparison.Ordinal);
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
    public async Task ALegacyPixelDocumentIsMigratedAndRewrittenWithoutPixelKeys()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        Directory.CreateDirectory(paths.RootDirectory);
        await File.WriteAllTextAsync(
            paths.SettingsFile,
            """{"format":"mocassin.settings","layout":{"leftWidth":300,"rightWidth":400,"consoleHeight":240}}""");
        var store = new AppSettingsStore(paths);

        var loaded = store.Load();

        Assert.False(loaded.IsFatal);
        Assert.Equal(300d / 1200d, loaded.Settings.Layout.LeftFraction, 10);
        Assert.Equal(400d / 1200d, loaded.Settings.Layout.RightFraction, 10);
        Assert.Equal(240d / 800d, loaded.Settings.Layout.ConsoleFraction, 10);

        var service = new SettingsService(store, loaded);
        Assert.True((await service.UpdateAsync(service.Current)).IsSuccess);
        var json = await File.ReadAllTextAsync(paths.SettingsFile);
        Assert.Contains("\"leftFraction\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"leftWidth\"", json, StringComparison.Ordinal);
        Assert.Equal(loaded.Settings.Layout, store.Load().Settings.Layout);
    }

    [Fact]
    public async Task TheDefaultsRevisionIsWrittenAndSurvivesARewrite()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        paths.EnsureDirectories();
        var store = new AppSettingsStore(paths);
        var layout = new LayoutSettings
        {
            LeftFraction = 0.24,
            RightFraction = 0.18,
            ConsoleFraction = 0.36,
            DefaultsRevision = SettingsDefaults.DefaultsRevision,
        };

        Assert.True((await store.SaveAsync(new AppSettings { Layout = layout })).IsSuccess);
        var first = store.Load();
        var json = await File.ReadAllTextAsync(paths.SettingsFile);
        Assert.Contains("\"defaultsRevision\": 1", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"leftWidth\"", json, StringComparison.Ordinal);
        Assert.Equal(layout, first.Settings.Layout);

        var service = new SettingsService(store, first);
        Assert.True((await service.UpdateAsync(service.Current)).IsSuccess);
        Assert.Equal(first.Settings.Layout, store.Load().Settings.Layout);
    }

    [Fact]
    public async Task AnOldDefaultLayoutIsUpgradedWhenTheRealStoreWritesItBack()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        Directory.CreateDirectory(paths.RootDirectory);
        await File.WriteAllTextAsync(
            paths.SettingsFile,
            """{"format":"mocassin.settings","layout":{"leftFraction":0.21666666666666667,"rightFraction":0.26666666666666666,"consoleFraction":0.225}}""");
        var store = new AppSettingsStore(paths);

        var loaded = store.Load();
        Assert.Equal(SettingsDefaults.DefaultLeftFraction, loaded.Settings.Layout.LeftFraction, 10);
        Assert.Equal(SettingsDefaults.DefaultRightFraction, loaded.Settings.Layout.RightFraction, 10);
        Assert.Equal(SettingsDefaults.DefaultConsoleFraction, loaded.Settings.Layout.ConsoleFraction, 10);
        Assert.Equal(SettingsDefaults.DefaultsRevision, loaded.Settings.Layout.DefaultsRevision);

        var service = new SettingsService(store, loaded);
        Assert.True((await service.UpdateAsync(service.Current)).IsSuccess);
        Assert.Equal(loaded.Settings.Layout, store.Load().Settings.Layout);
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
            Layout = new LayoutSettings { LeftFraction = 0.28 },
        };
        Assert.True((await service.UpdateAsync(update)).IsSuccess);
        Assert.Equal(ThemeSelection.Light, service.Current.Theme);
        Assert.Equal("/tmp/projects", service.Current.Paths.LastOpenDirectory);
        Assert.Equal(0.28, service.Current.Layout.LeftFraction, 10);
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
    public async Task ARealIoWriteFailureLeavesThePreviousFileIntact()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        paths.EnsureDirectories();
        var store = new AppSettingsStore(paths);
        Assert.True((await store.SaveAsync(new AppSettings { Theme = ThemeSelection.Light })).IsSuccess);
        var original = await File.ReadAllBytesAsync(paths.SettingsFile);
        File.SetUnixFileMode(
            paths.RootDirectory,
            UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            var result = await store.SaveAsync(new AppSettings { Theme = ThemeSelection.Dark });
            Assert.False(result.IsSuccess);
        }
        finally
        {
            File.SetUnixFileMode(
                paths.RootDirectory,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        Assert.Equal(original, await File.ReadAllBytesAsync(paths.SettingsFile));
    }

    [Fact]
    public async Task AFutureMajorFileIsNotOverwrittenByServiceUpdates()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        Directory.CreateDirectory(paths.RootDirectory);
        var original = """{"format":"mocassin.settings","schemaVersion":9,"theme":"dark"}""";
        await File.WriteAllTextAsync(paths.SettingsFile, original);
        var store = new AppSettingsStore(paths);
        var load = store.Load();
        var service = new SettingsService(store, load);

        var result = await service.UpdateAsync(service.Current with { Theme = ThemeSelection.Light });

        Assert.False(result.IsSuccess);
        Assert.Contains("preserved", result.Message, StringComparison.Ordinal);
        Assert.Equal(original, await File.ReadAllTextAsync(paths.SettingsFile));
    }

    [Fact]
    public async Task RecentFilesUseThePlatformCaseRule()
    {
        var linuxStore = new InMemorySettingsStore();
        var linux = new SettingsService(linuxStore, AppSettingsLoadResult.Missing());
        Assert.True((await linux.AddRecentFileAsync("/tmp/A.mocprj", DateTimeOffset.UnixEpoch)).IsSuccess);
        Assert.True((await linux.AddRecentFileAsync("/tmp/a.mocprj", DateTimeOffset.UnixEpoch)).IsSuccess);
        Assert.Equal(2, linux.Current.RecentFiles.Length);

        var windowsStore = new InMemorySettingsStore();
        var windows = new SettingsService(windowsStore, AppSettingsLoadResult.Missing(), ignoreCasePaths: true);
        Assert.True((await windows.AddRecentFileAsync("/tmp/A.mocprj", DateTimeOffset.UnixEpoch)).IsSuccess);
        Assert.True((await windows.AddRecentFileAsync("/tmp/a.mocprj", DateTimeOffset.UnixEpoch)).IsSuccess);
        Assert.Single(windows.Current.RecentFiles);
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
