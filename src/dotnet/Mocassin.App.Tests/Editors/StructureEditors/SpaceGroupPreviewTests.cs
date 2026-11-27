// SPDX-License-Identifier: MIT
using System.Collections.Immutable;

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors.Structure;
using Mocassin.App.ViewModels.Projects;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Symmetry;
using Mocassin.Symmetry.SpaceGroups;
using Mocassin.Symmetry.Structure;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.Tests.Editors.StructureEditors;

/// <summary>
/// Pins the space-group form: searchable picker, settings, operation details, pre-apply orbit/overlap preview,
/// cancel, and one-undo-entry apply.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class SpaceGroupPreviewTests
{
    [Fact]
    public async Task ThePickerSearchesByNumberSymbolHallAndCrystalSystem()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var preview = new StructurePreviewService();
        var current = preview.TryResolve(entry.Project.Structure.SpaceGroup, out _)!;
        using var form = new SpaceGroupEditorViewModel(entry.Session, preview, new StructureValidator());

        form.SearchText = current.Number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains(form.Results, setting => setting.Number == current.Number);

        form.SearchText = current.HermannMauguin;
        Assert.Contains(form.Results, setting => setting.HermannMauguin == current.HermannMauguin);

        form.SearchText = current.HallSymbol;
        Assert.Contains(form.Results, setting => setting.HallSymbol == current.HallSymbol);

        form.SearchText = "Cubic";
        Assert.NotEmpty(form.Results);
        Assert.All(form.Results, setting => Assert.Equal(CrystalSystem.Cubic, setting.CrystalSystem));
    }

    [Fact]
    public async Task TheSettingsSelectorOffersAxisAndOriginChoices()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var catalog = SpaceGroupCatalog.Default;
        var multi = catalog.Settings.GroupBy(static setting => setting.Number)
            .First(group => group.Count() > 1)
            .OrderBy(static setting => setting.Axis)
            .ThenBy(static setting => setting.Origin)
            .ToArray();
        using var form = new SpaceGroupEditorViewModel(
            entry.Session,
            new StructurePreviewService(),
            new StructureValidator());

        form.Select(multi[0]);
        Assert.Equal(multi.Length, form.GroupSettings.Count);
        form.Select(multi[1]);

        Assert.Equal(multi[1].Number, form.SelectedSetting?.Number);
        Assert.Equal(multi[1].Axis, form.SelectedSetting?.Axis);
        Assert.Equal(multi[1].Origin, form.SelectedSetting?.Origin);
    }

    [Fact]
    public async Task TheDetailsPanelMatchesTheCatalogOperations()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var preview = new StructurePreviewService();
        var current = preview.TryResolve(entry.Project.Structure.SpaceGroup, out _)!;
        using var form = new SpaceGroupEditorViewModel(entry.Session, preview, new StructureValidator());

        Assert.Equal(current.Operations.Length, form.OperationCount);
        Assert.Equal(current.Operations.Length, form.GeneralMultiplicity);
        Assert.Equal(current.Operations.Length, form.Operations.Count);
        Assert.Equal(current.Operations[0].Linear.ToString(), form.Operations[0].MatrixText);
        Assert.Equal(current.Operations[0].Translation.ToString(), form.Operations[0].TranslationText);
        Assert.Contains("x", form.Operations[0].HumanReadableText, StringComparison.Ordinal);
        Assert.Equal(CrystalSystem.Cubic.ToString(), form.CrystalSystemText);
    }

    [Fact]
    public async Task ACandidatePreviewsTheNewOrbitSizesAndCancelChangesNothing()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var catalog = SpaceGroupCatalog.Default;
        using var form = new SpaceGroupEditorViewModel(
            entry.Session,
            new StructurePreviewService(),
            new StructureValidator());
        var p1 = catalog.GetSetting(1, AxisSetting.None, OriginChoice.None)!;
        var original = entry.Project.Structure.SpaceGroup;

        form.Select(p1);

        Assert.True(form.CanApply);
        Assert.False(form.HasErrors);
        Assert.All(form.Orbits, orbit => Assert.Equal(1, orbit.CandidateMultiplicity));
        Assert.Contains(form.Orbits, orbit => orbit.CurrentMultiplicity > 1);
        Assert.Contains("Compatible", form.CompatibilityText, StringComparison.Ordinal);

        form.Cancel();

        Assert.Equal(original, entry.Session.Project.Structure.SpaceGroup);
        Assert.False(form.CanApply);
        Assert.Equal(original.Number, form.SelectedSetting?.Number);
    }

    [Fact]
    public async Task AnOverlappingCandidateIsReportedAndCannotBeApplied()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var project = CreateInvertedPairProject(entry.Project);
        var session = ProjectSession.CreateStandalone(project, "overlap.mocprj");
        var catalog = SpaceGroupCatalog.Default;
        var pMinus1 = catalog.GetSetting(2, AxisSetting.None, OriginChoice.None)!;
        using var form = new SpaceGroupEditorViewModel(
            session,
            new StructurePreviewService(),
            new StructureValidator());

        form.Select(pMinus1);

        Assert.True(form.HasOverlap);
        Assert.True(form.HasErrors);
        Assert.False(form.CanApply);
        Assert.All(form.Orbits, orbit => Assert.True(orbit.HasOverlap));
        Assert.Contains("STR007", form.DiagnosticsText, StringComparison.Ordinal);
        Assert.Null(form.Apply());
        Assert.Equal(1, session.Project.Structure.SpaceGroup.Number);
    }

    [Fact]
    public async Task ApplyingASpaceGroupIsOneUndoEntry()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var catalog = SpaceGroupCatalog.Default;
        using var form = new SpaceGroupEditorViewModel(
            entry.Session,
            new StructurePreviewService(),
            new StructureValidator());
        var p1 = catalog.GetSetting(1, AxisSetting.None, OriginChoice.None)!;
        var undoDepth = entry.Session.Document.UndoStack.Length;

        form.Select(p1);
        var result = form.Apply();

        Assert.NotNull(result);
        Assert.Equal(Mocassin.App.ViewModels.Editing.EditApplyOutcome.Applied, result!.Outcome);
        Assert.Equal(undoDepth + 1, entry.Session.Document.UndoStack.Length);
        Assert.Equal(1, entry.Session.Project.Structure.SpaceGroup.Number);

        Assert.True(entry.Session.EditCoordinator.Undo());
        Assert.Equal(225, entry.Session.Project.Structure.SpaceGroup.Number);
        Assert.Equal(undoDepth, entry.Session.Document.UndoStack.Length);
    }

    private static ProjectModel CreateInvertedPairProject(ProjectModel source)
    {
        var set = source.Model.ParticleSets[0];
        var sites = ImmutableArray.Create(
            new CellSite(CellSiteId.New(), "Pair A", new Fractional3(0.1, 0.1, 0.1), set.Id, SiteStability.Stable),
            new CellSite(CellSiteId.New(), "Pair B", new Fractional3(0.9, 0.9, 0.9), set.Id, SiteStability.Stable));
        var structure = source.Structure with
        {
            SpaceGroup = new SpaceGroupRef(1, AxisSetting.None, OriginChoice.None),
            Cell = new Mocassin.Core.Model.CellParameters(
                new Mocassin.Core.Units.LengthAngstrom(5d),
                new Mocassin.Core.Units.LengthAngstrom(5d),
                new Mocassin.Core.Units.LengthAngstrom(5d),
                new Mocassin.Core.Units.AngleDegrees(90d),
                new Mocassin.Core.Units.AngleDegrees(90d),
                new Mocassin.Core.Units.AngleDegrees(90d)),
            Sites = sites,
            DummyPositions = [],
        };
        return source with { Model = source.Model with { Structure = structure } };
    }
}
