// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Project;
using Mocassin.Core.Units;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// Proves the shared editor framework's feature-1 done-when on a real project: CRUD and undo through the
/// coordinator, the blocked-delete pre-check, bulk all-or-nothing, read-only refusal, and diagnostic navigation.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ModelEditorFrameworkTests
{
    [Fact]
    public async Task CrudAndUndoWorkHeadlesslyOnARealProject()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var session = entry.Session;
        using var editor = new TestParticleEditor(session, new ShellRegionsViewModel());
        var originalCount = entry.Project.Particles.Length;

        editor.AddCommand.Execute(null);
        Assert.Equal(originalCount + 1, session.Project.Particles.Length);
        Assert.NotNull(editor.Table.SelectedRow);
        var detail = Assert.IsType<TestParticleDetail>(editor.Detail);
        Assert.True(detail.CanApply);

        detail.Name = "Renamed particle";
        var applied = detail.Commit();
        Assert.Equal(EditApplyOutcome.Applied, applied.Outcome);
        Assert.Equal("Renamed particle", session.Project.Find(ParticleId.Create(detail.ObjectId))?.Name);
        Assert.True(session.EditCoordinator.CanUndo);

        Assert.True(session.EditCoordinator.Undo());
        Assert.Equal(originalCount + 1, session.Project.Particles.Length);
        var reverted = session.Project.Find(ParticleId.Create(detail.ObjectId))!;
        Assert.NotEqual("Renamed particle", reverted.Name);

        Assert.True(session.EditCoordinator.Redo());
        Assert.Equal("Renamed particle", session.Project.Find(ParticleId.Create(detail.ObjectId))?.Name);

        editor.DuplicateCommand.Execute(null);
        Assert.Equal(originalCount + 2, session.Project.Particles.Length);

        editor.RemoveCommand.Execute(null);
        Assert.Equal(originalCount + 1, session.Project.Particles.Length);
        Assert.True(session.EditCoordinator.CanUndo);
    }

    [Fact]
    public async Task ReadOnlySessionDisablesEveryEditPathAndRefusesTheCoordinator()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var session = ProjectSession.CreateStandalone(
            entry.Project,
            entry.Path,
            isReadOnly: true);
        using var editor = new TestParticleEditor(session, new ShellRegionsViewModel());

        Assert.Equal(ModelEditorState.ReadOnly, editor.State);
        Assert.True(editor.IsReadOnly);
        Assert.False(editor.AddCommand.CanExecute(null));
        Assert.False(editor.RemoveCommand.CanExecute(null));
        Assert.False(editor.DuplicateCommand.CanExecute(null));

        var result = session.EditCoordinator.Apply(
            "Add particle",
            (core, project) => core.AddParticle(
                project,
                new Particle(ParticleId.New(), "Forbidden", "X", new ChargeElementary(0d), false)));

        Assert.Equal(EditApplyOutcome.ReadOnly, result.Outcome);
        Assert.False(session.EditCoordinator.CanUndo);
        Assert.False(session.Document.IsDirty);
    }

    [Fact]
    public async Task BulkEditIsOneUndoEntryAndAPartialRejectionChangesNothing()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var session = entry.Session;
        using var editor = new TestParticleEditor(session, new ShellRegionsViewModel());
        var originalCount = entry.Project.Particles.Length;

        var firstId = ParticleId.New();
        var secondId = ParticleId.New();
        var composed = BulkEditComposer.Compose(
        [
            (core, project) => core.AddParticle(
                project,
                new Particle(firstId, "Bulk one", "X", new ChargeElementary(0d), false)),
            (core, project) => core.AddParticle(
                project,
                new Particle(secondId, "Bulk two", "X", new ChargeElementary(0d), false)),
        ]);

        var result = session.EditCoordinator.Apply("Bulk add", composed);

        Assert.Equal(EditApplyOutcome.Applied, result.Outcome);
        Assert.Equal(originalCount + 2, session.Project.Particles.Length);
        Assert.Single(session.Document.UndoStack);
        Assert.True(session.EditCoordinator.Undo());
        Assert.Equal(originalCount, session.Project.Particles.Length);

        var undoDepth = session.Document.UndoStack.Length;
        var dirty = session.Document.IsDirty;
        var existingName = session.Project.Particles[0].Name;
        var failing = BulkEditComposer.Compose(
        [
            (core, project) => core.AddParticle(
                project,
                new Particle(ParticleId.New(), "Partial one", "X", new ChargeElementary(0d), false)),
            (core, project) => core.AddParticle(
                project,
                new Particle(ParticleId.New(), existingName, "X", new ChargeElementary(0d), false)),
        ]);

        var rejected = session.EditCoordinator.Apply("Bulk add", failing);

        Assert.Equal(EditApplyOutcome.Rejected, rejected.Outcome);
        Assert.Equal(originalCount, session.Project.Particles.Length);
        Assert.Equal(undoDepth, session.Document.UndoStack.Length);
        Assert.Equal(dirty, session.Document.IsDirty);
        Assert.DoesNotContain(session.Project.Particles, particle => particle.Name == "Partial one");
    }

    [Fact]
    public async Task BlockedDeleteAgreesWithTheCoreRejection()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var session = entry.Session;
        using var editor = new TestParticleEditor(session, new ShellRegionsViewModel());
        var set = entry.Project.Model.ParticleSets.First(candidate => candidate.ParticleIds.Length > 0);
        var particleId = set.ParticleIds[0];

        Assert.True(editor.RequestSelection(particleId.Value));
        editor.ShowUsagesCommand.Execute(null);
        Assert.Contains(editor.SelectedUsages, usage => usage.DisplayName == set.Name);

        Assert.False(editor.TryRemoveSelected());
        Assert.True(editor.HasBlockedDelete);
        Assert.Contains(set.Name, editor.BlockedDeleteMessage, StringComparison.Ordinal);

        var direct = session.EditCoordinator.Apply(
            "Remove particle",
            (core, project) => core.RemoveParticle(project, particleId));
        Assert.Equal(EditApplyOutcome.Rejected, direct.Outcome);
        Assert.Contains(direct.Edit!.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task ADiagnosticNavigatesIntoAnEditorAndRequestsTheField()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var duplicate = entry.Project.Particles[0] with
        {
            Id = ParticleId.New(),
            Name = entry.Project.Particles[0].Name,
        };
        var project = entry.Project with
        {
            Model = entry.Project.Model with
            {
                Particles = [.. entry.Project.Particles, duplicate],
            },
        };

        var diagnostics = ProjectValidator.Validate(project);
        var target = diagnostics.First(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error
            && diagnostic.Path is { Domain: ModelEditorDomains.Particle, Property: "name" });

        var session = ProjectSession.CreateStandalone(project, "diagnostic.mocprj");
        var workspace = new WorkspaceViewModel();
        workspace.Add(new ProjectWorkspaceEntry(session));
        var documents = new Mocassin.App.ViewModels.Documents.DocumentCollectionViewModel();
        var provider = new Mocassin.App.ViewModels.Editors.Navigation.ModelEditorNavigationTargetProvider(
            workspace,
            documents,
            [new TestParticleEditorFactory()],
            new ShellRegionsViewModel());
        var navigation = new Mocassin.App.ViewModels.Navigation.NavigationService(
            documents,
            workspace,
            new Mocassin.App.ViewModels.Consoles.ConsoleService(
                new InlineUiDispatcher(),
                new ManualTimeProvider()),
            [provider]);

        var opened = await navigation.NavigateToAsync(
            target.Path!,
            Mocassin.App.ViewModels.Navigation.NavigationIntent.FocusProperty);

        Assert.True(opened);
        var document = Assert.IsType<ModelEditorDocumentViewModel>(documents.ActiveDocument);
        Assert.Equal(target.Path!.ObjectId, document.Editor.Table.SelectedRow?.ObjectId);
        Assert.Equal("name", document.Editor.PendingFocus);
    }
}
