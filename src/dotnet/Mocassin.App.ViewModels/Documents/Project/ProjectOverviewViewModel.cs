// SPDX-License-Identifier: MIT

using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Diagnostics;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects.Templates;

namespace Mocassin.App.ViewModels.Documents.Project;

/// <summary>
/// Project overview document with the editable metadata form, validation banner, and statistics toggle.
/// </summary>
internal sealed partial class ProjectOverviewViewModel : DocumentViewModel, IInspectorContributor
{
    private readonly ProjectWorkspaceEntry entry;
    private readonly ProjectSession session;
    private readonly ProjectEditCoordinator editor;
    private readonly IClipboardService clipboard;

    [ObservableProperty]
    private string nameDraft = string.Empty;

    [ObservableProperty]
    private string descriptionDraft = string.Empty;

    [ObservableProperty]
    private string authorDraft = string.Empty;

    [ObservableProperty]
    private string notesDraft = string.Empty;

    [ObservableProperty]
    private string? nameError;

    [ObservableProperty]
    private string? inlineError;

    /// <summary>
    /// Initializes a project overview document.
    /// </summary>
    /// <param name="entry">The project workspace entry.</param>
    /// <param name="clipboard">The clipboard boundary.</param>
    public ProjectOverviewViewModel(ProjectWorkspaceEntry entry, IClipboardService clipboard)
        : base(new DocumentKey(DocumentKinds.ProjectOverview, entry.Id))
    {
        this.entry = entry ?? throw new ArgumentNullException(nameof(entry));
        this.clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
        session = entry.Session;
        editor = session.EditCoordinator;
        RefreshFromProject();
        editor.ProjectChanged += OnProjectChanged;
        editor.EditRejected += OnEditRejected;
    }

    /// <inheritdoc />
    public override string Title => $"Overview — {entry.Name}";

    /// <inheritdoc />
    public override string? ToolTip => entry.Path;

    /// <summary>
    /// Gets the live validation surface.
    /// </summary>
    public ValidationCoordinator Validation => session.ValidationCoordinator;

    /// <summary>
    /// Gets a value indicating whether the metadata form can edit the project.
    /// </summary>
    public bool CanEdit => editor.CanEdit;

    /// <summary>
    /// Gets a value indicating whether the session is read-only.
    /// </summary>
    public bool IsReadOnly => session.IsReadOnly;

    /// <summary>
    /// Gets a value indicating whether the document differs from the save point.
    /// </summary>
    public bool IsDirty => editor.IsDirty;

    /// <summary>
    /// Gets the dirty-state text.
    /// </summary>
    public string DirtyText => IsDirty ? "Unsaved changes" : "Saved";

    /// <summary>
    /// Gets a value indicating whether the name field has an inline error.
    /// </summary>
    public bool HasNameError => !string.IsNullOrWhiteSpace(NameError);

    /// <summary>
    /// Gets a value indicating whether the form has a general inline error.
    /// </summary>
    public bool HasInlineError => !string.IsNullOrWhiteSpace(InlineError);

    /// <summary>
    /// Gets the project id text.
    /// </summary>
    public string ProjectIdText => entry.Id.ToString("D", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the committed project name.
    /// </summary>
    public string Name => entry.Name;

    /// <summary>
    /// Gets the committed project description.
    /// </summary>
    public string Description => entry.Project.Metadata.Description ?? "—";

    /// <summary>
    /// Gets the committed project author.
    /// </summary>
    public string Author => entry.Project.Metadata.Author ?? "—";

    /// <summary>
    /// Gets the committed project notes.
    /// </summary>
    public string Notes => entry.Project.Metadata.Notes ?? "—";

    /// <summary>
    /// Gets the creation timestamp text.
    /// </summary>
    public string CreatedUtcText => FormatUtc(entry.Project.Metadata.CreatedUtc);

    /// <summary>
    /// Gets the last-saved timestamp text.
    /// </summary>
    public string LastSavedText => FormatUtc(entry.LastWrittenUtc);

    /// <summary>
    /// Gets the model-modified timestamp text.
    /// </summary>
    public string ModifiedUtcText => FormatUtc(entry.Project.Metadata.ModifiedUtc);

    /// <summary>
    /// Gets the master seed.
    /// </summary>
    public string MasterSeed => entry.Project.Metadata.MasterSeed;

    /// <summary>
    /// Gets the schema version text.
    /// </summary>
    public string SchemaVersionText => entry.Project.SchemaVersion.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the symmetry metadata summary.
    /// </summary>
    public string SymmetryText
    {
        get
        {
            var symmetry = entry.Project.Symmetry;
            return symmetry is null
                ? "Not recorded"
                : string.Create(
                    CultureInfo.InvariantCulture,
                    $"Catalog {symmetry.SpaceGroupDataVersion ?? "unknown"} · contract {symmetry.SymmetryContractVersion ?? "unknown"}");
        }
    }

    /// <summary>
    /// Gets the project file path.
    /// </summary>
    public string FilePath => entry.Path;

    /// <summary>
    /// Gets the lock and read-only state text.
    /// </summary>
    public string LockStateText => entry.IsReadOnly
        ? entry.LockPath is null
            ? "Read-only (another session owns the lock)"
            : $"Read-only (locked at {entry.LockPath}; acquisition time unavailable)"
        : entry.Session.LockAcquiredUtc is { } acquired
            ? $"Writable (lock acquired {FormatUtc(acquired)})"
            : "Writable";

    /// <summary>
    /// Occurs when a diagnostic navigation asks the view to focus a metadata field.
    /// </summary>
    public event EventHandler<string>? FocusRequested;

    /// <summary>
    /// Gets the pending focus request, if any.
    /// </summary>
    public string? PendingFocus { get; private set; }

    /// <summary>
    /// Requests focus for one metadata field.
    /// </summary>
    /// <param name="property">The field property name: name, description, author, or notes.</param>
    public void RequestFocus(string property)
    {
        if (string.IsNullOrWhiteSpace(property))
        {
            return;
        }

        PendingFocus = property;
        FocusRequested?.Invoke(this, property);
    }

    /// <summary>
    /// Consumes the pending focus request.
    /// </summary>
    /// <returns>The property name, or <see langword="null"/>.</returns>
    public string? ConsumePendingFocus()
    {
        var pending = PendingFocus;
        PendingFocus = null;
        return pending;
    }

    /// <inheritdoc />
    public object? CreateInspector(InspectorSelection selection) =>
        new InspectorSummaryViewModel(
            Name,
            [FilePath, LockStateText, $"Project {ProjectIdText}"]);

    /// <inheritdoc />
    public override Task OnClosingAsync(CancellationToken cancellationToken = default)
    {
        editor.ProjectChanged -= OnProjectChanged;
        editor.EditRejected -= OnEditRejected;
        FocusRequested = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Commits the name draft through the edit coordinator.
    /// </summary>
    [RelayCommand]
    private void CommitName()
    {
        var normalized = ProjectPathRules.NormalizeName(NameDraft);
        if (!normalized.IsSuccess)
        {
            NameError = normalized.Message ?? "The project name is invalid.";
            return;
        }

        var result = editor.Apply(
            "Set project name",
            project => project with
            {
                Metadata = project.Metadata with { Name = normalized.Name },
            });
        Handle(result, nameField: true);
    }

    /// <summary>
    /// Commits the description draft through the edit coordinator.
    /// </summary>
    [RelayCommand]
    private void CommitDescription()
    {
        var value = NormalizeOptional(DescriptionDraft);
        var result = editor.Apply(
            "Set project description",
            project => project with
            {
                Metadata = project.Metadata with { Description = value },
            });
        Handle(result);
    }

    /// <summary>
    /// Commits the author draft through the edit coordinator.
    /// </summary>
    [RelayCommand]
    private void CommitAuthor()
    {
        var value = NormalizeOptional(AuthorDraft);
        var result = editor.Apply(
            "Set project author",
            project => project with
            {
                Metadata = project.Metadata with { Author = value },
            });
        Handle(result);
    }

    /// <summary>
    /// Commits the notes draft through the edit coordinator.
    /// </summary>
    [RelayCommand]
    private void CommitNotes()
    {
        var value = NormalizeOptional(NotesDraft);
        var result = editor.Apply(
            "Set project notes",
            project => project with
            {
                Metadata = project.Metadata with { Notes = value },
            });
        Handle(result);
    }

    /// <summary>
    /// Toggles the explicit statistics panel.
    /// </summary>
    [RelayCommand]
    private Task ToggleStatisticsAsync(CancellationToken cancellationToken) =>
        Validation.SetStatisticsActiveAsync(!Validation.IsStatisticsActive, cancellationToken);

    /// <summary>
    /// Copies the master seed.
    /// </summary>
    [RelayCommand]
    private Task CopyMasterSeedAsync(CancellationToken cancellationToken) =>
        clipboard.SetTextAsync(MasterSeed, cancellationToken);

    /// <summary>
    /// Copies the project path.
    /// </summary>
    [RelayCommand]
    private Task CopyPathAsync(CancellationToken cancellationToken) =>
        clipboard.SetTextAsync(FilePath, cancellationToken);

    partial void OnNameErrorChanged(string? value) => OnPropertyChanged(nameof(HasNameError));

    partial void OnInlineErrorChanged(string? value) => OnPropertyChanged(nameof(HasInlineError));

    private void Handle(EditApplyResult result, bool nameField = false)
    {
        switch (result.Outcome)
        {
            case EditApplyOutcome.ReadOnly:
                InlineError = result.Message;
                return;
            case EditApplyOutcome.Rejected:
                if (nameField)
                {
                    NameError = result.Message;
                }
                else
                {
                    InlineError = result.Message;
                }

                return;
            default:
                NameError = null;
                InlineError = null;
                RefreshFromProject();
                return;
        }
    }

    private void OnProjectChanged(object? sender, ProjectChangedEventArgs args)
    {
        NameError = null;
        InlineError = null;
        RefreshFromProject();
    }

    private void OnEditRejected(object? sender, EditRejectedEventArgs args)
    {
        var error = args.Diagnostics.FirstOrDefault(
            static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var message = error?.Message ?? "The edit was rejected.";
        var isName = string.Equals(error?.Path?.Property, "name", StringComparison.Ordinal)
            || message.Contains("name", StringComparison.OrdinalIgnoreCase);
        if (isName)
        {
            NameError = message;
            InlineError = null;
        }
        else
        {
            InlineError = message;
            NameError = null;
        }
    }

    private void RefreshFromProject()
    {
        var metadata = entry.Project.Metadata;
        NameDraft = metadata.Name;
        DescriptionDraft = metadata.Description ?? string.Empty;
        AuthorDraft = metadata.Author ?? string.Empty;
        NotesDraft = metadata.Notes ?? string.Empty;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(Author));
        OnPropertyChanged(nameof(Notes));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(DirtyText));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(LastSavedText));
        OnPropertyChanged(nameof(ModifiedUtcText));
        OnPropertyChanged(nameof(LockStateText));
        OnPropertyChanged(nameof(Validation));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatUtc(DateTimeOffset? value) =>
        value is null
            ? "Not yet saved"
            : value.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
}
