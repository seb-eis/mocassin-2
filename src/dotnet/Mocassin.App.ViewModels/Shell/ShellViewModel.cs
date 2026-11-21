// SPDX-License-Identifier: MIT

using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Commands;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.Packages.Projects.Templates;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Settings.Blocks;
using Mocassin.App.ViewModels.Settings.Dialog;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Root view model of the application shell.
/// </summary>
internal sealed partial class ShellViewModel : ObservableObject
{
    private readonly IShellLifetime lifetime;
    private readonly ISettingsService settings;
    private readonly SessionSettings startupSessionSnapshot;
    private readonly IConsoleService consoleService;
    private ProjectEditCoordinator? subscribedEditor;
    private bool startupRestoreStarted;
    private bool applyingLayout;
    private readonly AsyncRelayCommand backCommand;
    private readonly AsyncRelayCommand forwardCommand;
    private readonly AsyncRelayCommand closeActiveCommand;
    private readonly AsyncRelayCommand saveProjectCommand;
    private readonly AsyncRelayCommand saveAsProjectCommand;
    private readonly AsyncRelayCommand closeProjectCommand;
    private readonly AsyncRelayCommand reloadProjectCommand;
    private readonly ShellEditCommands editCommands;
    private readonly ShellLifecycleCoordinator lifecycle;
    private readonly ShellProjectOperations projectOperations;
    private readonly IProjectSessionService sessions;
    private readonly IStorageService storage;
    private readonly IDialogService dialogs;
    private readonly string? startupProjectPath;
    private Task pendingLayoutPersist = Task.CompletedTask;
    private bool suppressSettingsApply;

    [ObservableProperty]
    private string windowTitle = "MOCASSIN";

    [ObservableProperty]
    private object? inspectorContent;

    [ObservableProperty]
    private bool isPaletteOpen;

    [ObservableProperty]
    private bool isCheatSheetOpen;

    [ObservableProperty]
    private bool hasInspectorContent;

    [ObservableProperty]
    private string? readOnlyBannerText;

    /// <summary>
    /// Initializes a new shell view model.
    /// </summary>
    /// <param name="consoleService">The console service that owns the channels.</param>
    /// <param name="consoleChannels">The registered console channel providers.</param>
    /// <param name="regions">The region visibility state.</param>
    /// <param name="statusBar">The status-bar state.</param>
    /// <param name="progress">The status-bar progress state.</param>
    /// <param name="lifetime">The application lifetime used by the exit command.</param>
    /// <param name="documents">The document collection.</param>
    /// <param name="workspace">The project workspace.</param>
    /// <param name="navigation">The navigation service.</param>
    /// <param name="explorer">The project explorer.</param>
    /// <param name="commands">The command catalog.</param>
    /// <param name="menuProvider">The menu contribution provider.</param>
    /// <param name="platform">The platform used to format gestures.</param>
    /// <param name="settings">The application settings.</param>
    /// <param name="settingsDialog">The settings dialog state.</param>
    /// <param name="sessions">The project session service.</param>
    /// <param name="storage">The picker service.</param>
    /// <param name="dialogs">The dialog service.</param>
    /// <param name="startupProjectPath">The optional project path from the startup options.</param>
    /// <param name="probe">The project file probe used by the recent list.</param>
    public ShellViewModel(
        IConsoleService consoleService,
        IEnumerable<IConsoleChannelProvider> consoleChannels,
        ShellRegionsViewModel regions,
        StatusBarViewModel statusBar,
        ProgressService progress,
        IShellLifetime lifetime,
        DocumentCollectionViewModel documents,
        WorkspaceViewModel workspace,
        INavigationService navigation,
        ProjectExplorerViewModel explorer,
        ICommandCatalog commands,
        IMenuItemProvider menuProvider,
        GesturePlatform platform,
        ISettingsService settings,
        SettingsViewModel settingsDialog,
        IProjectSessionService sessions,
        IStorageService storage,
        IDialogService dialogs,
        string? startupProjectPath,
        IProjectFileProbe probe)
    {
        Regions = regions;
        StatusBar = statusBar;
        Progress = progress;
        this.lifetime = lifetime;
        Documents = documents;
        Workspace = workspace;
        Navigation = navigation;
        Explorer = explorer;
        Commands = commands;
        Platform = platform;
        this.settings = settings;
        startupSessionSnapshot = settings.Current.Session;
        this.consoleService = consoleService;
        this.sessions = sessions;
        this.storage = storage;
        this.dialogs = dialogs;
        this.startupProjectPath = startupProjectPath;
        Settings = settingsDialog;
        RecentProjects = new RecentProjectsViewModel(settings, probe, OpenProjectPathAsync);
        Console = new ConsoleViewModel(consoleService, consoleChannels);

        editCommands = new ShellEditCommands(sessions);
        lifecycle = new ShellLifecycleCoordinator(sessions, settings, dialogs, consoleService, probe, lifetime);
        projectOperations = new ShellProjectOperations(sessions, dialogs, consoleService, Navigation, Documents, lifecycle);
        backCommand = new AsyncRelayCommand(() => Navigation.GoBackAsync(), () => Navigation.CanGoBack);
        forwardCommand = new AsyncRelayCommand(() => Navigation.GoForwardAsync(), () => Navigation.CanGoForward);
        closeActiveCommand = new AsyncRelayCommand(CloseActiveAsync, () => Documents.ActiveDocument is not null);
        saveProjectCommand = new AsyncRelayCommand(SaveProjectAsync, () => sessions.ActiveSession is not null);
        saveAsProjectCommand = new AsyncRelayCommand(SaveAsProjectAsync, () => sessions.ActiveSession is not null);
        closeProjectCommand = new AsyncRelayCommand(projectOperations.CloseActiveAsync, () => sessions.ActiveSession is not null);
        reloadProjectCommand = new AsyncRelayCommand(projectOperations.ReloadActiveAsync, () => sessions.ActiveSession is not null);

        RegisterCommands(commands);
        Palette = new CommandPaletteViewModel(commands, () => IsPaletteOpen = false);
        Shortcuts = new ShortcutCheatSheetViewModel(commands, () => IsCheatSheetOpen = false);
        Menus = ShellMenuFactory.Build(menuProvider);

        documents.ActiveDocumentChanged += OnActiveDocumentChanged;
        workspace.ActiveProjectChanged += OnActiveProjectChanged;
        navigation.HistoryChanged += OnHistoryChanged;
        settings.Changed += OnSettingsChanged;
        Regions.PropertyChanged += OnRegionPropertyChanged;
        ApplySettings(settings.Current);

        if (settings.LoadResult.IsFatal)
        {
            consoleService.Write(
                ConsoleSeverity.Warning,
                "settings",
                settings.LoadResult.FatalMessage ?? "The settings file was rejected; defaults are active.");
        }

        foreach (var warning in settings.LoadResult.Warnings)
        {
            consoleService.Write(ConsoleSeverity.Warning, "settings", warning);
        }

        consoleService.Write(ConsoleSeverity.Information, "shell", "MOCASSIN shell started.");
        if (settings.Current.Ui.ShowWelcomeOnStart)
        {
            documents.OpenOrActivate(
                new WelcomeDocumentViewModel(
                    ShowShortcutsCommand,
                    NewProjectCommand,
                    OpenProjectCommand,
                    RecentProjects));
        }

        if (!string.IsNullOrWhiteSpace(startupProjectPath))
        {
            StartupProjectTask = OpenProjectPathAsync(startupProjectPath);
        }

        AttachActiveEditor();
        UpdateActiveDocument();
        UpdateActiveProject();
    }

    /// <summary>
    /// Gets the region state.
    /// </summary>
    public ShellRegionsViewModel Regions { get; }

    /// <summary>
    /// Gets the status-bar state.
    /// </summary>
    public StatusBarViewModel StatusBar { get; }

    /// <summary>
    /// Gets the console state.
    /// </summary>
    public ConsoleViewModel Console { get; }

    /// <summary>
    /// Gets the progress state shown by the status bar.
    /// </summary>
    public ProgressService Progress { get; }

    /// <summary>
    /// Gets the document collection.
    /// </summary>
    public DocumentCollectionViewModel Documents { get; }

    /// <summary>
    /// Gets the project workspace.
    /// </summary>
    public WorkspaceViewModel Workspace { get; }

    /// <summary>
    /// Gets the navigation service.
    /// </summary>
    public INavigationService Navigation { get; }

    /// <summary>
    /// Gets the project explorer.
    /// </summary>
    public ProjectExplorerViewModel Explorer { get; }

    /// <summary>
    /// Gets the command catalog.
    /// </summary>
    public ICommandCatalog Commands { get; }

    /// <summary>
    /// Gets the command palette.
    /// </summary>
    public CommandPaletteViewModel Palette { get; }

    /// <summary>
    /// Gets the shortcut cheat sheet.
    /// </summary>
    public ShortcutCheatSheetViewModel Shortcuts { get; }

    /// <summary>
    /// Gets the menu bar model.
    /// </summary>
    public IReadOnlyList<MenuViewModel> Menus { get; }

    /// <summary>
    /// Gets the platform used to format gestures.
    /// </summary>
    public GesturePlatform Platform { get; }

    /// <summary>
    /// Gets the settings dialog state.
    /// </summary>
    public SettingsViewModel Settings { get; }

    /// <summary>
    /// Gets the recent-project list shown by the welcome document.
    /// </summary>
    public RecentProjectsViewModel RecentProjects { get; }

    /// <summary>
    /// Gets the startup project-open task. It is completed when no project was supplied.
    /// </summary>
    internal Task StartupProjectTask { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// Gets the startup session-restore task. It is completed when no restore was offered.
    /// </summary>
    internal Task StartupRestoreTask { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// Gets the text shown when the documents region is empty.
    /// </summary>
    public string EmptyDocumentText { get; } = "No document is open.";

    /// <summary>
    /// Exits the application.
    /// </summary>
    [RelayCommand]
    private Task ExitAsync() => lifecycle.ExitAsync(StartupProjectTask);

    /// <summary>
    /// Begins the unclean-exit restore offer after the window is shown.
    /// </summary>
    internal void BeginStartupRestore()
    {
        if (startupRestoreStarted)
        {
            return;
        }

        startupRestoreStarted = true;
        StartupRestoreTask = lifecycle.RestoreSessionAsync(StartupProjectTask, startupSessionSnapshot);
    }

    /// <summary>
    /// Prompts for dirty sessions and closes them before the window closes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the window may close.</returns>
    internal Task<bool> RequestCloseAsync(CancellationToken cancellationToken = default) =>
        lifecycle.RequestCloseAsync(StartupProjectTask, cancellationToken);

    /// <summary>
    /// Opens the command palette.
    /// </summary>
    [RelayCommand]
    private void ShowPalette()
    {
        Palette.Refresh();
        IsPaletteOpen = true;
    }

    /// <summary>
    /// Opens the shortcut cheat sheet.
    /// </summary>
    [RelayCommand]
    private void ShowShortcuts()
    {
        IsCheatSheetOpen = true;
        IsPaletteOpen = false;
    }

    /// <summary>
    /// Closes the shortcut cheat sheet.
    /// </summary>
    [RelayCommand]
    private void CloseShortcuts() => IsCheatSheetOpen = false;

    /// <summary>
    /// Closes the command palette.
    /// </summary>
    [RelayCommand]
    private void ClosePalette() => IsPaletteOpen = false;

    private void RegisterCommands(ICommandCatalog catalog)
    {
        catalog.Add("shell.exit", "Exit", "File", MenuKind.File, null, ExitCommand);
        catalog.Add(
            "view.projects",
            "Toggle Projects",
            "View",
            MenuKind.View,
            new KeyGestureDescriptor("1", GestureModifier.Alt),
            Regions.ToggleProjectsCommand);
        catalog.Add(
            "view.inspector",
            "Toggle Inspector",
            "View",
            MenuKind.View,
            new KeyGestureDescriptor("2", GestureModifier.Alt),
            Regions.ToggleInspectorCommand);
        catalog.Add(
            "view.console",
            "Toggle Console",
            "View",
            MenuKind.View,
            new KeyGestureDescriptor("0", GestureModifier.Alt),
            Regions.ToggleConsoleCommand);
        catalog.Add(
            "project.new",
            "New Project…",
            "Project",
            MenuKind.File,
            new KeyGestureDescriptor("N", GestureModifier.Primary),
            NewProjectCommand);
        catalog.Add(
            "project.open",
            "Open Project…",
            "Project",
            MenuKind.File,
            new KeyGestureDescriptor("O", GestureModifier.Primary),
            OpenProjectCommand);
        catalog.Add(
            "project.save",
            "Save Project",
            "Project",
            MenuKind.File,
            new KeyGestureDescriptor("S", GestureModifier.Primary),
            saveProjectCommand);
        catalog.Add(
            "project.saveAs",
            "Save Project As…",
            "Project",
            MenuKind.File,
            new KeyGestureDescriptor("S", GestureModifier.Primary | GestureModifier.Shift),
            saveAsProjectCommand);
        catalog.Add("project.close", "Close Project", "Project", MenuKind.File, null, closeProjectCommand);
        catalog.Add(
            "project.reload",
            "Reload Project",
            "Project",
            MenuKind.File,
            new KeyGestureDescriptor("F5"),
            reloadProjectCommand);
        editCommands.Register(catalog);
        catalog.Add(
            "view.settings",
            "Settings…",
            "View",
            MenuKind.View,
            new KeyGestureDescriptor("OemComma", GestureModifier.Primary),
            new RelayCommand(() => Settings.Open()));
        catalog.Add(
            "view.palette",
            "Command Palette",
            "View",
            MenuKind.View,
            new KeyGestureDescriptor("P", GestureModifier.Primary | GestureModifier.Shift),
            ShowPaletteCommand);
        catalog.Add(
            "help.shortcuts",
            "Keyboard Shortcuts",
            "Help",
            MenuKind.Help,
            new KeyGestureDescriptor("F1"),
            ShowShortcutsCommand);
        catalog.Add(
            "navigation.back",
            "Back",
            "Navigation",
            null,
            new KeyGestureDescriptor("Left", GestureModifier.Alt),
            backCommand);
        catalog.Add(
            "navigation.forward",
            "Forward",
            "Navigation",
            null,
            new KeyGestureDescriptor("Right", GestureModifier.Alt),
            forwardCommand);
        catalog.Add(
            "document.close",
            "Close Document",
            "Document",
            null,
            new KeyGestureDescriptor("W", GestureModifier.Primary),
            closeActiveCommand);
        catalog.Add(
            "document.closeAll",
            "Close All Documents",
            "Document",
            null,
            new KeyGestureDescriptor("W", GestureModifier.Primary | GestureModifier.Shift),
            new AsyncRelayCommand(Documents.CloseAllAsync));
        catalog.Add(
            "document.next",
            "Next Document",
            "Document",
            null,
            new KeyGestureDescriptor("Tab", GestureModifier.Primary),
            new RelayCommand(NextDocument));
        catalog.Add(
            "document.previous",
            "Previous Document",
            "Document",
            null,
            new KeyGestureDescriptor("Tab", GestureModifier.Primary | GestureModifier.Shift),
            new RelayCommand(PreviousDocument));
    }

    private async Task CloseActiveAsync(CancellationToken cancellationToken)
    {
        if (Documents.ActiveDocument is { } document)
        {
            await Documents.CloseAsync(document, cancellationToken);
        }
    }

    private void NextDocument() => MoveActive(1);

    private void PreviousDocument() => MoveActive(-1);

    private void MoveActive(int offset)
    {
        if (Documents.Documents.Count == 0)
        {
            return;
        }

        var current = Documents.ActiveDocument is null ? 0 : Documents.Documents.IndexOf(Documents.ActiveDocument);
        var next = (current + offset + Documents.Documents.Count) % Documents.Documents.Count;
        Documents.ActiveDocument = Documents.Documents[next];
    }

    private void OnActiveDocumentChanged(object? sender, DocumentViewModel? document)
    {
        UpdateActiveDocument();
        closeActiveCommand.NotifyCanExecuteChanged();
    }

    private void OnActiveProjectChanged(object? sender, EventArgs e)
    {
        AttachActiveEditor();
        UpdateActiveDocument();
        UpdateActiveProject();
        editCommands.Attach(sessions.ActiveSession);
        saveProjectCommand.NotifyCanExecuteChanged();
        saveAsProjectCommand.NotifyCanExecuteChanged();
        closeProjectCommand.NotifyCanExecuteChanged();
        reloadProjectCommand.NotifyCanExecuteChanged();
    }

    private void AttachActiveEditor()
    {
        var editor = sessions.ActiveSession?.EditCoordinator;
        if (ReferenceEquals(subscribedEditor, editor))
        {
            return;
        }

        if (subscribedEditor is not null)
        {
            subscribedEditor.ProjectChanged -= OnActiveEditorChanged;
        }

        subscribedEditor = editor;
        if (subscribedEditor is not null)
        {
            subscribedEditor.ProjectChanged += OnActiveEditorChanged;
        }
    }

    private void OnActiveEditorChanged(object? sender, ProjectChangedEventArgs args)
    {
        UpdateActiveDocument();
        UpdateActiveProject();
    }

    private void OnSettingsChanged(object? sender, AppSettings updated)
    {
        if (!suppressSettingsApply)
        {
            ApplySettings(updated);
        }

        RecentProjects.Refresh();
    }

    private void OnRegionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (applyingLayout)
        {
            return;
        }

        if (e.PropertyName is nameof(ShellRegionsViewModel.IsProjectsVisible)
            or nameof(ShellRegionsViewModel.IsInspectorVisible)
            or nameof(ShellRegionsViewModel.IsConsoleVisible)
            or nameof(ShellRegionsViewModel.LeftWidth)
            or nameof(ShellRegionsViewModel.RightWidth)
            or nameof(ShellRegionsViewModel.ConsoleHeight))
        {
            ScheduleLayoutPersist();
        }
    }

    /// <summary>
    /// Gets the pending layout-persistence task; tests await it before disposing the data directory.
    /// </summary>
    internal Task PendingLayoutPersist => pendingLayoutPersist;

    private void ScheduleLayoutPersist()
    {
        var previous = pendingLayoutPersist;
        pendingLayoutPersist = PersistLayoutAfterAsync(previous, CaptureLayout());
    }

    private LayoutSettings CaptureLayout() => new()
    {
        LeftVisible = Regions.IsProjectsVisible,
        RightVisible = Regions.IsInspectorVisible,
        ConsoleVisible = Regions.IsConsoleVisible,
        LeftWidth = (int)Math.Round(Regions.LeftWidth),
        RightWidth = (int)Math.Round(Regions.RightWidth),
        ConsoleHeight = (int)Math.Round(Regions.ConsoleHeight),
        ActiveConsoleChannel = Console.ActiveChannel?.Id ?? SettingsDefaults.DefaultConsoleChannel,
    };

    private async Task PersistLayoutAfterAsync(Task previous, LayoutSettings layout)
    {
        await previous.ConfigureAwait(true);
        try
        {
            await PersistLayoutAsync(layout).ConfigureAwait(true);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            consoleService.Write(
                ConsoleSeverity.Warning,
                "settings",
                $"The layout could not be saved: {exception.Message}");
        }
    }

    private void ApplySettings(AppSettings value)
    {
        StatusBar.SetAppearance(
            string.Create(
                CultureInfo.InvariantCulture,
                $"{value.Theme} · {value.Density} · {value.FontScale:0.00}×"));
        applyingLayout = true;
        Regions.IsProjectsVisible = value.Layout.LeftVisible;
        Regions.IsInspectorVisible = value.Layout.RightVisible;
        Regions.IsConsoleVisible = value.Layout.ConsoleVisible;
        Regions.LeftWidth = value.Layout.LeftWidth;
        Regions.RightWidth = value.Layout.RightWidth;
        Regions.ConsoleHeight = value.Layout.ConsoleHeight;
        Console.ActiveChannel =
            Console.Channels.FirstOrDefault(channel => channel.Id == value.Layout.ActiveConsoleChannel)
            ?? Console.Channels.FirstOrDefault();
        applyingLayout = false;
    }

    private async Task PersistLayoutAsync(LayoutSettings layout)
    {
        AppSettingsSaveResult result;
        suppressSettingsApply = true;
        try
        {
            result = await settings.UpdateAsync(settings.Current with { Layout = layout });
        }
        finally
        {
            suppressSettingsApply = false;
        }

        if (!result.IsSuccess)
        {
            consoleService.Write(
                ConsoleSeverity.Warning,
                "settings",
                result.Message ?? "The layout could not be saved.");
        }
    }

    /// <summary>
    /// Creates a project from a template.
    /// </summary>
    [RelayCommand]
    private async Task NewProjectAsync(CancellationToken cancellationToken)
    {
        var directory = await storage.PickFolderAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        var name = await dialogs.PromptAsync(
            new TextPromptRequest("New project", "Enter the project name.", Watermark: "Project name"),
            cancellationToken);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var template = await dialogs.ChooseAsync(
            new ChoiceDialogRequest(
                "Project template",
                "Choose the starter for the new project.",
                [
                    new DialogChoice("blank", "Blank", IsDefault: true),
                    new DialogChoice("ceria", "Ceria"),
                ]),
            cancellationToken);
        if (template.Kind != DialogResultKind.ChoiceSelected)
        {
            return;
        }

        var kind = string.Equals(template.ChoiceId, "ceria", StringComparison.Ordinal)
            ? ProjectTemplateKind.Ceria
            : ProjectTemplateKind.Blank;
        var result = await sessions.CreateAsync(kind, name, directory, cancellationToken);
        if (!result.IsSuccess)
        {
            await dialogs.ShowErrorAsync(
                new ErrorDialogRequest(
                    "New project",
                    result.Message ?? "The project could not be created."),
                cancellationToken);
            return;
        }

        await projectOperations.ApplyResultAsync(result, cancellationToken);
    }

    /// <summary>
    /// Opens a project with the picker.
    /// </summary>
    [RelayCommand]
    private async Task OpenProjectAsync(CancellationToken cancellationToken)
    {
        var path = await storage.PickOpenProjectAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(path))
        {
            await OpenProjectPathAsync(path);
        }
    }

    internal async Task OpenProjectPathAsync(string path)
    {
        await projectOperations.OpenAsync(path);
        UpdateActiveProject();
    }

    private async Task SaveProjectAsync()
    {
        if (sessions.ActiveSession is { } session)
        {
            await projectOperations.ApplyResultAsync(await sessions.SaveAsync(session), CancellationToken.None);
        }
    }

    private async Task SaveAsProjectAsync()
    {
        if (sessions.ActiveSession is not { } session)
        {
            return;
        }

        var path = await storage.PickSaveProjectAsync(session.Name + ".mocprj");
        if (!string.IsNullOrWhiteSpace(path))
        {
            await projectOperations.ApplyResultAsync(await sessions.SaveAsAsync(session, path), CancellationToken.None);
        }
    }

    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        backCommand.NotifyCanExecuteChanged();
        forwardCommand.NotifyCanExecuteChanged();
    }

    private void UpdateActiveDocument()
    {
        var active = Documents.ActiveDocument;
        StatusBar.SetActiveDocument(active?.Title);
        var isWelcome = active is not null
            && string.Equals(active.Key.Kind, DocumentKinds.Welcome, StringComparison.Ordinal);
        var dirtyMarker = Workspace.ActiveProject?.IsDirty == true ? "* " : string.Empty;
        WindowTitle = active is null || isWelcome
            ? $"{dirtyMarker}MOCASSIN"
            : $"{dirtyMarker}{active.Title} — MOCASSIN";

        if (active is IInspectorContributor contributor)
        {
            InspectorContent = contributor.CreateInspector(
                new InspectorSelection(
                    active.Key,
                    active.Key.ScopeId,
                    [],
                    active.Key.Kind.Split('.')[0]));
        }
        else
        {
            InspectorContent = null;
        }

        HasInspectorContent = InspectorContent is not null;

        if (active?.Key.ScopeId is { } projectId)
        {
            var project = Workspace.Find(projectId);
            if (project is not null)
            {
                project.LastDocumentKey = active.Key;
                project.LastDocumentTitle = active.Title;
            }
        }
    }

    private void UpdateActiveProject()
    {
        var project = Workspace.ActiveProject;
        var recovery = sessions.ActiveSession?.PendingRecovery is not null
            ? " — Recovery available"
            : string.Empty;
        StatusBar.SetProject(
            project is null
                ? "No project"
                : $"{project.Name} — {project.Path}{(project.IsReadOnly ? " (read-only)" : string.Empty)}"
                + (project.IsDirty ? " — Unsaved changes" : " — Saved")
                + recovery);
        ReadOnlyBannerText = project?.IsReadOnly == true
            ? $"This project is open read-only. Lock: {project.LockPath ?? "held by another session"}."
            : null;
        if (project?.LastDocumentKey is { } key)
        {
            _ = Navigation.ActivateAsync(key);
        }
    }
}
