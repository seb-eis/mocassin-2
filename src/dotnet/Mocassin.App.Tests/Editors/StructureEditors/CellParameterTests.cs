// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors.Structure;
using Mocassin.App.ViewModels.Projects;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using Mocassin.Symmetry.Geometry;
using Mocassin.Symmetry.SpaceGroups;
using Mocassin.Symmetry.Structure;

namespace Mocassin.App.Tests.Editors.StructureEditors;

/// <summary>
/// Pins the cell-parameter form: the crystal-system lock table against <c>CellGeometry</c> for every catalog
/// setting, live vectors/volume, the violated-inequality message, and the undoable resets.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class CellParameterTests
{
    private static readonly CellParameters baseCell = new(
        new LengthAngstrom(5d),
        new LengthAngstrom(5d),
        new LengthAngstrom(5d),
        new AngleDegrees(90d),
        new AngleDegrees(90d),
        new AngleDegrees(90d));

    [Fact]
    public void TheLockTableMatchesCellGeometryForEveryCatalogSetting()
    {
        var failures = new List<string>();
        foreach (var setting in SpaceGroupCatalog.Default.Settings)
        {
            // A trigonal entry without a hexagonal/rhombohedral axis is an unresolvable reference, not a lock-table
            // case: CellGeometry rejects the axis itself. The editor shows that error instead of masking it.
            if (setting.CrystalSystem == CrystalSystem.Trigonal && setting.Axis == AxisSetting.None)
            {
                continue;
            }

            var locks = CellParameterLocks.For(setting.CrystalSystem, setting.Axis);
            var locked = locks.Apply(baseCell);
            if (!CellGeometry.TryCreate(locked, setting, out _, out var baseDiagnostics))
            {
                failures.Add($"base {setting.Number}/{setting.Axis}/{setting.Origin}: "
                    + string.Join("; ", baseDiagnostics.Select(static diagnostic => diagnostic.Message)));
                continue;
            }

            foreach (var kind in Enum.GetValues<CellFieldKind>())
            {
                var changed = WithField(locked, kind, Perturb(locked, kind));
                var cellLock = locks[kind];
                if (cellLock.IsLocked)
                {
                    if (CellGeometry.TryCreate(changed, setting, out _, out _))
                    {
                        failures.Add($"{setting.Number}/{setting.Axis}: locked {kind} was accepted after a change.");
                    }
                }
                else
                {
                    var applied = locks.Apply(changed);
                    if (!CellGeometry.TryCreate(applied, setting, out _, out var freedDiagnostics))
                    {
                        failures.Add($"{setting.Number}/{setting.Axis}: free {kind} was rejected: "
                            + string.Join("; ", freedDiagnostics.Select(static diagnostic => diagnostic.Message)));
                    }
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public async Task TheCubicFormLocksTheDependentFieldsAndShowsLiveGeometry()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var form = new CellParameterEditorViewModel(entry.Session, new StructurePreviewService());

        Assert.Equal(6, form.Fields.Count);
        Assert.False(Field(form, CellFieldKind.A).IsLocked);
        Assert.True(Field(form, CellFieldKind.B).IsLocked);
        Assert.True(Field(form, CellFieldKind.C).IsLocked);
        Assert.True(Field(form, CellFieldKind.Alpha).IsLocked);
        Assert.NotNull(form.Geometry);
        Assert.True(form.CanApply);

        Field(form, CellFieldKind.A).Text = "6";
        Assert.NotNull(form.Geometry);
        Assert.Equal(6d, form.Geometry!.A.X, 10);
        Assert.Equal(6d, form.Geometry.B.Length, 10);
        Assert.Equal(6d, form.Geometry.C.Length, 10);
        Assert.Contains(
            form.Geometry.VolumeAngstrom3.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture),
            form.VolumeText,
            StringComparison.Ordinal);
        Assert.Contains("6", form.VectorsText, StringComparison.Ordinal);

        var undoDepth = entry.Session.Document.UndoStack.Length;
        var result = form.Apply();
        Assert.NotNull(result);
        Assert.Equal(undoDepth + 1, entry.Session.Document.UndoStack.Length);
        Assert.Equal(6d, entry.Session.Project.Structure.Cell.A.Value, 10);
        Assert.True(entry.Session.EditCoordinator.Undo());
        Assert.Equal(5.41d, entry.Session.Project.Structure.Cell.A.Value, 10);
    }

    [Fact]
    public async Task TheResetActionsAreConfirmedAndUndoable()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var form = new CellParameterEditorViewModel(entry.Session, new StructurePreviewService());
        Field(form, CellFieldKind.A).Text = "6";
        Assert.NotNull(form.Apply());
        var undoDepth = entry.Session.Document.UndoStack.Length;

        form.RequestReset("cubic");
        Assert.True(form.HasPendingReset);
        form.CancelReset();
        Assert.False(form.HasPendingReset);
        Assert.Equal(6d, entry.Session.Project.Structure.Cell.A.Value, 10);

        form.RequestReset("cubic");
        form.ConfirmReset();
        Assert.Equal(5d, entry.Session.Project.Structure.Cell.A.Value, 10);
        Assert.Equal(5d, entry.Session.Project.Structure.Cell.B.Value, 10);
        Assert.Equal(5d, entry.Session.Project.Structure.Cell.C.Value, 10);
        Assert.Equal(undoDepth + 1, entry.Session.Document.UndoStack.Length);
        Assert.True(entry.Session.EditCoordinator.Undo());
        Assert.Equal(6d, entry.Session.Project.Structure.Cell.A.Value, 10);
    }

    [Fact]
    public async Task AnInvalidTriclinicAngleShowsTheGeometryInequalityAndBlocksApply()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var p1 = new SpaceGroupRef(1, AxisSetting.None, OriginChoice.None);
        var structure = entry.Project.Structure with
        {
            SpaceGroup = p1,
            Cell = new CellParameters(
                new LengthAngstrom(5d),
                new LengthAngstrom(5d),
                new LengthAngstrom(5d),
                new AngleDegrees(80d),
                new AngleDegrees(70d),
                new AngleDegrees(75d)),
        };
        var project = entry.Project with
        {
            Model = entry.Project.Model with { Structure = structure },
        };
        var session = ProjectSession.CreateStandalone(project, "triclinic.mocprj");
        using var form = new CellParameterEditorViewModel(session, new StructurePreviewService());
        Assert.False(form.CanResetToCubic);

        Field(form, CellFieldKind.Alpha).Text = "0";
        Assert.Null(form.Geometry);
        Assert.True(form.HasErrors);
        Assert.Contains("strictly between 0° and 180°", form.DiagnosticsText, StringComparison.Ordinal);
        Assert.False(form.CanApply);
    }

    [Fact]
    public async Task TheLocksFollowTheCrystalSystem()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var setting = SpaceGroupCatalog.Default.Settings.First(candidate =>
            candidate.CrystalSystem == CrystalSystem.Monoclinic && candidate.Axis == AxisSetting.UniqueAxisB);
        var structure = entry.Project.Structure with
        {
            SpaceGroup = new SpaceGroupRef(setting.Number, setting.Axis, setting.Origin),
            Cell = new CellParameters(
                new LengthAngstrom(5d),
                new LengthAngstrom(5d),
                new LengthAngstrom(5d),
                new AngleDegrees(90d),
                new AngleDegrees(100d),
                new AngleDegrees(90d)),
        };
        var project = entry.Project with
        {
            Model = entry.Project.Model with { Structure = structure },
        };
        var session = ProjectSession.CreateStandalone(project, "monoclinic.mocprj");
        using var form = new CellParameterEditorViewModel(session, new StructurePreviewService());

        Assert.True(Field(form, CellFieldKind.Alpha).IsLocked);
        Assert.True(Field(form, CellFieldKind.Gamma).IsLocked);
        Assert.False(Field(form, CellFieldKind.Beta).IsLocked);
        Assert.False(Field(form, CellFieldKind.A).IsLocked);
        Assert.False(form.CanResetToCubic);
        Assert.True(form.CanApply);
    }

    [Fact]
    public async Task ReadOnlyDisablesBothStructureForms()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var session = ProjectSession.CreateStandalone(entry.Project, entry.Path, isReadOnly: true);
        using var structure = new StructureEditorViewModel(
            session,
            new Mocassin.App.ViewModels.Shell.ShellRegionsViewModel(),
            new StructurePreviewService(),
            new StructureValidator());

        Assert.False(structure.SpaceGroupEditor.CanApply);
        Assert.False(structure.SpaceGroupEditor.ApplyCommand.CanExecute(null));
        Assert.All(structure.CellParameters.Fields, field => Assert.False(field.IsEditable));
        Assert.False(structure.CellParameters.CanApply);
        Assert.False(structure.CellParameters.ApplyCommand.CanExecute(null));
        Assert.False(structure.CellParameters.RequestResetToDefaultsCommand.CanExecute(null));
    }

    [Fact]
    public async Task AForeignCellEditKeepsTheDraftAndShowsTheConflict()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var form = new CellParameterEditorViewModel(entry.Session, new StructurePreviewService());
        Field(form, CellFieldKind.A).Text = "6";
        Assert.True(form.HasUncommittedChanges);

        var foreign = entry.Session.Project.Structure.Cell with { A = new LengthAngstrom(5d) };
        var result = entry.Session.EditCoordinator.Apply(
            "Set cell parameters",
            (editor, project) => editor.SetCellParameters(project, foreign));
        Assert.Equal(Mocassin.App.ViewModels.Editing.EditApplyOutcome.Applied, result.Outcome);

        Assert.True(form.HasConflict);
        Assert.Equal("6", Field(form, CellFieldKind.A).Text);

        form.RevertCommand.Execute(null);
        Assert.False(form.HasConflict);
        Assert.Equal("5", Field(form, CellFieldKind.A).Text);
    }

    [Fact]
    public async Task RejectsAnInvalidFormAndKeepsTheProjectUnchanged()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var form = new CellParameterEditorViewModel(entry.Session, new StructurePreviewService());
        var original = entry.Session.Project.Structure.Cell;
        Field(form, CellFieldKind.A).Text = "not a number";
        Assert.True(Field(form, CellFieldKind.A).HasError);
        Assert.False(form.CanApply);
        Assert.Null(form.Apply());
        Assert.Equal(original, entry.Session.Project.Structure.Cell);
    }

    private static CellFieldViewModel Field(CellParameterEditorViewModel form, CellFieldKind kind) =>
        form.Fields.Single(field => field.Kind == kind);

    private static CellParameters WithField(CellParameters parameters, CellFieldKind kind, double value) =>
        kind switch
        {
            CellFieldKind.A => parameters with { A = new LengthAngstrom(value) },
            CellFieldKind.B => parameters with { B = new LengthAngstrom(value) },
            CellFieldKind.C => parameters with { C = new LengthAngstrom(value) },
            CellFieldKind.Alpha => parameters with { Alpha = new AngleDegrees(value) },
            CellFieldKind.Beta => parameters with { Beta = new AngleDegrees(value) },
            CellFieldKind.Gamma => parameters with { Gamma = new AngleDegrees(value) },
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    private static double Perturb(CellParameters parameters, CellFieldKind kind) => kind switch
    {
        CellFieldKind.A => parameters.A.Value + 0.5,
        CellFieldKind.B => parameters.B.Value + 0.5,
        CellFieldKind.C => parameters.C.Value + 0.5,
        CellFieldKind.Alpha => parameters.Alpha.Value + 2d,
        CellFieldKind.Beta => parameters.Beta.Value + 2d,
        CellFieldKind.Gamma => parameters.Gamma.Value + 2d,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
