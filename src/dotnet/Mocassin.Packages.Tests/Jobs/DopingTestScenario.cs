// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Structure;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Core.Units;
using Mocassin.Packages.Jobs;

namespace Mocassin.Packages.Tests.Jobs;

/// <summary>
/// Builds the deterministic doping model used by the Step 10 doping tests.
/// </summary>
internal static class DopingTestScenario
{
    internal static readonly ParticleId particleA = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a01");

    internal static readonly ParticleId particleB = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a02");

    internal static readonly ParticleId particleC = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a03");

    internal static readonly ParticleId particleA2 = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a04");

    internal static readonly ParticleId particleX = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a05");

    internal static readonly ParticleId particleY = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a06");

    internal static readonly ParticleId particleZ = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a07");

    internal static readonly ParticleId particleD = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a08");

    internal static readonly ParticleSetId setA = CreateSet("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9b01");

    internal static readonly ParticleSetId setX = CreateSet("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9b02");

    internal static readonly ParticleSetId setU = CreateSet("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9b03");

    internal static readonly ParticleSetId setZ = CreateSet("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9b04");

    internal static readonly CellSiteId siteAId = CreateSite("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c01");

    internal static readonly CellSiteId siteXId = CreateSite("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c02");

    internal static readonly CellSiteId siteUId = CreateSite("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c03");

    internal static readonly CellSiteId siteZId = CreateSite("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c04");

    internal static readonly OccupationExchangeId exchangeAb =
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9d01", CultureInfo.InvariantCulture));

    internal static readonly OccupationExchangeId exchangeAc =
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9d02", CultureInfo.InvariantCulture));

    internal static readonly OccupationExchangeId exchangeAa =
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9d03", CultureInfo.InvariantCulture));

    internal static readonly OccupationExchangeId exchangeXy =
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9d04", CultureInfo.InvariantCulture));

    internal static readonly OccupationExchangeId exchangeAd =
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9d05", CultureInfo.InvariantCulture));

    internal static readonly DopingId consumeXDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e01", CultureInfo.InvariantCulture));

    internal static readonly DopingId compensatedIntegerDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e02", CultureInfo.InvariantCulture));

    internal static readonly DopingId compensatedFractionalDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e03", CultureInfo.InvariantCulture));

    internal static readonly DopingId plainDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e04", CultureInfo.InvariantCulture));

    internal static readonly DopingId sameSiteDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e05", CultureInfo.InvariantCulture));

    internal static readonly DopingId isovalentDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e06", CultureInfo.InvariantCulture));

    internal static readonly DopingId overlappingCompensatedDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e07", CultureInfo.InvariantCulture));

    internal static SupercellGenerationResult Generate(
        Project project,
        CompiledModel model,
        LatticeRequest request,
        params (DopingId DopingId, double Fraction)[] dopings)
    {
        var values = dopings.Select(static doping => new DopingValue(doping.DopingId, doping.Fraction));
        return SupercellGenerator.Generate(project, model, CreateJob(request, [.. values]));
    }

    internal static int CountParticle(GeneratedLattice lattice, CompiledModel model, ParticleId particleId)
    {
        Assert.True(model.Particles.TryGetIndex(particleId, out var index));
        return lattice.Particles.Count(value => value == index);
    }

    internal static JobDescriptor CreateJob(
        LatticeRequest request,
        ImmutableArray<DopingValue>? dopings = null) => new(
        1,
        0,
        0,
        0,
        "collection",
        "config",
        SimulationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9f01", CultureInfo.InvariantCulture)),
        0,
        SimulationKind.Kmc,
        UseDualDofCorrection: false,
        new ResolvedJobSettings(
            900.0,
            500,
            7,
            null,
            null,
            ExecutionFlags.Deterministic,
            null,
            0,
            0.0,
            Fractional3.Zero,
            null,
            null,
            0.01,
            1000,
            100,
            200,
            []),
        request,
        dopings ?? [],
        [],
        [.. Enumerable.Repeat((byte)0x5a, LatticeSeedDeriver.SeedByteLength)]);

    internal static (Project Project, CompiledModel Model) CreateScenario(bool chargedBase = false)
    {
        var particles = new Particle[]
        {
            new(particleA, "A", "A", new ChargeElementary(1.0), IsVacancy: false),
            new(particleB, "B", "B", new ChargeElementary(2.0), IsVacancy: false),
            new(particleC, "C", "C", new ChargeElementary(2.5), IsVacancy: false),
            new(particleA2, "A2", "A2", new ChargeElementary(1.0), IsVacancy: false),
            new(particleX, "X", "X", new ChargeElementary(-1.0), IsVacancy: false),
            new(particleY, "Y", "Y", new ChargeElementary(-2.0), IsVacancy: false),
            new(particleZ, "Z", "Z", new ChargeElementary(1.0), IsVacancy: false),
            new(particleD, "D", "D", new ChargeElementary(0.0), IsVacancy: false),
        };

        var sites = new List<CellSite>
        {
            new(siteAId, "A", new Fractional3(0.0, 0.0, 0.0), setA, SiteStability.Stable),
            new(siteXId, "X", new Fractional3(0.5, 0.0, 0.0), setX, SiteStability.Stable),
            new(siteUId, "U", new Fractional3(0.5, 0.5, 0.5), setU, SiteStability.Unstable),
        };

        var compiledSites = new List<CompiledSite>
        {
            new(0, siteAId, "A", new Fractional3(0.0, 0.0, 0.0), SiteStability.Stable, setA, 0, [0]),
            new(1, siteXId, "X", new Fractional3(0.5, 0.0, 0.0), SiteStability.Stable, setX, 1, [1]),
            new(2, siteUId, "U", new Fractional3(0.5, 0.5, 0.5), SiteStability.Unstable, setU, 2, [2]),
        };

        var positions = new List<CompiledPosition>
        {
            CompiledPositionAt(0, siteAId, SiteStability.Stable, 0, 0b100011110UL),
            CompiledPositionAt(1, siteXId, SiteStability.Stable, 1, 0b1100000UL),
            CompiledPositionAt(2, siteUId, SiteStability.Unstable, 2, 0b10UL),
        };

        if (chargedBase)
        {
            sites.Add(new CellSite(siteZId, "Z", new Fractional3(0.25, 0.25, 0.25), setZ, SiteStability.Stable));
            compiledSites.Add(
                new CompiledSite(3, siteZId, "Z", new Fractional3(0.25, 0.25, 0.25), SiteStability.Stable, setZ, 3, [3]));
            positions.Add(CompiledPositionAt(3, siteZId, SiteStability.Stable, 3, 1UL << 7));
        }

        var project = new Project(
            null,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9f00", CultureInfo.InvariantCulture),
                new string('a', 64),
                "Doping test",
                null,
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch),
            new ModelDefinition
            {
                Particles = [.. particles],
                ParticleSets =
                [
                    new ParticleSet(setA, "A site", [particleA, particleB, particleC, particleA2, particleD]),
                    new ParticleSet(setX, "X site", [particleX, particleY]),
                    new ParticleSet(setU, "U site", [particleA]),
                    new ParticleSet(setZ, "Z site", [particleZ]),
                ],
                Structure = StructureModel.Default with
                {
                    Name = "Doping test",
                    Sites = [.. sites],
                },
                OccupationExchanges =
                [
                    new OccupationExchange(exchangeAb, "A to B", siteAId, particleA, particleB),
                    new OccupationExchange(exchangeAc, "A to C", siteAId, particleA, particleC),
                    new OccupationExchange(exchangeAa, "A to A2", siteAId, particleA, particleA2),
                    new OccupationExchange(exchangeXy, "X to Y", siteXId, particleX, particleY),
                    new OccupationExchange(exchangeAd, "A to D", siteAId, particleA, particleD),
                ],
                Dopings =
                [
                    new Doping(consumeXDoping, "Consume X", 5, exchangeXy, null, ChargeCompensation: false),
                    new Doping(
                        compensatedIntegerDoping,
                        "Compensated integer",
                        10,
                        exchangeAb,
                        exchangeXy,
                        ChargeCompensation: true),
                    new Doping(
                        compensatedFractionalDoping,
                        "Compensated fractional",
                        20,
                        exchangeAc,
                        exchangeXy,
                        ChargeCompensation: true),
                    new Doping(plainDoping, "Plain", 30, exchangeAb, null, ChargeCompensation: false),
                    new Doping(sameSiteDoping, "Same site", 40, exchangeAc, null, ChargeCompensation: false),
                    new Doping(isovalentDoping, "Isovalent", 50, exchangeAa, null, ChargeCompensation: false),
                    new Doping(
                        overlappingCompensatedDoping,
                        "Overlapping compensated",
                        60,
                        exchangeAb,
                        exchangeAd,
                        ChargeCompensation: true),
                ],
            },
            [],
            [],
            [],
            ViewSettings.Default);

        var compiledModel = new CompiledModel(
            CompiledModel.CurrentFormatVersion,
            "test-model",
            "symmetry-1",
            "2026.1",
            new string('b', 64))
        {
            Particles = new CompiledParticleTable(
            [
                new CompiledParticle(0, ParticleId.Void, "Void", "V", 0.0, IsVacancy: false),
                new CompiledParticle(1, particleA, "A", "A", 1.0, IsVacancy: false),
                new CompiledParticle(2, particleB, "B", "B", 2.0, IsVacancy: false),
                new CompiledParticle(3, particleC, "C", "C", 2.5, IsVacancy: false),
                new CompiledParticle(4, particleA2, "A2", "A2", 1.0, IsVacancy: false),
                new CompiledParticle(5, particleX, "X", "X", -1.0, IsVacancy: false),
                new CompiledParticle(6, particleY, "Y", "Y", -2.0, IsVacancy: false),
                new CompiledParticle(7, particleZ, "Z", "Z", 1.0, IsVacancy: false),
                new CompiledParticle(8, particleD, "D", "D", 0.0, IsVacancy: false),
            ]),
            Structure = new CompiledStructureModel(
                [new Cartesian3(5.0, 0.0, 0.0), new Cartesian3(0.0, 5.0, 0.0), new Cartesian3(0.0, 0.0, 5.0)],
                125.0,
                [.. compiledSites],
                [],
                [.. positions],
                [0, 0, 0, 0]),
        };

        return (project, compiledModel);
    }

    private static CompiledPosition CompiledPositionAt(
        int position,
        CellSiteId siteId,
        SiteStability stability,
        int siteIndex,
        ulong allowedParticleMask) => new(
        position,
        new Vector4I(0, 0, 0, position),
        Fractional3.Zero,
        Cartesian3.Zero,
        siteIndex,
        siteId,
        stability,
        allowedParticleMask,
        siteIndex,
        siteIndex,
        [],
        []);

    private static ParticleId CreateParticle(string value) =>
        ParticleId.Create(Guid.Parse(value, CultureInfo.InvariantCulture));

    private static ParticleSetId CreateSet(string value) =>
        ParticleSetId.Create(Guid.Parse(value, CultureInfo.InvariantCulture));

    private static CellSiteId CreateSite(string value) =>
        CellSiteId.Create(Guid.Parse(value, CultureInfo.InvariantCulture));
}
