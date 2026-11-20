// SPDX-License-Identifier: MIT

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocassin.App.Composition;
using Mocassin.App.Startup;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Notifications;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Tests.Composition;

/// <summary>
/// Verifies the real dependency-injection composition.
/// </summary>
public sealed class AppHostTests
{
    [Fact]
    public void EveryRegisteredServiceResolves()
    {
        using var workspace = new TempWorkspace();
        using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });

        Assert.NotNull(host.Services.GetRequiredService<AppPaths>());
        Assert.NotNull(host.Services.GetRequiredService<TimeProvider>());
        Assert.NotNull(host.Services.GetRequiredService<IUiDispatcher>());
        Assert.NotNull(host.Services.GetRequiredService<IDialogService>());
        Assert.NotNull(host.Services.GetRequiredService<IStorageService>());
        Assert.NotNull(host.Services.GetRequiredService<IConsoleService>());
        Assert.NotNull(host.Services.GetRequiredService<INotificationService>());
        Assert.NotNull(host.Services.GetRequiredService<ProgressService>());
        Assert.NotNull(host.Services.GetRequiredService<IShellLifetime>());
        Assert.NotNull(host.Services.GetRequiredService<IShellLauncher>());
        Assert.NotNull(host.Services.GetRequiredService<CrashHandler>());
    }

    [Fact]
    public void TheConsoleServiceAndDialogServiceAreSingletons()
    {
        using var workspace = new TempWorkspace();
        using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });

        Assert.Same(
            host.Services.GetRequiredService<IConsoleService>(),
            host.Services.GetRequiredService<IConsoleService>());
        Assert.Same(
            host.Services.GetRequiredService<IDialogService>(),
            host.Services.GetRequiredService<IDialogService>());
    }

    [Fact]
    public void TheHostWritesALogFileInTheApplicationDataDirectory()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("test");
#pragma warning disable CA1848 // Test-only log entry to flush the real sink.
        logger.LogInformation("hello log");
#pragma warning restore CA1848
        host.Dispose();

        Assert.True(Directory.Exists(paths.LogsDirectory));
        Assert.NotEmpty(Directory.GetFiles(paths.LogsDirectory, "mocassin-*.log"));
    }
}
