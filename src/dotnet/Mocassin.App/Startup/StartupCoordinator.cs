// SPDX-License-Identifier: MIT

using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocassin.App.Composition;
using Mocassin.App.Scheduling;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.Views.Shell;

namespace Mocassin.App.Startup;

/// <summary>
/// Starts the application host, installs the crash boundary, and creates the shell window.
/// </summary>
internal sealed partial class StartupCoordinator : IDisposable
{
    private readonly AppHost host;
    private bool disposed;

    private StartupCoordinator(AppHost host, ShellWindow mainWindow)
    {
        this.host = host;
        MainWindow = mainWindow;
    }

    /// <summary>
    /// Gets the shell window.
    /// </summary>
    public ShellWindow MainWindow { get; }

    /// <summary>
    /// Starts the shell for the supplied options.
    /// </summary>
    /// <param name="options">The startup options.</param>
    /// <param name="application">The Avalonia application.</param>
    /// <returns>The running startup coordinator.</returns>
    public static StartupCoordinator Start(AppStartupOptions options, Application application)
    {
        var host = AppHost.Build(options);
        try
        {
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            application.DataTemplates.Add(new ViewLocator(loggerFactory.CreateLogger<ViewLocator>()));

            host.Services.GetRequiredService<CrashHandler>().Install();
            var settingsService = host.Services.GetRequiredService<ISettingsService>();
            var themeApplier = host.Services.GetRequiredService<IThemeApplier>();
            themeApplier.Apply(settingsService.Current);
            settingsService.Changed += (_, updated) => themeApplier.Apply(updated);
            var window = host.Services.GetRequiredService<ShellWindow>();
            host.Services.GetRequiredService<AutosaveTimer>().Start();
            var startupLogger = loggerFactory.CreateLogger<StartupCoordinator>();
            LogShellStarted(startupLogger, host.Paths.RootDirectory);

            return new StartupCoordinator(host, window);
        }
        catch
        {
            host.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        host.Dispose();
    }

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "MOCASSIN shell started with data directory {DataDirectory}")]
    private static partial void LogShellStarted(ILogger logger, string dataDirectory);
}
