// SPDX-License-Identifier: MIT

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Files;
using Mocassin.App.Settings;
using Mocassin.App.Startup;
using Mocassin.App.ViewModels.Commands;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Diagnostics;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Notifications;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Settings.Dialog;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Validation;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Dialogs;
using Mocassin.App.Views.Platform;
using Mocassin.App.Views.Shell;
using Mocassin.App.Views.Theming;

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
        services.AddSingleton<ISettingsEnvironment, SettingsEnvironment>();
        services.AddSingleton<IProjectFileProbe, ProjectFileProbe>();
        services.AddSingleton(provider => new ProjectStore(
            provider.GetRequiredService<TimeProvider>(),
            new ProjectStoreOptions { ValidateOnLoad = false }));
        services.AddSingleton<ProjectValidationPipeline>();
        services.AddSingleton<IProjectSessionService, ProjectSessionService>();
        var platform = OperatingSystem.IsMacOS()
            ? GesturePlatform.MacOS
            : OperatingSystem.IsWindows() ? GesturePlatform.Windows : GesturePlatform.Linux;
        services.AddSingleton(typeof(GesturePlatform), platform);
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
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<IConsoleChannelProvider, ApplicationConsoleChannelProvider>();
        services.AddSingleton<ProblemsChannelViewModel>();
        services.AddSingleton<IConsoleChannelProvider, ProblemsChannelProvider>();
        services.AddSingleton<IStatusBarItemProvider, ValidationStatusItemProvider>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ProgressService>();
        services.AddSingleton<GestureFormatter>();
        services.AddSingleton<ICommandCatalog, CommandCatalog>();
        services.AddSingleton<IMenuItemProvider, ShellMenuProvider>();
        services.AddSingleton<WorkspaceViewModel>();
        services.AddSingleton<IProjectWorkspace>(provider => provider.GetRequiredService<WorkspaceViewModel>());
        services.AddSingleton<DocumentCollectionViewModel>();
        services.AddSingleton<INavigationTargetProvider, ProjectNavigationTargetProvider>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IExplorerNodeProvider, ProjectExplorerNodeProvider>();
        services.AddSingleton<ProjectExplorerViewModel>();
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
        services.AddSingleton<AvaloniaClipboardService>();
        services.AddSingleton<IClipboardService>(provider => provider.GetRequiredService<AvaloniaClipboardService>());
        services.AddSingleton<IThemeApplier, AvaloniaThemeApplier>();
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
            provider.GetServices<IConsoleChannelProvider>(),
            new ShellRegionsViewModel(),
            new StatusBarViewModel(GetVersion(), provider.GetServices<IStatusBarItemProvider>()),
            provider.GetRequiredService<ProgressService>(),
            provider.GetRequiredService<IShellLifetime>(),
            provider.GetRequiredService<DocumentCollectionViewModel>(),
            provider.GetRequiredService<WorkspaceViewModel>(),
            provider.GetRequiredService<INavigationService>(),
            provider.GetRequiredService<ProjectExplorerViewModel>(),
            provider.GetRequiredService<ICommandCatalog>(),
            provider.GetRequiredService<IMenuItemProvider>(),
            provider.GetRequiredService<GesturePlatform>(),
            provider.GetRequiredService<ISettingsService>(),
            provider.GetRequiredService<SettingsViewModel>(),
            provider.GetRequiredService<IProjectSessionService>(),
            provider.GetRequiredService<IStorageService>(),
            provider.GetRequiredService<IDialogService>(),
            provider.GetRequiredService<AppStartupOptions>().ProjectPath,
            provider.GetRequiredService<IProjectFileProbe>()));
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
