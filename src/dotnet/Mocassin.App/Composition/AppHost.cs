// SPDX-License-Identifier: MIT

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mocassin.App.Settings;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.Composition;

/// <summary>
/// Builds and owns the application host. Only the application entry point resolves services.
/// </summary>
internal sealed class AppHost : IDisposable
{
    private readonly IHost host;
    private bool disposed;

    private AppHost(IHost host, AppPaths paths)
    {
        this.host = host;
        Paths = paths;
    }

    /// <summary>
    /// Gets the resolved service provider.
    /// </summary>
    public IServiceProvider Services => host.Services;

    /// <summary>
    /// Gets the resolved application-data paths.
    /// </summary>
    public AppPaths Paths { get; }

    /// <summary>
    /// Builds the host for the supplied startup options.
    /// </summary>
    /// <param name="options">The startup options.</param>
    /// <returns>The built host.</returns>
    public static AppHost Build(AppStartupOptions options)
    {
        var paths = AppPaths.ResolveForCurrentProcess(options);
        paths.EnsureDirectories();

        var settingsStore = new AppSettingsStore(paths);
        var settingsLoad = settingsStore.Load();
        var logLevel = options.IsLogLevelSpecified
            ? options.LogLevel
            : ToLogLevel(settingsLoad.Settings.Logging.Level);

        var builder = Host.CreateEmptyApplicationBuilder(
            new HostApplicationBuilderSettings { ApplicationName = "Mocassin" });

        LoggingSetup.Configure(builder.Logging, paths, logLevel);

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(paths);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(settingsLoad);
        builder.Services.AddSingleton<IAppSettingsStore>(settingsStore);
        builder.Services.AddSingleton<ISettingsService>(
            provider => new SettingsService(
                settingsStore,
                settingsLoad,
                OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()));
        builder.Services.AddMocassinApp();
        builder.Services.AddMocassinViewModels();
        builder.Services.AddMocassinViews();
        builder.Services.AddMocassinShell();
        builder.Services.AddSingleton<ILoggerProvider>(
            provider => new ConsoleLoggerProvider(provider.GetRequiredService<IConsoleService>()));

        var host = builder.Build();
        if (settingsLoad.IsFatal)
        {
            var settingsLogger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger<AppHost>();
#pragma warning disable CA1848 // Startup has one warning path; a LoggerMessage delegate adds no value here.
            settingsLogger.LogWarning("The settings file was rejected: {Reason}", settingsLoad.FatalMessage);
#pragma warning restore CA1848
        }

        return new AppHost(host, paths);
    }

    private static LogLevel ToLogLevel(SettingsLogLevel level) =>
        level switch
        {
            SettingsLogLevel.Trace => LogLevel.Trace,
            SettingsLogLevel.Debug => LogLevel.Debug,
            SettingsLogLevel.Warning => LogLevel.Warning,
            SettingsLogLevel.Error => LogLevel.Error,
            SettingsLogLevel.Fatal => LogLevel.Critical,
            _ => LogLevel.Information,
        };

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
}
