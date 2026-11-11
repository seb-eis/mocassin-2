// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Transitions;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.TestSupport;

internal static class SimulationTestModels
{
    public static readonly SimulationId KineticId =
        SimulationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6b10"));

    public static readonly SimulationId MetropolisId =
        SimulationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6b11"));

    public static KineticSimulation CreateKinetic(
        ImmutableArray<TransitionRef>? transitionIds = null,
        string name = "KMC simulation")
        => new()
        {
            Id = KineticId,
            Name = name,
            TransitionIds = transitionIds ?? [TransitionRef.From(TransitionTestModels.KineticTransition.Id)],
        };

    public static MetropolisSimulation CreateMetropolis(
        ImmutableArray<TransitionRef>? transitionIds = null,
        string name = "MMC simulation")
        => new()
        {
            Id = MetropolisId,
            Name = name,
            TransitionIds = transitionIds ?? [TransitionRef.From(TransitionTestModels.MetropolisTransition.Id)],
        };

    public static ModelDefinition CreateModel(params Simulation[] simulations)
    {
        var model = TransitionTestModels.CreateCombinedModel();
        return model with { Simulations = [.. simulations] };
    }

    public static ProjectModel CreateProject(params Simulation[] simulations) =>
        TransitionTestModels.CreateProject(CreateModel(simulations));

    public static ModelDefinition AddSecondKineticTransition(this ModelDefinition model)
    {
        var transition = TransitionTestModels.KineticTransition with
        {
            Id = KineticTransitionId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6b20")),
            Name = "Second kinetic transition",
        };
        return model with { KineticTransitions = [.. model.KineticTransitions, transition] };
    }
}
