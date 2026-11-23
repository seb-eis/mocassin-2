// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Projects;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Structure;
using Mocassin.Symmetry.SpaceGroups;
using Mocassin.Symmetry.Structure;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors.Structure;

/// <summary>
/// One operation row in the space-group details panel.
/// </summary>
/// <param name="Number">The one-based operation index.</param>
/// <param name="MatrixText">The linear matrix.</param>
/// <param name="TranslationText">The exact translation.</param>
/// <param name="HumanReadableText">The human-readable affine form.</param>
internal sealed record SpaceGroupOperationViewModel(
    int Number,
    string MatrixText,
    string TranslationText,
    string HumanReadableText);

/// <summary>
/// One site's current versus candidate orbit size in the space-group compatibility preview.
/// </summary>
/// <param name="SiteName">The reference-site name.</param>
/// <param name="CurrentMultiplicity">The current orbit multiplicity, when a preview exists.</param>
/// <param name="CandidateMultiplicity">The candidate orbit multiplicity, when a preview exists.</param>
/// <param name="HasOverlap">Whether the candidate reports an orbit overlap.</param>
internal sealed record SpaceGroupOrbitPreviewViewModel(
    string SiteName,
    int? CurrentMultiplicity,
    int? CandidateMultiplicity,
    bool HasOverlap)
{
    /// <summary>
    /// Gets the change text.
    /// </summary>
    public string ChangeText => HasOverlap
        ? "overlap"
        : CurrentMultiplicity is { } current && CandidateMultiplicity is { } candidate
            ? current == candidate ? $"{current}" : $"{current} → {candidate}"
            : "unavailable";
}

/// <summary>
/// The space-group form: searchable picker, axis/origin settings, operation details, compatibility/orbit preview,
/// and the one-undo-entry apply/cancel.
/// </summary>
internal sealed class SpaceGroupEditorViewModel : ObservableObject, IDisposable
{
    private readonly ProjectSession session;
    private readonly IStructurePreviewService previewService;
    private readonly IStructureValidator validator;
    private readonly ProjectEditCoordinator coordinator;
    private SpaceGroupSetting? currentSetting;
    private SpaceGroupSetting? selectedSetting;
    private string searchText = string.Empty;
    private ImmutableArray<Diagnostic> candidateDiagnostics = [];
    private StructurePreview? candidatePreview;
    private string? errorMessage;
    private bool disposed;
    private bool refreshing;

    /// <summary>
    /// Initializes the space-group form.
    /// </summary>
    /// <param name="session">The project session.</param>
    /// <param name="previewService">The structure preview service.</param>
    /// <param name="validator">The structure validator.</param>
    public SpaceGroupEditorViewModel(
        ProjectSession session,
        IStructurePreviewService previewService,
        IStructureValidator validator)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.previewService = previewService ?? throw new ArgumentNullException(nameof(previewService));
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        coordinator = session.EditCoordinator;
        ApplyCommand = new RelayCommand(() => Apply(), () => CanApply);
        CancelCommand = new RelayCommand(
            Cancel,
            () => selectedSetting is not null
                && (currentSetting is null || !IsSameSetting(selectedSetting, currentSetting)));

        currentSetting = Resolve(Project.Structure.SpaceGroup);
        selectedSetting = currentSetting ?? SpaceGroupCatalog.Default.Settings[0];
        FilterResults();
        RebuildGroupSettings();
        RefreshPreview();
        coordinator.ProjectChanged += OnProjectChanged;
    }

    private ProjectModel Project => session.Project;

    /// <summary>
    /// Gets the searchable catalog results.
    /// </summary>
    public System.Collections.ObjectModel.ObservableCollection<SpaceGroupSetting> Results { get; } = [];

    /// <summary>
    /// Gets the settings for the selected group number (axis/origin choices).
    /// </summary>
    public System.Collections.ObjectModel.ObservableCollection<SpaceGroupSetting> GroupSettings { get; } = [];

    /// <summary>
    /// Gets the operation rows for the selected setting.
    /// </summary>
    public System.Collections.ObjectModel.ObservableCollection<SpaceGroupOperationViewModel> Operations { get; } = [];

    /// <summary>
    /// Gets the per-site orbit compatibility rows.
    /// </summary>
    public System.Collections.ObjectModel.ObservableCollection<SpaceGroupOrbitPreviewViewModel> Orbits { get; } = [];

    /// <summary>
    /// Gets or sets the search text; number, Hermann–Mauguin, Hall symbol, crystal system, axis, and origin all
    /// match.
    /// </summary>
    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value))
            {
                FilterResults();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected catalog setting.
    /// </summary>
    public SpaceGroupSetting? SelectedSetting
    {
        get => selectedSetting;
        set
        {
            if (value is null || !SetProperty(ref selectedSetting, value))
            {
                return;
            }

            RebuildGroupSettings();
            RefreshPreview();
        }
    }

    /// <summary>
    /// Gets the current committed setting text.
    /// </summary>
    public string CurrentSettingText => currentSetting is null
        ? "Unresolved"
        : Describe(currentSetting);

    /// <summary>
    /// Gets the selected setting text.
    /// </summary>
    public string SelectedSettingText => selectedSetting is null ? "—" : Describe(selectedSetting);

    /// <summary>
    /// Gets the operation count.
    /// </summary>
    public int OperationCount => selectedSetting?.Operations.Length ?? 0;

    /// <summary>
    /// Gets the general multiplicity (the catalog operation count for a general position).
    /// </summary>
    public int GeneralMultiplicity => OperationCount;

    /// <summary>
    /// Gets the crystal system text.
    /// </summary>
    public string CrystalSystemText => selectedSetting?.CrystalSystem.ToString() ?? "—";

    /// <summary>
    /// Gets the compatibility summary.
    /// </summary>
    public string CompatibilityText
    {
        get
        {
            if (selectedSetting is null)
            {
                return "Select a space group.";
            }

            if (currentSetting is not null && IsSameSetting(selectedSetting, currentSetting))
            {
                return "This is the current space group.";
            }

            return HasErrors
                ? $"Incompatible — {ErrorCount} error(s)."
                : candidatePreview is null
                    ? "The candidate preview is unavailable."
                    : "Compatible with the current sites and cell.";
        }
    }

    /// <summary>
    /// Gets the candidate diagnostics text.
    /// </summary>
    public string DiagnosticsText => candidateDiagnostics.IsDefaultOrEmpty
        ? "No symmetry or cell problems."
        : string.Join(
            "\n",
            candidateDiagnostics.Select(static diagnostic =>
                string.Create(CultureInfo.InvariantCulture, $"{diagnostic.Code}: {diagnostic.Message}")));

    /// <summary>
    /// Gets a value indicating whether the candidate has an error.
    /// </summary>
    public bool HasErrors => ErrorCount > 0;

    /// <summary>
    /// Gets the candidate error count.
    /// </summary>
    public int ErrorCount => candidateDiagnostics.Count(
        static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Gets a value indicating whether the candidate reports an orbit overlap.
    /// </summary>
    public bool HasOverlap => candidateDiagnostics.Any(
        static diagnostic => string.Equals(diagnostic.Code, "STR007", StringComparison.Ordinal));

    /// <summary>
    /// Gets the inline apply error.
    /// </summary>
    public string? ErrorMessage
    {
        get => errorMessage;
        private set
        {
            if (SetProperty(ref errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether an inline error is present.
    /// </summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>
    /// Gets a value indicating whether the session may edit.
    /// </summary>
    public bool CanEdit => coordinator.CanEdit;

    /// <summary>
    /// Gets a value indicating whether the candidate can be applied.
    /// </summary>
    public bool CanApply =>
        CanEdit
        && selectedSetting is not null
        && (currentSetting is null || !IsSameSetting(selectedSetting, currentSetting))
        && !HasErrors;

    /// <summary>
    /// Gets the command that applies the candidate as one undo entry.
    /// </summary>
    public IRelayCommand ApplyCommand { get; }

    /// <summary>
    /// Gets the command that discards the candidate.
    /// </summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>
    /// Selects a catalog setting and refreshes the preview.
    /// </summary>
    /// <param name="setting">The setting.</param>
    public void Select(SpaceGroupSetting setting) => SelectedSetting = setting;

    /// <summary>
    /// Applies the selected setting through the coordinator.
    /// </summary>
    /// <returns>The app-level result, or <see langword="null"/> when nothing is applicable.</returns>
    public EditApplyResult? Apply()
    {
        if (!CanApply || selectedSetting is null)
        {
            return null;
        }

        var reference = new SpaceGroupRef(selectedSetting.Number, selectedSetting.Axis, selectedSetting.Origin);
        var result = coordinator.Apply(
            "Set space group",
            (editor, project) => editor.SetSpaceGroup(project, reference));
        ErrorMessage = result.Outcome == EditApplyOutcome.Applied
            ? null
            : result.Message ?? "The space-group change was rejected.";
        return result;
    }

    /// <summary>
    /// Discards the candidate and selects the current setting.
    /// </summary>
    public void Cancel()
    {
        ErrorMessage = null;
        if (currentSetting is not null)
        {
            SelectedSetting = currentSetting;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        coordinator.ProjectChanged -= OnProjectChanged;
    }

    private void FilterResults()
    {
        var term = SearchText.Trim();
        var settings = SpaceGroupCatalog.Default.Settings;
        var filtered = string.IsNullOrWhiteSpace(term)
            ? settings
            : settings.Where(setting => Matches(setting, term)).ToImmutableArray();
        Results.Clear();
        foreach (var setting in filtered)
        {
            Results.Add(setting);
        }
    }

    private static bool Matches(SpaceGroupSetting setting, string term)
    {
        if (int.TryParse(term, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            && setting.Number == number)
        {
            return true;
        }

        return setting.Number.ToString(CultureInfo.InvariantCulture).Contains(term, StringComparison.Ordinal)
            || setting.HermannMauguin.Contains(term, StringComparison.OrdinalIgnoreCase)
            || setting.HallSymbol.Contains(term, StringComparison.OrdinalIgnoreCase)
            || setting.CrystalSystem.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
            || setting.Axis.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
            || setting.Origin.ToString().Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private void RebuildGroupSettings()
    {
        GroupSettings.Clear();
        if (selectedSetting is null)
        {
            return;
        }

        foreach (var setting in SpaceGroupCatalog.Default.Settings.Where(
                     setting => setting.Number == selectedSetting.Number))
        {
            GroupSettings.Add(setting);
        }
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
            currentSetting = Resolve(Project.Structure.SpaceGroup);
            if (selectedSetting is null)
            {
                return;
            }

            var candidateReference = new SpaceGroupRef(
                selectedSetting.Number,
                selectedSetting.Axis,
                selectedSetting.Origin);
            var candidateStructure = Project.Structure with { SpaceGroup = candidateReference };
            var candidateDiagnostics = validator.Validate(candidateStructure);
            candidatePreview = previewService.TryCreatePreview(
                candidateStructure,
                1,
                1,
                1,
                out var previewDiagnostics);
            this.candidateDiagnostics = candidateDiagnostics
                .Concat(previewDiagnostics)
                .Distinct()
                .OrderBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .ToImmutableArray();

            var currentPreview = previewService.TryCreatePreview(
                Project.Structure,
                1,
                1,
                1,
                out _);
            RebuildOperations();
            RebuildOrbits(currentPreview);
            ErrorMessage = null;
        }
        finally
        {
            refreshing = false;
        }

        NotifyDerived();
    }

    private void RebuildOperations()
    {
        Operations.Clear();
        if (selectedSetting is null)
        {
            return;
        }

        for (var index = 0; index < selectedSetting.Operations.Length; index++)
        {
            var operation = selectedSetting.Operations[index];
            Operations.Add(new SpaceGroupOperationViewModel(
                index + 1,
                operation.Linear.ToString(),
                operation.Translation.ToString(),
                HumanReadable(operation)));
        }
    }

    private void RebuildOrbits(StructurePreview? currentPreview)
    {
        Orbits.Clear();
        var sites = Project.Structure.Sites.IsDefault
            ? ImmutableArray<CellSite>.Empty
            : Project.Structure.Sites;
        var overlap = HasOverlap;
        for (var index = 0; index < sites.Length; index++)
        {
            var current = index < (currentPreview?.Orbits.Length ?? 0)
                ? currentPreview!.Orbits[index].Multiplicity
                : (int?)null;
            var candidate = candidatePreview is not null && index < candidatePreview.Orbits.Length
                ? candidatePreview.Orbits[index].Multiplicity
                : (int?)null;
            Orbits.Add(new SpaceGroupOrbitPreviewViewModel(
                sites[index].Name,
                current,
                candidate,
                overlap));
        }
    }

    private void OnProjectChanged(object? sender, ProjectChangedEventArgs args)
    {
        if (args.Kind == ProjectChangeKind.Saved)
        {
            return;
        }

        if (args.Kind == ProjectChangeKind.Replaced)
        {
            currentSetting = Resolve(Project.Structure.SpaceGroup);
            selectedSetting = currentSetting;
            RebuildGroupSettings();
            OnPropertyChanged(nameof(SelectedSetting));
        }

        RefreshPreview();
    }

    private SpaceGroupSetting? Resolve(SpaceGroupRef reference) =>
        previewService.TryResolve(reference, out _);

    private void NotifyDerived()
    {
        OnPropertyChanged(nameof(CurrentSettingText));
        OnPropertyChanged(nameof(SelectedSettingText));
        OnPropertyChanged(nameof(OperationCount));
        OnPropertyChanged(nameof(GeneralMultiplicity));
        OnPropertyChanged(nameof(CrystalSystemText));
        OnPropertyChanged(nameof(CompatibilityText));
        OnPropertyChanged(nameof(DiagnosticsText));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(HasOverlap));
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(GroupSettings));
        ApplyCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }

    private static bool IsSameSetting(SpaceGroupSetting left, SpaceGroupSetting right) =>
        left.Number == right.Number && left.Axis == right.Axis && left.Origin == right.Origin;

    private static string Describe(SpaceGroupSetting setting) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{setting.Number} — {setting.HermannMauguin} ({setting.CrystalSystem}; {setting.Axis}; {setting.Origin})");

    private static string HumanReadable(SymmetryOperation operation) =>
        string.Join(
            ", ",
            Row(operation.Linear.M11, operation.Linear.M12, operation.Linear.M13, operation.Translation.A),
            Row(operation.Linear.M21, operation.Linear.M22, operation.Linear.M23, operation.Translation.B),
            Row(operation.Linear.M31, operation.Linear.M32, operation.Linear.M33, operation.Translation.C));

    private static string Row(
        int x,
        int y,
        int z,
        int translationNumerator)
    {
        var builder = new StringBuilder();
        AppendTerm(builder, x, "x");
        AppendTerm(builder, y, "y");
        AppendTerm(builder, z, "z");
        if (builder.Length == 0)
        {
            builder.Append('0');
        }

        if (translationNumerator != 0)
        {
            builder.Append(" + ");
            builder.Append(Fraction(translationNumerator));
        }

        return builder.ToString();
    }

    private static void AppendTerm(StringBuilder builder, int coefficient, string variable)
    {
        if (coefficient == 0)
        {
            return;
        }

        var magnitude = Math.Abs(coefficient);
        var term = magnitude == 1 ? variable : string.Create(CultureInfo.InvariantCulture, $"{magnitude}{variable}");
        if (builder.Length == 0)
        {
            if (coefficient < 0)
            {
                builder.Append('-');
            }

            builder.Append(term);
            return;
        }

        builder.Append(coefficient < 0 ? " - " : " + ");
        builder.Append(term);
    }

    private static string Fraction(int numerator)
    {
        var divisor = GreatestCommonDivisor(Math.Abs(numerator), Rational3.Denominator);
        var reduced = numerator / divisor;
        var denominator = Rational3.Denominator / divisor;
        return denominator == 1
            ? reduced.ToString(CultureInfo.InvariantCulture)
            : string.Create(CultureInfo.InvariantCulture, $"{reduced}/{denominator}");
    }

    private static int GreatestCommonDivisor(int left, int right)
    {
        while (right != 0)
        {
            (left, right) = (right, left % right);
        }

        return Math.Max(1, left);
    }
}
