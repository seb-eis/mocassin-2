// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Editors.Particles;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// Pins the D-14 refresh contract: a save does not invalidate an open form, another object's edit does not warn,
/// the open object's foreign edit shows a conflict, removal disables Apply, and replacement discards forms.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class EditorRefreshTests
{
    [Fact]
    public async Task SavedDoesNotInvalidateTheOpenForm()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticlesEditorViewModel(entry.Session, new ShellRegionsViewModel());
        var site = entry.Project.Particles.First(particle => particle.Name == "O2-");
        Assert.True(editor.RequestSelection(site.Id.Value));
        var detail = Assert.IsType<ParticleDetailViewModel>(editor.Detail);

        entry.Session.EditCoordinator.NotifySaved();

        Assert.Same(detail, editor.Detail);
        Assert.False(detail.HasConflict);
        Assert.False(detail.IsRemoved);
    }

    [Fact]
    public async Task AForeignEditToAnotherObjectDoesNotWarn()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticlesEditorViewModel(entry.Session, new ShellRegionsViewModel());
        var selected = entry.Project.Particles.First(particle => particle.Name == "O2-");
        var other = entry.Project.Particles.First(particle => particle.Name == "Ce4+");
        Assert.True(editor.RequestSelection(selected.Id.Value));
        var detail = Assert.IsType<ParticleDetailViewModel>(editor.Detail);

        var result = entry.Session.EditCoordinator.Apply(
            "Rename other particle",
            (core, project) => core.UpdateParticle(project, other with { Name = "Ce4+ renamed" }));

        Assert.Equal(EditApplyOutcome.Applied, result.Outcome);
        Assert.Same(detail, editor.Detail);
        Assert.False(detail.HasConflict);
    }

    [Fact]
    public async Task AnEditToTheOpenObjectShowsAConflictAndRevertLoadsTheCommittedValue()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticlesEditorViewModel(entry.Session, new ShellRegionsViewModel());
        var selected = entry.Project.Particles.First(particle => particle.Name == "O2-");
        Assert.True(editor.RequestSelection(selected.Id.Value));
        var detail = Assert.IsType<ParticleDetailViewModel>(editor.Detail);
        Assert.Equal(-2d, detail.Charge);

        var result = entry.Session.EditCoordinator.Apply(
            "Foreign charge",
            (core, project) => core.UpdateParticle(project, selected with { Charge = new Mocassin.Core.Units.ChargeElementary(-1d) }));

        Assert.Equal(EditApplyOutcome.Applied, result.Outcome);
        Assert.True(detail.HasConflict);
        Assert.Equal(-2d, detail.Charge);

        detail.RevertCommand.Execute(null);
        Assert.False(detail.HasConflict);
        Assert.Equal(-1d, detail.Charge);
    }

    [Fact]
    public async Task RemovingTheOpenObjectDisablesApplyAndReplacingDiscardsTheForm()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticlesEditorViewModel(entry.Session, new ShellRegionsViewModel());
        var added = editor.AddParticle("Refresh target");
        Assert.True(editor.RequestSelection(added.Value));
        var detail = Assert.IsType<ParticleDetailViewModel>(editor.Detail);

        var removed = entry.Session.EditCoordinator.Apply(
            "Remove target",
            (core, project) => core.RemoveParticle(project, added));

        Assert.Equal(EditApplyOutcome.Applied, removed.Outcome);
        Assert.Same(detail, editor.Detail);
        Assert.True(detail.IsRemoved);
        Assert.False(detail.ApplyCommand.CanExecute(null));

        entry.Session.EditCoordinator.ReplaceDocument(entry.Session.Document, canEdit: true);
        Assert.Null(editor.Detail);
    }
}
