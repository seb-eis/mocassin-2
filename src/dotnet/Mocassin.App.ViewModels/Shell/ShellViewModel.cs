// SPDX-License-Identifier: MIT

using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Commands;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Settings.Dialog;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Root view model of the application shell.
/// </summary>
internal sealed partial class ShellViewModel : ObservableObject
{
    private static readonly MenuKind[] menuOrder = [MenuKind.File, MenuKind.Edit, MenuKind.View, MenuKind.Help];

    private readonly IShellLifetime lifetime;
    private readonly ISettingsService settings;
    private readonly IConsoleService consoleService;
    private bool applyingLayout;
    private readonly AsyncRelayCommand backCommand;
    private readonly AsyncRelayCommand forwardCommand;
    private readonly AsyncRelayCommand closeActiveCommand;

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
        SettingsViewModel settingsDialog)
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
        this.consoleService = consoleService;
        Settings = settingsDialog;
        Console = new ConsoleViewModel(consoleService, consoleChannels);

        backCommand = new AsyncRelayCommand(() => Navigation.GoBackAsync(), () => Navigation.CanGoBack);
        forwardCommand = new AsyncRelayCommand(() => Navigation.GoForwardAsync(), () => Navigation.CanGoForward);
        closeActiveCommand = new AsyncRelayCommand(CloseActiveAsync, () => Documents.ActiveDocument is not null);

        RegisterCommands(commands);
        Palette = new CommandPaletteViewModel(commands, () => IsPaletteOpen = false);
        Shortcuts = new ShortcutCheatSheetViewModel(commands, () => IsCheatSheetOpen = false);
        Menus = BuildMenus(menuProvider);

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
            documents.OpenOrActivate(new WelcomeDocumentViewModel(ShowShortcutsCommand));
        }
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
    /// Gets the text shown when the documents region is empty.
    /// </summary>
    public string EmptyDocumentText { get; } = "No document is open.";

    /// <summary>
    /// Exits the application.
    /// </summary>
    [RelayCommand]
    private void Exit() => lifetime.Shutdown();

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
        catalog.Add("shell.exit", "Exit", "File", MenuKind.File, null, new RelayCommand(() => lifetime.Shutdown()));
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

    private static List<MenuViewModel> BuildMenus(IMenuItemProvider provider)
    {
        var menus = new List<MenuViewModel>();
        foreach (var menu in menuOrder)
        {
            var items = provider.CreateMenuItems(menu)
                .Select(command => new MenuItemViewModel(
                    command.Title,
                    command.Command,
                    command.Gesture,
                    command.GestureText))
                .ToList();
            if (items.Count > 0)
            {
                menus.Add(new MenuViewModel(HeaderFor(menu), items));
            }
        }

        return menus;
    }

    private static string HeaderFor(MenuKind menu) =>
        menu switch
        {
            MenuKind.File => "_File",
            MenuKind.Edit => "_Edit",
            MenuKind.View => "_View",
            MenuKind.Help => "_Help",
            _ => menu.ToString(),
        };

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

    private void OnActiveProjectChanged(object? sender, EventArgs e) => UpdateActiveProject();

    private void OnSettingsChanged(object? sender, AppSettings updated) => ApplySettings(updated);

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
            _ = PersistLayoutAsync();
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

    private async Task PersistLayoutAsync()
    {
        var layout = new LayoutSettings
        {
            LeftVisible = Regions.IsProjectsVisible,
            RightVisible = Regions.IsInspectorVisible,
            ConsoleVisible = Regions.IsConsoleVisible,
            LeftWidth = (int)Math.Round(Regions.LeftWidth),
            RightWidth = (int)Math.Round(Regions.RightWidth),
            ConsoleHeight = (int)Math.Round(Regions.ConsoleHeight),
            ActiveConsoleChannel = Console.ActiveChannel?.Id ?? SettingsDefaults.DefaultConsoleChannel,
        };
        var result = await settings.UpdateAsync(settings.Current with { Layout = layout });
        if (!result.IsSuccess)
        {
            consoleService.Write(
                ConsoleSeverity.Warning,
                "settings",
                result.Message ?? "The layout could not be saved.");
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
        WindowTitle = active is null || isWelcome ? "MOCASSIN" : $"{active.Title} — MOCASSIN";

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
            }
        }
    }

    private void UpdateActiveProject()
    {
        var project = Workspace.ActiveProject;
        StatusBar.SetProject(project is null ? "No project" : $"{project.Name} — {project.Path}");
        if (project?.LastDocumentKey is { } key)
        {
            _ = Navigation.ActivateAsync(key);
        }
    }
}
