// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Editing;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Editing;

[Trait("Tier", "PR-fast")]
public sealed class ProjectEditorTransitionTests
{
    [Fact]
    public void AddUpdateAndRemoveStateChangeAndGroup()
    {
        var editor = new ProjectEditor();
        var project = TransitionTestModels.CreateProject(TransitionTestModels.CreateCombinedModel());
        var stateChange = new StateChange(
            StateChangeId.New(),
            "New state change",
            TransitionTestModels.ForwardStateChange.DonorParticleId,
            TransitionTestModels.ForwardStateChange.AcceptorParticleId);

        var added = editor.AddStateChange(project, stateChange);
        Assert.True(added.IsSuccess);
        Assert.Contains(added.Project.Model.StateChanges, candidate => candidate.Id == stateChange.Id);

        var updated = editor.UpdateStateChange(added.Project, stateChange with { Name = "Updated state change" });
        Assert.True(updated.IsSuccess);
        Assert.Equal(
            "Updated state change",
            updated.Project.Model.StateChanges.Single(candidate => candidate.Id == stateChange.Id).Name);

        var group = new StateChangeGroup(StateChangeGroupId.New(), "New group", [stateChange.Id]);
        var groupAdded = editor.AddStateChangeGroup(updated.Project, group);
        Assert.True(groupAdded.IsSuccess);
        var groupUpdated = editor.UpdateStateChangeGroup(groupAdded.Project, group with { Name = "Updated group" });
        Assert.True(groupUpdated.IsSuccess);

        var groupRemoved = editor.RemoveStateChangeGroup(groupUpdated.Project, group.Id);
        Assert.True(groupRemoved.IsSuccess);
        var stateChangeRemoved = editor.RemoveStateChange(groupRemoved.Project, stateChange.Id);
        Assert.True(stateChangeRemoved.IsSuccess);
    }

    [Fact]
    public void AddUpdateRemoveAndDuplicateChainsAndTransitions()
    {
        var editor = new ProjectEditor();
        var project = TransitionTestModels.CreateProject(TransitionTestModels.CreateCombinedModel());
        var chain = TransitionTestModels.MigrationChain with
        {
            Id = TransitionChainId.New(),
            Name = "New migration chain",
        };

        var chainAdded = editor.AddTransitionChain(project, chain);
        Assert.True(chainAdded.IsSuccess);
        var chainUpdated = editor.UpdateTransitionChain(chainAdded.Project, chain with { Name = "Updated chain" });
        Assert.True(chainUpdated.IsSuccess);
        var chainIdsBeforeDuplicate = chainUpdated.Project.Model.Chains.Select(static item => item.Id).ToHashSet();
        var chainDuplicated = editor.DuplicateTransitionChain(chainUpdated.Project, chain.Id);
        Assert.True(chainDuplicated.IsSuccess);
        var duplicateChain = chainDuplicated.Project.Model.Chains.Single(
            candidate => !chainIdsBeforeDuplicate.Contains(candidate.Id));
        Assert.NotEqual(chain.Id, duplicateChain.Id);
        Assert.NotEqual(chain.Name, duplicateChain.Name);

        var kinetic = new KineticTransition(
            KineticTransitionId.New(),
            "New kinetic transition",
            chain.Id,
            TransitionTestModels.KineticTransition.ReferencePath,
            ReverseTransitionId: null);
        var kineticAdded = editor.AddKineticTransition(chainUpdated.Project, kinetic);
        Assert.True(kineticAdded.IsSuccess);
        var kineticUpdated = editor.UpdateKineticTransition(kineticAdded.Project, kinetic with { Name = "Updated kinetic" });
        Assert.True(kineticUpdated.IsSuccess);
        var kineticIdsBeforeDuplicate = kineticUpdated.Project.Model.KineticTransitions
            .Select(static item => item.Id)
            .ToHashSet();
        var kineticDuplicated = editor.DuplicateKineticTransition(kineticUpdated.Project, kinetic.Id);
        Assert.True(kineticDuplicated.IsSuccess);
        var duplicateKinetic = kineticDuplicated.Project.Model.KineticTransitions.Single(
            candidate => !kineticIdsBeforeDuplicate.Contains(candidate.Id));
        Assert.Null(duplicateKinetic.ReverseTransitionId);
        Assert.True(editor.RemoveKineticTransition(kineticDuplicated.Project, duplicateKinetic.Id).IsSuccess);

        var metropolis = new MetropolisTransition(
            MetropolisTransitionId.New(),
            "New Metropolis transition",
            TransitionTestModels.MetropolisChain.Id,
            TransitionTestModels.MetropolisTransition.SiteAId,
            TransitionTestModels.MetropolisTransition.SiteBId,
            ReverseTransitionId: null);
        var metropolisAdded = editor.AddMetropolisTransition(kineticUpdated.Project, metropolis);
        Assert.True(metropolisAdded.IsSuccess);
        var metropolisUpdated = editor.UpdateMetropolisTransition(
            metropolisAdded.Project,
            metropolis with { Name = "Updated Metropolis" });
        Assert.True(metropolisUpdated.IsSuccess);
        var metropolisIdsBeforeDuplicate = metropolisUpdated.Project.Model.MetropolisTransitions
            .Select(static item => item.Id)
            .ToHashSet();
        var metropolisDuplicated = editor.DuplicateMetropolisTransition(metropolisUpdated.Project, metropolis.Id);
        Assert.True(metropolisDuplicated.IsSuccess);
        var duplicateMetropolis = metropolisDuplicated.Project.Model.MetropolisTransitions.Single(
            candidate => !metropolisIdsBeforeDuplicate.Contains(candidate.Id));
        Assert.Null(duplicateMetropolis.ReverseTransitionId);
        Assert.True(editor.RemoveMetropolisTransition(metropolisDuplicated.Project, duplicateMetropolis.Id).IsSuccess);

        Assert.True(editor.RemoveTransitionChain(chainDuplicated.Project, duplicateChain.Id).IsSuccess);
        Assert.True(editor.RemoveTransitionChain(chainUpdated.Project, chain.Id).IsSuccess);
    }

    [Fact]
    public void RemovingReferencedObjectFailsAndReportsDependents()
    {
        var editor = new ProjectEditor();
        var project = TransitionTestModels.CreateProject(TransitionTestModels.CreateCombinedModel());
        var stateChangeId = TransitionTestModels.ForwardStateChange.Id;

        var result = editor.RemoveStateChange(project, stateChangeId);

        Assert.False(result.IsSuccess);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(ProjectDiagnosticCodes.InvalidDocument, diagnostic.Code);
        Assert.NotEmpty(diagnostic.Related);
        Assert.Same(project, result.Project);
    }

    [Fact]
    public void RemovingDependentsCascadesGroupsChainsAndTransitions()
    {
        var editor = new ProjectEditor();
        var project = TransitionTestModels.CreateProject(TransitionTestModels.CreateCombinedModel());

        var result = editor.RemoveStateChange(
            project,
            TransitionTestModels.ForwardStateChange.Id,
            removeDependents: true);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(result.Project.Model.StateChanges, static item => item.Name == "O2- -> VO");
        Assert.Empty(result.Project.Model.Chains);
        Assert.Empty(result.Project.Model.KineticTransitions);
        Assert.Empty(result.Project.Model.MetropolisTransitions);
    }

    [Fact]
    public void MakeStateChangeGroupAddsAnOrderedGroup()
    {
        var editor = new ProjectEditor();
        var project = TransitionTestModels.CreateProject(TransitionTestModels.CreateCombinedModel());
        var ids = ImmutableArray.Create(
            TransitionTestModels.ForwardStateChange.Id,
            TransitionTestModels.ReverseStateChange.Id);

        var result = editor.MakeStateChangeGroup(project, "Made group", ids);

        Assert.True(result.IsSuccess);
        var group = Assert.Single(result.Project.Model.StateChangeGroups, candidate => candidate.Name == "Made group");
        Assert.Equal(ids, group.StateChangeIds);
    }
}
