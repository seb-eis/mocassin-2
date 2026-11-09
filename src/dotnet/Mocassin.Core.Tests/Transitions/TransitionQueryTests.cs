// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Transitions;

[Trait("Tier", "PR-fast")]
public sealed class TransitionQueryTests
{
    [Fact]
    public void ProjectImplementsProjectQueryAndFindsTransitionObjects()
    {
        var project = TransitionTestModels.CreateProject(TransitionTestModels.CreateCombinedModel());
        Assert.IsAssignableFrom<IProjectQuery>(project);
        var stateChange = project.Model.StateChanges[0];
        var group = project.Model.StateChangeGroups[0];
        var chain = project.Model.Chains[0];
        var kinetic = project.Model.KineticTransitions[0];
        var metropolis = project.Model.MetropolisTransitions[0];

        Assert.Same(stateChange, project.Find(stateChange.Id));
        Assert.Same(group, project.Find(group.Id));
        Assert.Same(chain, project.Find(chain.Id));
        Assert.Same(kinetic, project.Find(kinetic.Id));
        Assert.Same(metropolis, project.Find(metropolis.Id));
        Assert.Null(project.Find(StateChangeId.New()));
        Assert.Null(project.Find(StateChangeGroupId.New()));
        Assert.Null(project.Find(TransitionChainId.New()));
        Assert.Null(project.Find(KineticTransitionId.New()));
        Assert.Null(project.Find(MetropolisTransitionId.New()));
    }

    [Fact]
    public void TransitionHashChangesWithClosureAndFullParticleOrder()
    {
        var project = TransitionTestModels.CreateProject(TransitionTestModels.CreateCombinedModel());
        var transition = project.Model.KineticTransitions[0];
        var reference = TransitionRef.From(transition.Id);
        var original = ProjectHasher.ComputeTransition(project, reference);

        var renamedChange = project.Model.StateChanges.Single(static item => item.Name == "O2- -> VO") with
        {
            Name = "Renamed",
        };
        var renamed = project with
        {
            Model = project.Model with
            {
                StateChanges = [renamedChange, .. project.Model.StateChanges.Skip(1)],
            },
        };
        var extraParticle = project.Model.Particles[0] with
        {
            Id = ParticleId.New(),
            Name = "Unreferenced",
            Symbol = "U",
        };
        var withExtraParticle = project with
        {
            Model = project.Model with
            {
                Particles = [.. project.Model.Particles, extraParticle],
            },
        };
        var unrelated = project with
        {
            ViewSettings = ViewSettings.Default,
        };

        Assert.NotEqual(original, ProjectHasher.ComputeTransition(renamed, reference));
        Assert.NotEqual(original, ProjectHasher.ComputeTransition(withExtraParticle, reference));
        Assert.Equal(original, ProjectHasher.ComputeTransition(unrelated, reference));
    }

    [Fact]
    public void DifferentTransitionKindsHaveDifferentClosures()
    {
        var project = TransitionTestModels.CreateProject(TransitionTestModels.CreateCombinedModel());
        var kinetic = project.Model.KineticTransitions[0];
        var metropolis = project.Model.MetropolisTransitions[0];

        Assert.NotEqual(
            ProjectHasher.ComputeTransition(project, TransitionRef.From(kinetic.Id)),
            ProjectHasher.ComputeTransition(project, TransitionRef.From(metropolis.Id)));
    }
}
