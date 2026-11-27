// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Editors.Expressions;
using Mocassin.Core.Editing;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors.Dummies;

/// <summary>
/// The dummy-position detail form: name and three expression fractional fields. The distinct diamond glyph marks
/// the object as a visual-only helper; it has no particle set, stability, energy, or transition semantics.
/// </summary>
internal sealed class DummyPositionDetailViewModel : ModelDetailViewModel
{
    private string name;
    private string fieldA;
    private string fieldB;
    private string fieldC;
    private string? fieldError;
    private bool fieldsValid;

    /// <summary>
    /// Initializes the detail from a dummy position.
    /// </summary>
    /// <param name="dummy">The dummy position.</param>
    public DummyPositionDetailViewModel(CellDummyPosition dummy)
        : base(dummy.Id.Value)
    {
        name = dummy.Name;
        fieldA = Format(dummy.Fractional.A);
        fieldB = Format(dummy.Fractional.B);
        fieldC = Format(dummy.Fractional.C);
        RefreshFields();
    }

    /// <summary>
    /// Gets the distinct visual-only marker glyph.
    /// </summary>
    public string MarkerGlyph { get; } = DummiesEditorViewModel.MarkerGlyph;

    /// <summary>
    /// Gets the visual-only explanation.
    /// </summary>
    public string Explanation { get; } =
        "Visual-only helper: excluded from simulation, energy, and transition compilation. "
        + "It may coincide with a reference site.";

    /// <summary>
    /// Gets or sets the dummy name.
    /// </summary>
    public string Name
    {
        get => name;
        set
        {
            if (SetProperty(ref name, value))
            {
                HasUncommittedChanges = true;
                RefreshFields();
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
                RefreshFields();
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
                RefreshFields();
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
                RefreshFields();
            }
        }
    }

    /// <summary>
    /// Gets the field parse/range error.
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
    /// Gets a value indicating whether the form fields are editable.
    /// </summary>
    public bool CanEditForm => Coordinator?.CanEdit == true && !IsRemoved;

    /// <inheritdoc />
    public override bool CanApply => base.CanApply && fieldsValid;

    /// <inheritdoc />
    protected override string Description => "Update dummy position";

    /// <inheritdoc />
    protected override ProjectEditResult ApplyCore(ProjectEditor editor, ProjectModel project) =>
        TryBuildDummy(out var dummy)
            ? editor.UpdateDummyPosition(project, dummy)
            : new ProjectEditResult(project, null, []);

    /// <inheritdoc />
    protected override void RevertCore()
    {
        var dummy = Coordinator?.Document.Project.Structure.DummyPositions
            .FirstOrDefault(candidate => candidate.Id.Value == ObjectId);
        if (dummy is null)
        {
            return;
        }

        name = dummy.Name;
        fieldA = Format(dummy.Fractional.A);
        fieldB = Format(dummy.Fractional.B);
        fieldC = Format(dummy.Fractional.C);
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(FieldA));
        OnPropertyChanged(nameof(FieldB));
        OnPropertyChanged(nameof(FieldC));
        RefreshFields();
    }

    /// <inheritdoc />
    protected override void OnAttached() => OnPropertyChanged(nameof(CanEditForm));

    /// <inheritdoc />
    protected override void OnCanApplyChanged() => OnPropertyChanged(nameof(CanApply));

    private void RefreshFields()
    {
        TryBuildDummy(out _);
        NotifyCanApplyChanged();
    }

    private bool TryBuildDummy(out CellDummyPosition dummy)
    {
        dummy = null!;
        if (string.IsNullOrWhiteSpace(Name))
        {
            FieldError = "STR013: the dummy-position name must not be empty.";
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
            FieldError = "STR015: each fractional coordinate must be finite and inside [0, 1).";
            fieldsValid = false;
            return false;
        }

        FieldError = null;
        fieldsValid = true;
        dummy = new CellDummyPosition(
            CellDummyPositionId.Create(ObjectId),
            Name,
            new Fractional3(a, b, c));
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

    private static string Format(double value) =>
        value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
}
