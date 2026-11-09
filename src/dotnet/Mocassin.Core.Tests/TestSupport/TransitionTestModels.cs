// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.TestSupport;

internal static class TransitionTestModels
{
    public static readonly DateTimeOffset CreatedUtc =
        DateTimeOffset.Parse("2026-11-09T12:00:00Z", CultureInfo.InvariantCulture);

    public static readonly Guid ProjectId = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f00");

    public static StateChange ForwardStateChange { get; } = new(
        StateChangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f21")),
        "O2- -> VO",
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f10")),
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f11")));

    public static StateChange ReverseStateChange { get; } = new(
        StateChangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f20")),
        "VO -> O2-",
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f11")),
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f10")));

    public static StateChangeGroup ForwardGroup { get; } = new(
        StateChangeGroupId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f30")),
        "Forward group",
        [ForwardStateChange.Id]);

    public static StateChangeGroup ReverseGroup { get; } = new(
        StateChangeGroupId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f31")),
        "Reverse group",
        [ReverseStateChange.Id]);

    public static StateChange UnstableStateChange { get; } = new(
        StateChangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f22")),
        "O2- -> void",
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f10")),
        ParticleId.Void);

    public static StateChangeGroup UnstableGroup { get; } = new(
        StateChangeGroupId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f32")),
        "Unstable group",
        [UnstableStateChange.Id]);

    public static TransitionChain MigrationChain { get; } = new(
        TransitionChainId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f40")),
        "3-site migration",
        TransitionPattern.Migration3,
        [ForwardGroup.Id, UnstableGroup.Id, ForwardGroup.Id],
        [TransitionLinker.Dynamic, TransitionLinker.Dynamic],
        GeneratesReverse: true);

    public static TransitionChain MetropolisChain { get; } = new(
        TransitionChainId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f41")),
        "2-site exchange",
        TransitionPattern.Metropolis2,
        [ForwardGroup.Id, ReverseGroup.Id],
        [TransitionLinker.Dynamic],
        GeneratesReverse: true);

    public static KineticTransition KineticTransition { get; } = new(
        KineticTransitionId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f50")),
        "O migration",
        MigrationChain.Id,
        [
            new Fractional3(0.25, 0.25, 0.25),
            new Fractional3(0.5, 0.25, 0.25),
            new Fractional3(0.75, 0.25, 0.25),
        ],
        ReverseTransitionId: null);

    public static MetropolisTransition MetropolisTransition { get; } = new(
        MetropolisTransitionId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f60")),
        "O exchange",
        MetropolisChain.Id,
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f16")),
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f18")),
        ReverseTransitionId: null);

    public static ModelDefinition CreateEmptyModel() => new();

    public static ModelDefinition CreateTransitionOnlyModel() => new()
    {
        StateChanges = [ReverseStateChange, ForwardStateChange, UnstableStateChange],
        StateChangeGroups = [ReverseGroup, ForwardGroup, UnstableGroup],
        Chains = [MetropolisChain, MigrationChain],
        KineticTransitions = [KineticTransition],
        MetropolisTransitions = [MetropolisTransition],
    };

    public static ModelDefinition CreateCombinedModel()
    {
        var transitionModel = CreateTransitionOnlyModel();
        var oxygen = new Particle(
            ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f10")),
            "O2-",
            "O",
            new ChargeElementary(-2.0),
            IsVacancy: false);
        var vacancy = new Particle(
            ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f11")),
            "Vacancy",
            "Vc",
            new ChargeElementary(0.0),
            IsVacancy: true);
        var cerium = new Particle(
            ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f12")),
            "Ce4+",
            "Ce",
            new ChargeElementary(4.0),
            IsVacancy: false);
        var oxygenSet = new ParticleSet(
            ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f13")),
            "Oxygen site",
            [oxygen.Id, vacancy.Id]);
        var ceriumSet = new ParticleSet(
            ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f14")),
            "Cerium site",
            [cerium.Id]);

        return transitionModel with
        {
            Particles = [oxygen, vacancy, cerium],
            ParticleSets = [oxygenSet, ceriumSet],
            Structure = StructureModel.Default with
            {
                Name = "P1 transition test",
                SpaceGroup = new SpaceGroupRef(1, AxisSetting.None, OriginChoice.None),
                Cell = new CellParameters(
                    new LengthAngstrom(5.41),
                    new LengthAngstrom(5.41),
                    new LengthAngstrom(5.41),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0)),
                Sites =
                [
                    new CellSite(
                        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f15")),
                        "Ce",
                        Fractional3.Zero,
                        ceriumSet.Id,
                        SiteStability.Stable),
                    new CellSite(
                        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f16")),
                        "O",
                        new Fractional3(0.25, 0.25, 0.25),
                        oxygenSet.Id,
                        SiteStability.Stable),
                    new CellSite(
                        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f17")),
                        "O transition",
                        new Fractional3(0.5, 0.25, 0.25),
                        oxygenSet.Id,
                        SiteStability.Unstable),
                    new CellSite(
                        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f18")),
                        "O endpoint",
                        new Fractional3(0.75, 0.25, 0.25),
                        oxygenSet.Id,
                        SiteStability.Stable),
                ],
            },
        };
    }

    public static ProjectModel CreateProject(ModelDefinition? model = null) =>
        new(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                ProjectId,
                new string('a', 64),
                "Transition model",
                null,
                CreatedUtc,
                CreatedUtc),
            model ?? CreateEmptyModel(),
            [],
            [],
            [],
            ViewSettings.Default);
}
