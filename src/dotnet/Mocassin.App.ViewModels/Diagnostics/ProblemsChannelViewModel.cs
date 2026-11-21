// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Diagnostics;

/// <summary>
/// The Problems console channel: the active project's validation diagnostics plus transient edit diagnostics.
/// </summary>
/// <remarks>
/// The channel follows <see cref="IProjectWorkspace.ActiveProject"/> and never aggregates every open project. It
/// subscribes to the active session's <see cref="ValidationCoordinator"/> and edit coordinator, so switching a
/// project replaces the list and no stale project diagnostics remain. Edit diagnostics are cleared by the next
/// successful content change, exactly as the design requires.
/// </remarks>
internal sealed partial class ProblemsChannelViewModel : ConsoleChannelViewModel
{
    private readonly IProjectWorkspace workspace;
    private readonly INavigationService navigation;
    private readonly IShellLauncher launcher;
    private readonly List<DiagnosticItemViewModel> editItems = [];
    private ProjectSession? session;

    [ObservableProperty]
    private string textFilter = string.Empty;

    [ObservableProperty]
    private string codeFilter = string.Empty;

    [ObservableProperty]
    private string sourceFilter = "All";

    [ObservableProperty]
    private ProblemsSeverityOption severityOption;

    [ObservableProperty]
    private ProblemsSortMode sortMode = ProblemsSortMode.Severity;

    [ObservableProperty]
    private bool sortDescending = true;

    /// <summary>
    /// Initializes the Problems channel.
    /// </summary>
    /// <param name="workspace">The project workspace.</param>
    /// <param name="navigation">The navigation service.</param>
    /// <param name="launcher">The help-link launcher.</param>
    public ProblemsChannelViewModel(
        IProjectWorkspace workspace,
        INavigationService navigation,
        IShellLauncher launcher)
        : base("problems", "Problems", "Validation")
    {
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        this.launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        SeverityOptions = [new ProblemsSeverityOption("All severities", null), new("Error", DiagnosticSeverity.Error), new("Warning", DiagnosticSeverity.Warning), new("Info", DiagnosticSeverity.Info)];
        SeverityOption = SeverityOptions[0];
        RevalidateCommand = new AsyncRelayCommand(RevalidateAsync, () => session is not null);
        ClearFiltersCommand = new RelayCommand(ClearFilters);
        workspace.ActiveProjectChanged += OnActiveProjectChanged;
        Attach(workspace.ActiveProject?.Session);
    }

    /// <summary>
    /// Gets the visible, filtered, and sorted rows.
    /// </summary>
    public ObservableCollection<DiagnosticItemViewModel> Items { get; } = [];

    /// <summary>
    /// Gets the severity filter options.
    /// </summary>
    public IReadOnlyList<ProblemsSeverityOption> SeverityOptions { get; }

    /// <summary>
    /// Gets the source filter options.
    /// </summary>
    public IReadOnlyList<string> SourceOptions { get; } = ["All", "validation", "edit"];

    /// <summary>
    /// Gets the sort options.
    /// </summary>
    public IReadOnlyList<ProblemsSortMode> SortModes { get; } = Enum.GetValues<ProblemsSortMode>();

    /// <summary>
    /// Gets the channel header.
    /// </summary>
    public string HeaderText => session is null ? "Validation — no project" : $"Validation — {session.Name}";

    /// <summary>
    /// Gets the validation summary text.
    /// </summary>
    public string SummaryText => session?.ValidationCoordinator.StatusText ?? "No project";

    /// <summary>
    /// Gets the last-completed validation text.
    /// </summary>
    public string LastValidationText => session?.ValidationCoordinator.LastCompletedText ?? "Not validated";

    /// <summary>
    /// Gets a value indicating whether a project is active.
    /// </summary>
    public bool HasProject => session is not null;

    /// <summary>
    /// Gets a value indicating whether any row is visible.
    /// </summary>
    public bool HasItems => Items.Count > 0;

    /// <summary>
    /// Gets the empty-state text.
    /// </summary>
    public string EmptyText => session is null
        ? "Open a project to see validation problems."
        : "No problems match the current filters.";

    /// <summary>
    /// Gets the explicit revalidate command. It runs immediately, outside the debounce.
    /// </summary>
    public AsyncRelayCommand RevalidateCommand { get; }

    /// <summary>
    /// Gets the clear-filters command.
    /// </summary>
    public RelayCommand ClearFiltersCommand { get; }

    partial void OnTextFilterChanged(string value) => Rebuild();

    partial void OnCodeFilterChanged(string value) => Rebuild();

    partial void OnSourceFilterChanged(string value) => Rebuild();

    partial void OnSeverityOptionChanged(ProblemsSeverityOption value) => Rebuild();

    partial void OnSortModeChanged(ProblemsSortMode value) => Rebuild();

    partial void OnSortDescendingChanged(bool value) => Rebuild();

    private void OnActiveProjectChanged(object? sender, EventArgs e) =>
        Attach(workspace.ActiveProject?.Session);

    private void Attach(ProjectSession? value)
    {
        if (ReferenceEquals(session, value))
        {
            return;
        }

        if (session is not null)
        {
            session.ValidationCoordinator.Changed -= OnValidationChanged;
            session.EditCoordinator.EditRejected -= OnEditRejected;
            session.EditCoordinator.ProjectChanged -= OnProjectChanged;
        }

        session = value;
        editItems.Clear();
        if (session is not null)
        {
            session.ValidationCoordinator.Changed += OnValidationChanged;
            session.EditCoordinator.EditRejected += OnEditRejected;
            session.EditCoordinator.ProjectChanged += OnProjectChanged;
        }

        RevalidateCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HeaderText));
        OnPropertyChanged(nameof(HasProject));
        OnPropertyChanged(nameof(EmptyText));
        Rebuild();
    }

    private void OnValidationChanged(object? sender, EventArgs e) => Rebuild();

    private void OnProjectChanged(object? sender, ProjectChangedEventArgs args)
    {
        if (args.Kind is ProjectChangeKind.Saved)
        {
            return;
        }

        editItems.Clear();
        OnPropertyChanged(nameof(HeaderText));
        Rebuild();
    }

    private void OnEditRejected(object? sender, EditRejectedEventArgs args)
    {
        editItems.Clear();
        foreach (var diagnostic in args.Diagnostics)
        {
            editItems.Add(new DiagnosticItemViewModel(diagnostic, "edit", navigation, launcher));
        }

        Rebuild();
    }

    private async Task RevalidateAsync(CancellationToken cancellationToken)
    {
        if (session is not null)
        {
            await session.ValidationCoordinator.RevalidateAsync(cancellationToken);
        }
    }

    private void ClearFilters()
    {
        TextFilter = string.Empty;
        CodeFilter = string.Empty;
        SourceFilter = "All";
        SeverityOption = SeverityOptions[0];
        SortMode = ProblemsSortMode.Severity;
        SortDescending = true;
        Rebuild();
    }

    private void Rebuild()
    {
        var rows = new List<DiagnosticItemViewModel>();
        if (session is not null)
        {
            foreach (var diagnostic in session.ValidationCoordinator.Diagnostics)
            {
                var item = new DiagnosticItemViewModel(diagnostic, "validation", navigation, launcher);
                if (Matches(item))
                {
                    rows.Add(item);
                }
            }
        }

        foreach (var item in editItems)
        {
            if (Matches(item))
            {
                rows.Add(item);
            }
        }

        rows.Sort(Compare);
        if (SortDescending)
        {
            rows.Reverse();
        }

        Items.Clear();
        foreach (var row in rows)
        {
            Items.Add(row);
        }

        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(LastValidationText));
        OnPropertyChanged(nameof(EmptyText));
    }

    private bool Matches(DiagnosticItemViewModel item)
    {
        if (SeverityOption?.Severity is { } minimum && item.Severity < minimum)
        {
            return false;
        }

        if (!string.Equals(SourceFilter, "All", StringComparison.Ordinal)
            && !string.Equals(item.Source, SourceFilter, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(CodeFilter)
            && !item.Code.Contains(CodeFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(TextFilter))
        {
            return true;
        }

        return item.Message.Contains(TextFilter, StringComparison.OrdinalIgnoreCase)
            || item.Code.Contains(TextFilter, StringComparison.OrdinalIgnoreCase)
            || item.PathText.Contains(TextFilter, StringComparison.OrdinalIgnoreCase)
            || item.Hint.Contains(TextFilter, StringComparison.OrdinalIgnoreCase)
            || item.SourceText.Contains(TextFilter, StringComparison.OrdinalIgnoreCase);
    }

    private int Compare(DiagnosticItemViewModel left, DiagnosticItemViewModel right) =>
        SortMode switch
        {
            ProblemsSortMode.Code => string.Compare(left.Code, right.Code, StringComparison.Ordinal),
            ProblemsSortMode.Message => string.Compare(left.Message, right.Message, StringComparison.Ordinal),
            ProblemsSortMode.Source => string.Compare(left.SourceText, right.SourceText, StringComparison.Ordinal),
            _ => left.Severity.CompareTo(right.Severity),
        };
}
