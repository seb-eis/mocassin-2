// SPDX-License-Identifier: MIT

using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;

namespace Mocassin.App.Tests.Composition;

/// <summary>
/// Verifies the application-data layout.
/// </summary>
public sealed class AppPathsTests
{
    [Fact]
    public void TheCommandLineOverrideWinsOverTheEnvironment()
    {
        using var workspace = new TempWorkspace();
        var overridePath = workspace.Combine("override");

        var paths = AppPaths.Resolve(overridePath, workspace.Combine("environment"), workspace.Combine("local"));

        Assert.Equal(overridePath, paths.RootDirectory);
        Assert.Equal(System.IO.Path.Combine(overridePath, "logs"), paths.LogsDirectory);
        Assert.Equal(System.IO.Path.Combine(overridePath, "cache"), paths.CacheDirectory);
        Assert.Equal(System.IO.Path.Combine(overridePath, "recovery"), paths.RecoveryDirectory);
        Assert.Equal(System.IO.Path.Combine(overridePath, "Mocassin.Settings.json"), paths.SettingsFile);
    }

    [Fact]
    public void TheEnvironmentOverrideIsUsedWhenTheCommandLineIsEmpty()
    {
        using var workspace = new TempWorkspace();
        var environmentPath = workspace.Combine("environment");

        var paths = AppPaths.Resolve(null, environmentPath, workspace.Combine("local"));

        Assert.Equal(environmentPath, paths.RootDirectory);
    }

    [Fact]
    public void ThePlatformLocalDataRootIsTheDefault()
    {
        using var workspace = new TempWorkspace();
        var local = workspace.Combine("local");

        var paths = AppPaths.Resolve(null, null, local);

        Assert.Equal(System.IO.Path.Combine(local, "Mocassin"), paths.RootDirectory);
    }

    [Fact]
    public void EnsureDirectoriesCreatesTheLayout()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Combine("data"));

        paths.EnsureDirectories();

        Assert.True(Directory.Exists(paths.RootDirectory));
        Assert.True(Directory.Exists(paths.LogsDirectory));
        Assert.True(Directory.Exists(paths.CacheDirectory));
        Assert.True(Directory.Exists(paths.RecoveryDirectory));
    }
}
