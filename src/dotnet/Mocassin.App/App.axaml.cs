// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Mocassin.App.Composition;
using Mocassin.App.Startup;

namespace Mocassin.App;

/// <summary>
/// Avalonia application entry point. It owns the host and the shell window lifetime.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1724:Type names should not match namespaces",
    Justification = "The Avalonia application type is the documented public App entry type.")]
public sealed class App : Application
{
    private readonly AppStartupOptions options;
    private readonly bool startHost;
    private StartupCoordinator? startup;

    /// <summary>
    /// Initializes a new application for the designer and the default startup path.
    /// </summary>
    public App()
        : this(AppStartupOptions.Default, startHost: true)
    {
    }

    internal App(AppStartupOptions options, bool startHost)
    {
        this.options = options;
        this.startHost = startHost;
    }

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

        if (!startHost)
        {
            DataTemplates.Add(new ViewLocator());
            return;
        }

        try
        {
            startup = StartupCoordinator.Start(options, this);
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = startup.MainWindow;
                desktop.Exit += OnDesktopExit;
                return;
            }

            startup.Dispose();
            startup = null;
        }
#pragma warning disable CA1031 // A startup failure must become process exit code 1, not an unhandled crash.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            Console.Error.WriteLine($"MOCASSIN failed to start: {exception.Message}");
            startup?.Dispose();
            startup = null;
            ShutdownAfterStartupFailure();
        }
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        startup?.Dispose();
        startup = null;
    }

    private void ShutdownAfterStartupFailure()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown(1);
            return;
        }

        Environment.Exit(1);
    }
}
