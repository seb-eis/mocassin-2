// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.TestSupport;

internal static class EnergyTestModels
{
    public static readonly DateTimeOffset CreatedUtc =
        DateTimeOffset.Parse("2026-11-09T12:00:00Z", CultureInfo.InvariantCulture);

    public static readonly Guid ProjectId = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a00");

    public static ParticleId OxygenId { get; } =
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a10"));

    public static ParticleId VacancyId { get; } =
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a11"));

    public static ParticleId CeriumId { get; } =
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a12"));

    public static ParticleSetId OxygenSetId { get; } =
        ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a13"));

    public static ParticleSetId CeriumSetId { get; } =
        ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a14"));

    public static CellSiteId CeriumSiteId { get; } =
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a15"));

    public static CellSiteId OxygenSiteId { get; } =
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a16"));

    public static CellSiteId UnstableSiteId { get; } =
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a17"));

    public static CellSiteId EndpointSiteId { get; } =
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a18"));

    public static ClusterId ClusterIdA { get; } =
        ClusterId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a20"));

    public static ClusterId ClusterIdB { get; } =
        ClusterId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a21"));

    public static InteractionFilter StableFilterA { get; } = new(
        new LengthAngstrom(0.0),
        new LengthAngstrom(1.0),
        CeriumSiteId,
        OxygenSiteId);

    public static InteractionFilter StableFilterB { get; } = new(
        new LengthAngstrom(3.0),
        new LengthAngstrom(6.0),
        OxygenSiteId,
        EndpointSiteId);

    public static UnstableEnvironment UnstableEnvironment { get; } = new(
        UnstableSiteId,
        new LengthAngstrom(4.0),
        []);

    public static Cluster MigrationCluster { get; } = new(
        ClusterIdA,
        "Migration edge",
        UnstableSiteId,
        [
            new Fractional3(-0.25, 0.0, 0.0),
            new Fractional3(0.25, 0.0, 0.0),
        ]);

    public static Cluster SecondCluster { get; } = new(
        ClusterIdB,
        "Second cluster",
        CeriumSiteId,
        [
            new Fractional3(0.25, 0.25, 0.25),
            new Fractional3(0.75, 0.25, 0.25),
        ]);

    public static ModelDefinition CreateEnergyOnlyModel() => new()
    {
        StableEnvironment = new StableEnvironment(
            new LengthAngstrom(6.0),
            [StableFilterB, StableFilterA]),
        UnstableEnvironments = [UnstableEnvironment],
        Clusters = [SecondCluster, MigrationCluster],
        SiteEnergies =
        [
            new SiteEnergySet(
                EndpointSiteId,
                [
                    new SiteEnergyEntry(VacancyId, new EnergyEv(0.25)),
                    new SiteEnergyEntry(OxygenId, new EnergyEv(-0.5)),
                ]),
            new SiteEnergySet(
                CeriumSiteId,
                [new SiteEnergyEntry(CeriumId, new EnergyEv(1.5))]),
        ],
    };

    public static ModelDefinition CreateCombinedModel()
    {
        var oxygen = new Particle(OxygenId, "O2-", "O", new ChargeElementary(-2.0), IsVacancy: false);
        var vacancy = new Particle(VacancyId, "Vacancy", "Vc", new ChargeElementary(0.0), IsVacancy: true);
        var cerium = new Particle(CeriumId, "Ce4+", "Ce", new ChargeElementary(4.0), IsVacancy: false);
        var oxygenSet = new ParticleSet(OxygenSetId, "Oxygen site", [oxygen.Id, vacancy.Id]);
        var ceriumSet = new ParticleSet(CeriumSetId, "Cerium site", [cerium.Id]);

        return CreateEnergyOnlyModel() with
        {
            Particles = [oxygen, vacancy, cerium],
            ParticleSets = [oxygenSet, ceriumSet],
            Structure = StructureModel.Default with
            {
                Name = "Energy test structure",
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
                    new CellSite(CeriumSiteId, "Ce", Fractional3.Zero, CeriumSetId, SiteStability.Stable),
                    new CellSite(
                        OxygenSiteId,
                        "O",
                        new Fractional3(0.25, 0.25, 0.25),
                        OxygenSetId,
                        SiteStability.Stable),
                    new CellSite(
                        UnstableSiteId,
                        "O transition",
                        new Fractional3(0.5, 0.25, 0.25),
                        OxygenSetId,
                        SiteStability.Unstable),
                    new CellSite(
                        EndpointSiteId,
                        "O endpoint",
                        new Fractional3(0.75, 0.25, 0.25),
                        OxygenSetId,
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
                "Energy model",
                null,
                CreatedUtc,
                CreatedUtc),
            model ?? CreateEnergyOnlyModel(),
            [],
            [],
            [],
            ViewSettings.Default);
}
