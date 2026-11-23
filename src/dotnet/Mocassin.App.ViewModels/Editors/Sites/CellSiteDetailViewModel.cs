// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Editors.Expressions;
using Mocassin.App.ViewModels.Editors.ReferencePicker;
using Mocassin.App.ViewModels.Editors.Usage;
using Mocassin.App.ViewModels.Editing;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;
using Mocassin.Symmetry.Structure;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors.Sites;

/// <summary>
/// The cell-site detail form: expression fractional fields, the particle-set reference picker, the candidate
/// validation/overlap gate, the P1 orbit/whole-structure/projection preview, dependency-aware deletion, and the
/// stability warning.
/// </summary>
internal sealed partial class CellSiteDetailViewModel : ModelDetailViewModel
{
    private const double projectionCanvas = 300d;
    private const double projectionMargin = 12d;

    private readonly IStructurePreviewService previewService;
    private readonly IStructureValidator validator;
    private ProjectModel project;
    private string name;
    private string fieldA;
    private string fieldB;
    private string fieldC;
    private SiteStability stability;
    private string? fieldError;
    private string? stabilityWarning;
    private string? pendingEnergyDelete;
    private ImmutableArray<Diagnostic> candidateDiagnostics = [];
    private ImmutableArray<ModelUsageEntry> dependents = [];
    private StructurePreview? candidatePreview;
    private StructurePreview? currentPreview;
    private ImmutableArray<CellSite> previewSites;
    private bool fieldsValid;
    private bool showP1Preview = true;
    private string selectedPlane = "a–b";
    private bool refreshingOptions;
    private bool refreshing;

    /// <summary>
    /// Initializes a site detail.
    /// </summary>
    /// <param name="site">The site value.</param>
    /// <param name="isNew">Whether the form adds a new site.</param>
    /// <param name="project">The current project snapshot.</param>
    /// <param name="previewService">The structure preview service.</param>
    /// <param name="validator">The structure validator.</param>
    public CellSiteDetailViewModel(
        CellSite site,
        bool isNew,
        ProjectModel project,
        IStructurePreviewService previewService,
        IStructureValidator validator)
        : base(site.Id.Value)
    {
        this.previewService = previewService ?? throw new ArgumentNullException(nameof(previewService));
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        this.project = project ?? throw new ArgumentNullException(nameof(project));
        IsNew = isNew;
        name = site.Name;
        fieldA = Format(site.Fractional.A);
        fieldB = Format(site.Fractional.B);
        fieldC = Format(site.Fractional.C);
        stability = site.Stability;
        ParticleSetPicker = new ReferencePickerViewModel<ParticleSetId>("Select a particle set");
        ParticleSetPicker.SelectionChanged += OnParticleSetChanged;
        StabilityChoices = [SiteStability.Stable, SiteStability.Unstable];
        OpenDependentCommand = new RelayCommand<ModelUsageEntry>(
            entry => RequestNavigation(entry!.Reference.ToPath()),
            entry => entry is not null);
        RequestEnergyDeleteCommand = new RelayCommand(RequestEnergyDelete, () => CanDeleteEnergyOnly && CanEditForm);
        ConfirmEnergyDeleteCommand = new RelayCommand(ConfirmEnergyDelete, () => HasPendingEnergyDelete && CanEditForm);
        CancelEnergyDeleteCommand = new RelayCommand(CancelEnergyDelete, () => HasPendingEnergyDelete);
        RefreshParticleSetOptions(site.ParticleSetId);
        RefreshPreview();
    }

    /// <summary>
    /// Gets a value indicating whether this form adds a new site.
    /// </summary>
    public bool IsNew { get; }

    /// <summary>
    /// Raised after a new site was added and should be selected in the table.
    /// </summary>
    public event EventHandler<Guid>? Added;

    /// <summary>
    /// Gets or sets the site name.
    /// </summary>
    public string Name
    {
        get => name;
        set
        {
            if (SetProperty(ref name, value))
            {
                HasUncommittedChanges = true;
                RefreshPreview();
            }
        }
    }

    /// <summary>
    /// Gets or sets the a fractional expression.
    /// </summary>
    public string FieldA
    {
        get => fieldA;
        set
        {
            if (SetProperty(ref fieldA, value))
            {
                HasUncommittedChanges = true;
                RefreshPreview();
            }
        }
    }

    /// <summary>
    /// Gets or sets the b fractional expression.
    /// </summary>
    public string FieldB
    {
        get => fieldB;
        set
        {
            if (SetProperty(ref fieldB, value))
            {
                HasUncommittedChanges = true;
                RefreshPreview();
            }
        }
    }

    /// <summary>
    /// Gets or sets the c fractional expression.
    /// </summary>
    public string FieldC
    {
        get => fieldC;
        set
        {
            if (SetProperty(ref fieldC, value))
            {
                HasUncommittedChanges = true;
                RefreshPreview();
            }
        }
    }

    /// <summary>
    /// Gets or sets the site stability.
    /// </summary>
    public SiteStability Stability
    {
        get => stability;
        set
        {
            if (SetProperty(ref stability, value))
            {
                HasUncommittedChanges = true;
                UpdateStabilityWarning();
                RefreshPreview();
            }
        }
    }

    /// <summary>
    /// Gets the stability choices.
    /// </summary>
    public IReadOnlyList<SiteStability> StabilityChoices { get; }

    /// <summary>
    /// Gets the particle-set reference picker.
    /// </summary>
    public ReferencePickerViewModel<ParticleSetId> ParticleSetPicker { get; }

    /// <summary>
    /// Gets the parse/selection error for the form fields.
    /// </summary>
    public string? FieldError
    {
        get => fieldError;
        private set
        {
            if (SetProperty(ref fieldError, value))
            {
                OnPropertyChanged(nameof(HasFieldError));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the form has a field error.
    /// </summary>
    public bool HasFieldError => !string.IsNullOrWhiteSpace(FieldError);

    /// <summary>
    /// Gets the candidate diagnostics text.
    /// </summary>
    public string DiagnosticsText => candidateDiagnostics.IsDefaultOrEmpty
        ? "No symmetry or structure problems in the candidate."
        : string.Join(
            "\n",
            candidateDiagnostics.Select(static diagnostic =>
                string.Create(CultureInfo.InvariantCulture, $"{diagnostic.Code}: {diagnostic.Message}")));

    /// <summary>
    /// Gets a value indicating whether the candidate has an error.
    /// </summary>
    public bool HasCandidateErrors => candidateDiagnostics.Any(
        static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Gets a value indicating whether the candidate reports an orbit overlap.
    /// </summary>
    public bool HasOverlap => candidateDiagnostics.Any(
        static diagnostic => string.Equals(diagnostic.Code, "STR007", StringComparison.Ordinal));

    /// <summary>
    /// Gets the orbit size (the number of P1 positions in the site's orbit).
    /// </summary>
    public int OrbitSize { get; private set; }

    /// <summary>
    /// Gets the orbit multiplicity from the preview orbit info.
    /// </summary>
    public int Multiplicity { get; private set; }

    /// <summary>
    /// Gets the representative Cartesian position text.
    /// </summary>
    public string CartesianText { get; private set; } = "—";

    /// <summary>
    /// Gets the candidate preview status.
    /// </summary>
    public string PreviewStatus { get; private set; } = "No preview available.";

    /// <summary>
    /// Gets or sets a value indicating whether the P1 preview panel is expanded.
    /// </summary>
    public bool ShowP1Preview
    {
        get => showP1Preview;
        set => SetProperty(ref showP1Preview, value);
    }

    /// <summary>
    /// Gets the projection plane choices.
    /// </summary>
    public IReadOnlyList<string> PlaneChoices { get; } = ["a–b", "a–c", "b–c"];

    /// <summary>
    /// Gets or sets the selected projection plane.
    /// </summary>
    public string SelectedPlane
    {
        get => selectedPlane;
        set
        {
            if (SetProperty(ref selectedPlane, value))
            {
                RebuildProjection();
            }
        }
    }

    /// <summary>
    /// Gets the selected site's orbit rows.
    /// </summary>
    public ObservableCollection<SiteOrbitRowViewModel> OrbitRows { get; } = [];

    /// <summary>
    /// Gets the whole-structure P1 rows in canonical P order.
    /// </summary>
    public ObservableCollection<SiteWholeStructureRowViewModel> WholeStructureRows { get; } = [];

    /// <summary>
    /// Gets the deterministic 2D projection points.
    /// </summary>
    public ObservableCollection<SiteProjectionPointViewModel> ProjectionPoints { get; } = [];

    /// <summary>
    /// Gets the usage-index dependents of the selected site.
    /// </summary>
    public ImmutableArray<ModelUsageEntry> Dependents
    {
        get => dependents;
        private set
        {
            if (SetProperty(ref dependents, value))
            {
                OnPropertyChanged(nameof(HasDependents));
                OnPropertyChanged(nameof(CanDeleteEnergyOnly));
                OnPropertyChanged(nameof(EnergyDeleteText));
                RequestEnergyDeleteCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the site has any dependent.
    /// </summary>
    public bool HasDependents => !Dependents.IsDefaultOrEmpty;

    /// <summary>
    /// Gets a value indicating whether every dependent is an energy reference.
    /// </summary>
    public bool CanDeleteEnergyOnly =>
        !IsNew
        && HasDependents
        && Dependents.All(static entry => entry.IsEnergyReference);

    /// <summary>
    /// Gets the confirmation text for the energy-reference-only cascade.
    /// </summary>
    public string EnergyDeleteText => CanDeleteEnergyOnly
        ? "This removes the site and its energy references: "
            + string.Join(", ", Dependents.Select(static entry => entry.DisplayName)) + "."
        : "The energy-reference-only action is offered only when every dependent is an energy reference.";

    /// <summary>
    /// Gets a value indicating whether an energy-reference delete awaits confirmation.
    /// </summary>
    public bool HasPendingEnergyDelete => !string.IsNullOrWhiteSpace(pendingEnergyDelete);

    /// <summary>
    /// Gets the stability warning when an MMC/KMC transition uses the site.
    /// </summary>
    public string? StabilityWarning
    {
        get => stabilityWarning;
        private set
        {
            if (SetProperty(ref stabilityWarning, value))
            {
                OnPropertyChanged(nameof(HasStabilityWarning));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a stability warning is present.
    /// </summary>
    public bool HasStabilityWarning => !string.IsNullOrWhiteSpace(StabilityWarning);

    /// <summary>
    /// Gets a value indicating whether the form fields are editable.
    /// </summary>
    public bool CanEditForm => Coordinator?.CanEdit == true && !IsRemoved;

    /// <inheritdoc />
    public override bool CanApply => base.CanApply && fieldsValid && !HasCandidateErrors;

    /// <summary>
    /// Gets the command that opens one dependent's navigation target.
    /// </summary>
    public IRelayCommand<ModelUsageEntry> OpenDependentCommand { get; }

    /// <summary>
    /// Gets the command that requests the energy-reference-only delete.
    /// </summary>
    public IRelayCommand RequestEnergyDeleteCommand { get; }

    /// <summary>
    /// Gets the command that confirms the energy-reference delete.
    /// </summary>
    public IRelayCommand ConfirmEnergyDeleteCommand { get; }

    /// <summary>
    /// Gets the command that cancels the energy-reference delete.
    /// </summary>
    public IRelayCommand CancelEnergyDeleteCommand { get; }

    /// <inheritdoc />
    protected override string Description => IsNew ? "Add cell site" : "Update cell site";

    /// <inheritdoc />
    protected override ProjectEditResult ApplyCore(ProjectEditor editor, ProjectModel projectSnapshot) =>
        TryBuildSite(out var site)
            ? IsNew
                ? editor.AddCellSite(projectSnapshot, site)
                : editor.UpdateCellSite(projectSnapshot, site)
            : new ProjectEditResult(projectSnapshot, null, []);

    /// <inheritdoc />
    protected override void RevertCore()
    {
        var site = Coordinator?.Document.Project.Find(CellSiteId.Create(ObjectId));
        if (site is null)
        {
            return;
        }

        name = site.Name;
        fieldA = Format(site.Fractional.A);
        fieldB = Format(site.Fractional.B);
        fieldC = Format(site.Fractional.C);
        stability = site.Stability;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(FieldA));
        OnPropertyChanged(nameof(FieldB));
        OnPropertyChanged(nameof(FieldC));
        OnPropertyChanged(nameof(Stability));
        RefreshParticleSetOptions(site.ParticleSetId);
        RefreshPreview();
    }

    /// <inheritdoc />
    protected override void RefreshOptionsCore(ProjectModel projectSnapshot)
    {
        project = projectSnapshot;
        RefreshParticleSetOptions(ParticleSetPicker.Selected?.Value ?? default);
        RefreshDependents();
        RefreshPreview();
    }

    /// <inheritdoc />
    protected override void OnAttached()
    {
        OnPropertyChanged(nameof(CanEditForm));
        RefreshDependents();
        RefreshPreview();
    }

    /// <inheritdoc />
    protected override void OnCanApplyChanged() => OnPropertyChanged(nameof(CanApply));

    /// <inheritdoc />
    protected override void OnCommitted(EditApplyResult result)
    {
        if (IsNew && result.Outcome == EditApplyOutcome.Applied)
        {
            Added?.Invoke(this, ObjectId);
        }
    }

    /// <summary>
    /// Requests the energy-reference-only delete confirmation.
    /// </summary>
    public void RequestEnergyDelete()
    {
        if (CanDeleteEnergyOnly)
        {
            pendingEnergyDelete = "confirm";
            OnPropertyChanged(nameof(HasPendingEnergyDelete));
            ConfirmEnergyDeleteCommand.NotifyCanExecuteChanged();
            CancelEnergyDeleteCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Applies the energy-reference-only delete as one undo entry.
    /// </summary>
    public void ConfirmEnergyDelete()
    {
        if (!CanDeleteEnergyOnly)
        {
            return;
        }

        pendingEnergyDelete = null;
        OnPropertyChanged(nameof(HasPendingEnergyDelete));
        ConfirmEnergyDeleteCommand.NotifyCanExecuteChanged();
        CancelEnergyDeleteCommand.NotifyCanExecuteChanged();
        CommitOperation(
            "Delete site and energy references",
            (editor, projectSnapshot) => editor.RemoveCellSiteAndEnergyReferences(
                projectSnapshot,
                CellSiteId.Create(ObjectId)));
    }

    /// <summary>
    /// Cancels the energy-reference delete confirmation.
    /// </summary>
    public void CancelEnergyDelete()
    {
        pendingEnergyDelete = null;
        OnPropertyChanged(nameof(HasPendingEnergyDelete));
        ConfirmEnergyDeleteCommand.NotifyCanExecuteChanged();
        CancelEnergyDeleteCommand.NotifyCanExecuteChanged();
    }

    private void OnParticleSetChanged(object? sender, ReferenceOption<ParticleSetId>? option)
    {
        if (refreshingOptions)
        {
            return;
        }

        HasUncommittedChanges = true;
        RefreshPreview();
    }

    private void RefreshParticleSetOptions(ParticleSetId selected)
    {
        refreshingOptions = true;
        try
        {
            var options = project.Model.ParticleSets
                .Select(set => new ReferenceOption<ParticleSetId>(set.Id, set.Name, null))
                .ToArray();
            ParticleSetPicker.SetOptions(options);
            var choice = options.FirstOrDefault(option => option.Value == selected)
                ?? options.FirstOrDefault();
            ParticleSetPicker.Selected = choice;
        }
        finally
        {
            refreshingOptions = false;
        }
    }

    private bool TryBuildSite(out CellSite site)
    {
        site = null!;
        if (string.IsNullOrWhiteSpace(Name))
        {
            FieldError = "STR005: the site name must not be empty.";
            fieldsValid = false;
            return false;
        }

        if (project.Structure.Sites.Any(candidate =>
                candidate.Id.Value != ObjectId
                && string.Equals(candidate.Name, Name, StringComparison.OrdinalIgnoreCase)))
        {
            FieldError = "STR005: the site name is used more than once (case-insensitively).";
            fieldsValid = false;
            return false;
        }

        if (!TryParse(FieldA, "a", out var a)
            || !TryParse(FieldB, "b", out var b)
            || !TryParse(FieldC, "c", out var c))
        {
            fieldsValid = false;
            return false;
        }

        if (a is < 0d or >= 1d || b is < 0d or >= 1d || c is < 0d or >= 1d)
        {
            FieldError = "STR004: each fractional coordinate must be finite and inside [0, 1).";
            fieldsValid = false;
            return false;
        }

        if (ParticleSetPicker.Selected is not { } set || set.Value == ParticleSetId.Empty)
        {
            FieldError = "STR006: select a particle set for the site.";
            fieldsValid = false;
            return false;
        }

        FieldError = null;
        fieldsValid = true;
        site = new CellSite(
            CellSiteId.Create(ObjectId),
            Name,
            new Fractional3(a, b, c),
            set.Value,
            Stability);
        return true;
    }

    private bool TryParse(string text, string name, out double value)
    {
        if (NumericExpression.TryParse(text, out value, out var error))
        {
            return true;
        }

        FieldError = $"The fractional {name} is invalid: {error}";
        return false;
    }

    private void RefreshPreview()
    {
        if (refreshing)
        {
            return;
        }

        refreshing = true;
        try
        {
            if (!TryBuildSite(out var site))
            {
                candidatePreview = null;
                candidateDiagnostics = [];
                currentPreview = TryCreatePreview(project.Structure);
                previewSites = project.Structure.Sites.IsDefault
                    ? ImmutableArray<CellSite>.Empty
                    : project.Structure.Sites;
                RebuildOrbitRows();
                RebuildWholeStructureRows();
                RebuildProjection();
                PreviewStatus = HasFieldError
                    ? "The site form is incomplete."
                    : "No preview available.";
                RefreshDerived();
                return;
            }

            var structure = project.Structure;
            var sites = structure.Sites.IsDefault ? ImmutableArray<CellSite>.Empty : structure.Sites;
            var candidateSites = IsNew
                ? sites.Add(site)
                : sites.Select(candidate => candidate.Id == site.Id ? site : candidate).ToImmutableArray();
            var candidateStructure = structure with { Sites = candidateSites };
            previewSites = candidateSites;
            var validation = validator.Validate(candidateStructure);
            candidatePreview = previewService.TryCreatePreview(
                candidateStructure,
                1,
                1,
                1,
                out var previewDiagnostics);
            candidateDiagnostics = validation
                .Concat(previewDiagnostics)
                .Distinct()
                .OrderBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .ToImmutableArray();
            currentPreview = TryCreatePreview(structure);
            RebuildOrbitRows();
            RebuildWholeStructureRows();
            RebuildProjection();
            PreviewStatus = candidatePreview is not null
                ? "Candidate preview."
                : HasOverlap
                    ? "No candidate preview: the candidate overlaps (STR007); showing the current structure."
                    : "The candidate has errors; showing the current structure.";
            RefreshDerived();
        }
        finally
        {
            refreshing = false;
        }
    }

    private void RefreshDependents()
    {
        if (IsNew || Coordinator is null)
        {
            Dependents = [];
            StabilityWarning = null;
            return;
        }

        var index = ModelUsageIndex.Create(Coordinator.Document.Project);
        Dependents = index.GetDependents(ModelObjectRef.CellSite(CellSiteId.Create(ObjectId)));
        UpdateStabilityWarning();
    }

    private void UpdateStabilityWarning()
    {
        if (IsNew || Dependents.IsDefaultOrEmpty)
        {
            StabilityWarning = null;
            return;
        }

        var used = Dependents
            .Where(static entry => entry.Kind == ModelUsageKind.Transition)
            .Select(static entry => entry.DisplayName)
            .Distinct()
            .ToArray();
        StabilityWarning = used.Length == 0
            ? null
            : "Changing stability can invalidate TRN012/TRN020 because these MMC/KMC transitions use the site: "
                + string.Join(", ", used) + ". The Problems channel reports the residual diagnostics after the edit.";
    }

    private void RefreshDerived()
    {
        OnPropertyChanged(nameof(DiagnosticsText));
        OnPropertyChanged(nameof(HasCandidateErrors));
        OnPropertyChanged(nameof(HasOverlap));
        OnPropertyChanged(nameof(OrbitSize));
        OnPropertyChanged(nameof(Multiplicity));
        OnPropertyChanged(nameof(CartesianText));
        OnPropertyChanged(nameof(PreviewStatus));
        OnPropertyChanged(nameof(CanEditForm));
        NotifyCanApplyChanged();
        RequestEnergyDeleteCommand.NotifyCanExecuteChanged();
        ConfirmEnergyDeleteCommand.NotifyCanExecuteChanged();
        CancelEnergyDeleteCommand.NotifyCanExecuteChanged();
    }

    private static string Format(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string FormatFractional(Fractional3 value) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"({value.A:0.####}, {value.B:0.####}, {value.C:0.####})");

    private static string FormatCartesian(Cartesian3 value) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"({value.X:0.####}, {value.Y:0.####}, {value.Z:0.####}) Å");
}
