// SPDX-License-Identifier: MIT

using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Packages.Projects;

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// A real shell view model over the production DI composition with scripted dialogs and an inline dispatcher.
/// </summary>
internal sealed class ShellTestHost : IDisposable
{
    private readonly TempWorkspace workspace = new();
    private readonly ServiceProvider provider;

    public ShellTestHost(AppSettings? initialSettings = null)
    {
        Time = new ManualTimeProvider();
        SettingsStore = new InMemorySettingsStore();
        var load = initialSettings is null
            ? AppSettingsLoadResult.Missing()
            : new AppSettingsLoadResult(initialSettings, [], IsFatal: false, LoadedFromFile: true, FatalMessage: null);
        Settings = new SettingsService(SettingsStore, load);
        Dialogs = new RecordingDialogService();
        var options = new AppStartupOptions { DataDirectory = workspace.Path };
        var paths = new AppPaths(workspace.Path);
        paths.EnsureDirectories();

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton(paths);
        services.AddSingleton<TimeProvider>(Time);
        services.AddSingleton<IAppSettingsStore>(SettingsStore);
        services.AddSingleton<ISettingsService>(Settings);
        services.AddMocassinApp();
        services.AddMocassinViewModels();
        services.AddMocassinViews();
        services.AddSingleton<IUiDispatcher>(new InlineUiDispatcher());
        services.AddSingleton<IDialogService>(Dialogs);
        services.AddSingleton<IClipboardService>(new RecordingClipboardService());
        services.AddMocassinShell();
        provider = services.BuildServiceProvider();
        Shell = provider.GetRequiredService<ShellViewModel>();
    }

    public ManualTimeProvider Time { get; }

    public InMemorySettingsStore SettingsStore { get; }

    public SettingsService Settings { get; }

    public RecordingDialogService Dialogs { get; }

    public ShellViewModel Shell { get; }

    public WorkspaceViewModel Workspace => Get<WorkspaceViewModel>();

    public ProjectStore ProjectStore => Get<ProjectStore>();

    public IProjectSessionService Sessions => Get<IProjectSessionService>();

    public string DataDirectory => Get<AppPaths>().RootDirectory;

    public T Get<T>()
        where T : notnull =>
        provider.GetRequiredService<T>();

    public void Dispose()
    {
        provider.Dispose();
        workspace.Dispose();
    }
}
