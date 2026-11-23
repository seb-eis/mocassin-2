// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors.Particles;
using Mocassin.App.ViewModels.Editors.Sets;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Packages.Projects;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// The particle/set half of the 24A acceptance row: author the ceria particles and sets from the empty blank
/// project through the real editors and coordinator, compare by name/value, then undo every edit back to blank.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ModelEditorAcceptanceTests
{
    [Fact]
    public async Task CeriaParticlesAndSetsAreAuthoredFromBlankAndFullyUndone()
    {
        var blank = await LoadAsync("samples", "blank", "blank.mocprj");
        var ceria = await ProjectFixtures.LoadCeriaAsync();
        var session = ProjectSession.CreateStandalone(blank.Project, blank.Path);
        using var particles = new ParticlesEditorViewModel(session, new ShellRegionsViewModel());
        using var sets = new ParticleSetsEditorViewModel(session, new ShellRegionsViewModel());

        foreach (var source in ceria.Project.Particles)
        {
            var id = particles.AddParticle(source.Name);
            Assert.True(particles.RequestSelection(id.Value));
            var detail = Assert.IsType<ParticleDetailViewModel>(particles.Detail);
            detail.Symbol = source.Symbol;
            detail.Charge = source.Charge.Value;
            detail.IsVacancy = source.IsVacancy;
            detail.ApplyCommand.Execute(null);
        }

        Assert.Equal(ceria.Project.Particles.Length, session.Project.Particles.Length);
        foreach (var source in ceria.Project.Particles)
        {
            var authored = session.Project.Particles.Single(particle => particle.Name == source.Name);
            Assert.Equal(source.Symbol, authored.Symbol);
            Assert.Equal(source.Charge.Value, authored.Charge.Value);
            Assert.Equal(source.IsVacancy, authored.IsVacancy);
        }

        foreach (var source in ceria.Project.Model.ParticleSets)
        {
            var memberNames = source.ParticleIds
                .Select(id => ceria.Project.Find(id)!.Name)
                .ToArray();
            var members = memberNames
                .Select(name => session.Project.Particles.Single(particle => particle.Name == name).Id)
                .ToImmutableArray();
            var id = sets.AddSet(source.Name, members);
            Assert.NotNull(id);
        }

        Assert.Equal(ceria.Project.Model.ParticleSets.Length, session.Project.Model.ParticleSets.Length);
        foreach (var source in ceria.Project.Model.ParticleSets)
        {
            var authored = session.Project.Model.ParticleSets.Single(set => set.Name == source.Name);
            Assert.Equal(
                source.ParticleIds.Select(id => ceria.Project.Find(id)!.Name),
                authored.ParticleIds.Select(id => session.Project.Find(id)!.Name));
        }

        while (session.EditCoordinator.CanUndo)
        {
            Assert.True(session.EditCoordinator.Undo());
        }

        Assert.Empty(session.Project.Particles);
        Assert.Empty(session.Project.Model.ParticleSets);
        Assert.False(session.Document.IsDirty);
    }

    [Fact]
    public async Task EditingOneFieldOnAPopulatedProjectIsOneUndoEntry()
    {
        var ceria = await ProjectFixtures.LoadCeriaAsync();
        var session = ceria.Session;
        using var particles = new ParticlesEditorViewModel(session, new ShellRegionsViewModel());
        var target = session.Project.Particles.First(particle => particle.Name == "O2-");
        var originalCharge = target.Charge.Value;
        var undoDepth = session.Document.UndoStack.Length;

        Assert.True(particles.RequestSelection(target.Id.Value));
        var detail = Assert.IsType<ParticleDetailViewModel>(particles.Detail);
        detail.Charge = -1d;
        detail.CommitChargeCommand.Execute(null);

        Assert.Equal(undoDepth + 1, session.Document.UndoStack.Length);
        Assert.Equal(-1d, session.Project.Find(target.Id)?.Charge.Value);

        Assert.True(session.EditCoordinator.Undo());
        Assert.Equal(originalCharge, session.Project.Find(target.Id)?.Charge.Value);
        Assert.Equal(undoDepth, session.Document.UndoStack.Length);
    }

    private static async Task<ProjectWorkspaceEntry> LoadAsync(params string[] relativePath)
    {
        var path = RepositoryPaths.Combine(relativePath);
        var store = new ProjectStore(new ManualTimeProvider(), new ProjectStoreOptions { ValidateOnLoad = false });
        var result = await store.LoadAsync(path);
        if (result.Project is null)
        {
            throw new InvalidOperationException($"The fixture {path} did not load: {result.FailureKind}.");
        }

        return new ProjectWorkspaceEntry(
            result.Project,
            path,
            new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero),
            isReadOnly: false);
    }
}
