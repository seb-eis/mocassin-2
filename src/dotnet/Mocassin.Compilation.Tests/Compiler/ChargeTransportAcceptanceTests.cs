// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Covers the named charge-transport acceptance cases.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ChargeTransportAcceptanceTests
{
    [Fact]
    public void DistinctChargeExchangeHasNonZeroTransport()
    {
        var project = CreateMetropolisProject();

        var result = new ModelCompiler().Compile(project);

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        Assert.Contains(
            result.Model!.Transitions.Rules,
            static item => item.ChargeTransportVector.X != 0.0);
    }

    [Fact]
    public void NonCollinearPathHasTwoNonZeroComponents()
    {
        var project = CreateMigrationProject();

        var result = new ModelCompiler().Compile(project);

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        Assert.Contains(
            result.Model!.Transitions.Rules,
            static item => item.ChargeTransportVector.X != 0.0 && item.ChargeTransportVector.Y != 0.0);
    }

    private static Project CreateMetropolisProject()
    {
        var a = new Particle(ParticleId.New(), "A+", "A", new ChargeElementary(1.0), false);
        var b = new Particle(ParticleId.New(), "B-", "B", new ChargeElementary(-1.0), false);
        var set = new ParticleSet(ParticleSetId.New(), "AB", [a.Id, b.Id]);
        var s0 = new CellSite(CellSiteId.New(), "A site", new Fractional3(0.25, 0.0, 0.0), set.Id, SiteStability.Stable);
        var s1 = new CellSite(CellSiteId.New(), "B site", new Fractional3(0.75, 0.0, 0.0), set.Id, SiteStability.Stable);
        var forward = new StateChange(StateChangeId.New(), "A -> B", a.Id, b.Id);
        var reverse = new StateChange(StateChangeId.New(), "B -> A", b.Id, a.Id);
        var group = new StateChangeGroup(StateChangeGroupId.New(), "Exchange", [forward.Id, reverse.Id]);
        var chain = new TransitionChain(
            TransitionChainId.New(), "Exchange chain", TransitionPattern.Metropolis2,
            [group.Id, group.Id], [TransitionLinker.Dynamic], GeneratesReverse: true);
        var transition = new MetropolisTransition(
            MetropolisTransitionId.New(), "Exchange", chain.Id, s0.Id, s1.Id, ReverseTransitionId: null);
        var simulation = new MetropolisSimulation
        {
            Id = SimulationId.New(),
            Name = "MMC",
            TransitionIds = [TransitionRef.From(transition.Id)],
        };
        return CreateProject(
            [a, b],
            [set],
            [s0, s1],
            [forward, reverse],
            [group],
            [chain],
            kineticTransitions: [],
            metropolisTransitions: [transition],
            simulations: [simulation]);
    }

    private static Project CreateMigrationProject()
    {
        var oxygen = new Particle(ParticleId.New(), "O2-", "O", new ChargeElementary(-2.0), false);
        var vacancy = new Particle(ParticleId.New(), "Vacancy", "V", new ChargeElementary(0.0), true);
        var set = new ParticleSet(ParticleSetId.New(), "O/V", [oxygen.Id, vacancy.Id]);
        var s0 = new CellSite(CellSiteId.New(), "O0", new Fractional3(0.1, 0.1, 0.1), set.Id, SiteStability.Stable);
        var s1 = new CellSite(CellSiteId.New(), "O1", new Fractional3(0.2, 0.3, 0.1), set.Id, SiteStability.Unstable);
        var s2 = new CellSite(CellSiteId.New(), "O2", new Fractional3(0.4, 0.2, 0.1), set.Id, SiteStability.Stable);
        var forward = new StateChange(StateChangeId.New(), "O -> V", oxygen.Id, vacancy.Id);
        var reverse = new StateChange(StateChangeId.New(), "V -> O", vacancy.Id, oxygen.Id);
        var unstable = new StateChange(StateChangeId.New(), "O -> void", oxygen.Id, ParticleId.Void);
        var stableGroup = new StateChangeGroup(StateChangeGroupId.New(), "Stable", [forward.Id, reverse.Id]);
        var unstableGroup = new StateChangeGroup(StateChangeGroupId.New(), "Unstable", [unstable.Id]);
        var chain = new TransitionChain(
            TransitionChainId.New(), "Migration", TransitionPattern.Migration3,
            [stableGroup.Id, unstableGroup.Id, stableGroup.Id],
            [TransitionLinker.Dynamic, TransitionLinker.Dynamic], GeneratesReverse: true);
        var transition = new KineticTransition(
            KineticTransitionId.New(), "Migration", chain.Id,
            [s0.Fractional, s1.Fractional, s2.Fractional], ReverseTransitionId: null);
        var simulation = new KineticSimulation
        {
            Id = SimulationId.New(),
            Name = "KMC",
            TransitionIds = [TransitionRef.From(transition.Id)],
        };
        return CreateProject(
            [oxygen, vacancy],
            [set],
            [s0, s1, s2],
            [forward, reverse, unstable],
            [stableGroup, unstableGroup],
            [chain],
            kineticTransitions: [transition],
            metropolisTransitions: [],
            simulations: [simulation]);
    }

    private static Project CreateProject(
        ImmutableArray<Particle> particles,
        ImmutableArray<ParticleSet> sets,
        ImmutableArray<CellSite> sites,
        ImmutableArray<StateChange> stateChanges,
        ImmutableArray<StateChangeGroup> groups,
        ImmutableArray<TransitionChain> chains,
        ImmutableArray<KineticTransition> kineticTransitions,
        ImmutableArray<MetropolisTransition> metropolisTransitions,
        ImmutableArray<Simulation> simulations) => new(
        schema: null,
        schemaVersion: ProjectSchema.CurrentVersion,
        metadata: new ProjectMetadata(
            Guid.NewGuid(), new string('0', 64), "charge transport test",
            Description: null, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch),
        model: ModelDefinition.Empty with
        {
            Particles = particles,
            ParticleSets = sets,
            Structure = StructureModel.Default with
            {
                SpaceGroup = new SpaceGroupRef(1, AxisSetting.None, OriginChoice.None),
                Cell = new CellParameters(
                    new LengthAngstrom(5.0), new LengthAngstrom(5.0), new LengthAngstrom(5.0),
                    new AngleDegrees(90.0), new AngleDegrees(90.0), new AngleDegrees(90.0)),
                Sites = sites,
            },
            StateChanges = stateChanges,
            StateChangeGroups = groups,
            Chains = chains,
            KineticTransitions = kineticTransitions,
            MetropolisTransitions = metropolisTransitions,
            Simulations = simulations,
            StableEnvironment = new Core.Model.Energy.StableEnvironment(new LengthAngstrom(2.0), []),
        },
        parameterizations: [],
        jobTemplates: [],
        buildTemplates: [],
        viewSettings: ViewSettings.Default);
}
