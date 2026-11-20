// SPDX-License-Identifier: MIT

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Startup;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Notifications;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Dialogs;
using Mocassin.App.Views.Platform;
using Mocassin.App.Views.Shell;

namespace Mocassin.App.Composition;

/// <summary>
/// Explicit, enumerable service registration for the application and shell.
/// </summary>
internal static class AppServiceRegistration
{
    /// <summary>
    /// Registers application-local services and the crash boundary.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMocassinApp(this IServiceCollection services)
    {
        services.AddSingleton<IShellLifetime, AppShellLifetime>();
        services.AddSingleton<CrashHandler>();
        return services;
    }

    /// <summary>
    /// Registers the view-model services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMocassinViewModels(this IServiceCollection services)
    {
        services.AddSingleton<IConsoleService, ConsoleService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ProgressService>();
        return services;
    }

    /// <summary>
    /// Registers the view-layer services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMocassinViews(this IServiceCollection services)
    {
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<AvaloniaShellLauncher>();
        services.AddSingleton<IShellLauncher>(provider => provider.GetRequiredService<AvaloniaShellLauncher>());
        services.AddSingleton<AvaloniaStorageService>();
        services.AddSingleton<IStorageService>(provider => provider.GetRequiredService<AvaloniaStorageService>());
        services.AddSingleton<DialogService>();
        services.AddSingleton<IDialogService>(provider => provider.GetRequiredService<DialogService>());
        return services;
    }

    /// <summary>
    /// Registers the shell view model and window.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMocassinShell(this IServiceCollection services)
    {
        services.AddSingleton(provider => new ShellViewModel(
            provider.GetRequiredService<IConsoleService>(),
            new ShellRegionsViewModel(),
            new StatusBarViewModel(GetVersion()),
            provider.GetRequiredService<ProgressService>(),
            provider.GetRequiredService<IShellLifetime>()));
        services.AddSingleton<ShellWindow>();
        return services;
    }

    private static string GetVersion()
    {
        var assembly = typeof(App).Assembly;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        return informationalVersion ?? assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}
