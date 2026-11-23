// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Editors;
using Mocassin.App.ViewModels.Editors.Particles;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;

namespace Mocassin.App.Tests.Editors.Particles;

/// <summary>
/// Pins the particle editor's feature-2 acceptance rows: the pinned void row, add/rename/charge/symbol/duplicate,
/// the PAR001/PAR004/PAR008 inline surface, blocked delete with dependents, bulk charge, bulk delete, and the
/// read-only path.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ParticleEditorTests
{
    [Fact]
    public async Task TheVoidRowIsPinnedFirstAndReadOnly()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticlesEditorViewModel(entry.Session, new ShellRegionsViewModel());

        Assert.Equal(entry.Project.Particles.Length + 1, editor.Table.TotalCount);
        var voidRow = editor.Table.Rows[0];
        Assert.Equal(ParticleId.Void.Value, voidRow.ObjectId);
        Assert.Equal("Void (implicit)", voidRow.Cells[0].Text);
        Assert.Equal("0", voidRow.Cells[4].Text);

        Assert.True(editor.RequestSelection(ParticleId.Void.Value));
        Assert.Null(editor.Detail);
        Assert.Contains("pinned", editor.DetailMessage, StringComparison.Ordinal);
        Assert.True(editor.IsSelectedObjectReadOnly);
        Assert.False(editor.RemoveCommand.CanExecute(null));
        Assert.False(editor.DuplicateCommand.CanExecute(null));
        Assert.True(editor.AddCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddRenameChargeSymbolDuplicateAndBulkChargeAreUndoable()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticlesEditorViewModel(entry.Session, new ShellRegionsViewModel());
        var originalCount = entry.Project.Particles.Length;

        editor.AddCommand.Execute(null);
        Assert.Equal(originalCount + 1, entry.Session.Project.Particles.Length);
        var added = Assert.IsType<ParticleDetailViewModel>(editor.Detail);
        var addedParticle = entry.Session.Project.Find(ParticleId.Create(added.ObjectId))!;
        Assert.Equal(0d, addedParticle.Charge.Value);
        Assert.StartsWith("Particle ", addedParticle.Name, StringComparison.Ordinal);
        Assert.DoesNotContain(
            entry.Session.Project.Particles,
            particle => particle.Id != addedParticle.Id
                && string.Equals(particle.Name, addedParticle.Name, StringComparison.OrdinalIgnoreCase));

        added.Name = "Renamed";
        added.CommitNameCommand.Execute(null);
        Assert.Equal("Renamed", entry.Session.Project.Find(ParticleId.Create(added.ObjectId))?.Name);

        added.Charge = -2d;
        added.CommitChargeCommand.Execute(null);
        Assert.Equal(-2d, entry.Session.Project.Find(ParticleId.Create(added.ObjectId))?.Charge.Value);

        added.Symbol = "bad symbol";
        added.CommitSymbolCommand.Execute(null);
        Assert.Contains("PAR008", added.WarningMessage, StringComparison.Ordinal);

        added.Name = string.Empty;
        added.CommitNameCommand.Execute(null);
        Assert.True(added.HasError);
        Assert.Contains("name", added.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Renamed", entry.Session.Project.Find(ParticleId.Create(added.ObjectId))?.Name);

        editor.DuplicateCommand.Execute(null);
        Assert.Equal(originalCount + 2, entry.Session.Project.Particles.Length);
        var duplicate = Assert.IsType<ParticleDetailViewModel>(editor.Detail);
        Assert.NotEqual(added.ObjectId, duplicate.ObjectId);
        Assert.EndsWith("copy", entry.Session.Project.Find(ParticleId.Create(duplicate.ObjectId))?.Name ?? string.Empty, StringComparison.Ordinal);

        var authoredRows = editor.Table.Rows.Where(row => row.ObjectId != ParticleId.Void.Value).ToArray();
        editor.Table.UpdateSelection(authoredRows);
        Assert.NotNull(editor.ActionBar);
        editor.ActionBar!.ValueText = "1";
        editor.ActionBar.ApplyCommand.Execute(null);
        Assert.False(editor.ActionBar.HasError);
        Assert.All(
            entry.Session.Project.Particles,
            particle => Assert.Equal(1d, particle.Charge.Value));
        var undoDepth = entry.Session.Document.UndoStack.Length;
        Assert.True(entry.Session.Document.Undo());
        Assert.Equal(undoDepth - 1, entry.Session.Document.UndoStack.Length);
        Assert.DoesNotContain(entry.Session.Project.Particles, particle => particle.Charge.Value == 1d && particle.Name == "O2-");
    }

    [Fact]
    public async Task AChargedVacancySurfacesThePar004Warning()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticlesEditorViewModel(entry.Session, new ShellRegionsViewModel());
        editor.AddCommand.Execute(null);
        var detail = Assert.IsType<ParticleDetailViewModel>(editor.Detail);

        detail.Charge = 1d;
        detail.IsVacancy = true;
        detail.CommitVacancyCommand.Execute(null);

        Assert.True(detail.HasWarning);
        Assert.Contains("PAR004", detail.WarningMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AReferencedDeleteIsBlockedAndNamesItsDependents()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticlesEditorViewModel(entry.Session, new ShellRegionsViewModel());
        var set = entry.Project.Model.ParticleSets.First(candidate => candidate.ParticleIds.Length > 0);
        var particleId = set.ParticleIds[0];

        Assert.True(editor.RequestSelection(particleId.Value));
        editor.RemoveCommand.Execute(null);

        Assert.True(editor.HasBlockedDelete);
        Assert.Contains(set.Name, editor.BlockedDeleteMessage, StringComparison.Ordinal);
        Assert.Equal(particleId, entry.Project.Particles.First(particle => particle.Id == particleId).Id);
        Assert.Contains(
            entry.Session.Project.Particles,
            particle => particle.Id == particleId);

        var direct = entry.Session.EditCoordinator.Apply(
            "Remove particle",
            (core, project) => core.RemoveParticle(project, particleId));
        Assert.Equal(EditApplyOutcome.Rejected, direct.Outcome);
        Assert.Contains(direct.Edit!.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task BulkDeleteOfTwoUnreferencedParticlesIsOneUndoEntry()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticlesEditorViewModel(entry.Session, new ShellRegionsViewModel());
        var originalCount = entry.Project.Particles.Length;
        var first = editor.AddParticle("Bulk delete one");
        var second = editor.AddParticle("Bulk delete two");
        var rows = editor.Table.Rows
            .Where(row => row.ObjectId == first.Value || row.ObjectId == second.Value)
            .ToArray();
        editor.Table.UpdateSelection(rows);
        var undoBefore = entry.Session.Document.UndoStack.Length;

        editor.RemoveCommand.Execute(null);

        Assert.Equal(originalCount, entry.Session.Project.Particles.Length);
        Assert.Equal(undoBefore + 1, entry.Session.Document.UndoStack.Length);
        Assert.True(entry.Session.Document.Undo());
        Assert.Equal(originalCount + 2, entry.Session.Project.Particles.Length);
    }

    [Fact]
    public async Task ReadOnlySessionDisablesEveryParticlePath()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var session = ProjectSession.CreateStandalone(entry.Project, entry.Path, isReadOnly: true);
        using var editor = new ParticlesEditorViewModel(session, new ShellRegionsViewModel());

        Assert.False(editor.AddCommand.CanExecute(null));
        Assert.True(editor.RequestSelection(entry.Project.Particles[0].Id.Value));
        Assert.False(editor.RemoveCommand.CanExecute(null));
        Assert.False(editor.DuplicateCommand.CanExecute(null));
        Assert.NotNull(editor.ActionBar);
        Assert.False(editor.ActionBar!.ApplyCommand.CanExecute(null));
        Assert.NotNull(editor.Detail);
        Assert.False(editor.Detail!.ApplyCommand.CanExecute(null));
        Assert.Equal(ModelEditorState.ReadOnly, editor.State);
    }
}
