// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Editors.Expressions;
using Mocassin.App.ViewModels.Projects;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using Mocassin.Symmetry.Geometry;
using Mocassin.Symmetry.SpaceGroups;
using Mocassin.Symmetry.Structure;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors.Structure;

/// <summary>
/// The cell-parameter form: six expression fields with crystal-system locks, a live <c>CellGeometry</c>, the
/// violated geometry inequality on failure, and undoable reset actions.
/// </summary>
internal sealed class CellParameterEditorViewModel : ObservableObject, IDisposable
{
    private readonly ProjectSession session;
    private readonly IStructurePreviewService previewService;
    private readonly ProjectEditCoordinator coordinator;
    private readonly Dictionary<CellFieldKind, CellFieldViewModel> byKind = [];
    private CellParameterLockTable locks = CellParameterLocks.For(CrystalSystem.Triclinic, AxisSetting.None);
    private SpaceGroupSetting? setting;
    private CellGeometry? geometry;
    private ImmutableArray<Diagnostic> diagnostics = [];
    private string? errorMessage;
    private string? conflictMessage;
    private string? pendingReset;
    private bool hasUncommittedChanges;
    private bool recalculating;
    private bool refreshing;
    private bool disposed;

    /// <summary>
    /// Initializes the cell-parameter form.
    /// </summary>
    /// <param name="session">The project session.</param>
    /// <param name="previewService">The structure-preview service used to resolve the setting.</param>
    public CellParameterEditorViewModel(ProjectSession session, IStructurePreviewService previewService)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.previewService = previewService ?? throw new ArgumentNullException(nameof(previewService));
        coordinator = session.EditCoordinator;

        Fields =
        [
            Field(CellFieldKind.A, "a", "Å"),
            Field(CellFieldKind.B, "b", "Å"),
            Field(CellFieldKind.C, "c", "Å"),
            Field(CellFieldKind.Alpha, "α", "°"),
            Field(CellFieldKind.Beta, "β", "°"),
            Field(CellFieldKind.Gamma, "γ", "°"),
        ];
        ApplyCommand = new RelayCommand(() => Apply(), () => CanApply);
        RevertCommand = new RelayCommand(RefreshFromProject, () => CanEdit);
        RequestResetToCubicCommand = new RelayCommand(
            () => RequestReset("cubic"),
            () => CanEdit && CanResetToCubic);
        RequestResetToDefaultsCommand = new RelayCommand(
            () => RequestReset("defaults"),
            () => CanEdit);
        ConfirmResetCommand = new RelayCommand(ConfirmReset, () => HasPendingReset && CanEdit);
        CancelResetCommand = new RelayCommand(CancelReset, () => HasPendingReset);

        coordinator.ProjectChanged += OnProjectChanged;
        RefreshFromProject();
    }

    private ProjectModel Project => session.Project;

    /// <summary>
    /// Gets the six fields in display order.
    /// </summary>
    public ObservableCollection<CellFieldViewModel> Fields { get; }

    /// <summary>
    /// Gets the resolved crystal-system text.
    /// </summary>
    public string CrystalSystemText => setting?.CrystalSystem.ToString() ?? "Unresolved space group";

    /// <summary>
    /// Gets the resolved axis-setting text.
    /// </summary>
    public string AxisText => setting?.Axis.ToString() ?? "—";

    /// <summary>
    /// Gets the live validated geometry, or <see langword="null"/> when the form is invalid.
    /// </summary>
    public CellGeometry? Geometry => geometry;

    /// <summary>
    /// Gets the live cell vectors and volume text.
    /// </summary>
    public string VectorsText => geometry is null
        ? "The cell geometry is not valid."
        : string.Create(
            CultureInfo.InvariantCulture,
            $"a = ({geometry.A.X:0.####}, {geometry.A.Y:0.####}, {geometry.A.Z:0.####}) Å\n"
            + $"b = ({geometry.B.X:0.####}, {geometry.B.Y:0.####}, {geometry.B.Z:0.####}) Å\n"
            + $"c = ({geometry.C.X:0.####}, {geometry.C.Y:0.####}, {geometry.C.Z:0.####}) Å");

    /// <summary>
    /// Gets the live volume text.
    /// </summary>
    public string VolumeText => geometry is null
        ? "—"
        : string.Create(CultureInfo.InvariantCulture, $"{geometry.VolumeAngstrom3:0.####} Å³");

    /// <summary>
    /// Gets the geometry diagnostics text (the violated inequality is the diagnostic message).
    /// </summary>
    public string DiagnosticsText => diagnostics.IsDefaultOrEmpty
        ? "The cell geometry is valid."
        : string.Join(
            "\n",
            diagnostics.Select(static diagnostic =>
                string.Create(CultureInfo.InvariantCulture, $"{diagnostic.Code}: {diagnostic.Message}")));

    /// <summary>
    /// Gets a value indicating whether the candidate has an error.
    /// </summary>
    public bool HasErrors => diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Gets the inline commit error.
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
    /// Gets the conflict message when the structure changed elsewhere while this form held drafts.
    /// </summary>
    public string? ConflictMessage
    {
        get => conflictMessage;
        private set
        {
            if (SetProperty(ref conflictMessage, value))
            {
                OnPropertyChanged(nameof(HasConflict));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a conflict message is present.
    /// </summary>
    public bool HasConflict => !string.IsNullOrWhiteSpace(ConflictMessage);

    /// <summary>
    /// Gets a value indicating whether the form holds uncommitted drafts.
    /// </summary>
    public bool HasUncommittedChanges
    {
        get => hasUncommittedChanges;
        private set => SetProperty(ref hasUncommittedChanges, value);
    }

    /// <summary>
    /// Gets a value indicating whether the form can be committed.
    /// </summary>
    public bool CanApply => CanEdit && geometry is not null && !HasErrors
        && Fields.All(static cellField => !cellField.HasError);

    /// <summary>
    /// Gets a value indicating whether the session may edit.
    /// </summary>
    public bool CanEdit => coordinator.CanEdit;

    /// <summary>
    /// Gets a value indicating whether the reset-to-cubic action applies.
    /// </summary>
    public bool CanResetToCubic => setting?.CrystalSystem == CrystalSystem.Cubic;

    /// <summary>
    /// Gets the pending reset kind, or <see langword="null"/>.
    /// </summary>
    public string? PendingReset
    {
        get => pendingReset;
        private set
        {
            if (SetProperty(ref pendingReset, value))
            {
                OnPropertyChanged(nameof(HasPendingReset));
                OnPropertyChanged(nameof(PendingResetText));
                ConfirmResetCommand.NotifyCanExecuteChanged();
                CancelResetCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a reset awaits confirmation.
    /// </summary>
    public bool HasPendingReset => !string.IsNullOrWhiteSpace(PendingReset);

    /// <summary>
    /// Gets the reset confirmation text.
    /// </summary>
    public string PendingResetText => PendingReset switch
    {
        "cubic" => "Reset the cell to a cubic cell with a = b = c = 5 Å and 90° angles?",
        "defaults" => "Reset the cell to the 5 Å default with the current crystal-system locks?",
        _ => string.Empty,
    };

    /// <summary>
    /// Gets the command that commits the form.
    /// </summary>
    public IRelayCommand ApplyCommand { get; }

    /// <summary>
    /// Gets the command that restores the committed values.
    /// </summary>
    public IRelayCommand RevertCommand { get; }

    /// <summary>
    /// Gets the command that requests the cubic reset.
    /// </summary>
    public IRelayCommand RequestResetToCubicCommand { get; }

    /// <summary>
    /// Gets the command that requests the defaults reset.
    /// </summary>
    public IRelayCommand RequestResetToDefaultsCommand { get; }

    /// <summary>
    /// Gets the command that confirms the pending reset.
    /// </summary>
    public IRelayCommand ConfirmResetCommand { get; }

    /// <summary>
    /// Gets the command that cancels the pending reset.
    /// </summary>
    public IRelayCommand CancelResetCommand { get; }

    /// <summary>
    /// Requests one reset kind; the UI shows a confirmation before <see cref="ConfirmReset"/> applies it.
    /// </summary>
    /// <param name="kind">The reset kind, <c>cubic</c> or <c>defaults</c>.</param>
    public void RequestReset(string kind) => PendingReset = kind;

    /// <summary>
    /// Applies the pending reset, or does nothing when none is pending.
    /// </summary>
    public void ConfirmReset()
    {
        var kind = PendingReset;
        PendingReset = null;
        if (string.Equals(kind, "cubic", StringComparison.Ordinal))
        {
            ResetToCubic();
        }
        else if (string.Equals(kind, "defaults", StringComparison.Ordinal))
        {
            ResetToDefaults();
        }
    }

    /// <summary>
    /// Cancels the pending reset without touching the project.
    /// </summary>
    public void CancelReset() => PendingReset = null;

    /// <summary>
    /// Resets to a cubic 5 Å cell. Only valid for a cubic structure.
    /// </summary>
    /// <returns>The app-level result, or <see langword="null"/> when the action is unavailable.</returns>
    public EditApplyResult? ResetToCubic()
    {
        if (!CanResetToCubic)
        {
            return null;
        }

        return ApplyParameters(new CellParameters(
            new LengthAngstrom(5d),
            new LengthAngstrom(5d),
            new LengthAngstrom(5d),
            new AngleDegrees(90d),
            new AngleDegrees(90d),
            new AngleDegrees(90d)), "Reset cell to cubic");
    }

    /// <summary>
    /// Resets to a 5 Å cell with the current crystal-system locks.
    /// </summary>
    /// <returns>The app-level result, or <see langword="null"/> when the session is read-only.</returns>
    public EditApplyResult? ResetToDefaults()
    {
        if (!CanEdit)
        {
            return null;
        }

        var parameters = locks.Apply(new CellParameters(
            new LengthAngstrom(5d),
            new LengthAngstrom(5d),
            new LengthAngstrom(5d),
            new AngleDegrees(90d),
            new AngleDegrees(90d),
            new AngleDegrees(90d)));
        return ApplyParameters(parameters, "Reset cell to defaults");
    }

    /// <summary>
    /// Applies the current form fields through the coordinator.
    /// </summary>
    /// <returns>The app-level result, or <see langword="null"/> when the form is invalid.</returns>
    public EditApplyResult? Apply()
    {
        if (!CanApply || !TryBuildParameters(out var parameters))
        {
            return null;
        }

        return ApplyParameters(parameters, "Set cell parameters");
    }

    /// <summary>
    /// Recomputes the live geometry from the current field texts.
    /// </summary>
    public void Recalculate()
    {
        if (recalculating || refreshing)
        {
            return;
        }

        recalculating = true;
        ErrorMessage = null;
        try
        {
            ApplyLocksToText();
            HasUncommittedChanges = true;
            if (!TryBuildParameters(out var parameters))
            {
                geometry = null;
                RaiseDerived();
                return;
            }

            if (setting is null)
            {
                geometry = null;
                diagnostics =
                [
                    new Diagnostic(
                        "STR001",
                        DiagnosticSeverity.Error,
                        "The structure's space-group reference does not resolve to a catalog setting."),
                ];
                RaiseDerived();
                return;
            }

            CellGeometry.TryCreate(parameters, setting, out geometry, out diagnostics);
            ErrorMessage = null;
            HasUncommittedChanges = true;
            RaiseDerived();
        }
        finally
        {
            recalculating = false;
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

    private CellFieldViewModel Field(CellFieldKind kind, string label, string unit)
    {
        var field = new CellFieldViewModel(kind, label, unit, _ => Recalculate());
        byKind[kind] = field;
        return field;
    }

    private void RefreshFromProject()
    {
        refreshing = true;
        try
        {
            var structure = session.Project.Structure;
            ResolveSettingAndLocks();
            foreach (var field in Fields)
            {
                field.Error = null;
            }

            byKind[CellFieldKind.A].SetValueSilently(structure.Cell.A.Value);
            byKind[CellFieldKind.B].SetValueSilently(structure.Cell.B.Value);
            byKind[CellFieldKind.C].SetValueSilently(structure.Cell.C.Value);
            byKind[CellFieldKind.Alpha].SetValueSilently(structure.Cell.Alpha.Value);
            byKind[CellFieldKind.Beta].SetValueSilently(structure.Cell.Beta.Value);
            byKind[CellFieldKind.Gamma].SetValueSilently(structure.Cell.Gamma.Value);
            PendingReset = null;
            ErrorMessage = null;
        }
        finally
        {
            refreshing = false;
        }

        Recalculate();
        HasUncommittedChanges = false;
        ConflictMessage = null;
        OnPropertyChanged(nameof(CrystalSystemText));
        OnPropertyChanged(nameof(AxisText));
        OnPropertyChanged(nameof(CanResetToCubic));
        RequestResetToCubicCommand.NotifyCanExecuteChanged();
        RequestResetToDefaultsCommand.NotifyCanExecuteChanged();
        RevertCommand.NotifyCanExecuteChanged();
    }

    private void ResolveSettingAndLocks()
    {
        setting = previewService.TryResolve(session.Project.Structure.SpaceGroup, out _);
        locks = setting is null
            ? CellParameterLocks.For(CrystalSystem.Triclinic, AxisSetting.None)
            : CellParameterLocks.For(setting.CrystalSystem, setting.Axis);
        foreach (var field in Fields)
        {
            var cellLock = locks[field.Kind];
            field.IsLocked = cellLock.IsLocked;
            field.LockReason = cellLock.Reason;
            field.IsEditable = CanEdit;
        }
    }

    private void ApplyLocksToText()
    {
        foreach (var field in Fields)
        {
            var cellLock = locks[field.Kind];
            if (!cellLock.IsLocked)
            {
                continue;
            }

            if (cellLock.FixedValue is { } fixedValue)
            {
                field.SetValueSilently(fixedValue);
            }
            else if (cellLock.Source is { } source)
            {
                field.SetTextSilently(byKind[source].Text);
            }
        }
    }

    private bool TryBuildParameters(out CellParameters parameters)
    {
        parameters = null!;
        var values = new double[6];
        foreach (var field in Fields)
        {
            if (!NumericExpression.TryParse(field.Text, out var value, out var error))
            {
                field.Error = error;
                return false;
            }

            field.Error = null;
            values[(int)field.Kind] = value;
        }

        if (!LengthAngstrom.TryCreate(values[(int)CellFieldKind.A], out var a)
            || !LengthAngstrom.TryCreate(values[(int)CellFieldKind.B], out var b)
            || !LengthAngstrom.TryCreate(values[(int)CellFieldKind.C], out var c)
            || !AngleDegrees.TryCreate(values[(int)CellFieldKind.Alpha], out var alpha)
            || !AngleDegrees.TryCreate(values[(int)CellFieldKind.Beta], out var beta)
            || !AngleDegrees.TryCreate(values[(int)CellFieldKind.Gamma], out var gamma))
        {
            geometry = null;
            diagnostics =
            [
                new Diagnostic(
                    "STR002",
                    DiagnosticSeverity.Error,
                    "A cell length or angle is outside its accepted range."),
            ];
            RaiseDerived();
            return false;
        }

        parameters = locks.Apply(new CellParameters(a, b, c, alpha, beta, gamma));
        return true;
    }

    private EditApplyResult? ApplyParameters(CellParameters parameters, string description)
    {
        if (!CanEdit)
        {
            return null;
        }

        if (setting is null)
        {
            ErrorMessage = "The structure's space-group reference does not resolve to a catalog setting.";
            return null;
        }

        if (!CellGeometry.TryCreate(parameters, setting, out _, out var validationDiagnostics)
            || validationDiagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            diagnostics = validationDiagnostics;
            ErrorMessage = "The cell parameters are not valid for the current crystal system.";
            RaiseDerived();
            return null;
        }

        var result = coordinator.Apply(
            description,
            (editor, project) => editor.SetCellParameters(project, parameters));
        ErrorMessage = result.Outcome == EditApplyOutcome.Applied
            ? null
            : result.Message ?? "The cell parameters were rejected.";
        PendingReset = null;
        if (result.Outcome == EditApplyOutcome.Applied)
        {
            RefreshFromProject();
        }

        return result;
    }

    private void OnProjectChanged(object? sender, ProjectChangedEventArgs args)
    {
        if (args.Kind == ProjectChangeKind.Saved)
        {
            return;
        }

        if (args.Kind == ProjectChangeKind.Replaced)
        {
            RefreshFromProject();
            return;
        }

        var cellChanged = !Equals(args.Previous.Structure.Cell, args.Current.Structure.Cell);
        var spaceGroupChanged = !Equals(args.Previous.Structure.SpaceGroup, args.Current.Structure.SpaceGroup);
        if (!cellChanged && !spaceGroupChanged)
        {
            return;
        }

        if (!HasUncommittedChanges)
        {
            RefreshFromProject();
            return;
        }

        // D-14: keep the draft, refresh the locks, and show the conflict; Apply stays last-writer-wins.
        ResolveSettingAndLocks();
        ConflictMessage = "The structure changed elsewhere. Apply keeps this form's values; Revert loads the "
            + "current committed cell.";
        Recalculate();
        OnPropertyChanged(nameof(CrystalSystemText));
        OnPropertyChanged(nameof(AxisText));
        OnPropertyChanged(nameof(CanResetToCubic));
        RequestResetToCubicCommand.NotifyCanExecuteChanged();
    }

    private void RaiseDerived()
    {
        OnPropertyChanged(nameof(VectorsText));
        OnPropertyChanged(nameof(VolumeText));
        OnPropertyChanged(nameof(DiagnosticsText));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(CanEdit));
        ApplyCommand.NotifyCanExecuteChanged();
    }
}
