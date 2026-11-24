// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Editors.Sets;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;

namespace Mocassin.App.Tests.Editors.Sets;

/// <summary>
/// Pins the particle-set editor's feature-2 acceptance rows: ordered membership, the default badge, void/duplicate
/// refusal, the empty-set error, duplicate, drag/keyboard equivalence, and blocked delete.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ParticleSetEditorTests
{
    [Fact]
    public async Task OrderedMembershipAndTheDefaultBadgeFollowMemberOrder()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticleSetsEditorViewModel(entry.Session, new ShellRegionsViewModel());
        var set = entry.Project.Model.ParticleSets.First(candidate => candidate.Name == "Oxygen site");
        Assert.True(editor.RequestSelection(set.Id.Value));
        var detail = Assert.IsType<ParticleSetDetailViewModel>(editor.Detail);

        Assert.Equal(set.ParticleIds.Length, detail.MemberCount);
        Assert.True(detail.Members[0].IsDefault);
        Assert.Contains("default", detail.Members[0].DisplayName, StringComparison.Ordinal);
        Assert.Contains("default", detail.OccupationChips[0], StringComparison.Ordinal);
        Assert.False(detail.Members[1].IsDefault);

        Assert.True(detail.MoveMember(0, 1));
        Assert.Equal(set.ParticleIds[1], detail.Members[0].Id);
        Assert.True(detail.Members[0].IsDefault);
        Assert.Equal(set.ParticleIds[0], detail.Members[1].Id);
    }

    [Fact]
    public async Task VoidAndDuplicateMembersAreRefused()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticleSetsEditorViewModel(entry.Session, new ShellRegionsViewModel());
        var set = entry.Project.Model.ParticleSets.First(candidate => candidate.ParticleIds.Length > 1);
        Assert.True(editor.RequestSelection(set.Id.Value));
        var detail = Assert.IsType<ParticleSetDetailViewModel>(editor.Detail);

        Assert.False(detail.AddMember(ParticleId.Void));
        Assert.Contains("PAR005", detail.StatusMessage!, StringComparison.Ordinal);

        Assert.False(detail.AddMember(set.ParticleIds[0]));
        Assert.Contains("PAR012", detail.StatusMessage!, StringComparison.Ordinal);
        Assert.Equal(set.ParticleIds.Length, detail.MemberCount);
    }

    [Fact]
    public async Task AnEmptySetIsRejectedInlineAndApplyIsDisabled()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var empty = new ParticleSet(ParticleSetId.New(), "Empty set", []);
        var detail = new ParticleSetDetailViewModel(empty, entry.Project);

        Assert.Equal(0, detail.MemberCount);
        Assert.True(detail.HasValidationMessage);
        Assert.Contains("PAR005", detail.ValidationMessage!, StringComparison.Ordinal);
        Assert.False(detail.CanApply);

        var project = entry.Project with
        {
            Model = entry.Project.Model with { ParticleSets = [empty] },
        };
        var diagnostics = Mocassin.Core.Project.ProjectValidator.Validate(project);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "PAR005");
    }

    [Fact]
    public async Task DragAndKeyboardPathsProduceTheSameOrder()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticleSetsEditorViewModel(entry.Session, new ShellRegionsViewModel());
        var set = entry.Project.Model.ParticleSets.First(candidate => candidate.Name == "Oxygen site");
        var additional = entry.Project.Particles.First(particle =>
            set.ParticleIds.All(id => id != particle.Id));

        var keyboard = new ParticleSetDetailViewModel(set, entry.Project);
        keyboard.SelectedProjectParticle = keyboard.ProjectParticles.First(choice => choice.Id == additional.Id);
        keyboard.AddMember(additional.Id);
        var keyboardOrder = keyboard.Members.Select(member => member.Id).ToArray();

        var drag = new ParticleSetDetailViewModel(set, entry.Project);
        Assert.True(drag.TryDropParticle(additional.Id, drag.MemberCount));
        var dragOrder = drag.Members.Select(member => member.Id).ToArray();

        Assert.Equal(keyboardOrder, dragOrder);
    }

    [Fact]
    public async Task DuplicateCopiesMembersUnderANewIdAndName()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticleSetsEditorViewModel(entry.Session, new ShellRegionsViewModel());
        var set = entry.Project.Model.ParticleSets[0];
        Assert.True(editor.RequestSelection(set.Id.Value));
        var originalCount = entry.Project.Model.ParticleSets.Length;

        editor.DuplicateCommand.Execute(null);

        Assert.Equal(originalCount + 1, entry.Session.Project.Model.ParticleSets.Length);
        var copy = entry.Session.Project.Model.ParticleSets
            .Single(candidate => candidate.Id != set.Id && candidate.Name.StartsWith(set.Name, StringComparison.Ordinal));
        Assert.Equal(set.ParticleIds, copy.ParticleIds);
        Assert.NotEqual(set.Id, copy.Id);
    }

    [Fact]
    public async Task ADeletionBlockedByASiteNamesTheDependent()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticleSetsEditorViewModel(entry.Session, new ShellRegionsViewModel());
        var set = entry.Project.Model.ParticleSets.First(candidate =>
            entry.Project.Structure.Sites.Any(site => site.ParticleSetId == candidate.Id));
        var site = entry.Project.Structure.Sites.First(site => site.ParticleSetId == set.Id);

        Assert.True(editor.RequestSelection(set.Id.Value));
        editor.RemoveCommand.Execute(null);
        Assert.True(editor.HasBlockedDelete);
        Assert.Contains(site.Name, editor.BlockedDeleteMessage, StringComparison.Ordinal);
        Assert.NotEmpty(editor.SelectedUsages);
        ModelPath? requested = null;
        editor.NavigationRequested += (_, path) => requested = path;
        editor.OpenUsageCommand.Execute(editor.SelectedUsages[0]);
        Assert.NotNull(requested);
        Assert.Equal(editor.SelectedUsages[0].Reference.ObjectId, requested!.ObjectId);
        Assert.Contains(entry.Session.Project.Model.ParticleSets, candidate => candidate.Id == set.Id);

        var direct = entry.Session.EditCoordinator.Apply(
            "Remove set",
            (core, project) => core.RemoveParticleSet(project, set.Id));
        Assert.Equal(EditApplyOutcome.Rejected, direct.Outcome);
        Assert.Contains(direct.Edit!.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task AddCreatesASetWithTheFirstProjectParticleAsDefault()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new ParticleSetsEditorViewModel(entry.Session, new ShellRegionsViewModel());
        var originalCount = entry.Project.Model.ParticleSets.Length;

        editor.AddCommand.Execute(null);

        Assert.Equal(originalCount + 1, entry.Session.Project.Model.ParticleSets.Length);
        var detail = Assert.IsType<ParticleSetDetailViewModel>(editor.Detail);
        Assert.Equal(1, detail.MemberCount);
        Assert.Equal(entry.Project.Particles[0].Id, detail.Members[0].Id);
        Assert.True(detail.Members[0].IsDefault);
    }

    [Fact]
    public async Task ReadOnlySessionDisablesSetEdits()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var session = Mocassin.App.ViewModels.Projects.ProjectSession.CreateStandalone(
            entry.Project,
            entry.Path,
            isReadOnly: true);
        using var editor = new ParticleSetsEditorViewModel(session, new ShellRegionsViewModel());

        Assert.False(editor.AddCommand.CanExecute(null));
        Assert.True(editor.RequestSelection(entry.Project.Model.ParticleSets[0].Id.Value));
        var detail = Assert.IsType<ParticleSetDetailViewModel>(editor.Detail);
        Assert.False(detail.ApplyCommand.CanExecute(null));
        Assert.False(detail.AddMemberCommand.CanExecute(null));
    }
}
