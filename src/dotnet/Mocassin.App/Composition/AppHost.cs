// SPDX-License-Identifier: MIT

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mocassin.App.ViewModels.Consoles;

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

        var builder = Host.CreateEmptyApplicationBuilder(
            new HostApplicationBuilderSettings { ApplicationName = "Mocassin" });

        LoggingSetup.Configure(builder.Logging, paths, options.LogLevel);

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(paths);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddMocassinApp();
        builder.Services.AddMocassinViewModels();
        builder.Services.AddMocassinViews();
        builder.Services.AddMocassinShell();
        builder.Services.AddSingleton<ILoggerProvider>(
            provider => new ConsoleLoggerProvider(provider.GetRequiredService<IConsoleService>()));

        var host = builder.Build();
        return new AppHost(host, paths);
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
}
