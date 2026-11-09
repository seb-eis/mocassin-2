// SPDX-License-Identifier: MIT

using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;

namespace Mocassin.Symmetry.Tests.TestSupport;

internal static class TransitionTestModels
{
    public const string Site1 = "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f15";
    public const string Site2 = "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f16";
    public const string Site3 = "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f17";
    public const string Site4 = "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f18";
    public const string Site5 = "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f19";

    public static Particle Oxygen { get; } = new(
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f10")),
        "O2-",
        "O",
        new ChargeElementary(-2.0),
        IsVacancy: false);

    public static Particle Vacancy { get; } = new(
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f11")),
        "Vacancy",
        "Vc",
        new ChargeElementary(0.0),
        IsVacancy: true);

    public static Particle Cerium { get; } = new(
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f12")),
        "Ce4+",
        "Ce",
        new ChargeElementary(4.0),
        IsVacancy: false);

    public static ParticleSet OxygenSet { get; } = new(
        ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f13")),
        "Oxygen site",
        [Oxygen.Id, Vacancy.Id]);

    public static ParticleSet CeriumSet { get; } = new(
        ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f14")),
        "Cerium site",
        [Cerium.Id]);

    public static StateChange Forward { get; } = new(
        StateChangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f21")),
        "O -> vacancy",
        Oxygen.Id,
        Vacancy.Id);

    public static StateChange Reverse { get; } = new(
        StateChangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f22")),
        "Vacancy -> O",
        Vacancy.Id,
        Oxygen.Id);

    public static StateChange Unstable { get; } = new(
        StateChangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f23")),
        "O -> void",
        Oxygen.Id,
        ParticleId.Void);

    public static StateChangeGroup StableGroup { get; } = new(
        StateChangeGroupId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f31")),
        "Stable",
        [Forward.Id, Reverse.Id]);

    public static StateChangeGroup UnstableGroup { get; } = new(
        StateChangeGroupId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f32")),
        "Unstable",
        [Unstable.Id]);

    public static TransitionChain Migration3 { get; } = new(
        TransitionChainId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f41")),
        "Migration3",
        TransitionPattern.Migration3,
        [StableGroup.Id, UnstableGroup.Id, StableGroup.Id],
        [TransitionLinker.Dynamic, TransitionLinker.Dynamic],
        GeneratesReverse: true);

    public static TransitionChain Metropolis2 { get; } = new(
        TransitionChainId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f42")),
        "Metropolis2",
        TransitionPattern.Metropolis2,
        [StableGroup.Id, StableGroup.Id],
        [TransitionLinker.Dynamic],
        GeneratesReverse: true);

    public static ModelDefinition CreateModel() => new()
    {
        Particles = [Oxygen, Vacancy, Cerium],
        ParticleSets = [OxygenSet, CeriumSet],
        Structure = new StructureModel(
            "P1 transition test",
            new SpaceGroupRef(1, AxisSetting.None, OriginChoice.None),
            new CellParameters(
                new LengthAngstrom(5.0),
                new LengthAngstrom(5.0),
                new LengthAngstrom(5.0),
                new AngleDegrees(90.0),
                new AngleDegrees(90.0),
                new AngleDegrees(90.0)),
            [
                Site(Site1, "Ce", Fractional3.Zero, CeriumSet.Id, SiteStability.Stable),
                Site(Site2, "O1", new Fractional3(0.25, 0.25, 0.25), OxygenSet.Id, SiteStability.Stable),
                Site(Site3, "U1", new Fractional3(0.5, 0.25, 0.25), OxygenSet.Id, SiteStability.Unstable),
                Site(Site4, "O2", new Fractional3(0.75, 0.25, 0.25), OxygenSet.Id, SiteStability.Stable),
                Site(Site5, "U2", new Fractional3(0.5, 0.5, 0.25), OxygenSet.Id, SiteStability.Unstable),
            ],
            []),
        StateChanges = [Forward, Reverse, Unstable],
        StateChangeGroups = [StableGroup, UnstableGroup],
        Chains = [Migration3, Metropolis2],
    };

    public static ModelDefinition WithKineticPath(ModelDefinition model, params Fractional3[] path) =>
        model with
        {
            KineticTransitions =
            [
                KineticTransitionWith(model, path),
            ],
            MetropolisTransitions = [],
        };

    public static KineticTransition KineticTransitionWith(ModelDefinition model, params Fractional3[] path) =>
        new(
            KineticTransitionId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f51")),
            "O migration",
            Migration3.Id,
            [.. path],
            ReverseTransitionId: null);

    public static ModelDefinition WithMetropolisTransition(
        ModelDefinition model,
        CellSiteId siteA,
        CellSiteId siteB,
        TransitionChainId? chainId = null) =>
        model with
        {
            KineticTransitions = [],
            MetropolisTransitions =
            [
                new MetropolisTransition(
                    MetropolisTransitionId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f61")),
                    "O exchange",
                    chainId ?? Metropolis2.Id,
                    siteA,
                    siteB,
                    ReverseTransitionId: null),
            ],
        };

    public static CellSiteId SiteId(string id) => CellSiteId.Create(Guid.Parse(id));

    private static CellSite Site(
        string id,
        string name,
        Fractional3 position,
        ParticleSetId particleSetId,
        SiteStability stability) =>
        new(CellSiteId.Create(Guid.Parse(id)), name, position, particleSetId, stability);
}
