// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors.Sites;
using Mocassin.App.ViewModels.Editors.Usage;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects;
using Mocassin.Symmetry.Structure;

namespace Mocassin.App.Tests.Editors.Sites;

/// <summary>
/// Pins the site table and P1 preview: candidate overlap refusal (m-14), preview-index equality, no operation
/// label, deterministic projection, dependency-aware deletion (D-2), stability warnings, and read-only.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class CellSiteEditorTests
{
    [Fact]
    public async Task TheTableAndP1PreviewMatchTheEngineData()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = CreateEditor(entry);
        var site = entry.Project.Structure.Sites.First(candidate => candidate.Name == "O");
        Assert.True(editor.RequestSelection(site.Id.Value));
        var detail = Assert.IsType<CellSiteDetailViewModel>(editor.Detail);
        var preview = new StructurePreviewService().TryCreatePreview(entry.Project.Structure, 1, 1, 1, out _)!;
        var index = entry.Project.Structure.Sites.IndexOf(site);
        var orbit = preview.Orbits[index];

        Assert.True(detail.ShowP1Preview);
        Assert.Equal(orbit.Positions.Length, detail.OrbitSize);
        Assert.Equal(orbit.Multiplicity, detail.Multiplicity);
        Assert.Equal(orbit.Positions.Length, detail.OrbitRows.Count);
        for (var member = 0; member < orbit.Positions.Length; member++)
        {
            var row = detail.OrbitRows[member];
            var p = orbit.PositionIndices[member];
            Assert.Equal(p, row.P);
            Assert.Equal(preview.PositionList[p].OrbitMemberIndex, row.OrbitMemberIndex);
        }

        Assert.Equal(preview.PositionList.Length, detail.WholeStructureRows.Count);
        for (var p = 0; p < preview.PositionList.Length; p++)
        {
            Assert.Equal(p, detail.WholeStructureRows[p].P);
        }

        Assert.DoesNotContain(
            typeof(SiteOrbitRowViewModel).GetProperties(),
            property => property.Name.Contains("Operation", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            typeof(SiteWholeStructureRowViewModel).GetProperties(),
            property => property.Name.Contains("Operation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TheProjectionIsDeterministicAndFollowsTheSelectedPlane()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var first = CreateEditor(entry);
        using var second = CreateEditor(entry);
        var site = entry.Project.Structure.Sites.First(candidate => candidate.Name == "O");
        first.RequestSelection(site.Id.Value);
        second.RequestSelection(site.Id.Value);
        var firstDetail = Assert.IsType<CellSiteDetailViewModel>(first.Detail);
        var secondDetail = Assert.IsType<CellSiteDetailViewModel>(second.Detail);

        Assert.Equal(firstDetail.ProjectionPoints, secondDetail.ProjectionPoints);
        firstDetail.SelectedPlane = "a–c";
        secondDetail.SelectedPlane = "a–c";
        Assert.Equal(firstDetail.ProjectionPoints, secondDetail.ProjectionPoints);
        Assert.NotEmpty(firstDetail.ProjectionPoints);
        Assert.All(
            firstDetail.ProjectionPoints,
            point => Assert.InRange(point.X, 0d, 300d));
        Assert.All(
            firstDetail.ProjectionPoints,
            point => Assert.InRange(point.Y, 0d, 300d));
    }

    [Fact]
    public async Task AnOverlappingCandidateIsShownAndRefused()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = CreateEditor(entry);
        var transitionSite = entry.Project.Structure.Sites.First(candidate => candidate.Name == "O transition");
        Assert.True(editor.RequestSelection(transitionSite.Id.Value));
        var detail = Assert.IsType<CellSiteDetailViewModel>(editor.Detail);
        var original = entry.Session.Project.Structure.Sites.Length;

        detail.FieldA = "0.25";
        detail.FieldB = "0.25";
        detail.FieldC = "0.25";

        Assert.True(detail.HasOverlap);
        Assert.True(detail.HasCandidateErrors);
        Assert.False(detail.CanApply);
        Assert.Contains("STR007", detail.DiagnosticsText, StringComparison.Ordinal);
        Assert.False(detail.ApplyCommand.CanExecute(null));
        Assert.Equal(original, entry.Session.Project.Structure.Sites.Length);
    }

    [Fact]
    public async Task AddAndRemoveASiteAreUndoable()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = CreateEditor(entry);
        var original = entry.Session.Project.Structure.Sites.Length;
        var undoDepth = entry.Session.Document.UndoStack.Length;

        editor.AddCommand.Execute(null);
        var detail = Assert.IsType<CellSiteDetailViewModel>(editor.Detail);
        Assert.True(detail.IsNew);
        detail.Name = "Interstitial";
        detail.FieldA = "0.123";
        detail.FieldB = "0.234";
        detail.FieldC = "0.345";
        detail.ParticleSetPicker.Selected = detail.ParticleSetPicker.FilteredOptions[0];
        Assert.True(detail.CanApply);
        detail.ApplyCommand.Execute(null);

        Assert.Equal(original + 1, entry.Session.Project.Structure.Sites.Length);
        Assert.Equal(undoDepth + 1, entry.Session.Document.UndoStack.Length);
        Assert.NotNull(editor.Table.SelectedRow);
        Assert.Equal("Interstitial", entry.Session.Project.Structure.Sites
            .Single(site => site.Id.Value == editor.Table.SelectedRow!.ObjectId).Name);

        Assert.True(entry.Session.EditCoordinator.Undo());
        Assert.Equal(original, entry.Session.Project.Structure.Sites.Length);
    }

    [Fact]
    public async Task TheEnergyOnlyDeleteIsOfferedAndIsOneUndoEntry()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = CreateEditor(entry);
        var site = entry.Project.Structure.Sites.First(candidate => candidate.Name == "O transition");
        Assert.True(editor.RequestSelection(site.Id.Value));
        var detail = Assert.IsType<CellSiteDetailViewModel>(editor.Detail);

        Assert.True(detail.HasDependents);
        Assert.True(detail.CanDeleteEnergyOnly);
        Assert.All(detail.Dependents, usage => Assert.True(usage.IsEnergyReference));
        Assert.Contains("Unstable environment", detail.EnergyDeleteText, StringComparison.Ordinal);

        editor.RemoveCommand.Execute(null);
        Assert.True(editor.HasBlockedDelete);
        Assert.Contains("Unstable environment", editor.BlockedDeleteMessage, StringComparison.Ordinal);

        detail.RequestEnergyDeleteCommand.Execute(null);
        Assert.True(detail.HasPendingEnergyDelete);
        var undoDepth = entry.Session.Document.UndoStack.Length;
        detail.ConfirmEnergyDeleteCommand.Execute(null);

        Assert.DoesNotContain(
            entry.Session.Project.Structure.Sites,
            candidate => candidate.Id == site.Id);
        Assert.Empty(entry.Session.Project.UnstableEnvironments);
        Assert.Empty(entry.Session.Project.Clusters);
        Assert.Equal(undoDepth + 1, entry.Session.Document.UndoStack.Length);

        Assert.True(entry.Session.EditCoordinator.Undo());
        Assert.Contains(entry.Session.Project.Structure.Sites, candidate => candidate.Id == site.Id);
        Assert.NotEmpty(entry.Session.Project.UnstableEnvironments);
    }

    [Fact]
    public async Task ANonEnergyDependentDisablesTheEnergyOnlyActionAndWarnsOnStability()
    {
        var entry = await LoadAsync("samples", "transitions", "ceria-mmc.mocprj");
        using var editor = CreateEditor(entry);
        var transition = entry.Project.MetropolisTransitions[0];
        var site = entry.Project.Structure.Sites.First(candidate => candidate.Id == transition.SiteAId);
        Assert.True(editor.RequestSelection(site.Id.Value));
        var detail = Assert.IsType<CellSiteDetailViewModel>(editor.Detail);

        Assert.True(detail.HasDependents);
        Assert.False(detail.CanDeleteEnergyOnly);
        Assert.Contains(detail.Dependents, usage => usage.Kind == ModelUsageKind.Transition);
        Assert.True(detail.HasStabilityWarning);
        Assert.Contains("TRN012/TRN020", detail.StabilityWarning, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpeningADependentRequestsShellNavigation()
    {
        var entry = await LoadAsync("samples", "transitions", "ceria-mmc.mocprj");
        using var editor = CreateEditor(entry);
        var transition = entry.Project.MetropolisTransitions[0];
        var site = entry.Project.Structure.Sites.First(candidate => candidate.Id == transition.SiteAId);
        Assert.True(editor.RequestSelection(site.Id.Value));
        var detail = Assert.IsType<CellSiteDetailViewModel>(editor.Detail);
        var dependent = detail.Dependents.First(usage => usage.Kind == ModelUsageKind.Transition);
        ModelPath? requested = null;
        detail.NavigationRequested += (_, path) => requested = path;

        detail.OpenDependentCommand.Execute(dependent);

        Assert.NotNull(requested);
        Assert.Equal(dependent.Reference.Domain, requested!.Domain);
        Assert.Equal(dependent.Reference.ObjectId, requested.ObjectId);
    }

    [Fact]
    public async Task AKmcResolvedPathIsADependentAndAWarning()
    {
        var entry = await LoadAsync("samples", "transitions", "ceria-kmc.mocprj");
        using var editor = CreateEditor(entry);
        var transition = entry.Project.KineticTransitions[0];
        var resolver = Mocassin.Symmetry.Transitions.TransitionPathResolver.TryCreate(
            entry.Project.Structure,
            catalog: null,
            out var pathResolver,
            out _);
        Assert.True(resolver);
        var path = pathResolver!.TryResolve(transition, out _)!;
        var siteId = path.Positions[0].SiteId;
        Assert.True(editor.RequestSelection(siteId.Value));
        var detail = Assert.IsType<CellSiteDetailViewModel>(editor.Detail);

        Assert.Contains(detail.Dependents, usage => usage.Kind == ModelUsageKind.Transition);
        Assert.True(detail.HasStabilityWarning);
    }

    [Fact]
    public async Task ReadOnlyDisablesTheSiteFormAndActions()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var session = ProjectSession.CreateStandalone(entry.Project, entry.Path, isReadOnly: true);
        using var editor = CreateEditor(session);
        Assert.False(editor.AddCommand.CanExecute(null));
        Assert.True(editor.RequestSelection(entry.Project.Structure.Sites[0].Id.Value));
        var detail = Assert.IsType<CellSiteDetailViewModel>(editor.Detail);
        Assert.False(detail.CanEditForm);
        Assert.False(detail.ApplyCommand.CanExecute(null));
        Assert.False(detail.RequestEnergyDeleteCommand.CanExecute(null));
    }

    private static SitesEditorViewModel CreateEditor(Mocassin.App.ViewModels.Projects.ProjectWorkspaceEntry entry) =>
        new(
            entry.Session,
            new ShellRegionsViewModel(),
            new StructurePreviewService(),
            new StructureValidator());

    private static SitesEditorViewModel CreateEditor(ProjectSession session) =>
        new(session, new ShellRegionsViewModel(), new StructurePreviewService(), new StructureValidator());

    private static async Task<Mocassin.App.ViewModels.Projects.ProjectWorkspaceEntry> LoadAsync(
        params string[] relativePath)
    {
        var path = RepositoryPaths.Combine(relativePath);
        var store = new ProjectStore(new ManualTimeProvider(), new ProjectStoreOptions { ValidateOnLoad = false });
        var result = await store.LoadAsync(path);
        if (result.Project is null)
        {
            throw new InvalidOperationException($"The fixture {path} did not load: {result.FailureKind}.");
        }

        return new Mocassin.App.ViewModels.Projects.ProjectWorkspaceEntry(
            result.Project,
            path,
            new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero),
            isReadOnly: false);
    }
}
