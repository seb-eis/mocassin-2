// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Editors.Diagnostics;
using Mocassin.App.ViewModels.Editors.ModelTable;
using Mocassin.App.ViewModels.Editors.Usage;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Diagnostics;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors;

/// <summary>
/// The shared model-editor base: row projection, selection, search/filter/sort, the lazy usage index, inline
/// diagnostics, the empty/read-only state, and the document-scoped edit commands.
/// </summary>
/// <remarks>
/// The editor never mutates a project or a domain object and never builds a <c>ProjectChange</c>. A detail commits
/// a typed <c>ProjectEditor</c> operation through <see cref="ProjectEditCoordinator"/>, which remains the single
/// write path. Subscriptions are removed on <see cref="Dispose"/> or document close.
/// </remarks>
internal abstract partial class ModelEditorViewModel : ObservableObject, IDisposable
{
    private readonly ProjectSession session;
    private readonly IEditorPaneLayout layout;
    private ModelUsageIndex? usageIndex;
    private ProjectModel? usageSnapshot;
    private ImmutableArray<Diagnostic> rejectedDiagnostics = [];
    private ImmutableArray<ModelUsageEntry> selectedUsages = [];
    private string? blockedDeleteMessage;
    private string? pendingFocus;
    private string? detailMessage;
    private ModelEditorState state;
    private ModelDetailViewModel? detail;
    private ModelEditorActionBarViewModel? actionBar;
    private bool disposed;

    /// <summary>
    /// Initializes a model editor.
    /// </summary>
    /// <param name="session">The project session that owns the document and coordinator.</param>
    /// <param name="descriptor">The domain descriptor.</param>
    /// <param name="layout">The shell-owned editor pane layout.</param>
    /// <param name="openProblems">The optional Problems-channel activation.</param>
    protected ModelEditorViewModel(
        ProjectSession session,
        ModelEditorDescriptor descriptor,
        IEditorPaneLayout layout,
        Action? openProblems = null)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Coordinator = session.EditCoordinator;
        Table = new ModelTableViewModel { EmptyMessage = descriptor.EmptyMessage };
        DiagnosticSummary = new ModelDiagnosticSummaryViewModel(openProblems);

        AddCommand = new RelayCommand(Add, () => CanEdit);
        RemoveCommand = new RelayCommand(
            () => TryRemoveSelected(),
            () => CanMutateSelection);
        DuplicateCommand = new RelayCommand(
            () => DuplicateSelected(),
            () => CanMutateSelection);
        ShowUsagesCommand = new RelayCommand(ShowUsages, () => Table.SelectedRow is not null);

        Table.PropertyChanged += OnTablePropertyChanged;
        Coordinator.ProjectChanged += OnProjectChanged;
        Coordinator.EditRejected += OnEditRejected;
        session.ValidationCoordinator.Changed += OnValidationChanged;
        layout.PropertyChanged += OnLayoutPropertyChanged;

        Reload(preserveSelection: null);
    }

    /// <summary>
    /// Gets the project session.
    /// </summary>
    public ProjectSession Session => session;

    /// <summary>
    /// Gets the single write path.
    /// </summary>
    public ProjectEditCoordinator Coordinator { get; }

    /// <summary>
    /// Gets the table surface state.
    /// </summary>
    public ModelTableViewModel Table { get; }

    /// <summary>
    /// Gets the domain descriptor.
    /// </summary>
    public ModelEditorDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the inline validation summary.
    /// </summary>
    public ModelDiagnosticSummaryViewModel DiagnosticSummary { get; }

    /// <summary>
    /// Gets the editor state.
    /// </summary>
    public ModelEditorState State
    {
        get => state;
        private set => SetProperty(ref state, value);
    }

    /// <summary>
    /// Gets a value indicating whether the session may edit.
    /// </summary>
    public bool CanEdit => Coordinator.CanEdit;

    /// <summary>
    /// Gets a value indicating whether the session is read-only.
    /// </summary>
    public bool IsReadOnly => !CanEdit;

    /// <summary>
    /// Gets the shell-owned editor pane layout.
    /// </summary>
    public IEditorPaneLayout PaneLayout => layout;

    /// <summary>
    /// Gets a value indicating whether the selected row can be mutated.
    /// </summary>
    public bool CanMutateSelection =>
        CanEdit && Table.SelectedRow is not null && !IsSelectedObjectReadOnly;

    /// <summary>
    /// Gets the selection pane's share of the editor width.
    /// </summary>
    public double EditorFraction => layout.EditorFraction;

    /// <summary>
    /// Gets the active detail editor, if a row is selected.
    /// </summary>
    public ModelDetailViewModel? Detail
    {
        get => detail;
        private set => SetProperty(ref detail, value);
    }

    /// <summary>
    /// Gets the optional document-level action bar (for example the particle editor's bulk charge).
    /// </summary>
    public ModelEditorActionBarViewModel? ActionBar
    {
        get => actionBar;
        protected set => SetProperty(ref actionBar, value);
    }

    /// <summary>
    /// Gets the read-only explanation shown when a selected row has no editable detail (for example the void
    /// particle).
    /// </summary>
    public string? DetailMessage
    {
        get => detailMessage;
        private set => SetProperty(ref detailMessage, value);
    }

    /// <summary>
    /// Gets a value indicating whether the selected row is read-only (frameworks can pin a synthetic row).
    /// </summary>
    public bool IsSelectedObjectReadOnly => Table.SelectedRow is { } row && IsReadOnlyObject(row.ObjectId);

    /// <summary>
    /// Gets the usages of the latest show-usages request or blocked delete.
    /// </summary>
    public ImmutableArray<ModelUsageEntry> SelectedUsages
    {
        get => selectedUsages;
        private set => SetProperty(ref selectedUsages, value);
    }

    /// <summary>
    /// Gets the blocked-delete explanation, if any.
    /// </summary>
    public string? BlockedDeleteMessage
    {
        get => blockedDeleteMessage;
        private set
        {
            if (SetProperty(ref blockedDeleteMessage, value))
            {
                OnPropertyChanged(nameof(HasBlockedDelete));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a blocked-delete message is present.
    /// </summary>
    public bool HasBlockedDelete => !string.IsNullOrWhiteSpace(BlockedDeleteMessage);

    /// <summary>
    /// Gets the pending focus request for a view that attaches after navigation.
    /// </summary>
    public string? PendingFocus => pendingFocus;

    /// <summary>
    /// Gets the command that adds a new object.
    /// </summary>
    public IRelayCommand AddCommand { get; }

    /// <summary>
    /// Gets the command that removes the selected object after the blocked-delete check.
    /// </summary>
    public IRelayCommand RemoveCommand { get; }

    /// <summary>
    /// Gets the command that duplicates the selected object.
    /// </summary>
    public IRelayCommand DuplicateCommand { get; }

    /// <summary>
    /// Gets the command that shows the selected object's usages.
    /// </summary>
    public IRelayCommand ShowUsagesCommand { get; }

    /// <summary>
    /// Raised when a navigation request asks a view to focus a field.
    /// </summary>
    public event EventHandler<string>? FocusRequested;

    /// <summary>
    /// Gets the model-path domains whose diagnostics belong to this editor.
    /// </summary>
    protected virtual IReadOnlyCollection<string> Domains => [Descriptor.Domain];

    /// <summary>
    /// Rebuilds the rows after a domain editor has configured its column policy.
    /// </summary>
    protected void RebuildRows() => Reload(Table.SelectedRow?.ObjectId);

    /// <summary>
    /// Determines whether one projected row is read-only (the default is editable).
    /// </summary>
    /// <param name="objectId">The row's object id.</param>
    /// <returns><see langword="true"/> when the row is a pinned, read-only row.</returns>
    protected virtual bool IsReadOnlyObject(Guid objectId) => false;

    /// <summary>
    /// Gets the explanation for a selected row that has no detail.
    /// </summary>
    /// <param name="objectId">The row's object id.</param>
    /// <returns>The explanation, or <see langword="null"/>.</returns>
    protected virtual string? GetDetailMessage(Guid objectId) => null;

    /// <summary>
    /// Removes several objects after the framework's per-object blocked-delete check.
    /// </summary>
    /// <param name="objectIds">The selected object ids.</param>
    /// <returns><see langword="true"/> when the removal was attempted.</returns>
    protected virtual bool RemoveManyCore(IReadOnlyList<Guid> objectIds) =>
        objectIds.All(RemoveCore);

    /// <summary>
    /// Gets the current project snapshot.
    /// </summary>
    protected ProjectModel Project => session.Project;

    /// <summary>
    /// Gets the lazy usage index for the current snapshot.
    /// </summary>
    public ModelUsageIndex Usages => EnsureUsages();

    /// <summary>
    /// Projects the domain objects into rows.
    /// </summary>
    /// <param name="project">The current snapshot.</param>
    /// <returns>The rows in domain order.</returns>
    protected abstract ImmutableArray<ModelTableRow> BuildRows(ProjectModel project);

    /// <summary>
    /// Creates the detail editor for one object.
    /// </summary>
    /// <param name="project">The current snapshot.</param>
    /// <param name="objectId">The selected object id.</param>
    /// <returns>The detail, or <see langword="null"/>.</returns>
    protected abstract ModelDetailViewModel? CreateDetail(ProjectModel project, Guid objectId);

    /// <summary>
    /// Determines whether an object's value changed between two snapshots.
    /// </summary>
    /// <param name="previous">The snapshot before the change.</param>
    /// <param name="current">The snapshot after the change.</param>
    /// <param name="objectId">The object id.</param>
    /// <returns><see langword="true"/> when the value changed.</returns>
    protected abstract bool IsValueChanged(ProjectModel previous, ProjectModel current, Guid objectId);

    /// <summary>
    /// Adds a new object. The domain editor overrides this.
    /// </summary>
    protected virtual void AddCore()
    {
    }

    /// <summary>
    /// Removes one object after the framework's blocked-delete pre-check.
    /// </summary>
    /// <param name="objectId">The object id.</param>
    /// <returns><see langword="true"/> when the removal was attempted.</returns>
    protected virtual bool RemoveCore(Guid objectId) => false;

    /// <summary>
    /// Duplicates one object.
    /// </summary>
    /// <param name="objectId">The object id.</param>
    protected virtual void DuplicateCore(Guid objectId)
    {
    }

    /// <summary>
    /// Refreshes reference options after a foreign edit.
    /// </summary>
    /// <param name="project">The current snapshot.</param>
    protected virtual void RefreshOptions(ProjectModel project)
    {
    }

    /// <summary>
    /// Selects one object by id.
    /// </summary>
    /// <param name="objectId">The object id.</param>
    /// <returns><see langword="true"/> when the object exists and was selected.</returns>
    public bool RequestSelection(Guid objectId)
    {
        if (!Table.SelectObject(objectId))
        {
            return false;
        }

        RefreshDetail();
        return true;
    }

    /// <summary>
    /// Requests focus for one property.
    /// </summary>
    /// <param name="property">The property name.</param>
    public void RequestFocus(string property)
    {
        if (string.IsNullOrWhiteSpace(property))
        {
            return;
        }

        pendingFocus = property;
        Detail?.FocusField(property);
        FocusRequested?.Invoke(this, property);
    }

    /// <summary>
    /// Consumes the pending focus request when a view attaches.
    /// </summary>
    /// <returns>The property name, or <see langword="null"/>.</returns>
    public string? ConsumePendingFocus()
    {
        var pending = pendingFocus;
        pendingFocus = null;
        return pending;
    }

    /// <summary>
    /// Removes the selected object unless the usage index reports a dependent.
    /// </summary>
    /// <returns><see langword="true"/> when the removal was attempted.</returns>
    public bool TryRemoveSelected()
    {
        var rows = Table.SelectedRows.Count > 0
            ? Table.SelectedRows.ToArray()
            : Table.SelectedRow is { } single
                ? new[] { single }
                : Array.Empty<ModelTableRow>();
        var removable = rows.Where(row => !IsReadOnlyObject(row.ObjectId)).ToArray();
        if (removable.Length == 0)
        {
            return false;
        }

        var usages = EnsureUsages();
        var allDependents = new List<ModelUsageEntry>();
        foreach (var row in removable)
        {
            allDependents.AddRange(usages.GetDependents(new ModelObjectRef(Descriptor.Domain, row.ObjectId)));
        }

        if (allDependents.Count > 0)
        {
            SelectedUsages = [.. allDependents.Distinct()];
            BlockedDeleteMessage = string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"The {Descriptor.ObjectNoun} is used by {allDependents.Distinct().Count()} object(s): {string.Join(", ", allDependents.Distinct().Select(static entry => entry.DisplayName))}.");
            return false;
        }

        BlockedDeleteMessage = null;
        return RemoveManyCore(removable.Select(static row => row.ObjectId).ToArray());
    }

    /// <summary>
    /// Shows the selected object's usages.
    /// </summary>
    public void ShowUsages()
    {
        if (Table.SelectedRow is not { } row)
        {
            return;
        }

        SelectedUsages = EnsureUsages().GetDependents(new ModelObjectRef(Descriptor.Domain, row.ObjectId));
    }

    /// <summary>
    /// Creates the inspector content for the active selection.
    /// </summary>
    /// <returns>The inspector summary.</returns>
    public object CreateInspector()
    {
        if (Table.SelectedRow is not { } row)
        {
            return new InspectorSummaryViewModel(
                Descriptor.Title,
                [Table.MatchCountText, DiagnosticSummary.SummaryText]);
        }

        var name = row.Cells.Length > 0 ? row.Cells[0].Text : Descriptor.ObjectNoun;
        var dependents = EnsureUsages().GetDependents(new ModelObjectRef(Descriptor.Domain, row.ObjectId));
        var details = new List<string>
        {
            $"{dependents.Length} usage(s)",
            DiagnosticSummary.SummaryText,
        };
        details.AddRange(dependents.Select(static entry => $"{entry.DisplayName} — {entry.Description}"));
        return new InspectorSummaryViewModel(name, details);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Table.PropertyChanged -= OnTablePropertyChanged;
        Coordinator.ProjectChanged -= OnProjectChanged;
        Coordinator.EditRejected -= OnEditRejected;
        session.ValidationCoordinator.Changed -= OnValidationChanged;
        layout.PropertyChanged -= OnLayoutPropertyChanged;
        usageIndex = null;
        usageSnapshot = null;
        FocusRequested = null;
    }

    private ModelUsageIndex EnsureUsages()
    {
        var project = session.Project;
        if (usageIndex is null || !ReferenceEquals(usageSnapshot, project))
        {
            usageIndex = ModelUsageIndex.Create(project);
            usageSnapshot = project;
        }

        return usageIndex;
    }

    private void Add()
    {
        if (!CanEdit)
        {
            return;
        }

        AddCore();
    }

    private void DuplicateSelected()
    {
        if (!CanEdit || Table.SelectedRow is not { } row)
        {
            return;
        }

        DuplicateCore(row.ObjectId);
    }

    private void Reload(Guid? preserveSelection)
    {
        var project = session.Project;
        Table.SetRows(BuildRows(project), preserveSelection);
        usageIndex = null;
        usageSnapshot = null;
        State = !CanEdit
            ? ModelEditorState.ReadOnly
            : Table.TotalCount == 0 ? ModelEditorState.Empty : ModelEditorState.Ready;
        RefreshDiagnostics();
        RefreshDetail();
        RefreshCommands();
    }

    private void OnProjectChanged(object? sender, ProjectChangedEventArgs args)
    {
        rejectedDiagnostics = [];
        var previous = args.Previous;
        var current = args.Current;
        var selectedId = Table.SelectedRow?.ObjectId;

        if (args.Kind == ProjectChangeKind.Saved)
        {
            RefreshDiagnostics();
            RefreshCommands();
            return;
        }

        if (args.Kind == ProjectChangeKind.Replaced)
        {
            Detail = null;
        }

        if (args.Kind is ProjectChangeKind.Edited or ProjectChangeKind.Undo or ProjectChangeKind.Redo)
        {
            var open = Detail?.ObjectId;
            var changed = open is { } id && IsValueChanged(previous, current, id);
            Reload(selectedId);
            RefreshOptions(current);
            if (open is { } openId && Detail is not null && Detail.ObjectId == openId)
            {
                if (!RowExists(openId))
                {
                    Detail.MarkRemoved();
                }
                else if (changed)
                {
                    Detail.MarkConflict();
                }

                Detail.RefreshOptions(current);
            }

            return;
        }

        Reload(selectedId);
        RefreshOptions(current);
    }

    private void OnEditRejected(object? sender, EditRejectedEventArgs args)
    {
        rejectedDiagnostics = args.Diagnostics.IsDefault ? [] : args.Diagnostics;
        RefreshDiagnostics();
    }

    private void OnValidationChanged(object? sender, EventArgs args) => RefreshDiagnostics();

    private void OnLayoutPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(IEditorPaneLayout.EditorFraction))
        {
            OnPropertyChanged(nameof(EditorFraction));
        }
    }

    private void OnTablePropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(ModelTableViewModel.SelectedRow))
        {
            RefreshDetail();
            RefreshCommands();
        }
    }

    private bool RowExists(Guid objectId) => Table.ContainsObject(objectId);

    private void RefreshDetail()
    {
        var selected = Table.SelectedRow;
        if (selected is null)
        {
            Detail = null;
            DetailMessage = null;
            OnPropertyChanged(nameof(IsSelectedObjectReadOnly));
            return;
        }

        if (Detail is not null && Detail.ObjectId == selected.ObjectId)
        {
            DetailMessage = null;
            OnPropertyChanged(nameof(IsSelectedObjectReadOnly));
            return;
        }

        var created = CreateDetail(session.Project, selected.ObjectId);
        if (created is not null)
        {
            created.Attach(Coordinator);
            if (pendingFocus is { } focus)
            {
                created.FocusField(focus);
            }
        }

        Detail = created;
        DetailMessage = created is null ? GetDetailMessage(selected.ObjectId) : null;
        OnPropertyChanged(nameof(IsSelectedObjectReadOnly));
    }

    private void RefreshCommands()
    {
        AddCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
        DuplicateCommand.NotifyCanExecuteChanged();
        ShowUsagesCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(CanMutateSelection));
        OnPropertyChanged(nameof(IsSelectedObjectReadOnly));
        OnPropertyChanged(nameof(State));
    }

    private void RefreshDiagnostics()
    {
        var domains = Domains;
        var validation = session.ValidationCoordinator.Diagnostics;
        var filtered = validation
            .Where(diagnostic => diagnostic.Path is null
                || domains.Contains(diagnostic.Path.Domain, StringComparer.Ordinal))
            .Concat(rejectedDiagnostics)
            .Distinct()
            .OrderBy(static diagnostic => diagnostic.Path?.Domain, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal);
        DiagnosticSummary.SetDiagnostics(filtered);
    }
}
