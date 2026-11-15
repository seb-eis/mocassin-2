// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Structure;
using Mocassin.Core.Diagnostics;
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

[Trait("Tier", "PR-fast")]
public sealed class LatticePreviewServiceTests
{
    private static readonly ParticleId particleA = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4daa01");
    private static readonly ParticleId particleB = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4daa02");
    private static readonly ParticleId particleC = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4daa03");
    private static readonly ParticleId particleA2 = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4daa04");
    private static readonly ParticleId particleX = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4daa05");
    private static readonly ParticleId particleY = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4daa06");

    private static readonly ParticleSetId setA = CreateSet("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4dab01");
    private static readonly ParticleSetId setX = CreateSet("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4dab02");
    private static readonly ParticleSetId setU = CreateSet("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4dab03");

    private static readonly CellSiteId siteAId = CreateSite("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4dac01");
    private static readonly CellSiteId siteXId = CreateSite("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4dac02");
    private static readonly CellSiteId siteUId = CreateSite("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4dac03");

    private static readonly OccupationExchangeId exchangeAb =
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4dad01", CultureInfo.InvariantCulture));

    private static readonly OccupationExchangeId exchangeAc =
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4dad02", CultureInfo.InvariantCulture));

    private static readonly OccupationExchangeId exchangeAa =
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4dad03", CultureInfo.InvariantCulture));

    private static readonly OccupationExchangeId exchangeXy =
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4dad04", CultureInfo.InvariantCulture));

    private static readonly DopingId consumeXDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4dae01", CultureInfo.InvariantCulture));

    private static readonly DopingId compensatedIntegerDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4dae02", CultureInfo.InvariantCulture));

    private static readonly DopingId plainDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4dae03", CultureInfo.InvariantCulture));

    private static readonly SimulationId kmcSimulationId =
        SimulationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4daf01", CultureInfo.InvariantCulture));

    private static readonly SimulationId mmcSimulationId =
        SimulationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4daf02", CultureInfo.InvariantCulture));

    private static readonly JobTemplateId templateId =
        JobTemplateId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4db001", CultureInfo.InvariantCulture));

    [Fact]
    public void PreviewEchoesTheRequestAndDopingsAndMatchesTheGeneratedReport()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();
        var request = new LatticeRequest(10, 1, 1);
        DopingValue[] dopings = [new(compensatedIntegerDoping, 0.1)];

        var preview = service.TryCreatePreview(scenario.Project, scenario.Model, request, [.. dopings], out var diagnostics);

        Assert.NotNull(preview);
        Assert.Empty(diagnostics);
        Assert.Equal(request, preview.Request);
        Assert.Equal(dopings, preview.Dopings.ToArray());
        Assert.Equal(30, preview.Report.TotalPositions);
        Assert.Equal(0.0, preview.Report.TotalChargeBefore);
        Assert.Equal(0.0, preview.Report.TotalChargeAfter);
        Assert.True(preview.Report.IsChargeNeutral);

        var generated = SupercellGenerator.Generate(
            scenario.Project,
            scenario.Model,
            CreateJob(request, [.. dopings]));
        Assert.True(generated.IsSuccess);
        Assert.Equal(generated.Lattice!.Report.ToDopingInfo(), preview.Report.ToDopingInfo());
        Assert.Equal(generated.Lattice.Report.Applications.ToArray(), preview.Report.Applications.ToArray());
        Assert.Equal(generated.Lattice.Report.TotalPositions, preview.Report.TotalPositions);
        Assert.Equal(generated.Lattice.Report.TotalChargeBefore, preview.Report.TotalChargeBefore);
        Assert.Equal(generated.Lattice.Report.TotalChargeAfter, preview.Report.TotalChargeAfter);
        Assert.Equal(generated.Lattice.Report.IsChargeNeutral, preview.Report.IsChargeNeutral);
    }

    [Fact]
    public void RemainingPopulationsArePerSiteAndExcludeTheVoidParticle()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();

        var preview = service.TryCreatePreview(
            scenario.Project,
            scenario.Model,
            new LatticeRequest(10, 1, 1),
            [new DopingValue(compensatedIntegerDoping, 0.1)],
            out _);

        Assert.NotNull(preview);
        var sites = preview.RemainingPopulations.ToDictionary(static site => site.SiteId);
        Assert.Equal(
            [(particleA, 9), (particleB, 1)],
            sites[siteAId].Particles.Select(static particle => (particle.ParticleId, particle.Count)).ToArray());
        Assert.Equal(
            [(particleX, 9), (particleY, 1)],
            sites[siteXId].Particles.Select(static particle => (particle.ParticleId, particle.Count)).ToArray());
        Assert.DoesNotContain(siteUId, sites.Keys);
        Assert.DoesNotContain(
            preview.RemainingPopulations.SelectMany(static site => site.Particles),
            static particle => particle.ParticleId == ParticleId.Void);
        Assert.Equal(
            preview.RemainingPopulations.Select(static site => site.SiteId).OrderBy(static id => id.Value),
            preview.RemainingPopulations.Select(static site => site.SiteId));
    }

    [Fact]
    public void UnknownDopingReturnsJob006AndNoPreview()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();

        var preview = service.TryCreatePreview(
            scenario.Project,
            scenario.Model,
            new LatticeRequest(1, 1, 1),
            [new DopingValue(DopingId.New(), 0.1)],
            out var diagnostics);

        Assert.Null(preview);
        Assert.Contains(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.UnknownDoping &&
            diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void NonFiniteOrOutOfRangeFractionReturnsJob005AndNoPreview(double fraction)
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();

        var preview = service.TryCreatePreview(
            scenario.Project,
            scenario.Model,
            new LatticeRequest(10, 1, 1),
            [new DopingValue(plainDoping, fraction)],
            out var diagnostics);

        Assert.Null(preview);
        Assert.Contains(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidDopingFraction &&
            diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(257, 1, 1)]
    [InlineData(1, 257, 1)]
    [InlineData(1, 1, 257)]
    [InlineData(256, 256, 8)]
    public void InvalidSupercellReturnsJob004AndNoPreview(int a, int b, int c)
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();

        var preview = service.TryCreatePreview(
            scenario.Project,
            scenario.Model,
            new LatticeRequest(a, b, c),
            [],
            out var diagnostics);

        Assert.Null(preview);
        Assert.Contains(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidCellSize &&
            diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ImpossibleCompensationReturnsJob012AndNoPreview()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();

        var preview = service.TryCreatePreview(
            scenario.Project,
            scenario.Model,
            new LatticeRequest(10, 1, 1),
            [new DopingValue(consumeXDoping, 1.0), new DopingValue(compensatedIntegerDoping, 0.5)],
            out var diagnostics);

        Assert.Null(preview);
        Assert.Contains(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidCompensation &&
            diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void NullProjectModelOrRequestIsAProgrammerError()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();
        var request = new LatticeRequest(1, 1, 1);

        Assert.Throws<ArgumentNullException>(() =>
            service.TryCreatePreview(null!, scenario.Model, request, [], out _));
        Assert.Throws<ArgumentNullException>(() =>
            service.TryCreatePreview(scenario.Project, null!, request, [], out _));
        Assert.Throws<ArgumentNullException>(() =>
            service.TryCreatePreview(scenario.Project, scenario.Model, null!, [], out _));
    }

    [Fact]
    public void TheSameRequestIsServedFromTheCache()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();
        var request = new LatticeRequest(5, 1, 1);

        var first = service.TryCreatePreview(scenario.Project, scenario.Model, request, [], out var firstDiagnostics);
        var second = service.TryCreatePreview(scenario.Project, scenario.Model, request, [], out var secondDiagnostics);

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Equal(firstDiagnostics, secondDiagnostics);
        Assert.Equal(1, service.CachedEntryCount);
    }

    [Fact]
    public void CacheKeysSeparateTheRequestDopingsModelAndSymmetryProvenance()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();
        var request = new LatticeRequest(2, 1, 1);

        Assert.NotNull(service.TryCreatePreview(scenario.Project, scenario.Model, request, [], out _));
        Assert.Equal(1, service.CachedEntryCount);

        Assert.NotNull(service.TryCreatePreview(
            scenario.Project,
            scenario.Model,
            new LatticeRequest(3, 1, 1),
            [],
            out _));
        Assert.Equal(2, service.CachedEntryCount);

        Assert.NotNull(service.TryCreatePreview(
            scenario.Project,
            scenario.Model,
            request,
            [new DopingValue(plainDoping, 0.0)],
            out _));
        Assert.Equal(3, service.CachedEntryCount);

        var editedProject = CreateScenario(kmcTemperature: 901.0).Project;
        Assert.NotNull(service.TryCreatePreview(editedProject, scenario.Model, request, [], out _));
        Assert.Equal(4, service.CachedEntryCount);

        Assert.NotNull(service.TryCreatePreview(
            scenario.Project,
            scenario.Model with { SpaceGroupDataVersion = "2027.1" },
            request,
            [],
            out _));
        Assert.Equal(5, service.CachedEntryCount);

        Assert.NotNull(service.TryCreatePreview(
            scenario.Project,
            scenario.Model with { SpaceGroupDataSha256 = new string('c', 64) },
            request,
            [],
            out _));
        Assert.Equal(6, service.CachedEntryCount);

        Assert.NotNull(service.TryCreatePreview(
            scenario.Project,
            scenario.Model with { SymmetryContractVersion = "symmetry-2" },
            request,
            [],
            out _));
        Assert.Equal(7, service.CachedEntryCount);
    }

    [Fact]
    public void CacheEvictsTheLeastRecentlyUsedEntryAtTheEightEntryBound()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();

        for (var a = 1; a <= 9; a++)
        {
            Assert.NotNull(service.TryCreatePreview(
                scenario.Project,
                scenario.Model,
                new LatticeRequest(a, 1, 1),
                [],
                out _));
        }

        Assert.Equal(8, service.CachedEntryCount);
        Assert.False(service.IsCached(scenario.Project, scenario.Model, new LatticeRequest(1, 1, 1), []));
        Assert.True(service.IsCached(scenario.Project, scenario.Model, new LatticeRequest(9, 1, 1), []));
    }

    [Fact]
    public void CacheEvictsWhenTheEstimatedPayloadExceedsTheByteBound()
    {
        var scenario = CreateScenario();
        var reference = new LatticePreviewService();
        var firstRequest = new LatticeRequest(2, 1, 1);
        var secondRequest = new LatticeRequest(3, 1, 1);
        var referencePreview = reference.TryCreatePreview(
            scenario.Project,
            scenario.Model,
            firstRequest,
            [],
            out _);
        Assert.NotNull(referencePreview);
        var payload = LatticePreviewService.EstimatePayloadBytes(referencePreview);

        var service = new LatticePreviewService(8, (2 * payload) - 1);
        Assert.NotNull(service.TryCreatePreview(scenario.Project, scenario.Model, firstRequest, [], out _));
        Assert.Equal(payload, service.CachedBytes);
        Assert.NotNull(service.TryCreatePreview(scenario.Project, scenario.Model, secondRequest, [], out _));

        Assert.Equal(1, service.CachedEntryCount);
        Assert.Equal(payload, service.CachedBytes);
        Assert.False(service.IsCached(scenario.Project, scenario.Model, firstRequest, []));
        Assert.True(service.IsCached(scenario.Project, scenario.Model, secondRequest, []));

        var oversized = new LatticePreviewService(8, payload - 1);
        Assert.NotNull(oversized.TryCreatePreview(scenario.Project, scenario.Model, firstRequest, [], out _));
        Assert.Equal(0, oversized.CachedEntryCount);
    }

    [Fact]
    public void ConcurrentPreviewsAreSafeAndRemainBounded()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();

        Parallel.For(0, 64, index =>
        {
            var request = new LatticeRequest((index % 8) + 1, 1, 1);
            var preview = service.TryCreatePreview(scenario.Project, scenario.Model, request, [], out var diagnostics);
            Assert.NotNull(preview);
            Assert.Empty(diagnostics);
        });

        Assert.InRange(service.CachedEntryCount, 1, 8);
    }

    [Fact]
    public void JobCountPreviewReportsDefaultEffectiveAndWholeTemplateTotals()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();
        var template = CreateJobTemplate(scenario);

        var preview = service.TryCreateJobCountPreview(
            scenario.Project,
            template,
            scenario.KmcSimulationId,
            out var diagnostics);

        Assert.NotNull(preview);
        Assert.Empty(diagnostics);
        Assert.Equal(scenario.KmcSimulationId, preview.SimulationId);
        Assert.Equal(12, preview.DefaultJobCount);
        Assert.Equal(32, preview.EffectiveJobCount);
        Assert.Equal(35, preview.TotalJobs);
    }

    [Fact]
    public void JobCountPreviewUsesTheCollectionOverrideAndInheritsTheSimulationCount()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();
        var template = CreateJobTemplate(scenario);

        var mmcPreview = service.TryCreateJobCountPreview(
            scenario.Project,
            template,
            scenario.MmcSimulationId,
            out _);

        Assert.NotNull(mmcPreview);
        Assert.Equal(3, mmcPreview.DefaultJobCount);
        Assert.Equal(3, mmcPreview.EffectiveJobCount);
        Assert.Equal(35, mmcPreview.TotalJobs);
    }

    [Fact]
    public void JobCountPreviewRejectsAnUnknownSimulationWithJob002()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();

        var preview = service.TryCreateJobCountPreview(
            scenario.Project,
            CreateJobTemplate(scenario),
            SimulationId.New(),
            out var diagnostics);

        Assert.Null(preview);
        Assert.Contains(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.UnknownSimulation &&
            diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void JobCountPreviewRejectsAnInvalidCollectionCountWithJob008()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();

        var preview = service.TryCreateJobCountPreview(
            scenario.Project,
            CreateJobTemplate(scenario, collectionJobCount: 0),
            scenario.KmcSimulationId,
            out var diagnostics);

        Assert.Null(preview);
        Assert.Contains(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidJobCount &&
            diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void JobCountPreviewRejectsAnInvalidSimulationDefaultWithJob008()
    {
        var scenario = CreateScenario(kmcJobCount: 0);
        var service = new LatticePreviewService();

        var preview = service.TryCreateJobCountPreview(
            scenario.Project,
            CreateJobTemplate(scenario),
            scenario.KmcSimulationId,
            out var diagnostics);

        Assert.Null(preview);
        Assert.Contains(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidJobCount &&
            diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void JobCountPreviewDoesNotGenerateLattices()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();
        var template = CreateJobTemplate(
            scenario,
            collectionJobCount: 4,
            configLattice: new LatticeRequest(0, 0, 0));

        var preview = service.TryCreateJobCountPreview(
            scenario.Project,
            template,
            scenario.KmcSimulationId,
            out var diagnostics);

        Assert.NotNull(preview);
        Assert.Empty(diagnostics);
        Assert.Equal(32, preview.EffectiveJobCount);
        Assert.Equal(35, preview.TotalJobs);
    }

    [Fact]
    public void NullProjectOrTemplateInJobCountPreviewIsAProgrammerError()
    {
        var scenario = CreateScenario();
        var service = new LatticePreviewService();

        Assert.Throws<ArgumentNullException>(() =>
            service.TryCreateJobCountPreview(null!, CreateJobTemplate(scenario), scenario.KmcSimulationId, out _));
        Assert.Throws<ArgumentNullException>(() =>
            service.TryCreateJobCountPreview(scenario.Project, null!, scenario.KmcSimulationId, out _));
    }

    private static JobTemplate CreateJobTemplate(
        Scenario scenario,
        int? collectionJobCount = 4,
        LatticeRequest? configLattice = null)
    {
        var lattice = configLattice ?? new LatticeRequest(2, 2, 2);
        return new JobTemplate(templateId, "Job-count template")
        {
            Collections =
            [
                new JobCollection(
                    CreateCollection("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4db101"),
                    "kmc-1",
                    scenario.KmcSimulationId,
                    JobCountPerConfig: collectionJobCount,
                    Configs:
                    [
                        CreateConfig("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4db201", "x1", lattice),
                        CreateConfig("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4db202", "x2", lattice),
                    ]),
                new JobCollection(
                    CreateCollection("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4db102"),
                    "mmc-1",
                    scenario.MmcSimulationId,
                    Configs: [CreateConfig("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4db203", "m1", lattice)]),
                new JobCollection(
                    CreateCollection("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4db103"),
                    "kmc-2",
                    scenario.KmcSimulationId,
                    Configs:
                    [
                        CreateConfig("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4db204", "y1", lattice),
                        CreateConfig("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4db205", "y2", lattice),
                    ]),
            ],
        };
    }

    private static JobConfig CreateConfig(string id, string name, LatticeRequest lattice) =>
        new(
            JobConfigId.Create(Guid.Parse(id, CultureInfo.InvariantCulture)),
            name,
            lattice,
            Dopings: [new DopingValue(DopingId.New(), double.NaN)]);

    private static JobCollectionId CreateCollection(string id) =>
        JobCollectionId.Create(Guid.Parse(id, CultureInfo.InvariantCulture));

    private static JobDescriptor CreateJob(LatticeRequest request, ImmutableArray<DopingValue> dopings) => new(
        1,
        0,
        0,
        0,
        "collection",
        "config",
        kmcSimulationId,
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
        dopings,
        [],
        [.. Enumerable.Repeat((byte)0x5a, LatticeSeedDeriver.SeedByteLength)]);

    private static Scenario CreateScenario(double kmcTemperature = 900.0, int kmcJobCount = 12)
    {
        var project = new Project(
            null,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4da000", CultureInfo.InvariantCulture),
                new string('a', 64),
                "Lattice preview test",
                null,
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch),
            new ModelDefinition
            {
                Particles =
                [
                    new Particle(particleA, "A", "A", new ChargeElementary(1.0), IsVacancy: false),
                    new Particle(particleB, "B", "B", new ChargeElementary(2.0), IsVacancy: false),
                    new Particle(particleC, "C", "C", new ChargeElementary(2.5), IsVacancy: false),
                    new Particle(particleA2, "A2", "A2", new ChargeElementary(1.0), IsVacancy: false),
                    new Particle(particleX, "X", "X", new ChargeElementary(-1.0), IsVacancy: false),
                    new Particle(particleY, "Y", "Y", new ChargeElementary(-2.0), IsVacancy: false),
                ],
                ParticleSets =
                [
                    new ParticleSet(setA, "A site", [particleA, particleB, particleC, particleA2]),
                    new ParticleSet(setX, "X site", [particleX, particleY]),
                    new ParticleSet(setU, "U site", [particleA]),
                ],
                Structure = StructureModel.Default with
                {
                    Name = "Lattice preview test",
                    Sites =
                    [
                        new CellSite(siteAId, "A", new Fractional3(0.0, 0.0, 0.0), setA, SiteStability.Stable),
                        new CellSite(siteXId, "X", new Fractional3(0.5, 0.0, 0.0), setX, SiteStability.Stable),
                        new CellSite(siteUId, "U", new Fractional3(0.5, 0.5, 0.5), setU, SiteStability.Unstable),
                    ],
                },
                OccupationExchanges =
                [
                    new OccupationExchange(exchangeAb, "A to B", siteAId, particleA, particleB),
                    new OccupationExchange(exchangeAc, "A to C", siteAId, particleA, particleC),
                    new OccupationExchange(exchangeAa, "A to A2", siteAId, particleA, particleA2),
                    new OccupationExchange(exchangeXy, "X to Y", siteXId, particleX, particleY),
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
                    new Doping(plainDoping, "Plain", 30, exchangeAb, null, ChargeCompensation: false),
                ],
                Simulations =
                [
                    new KineticSimulation
                    {
                        Id = kmcSimulationId,
                        Name = "KMC",
                        JobCount = kmcJobCount,
                        TemperatureK = kmcTemperature,
                    },
                    new MetropolisSimulation
                    {
                        Id = mmcSimulationId,
                        Name = "MMC",
                        JobCount = 3,
                    },
                ],
            },
            [],
            [],
            [],
            ViewSettings.Default);

        return new Scenario(project, CreateCompiledModel(), kmcSimulationId, mmcSimulationId);
    }

    private static CompiledModel CreateCompiledModel() => new(
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
        ]),
        Structure = new CompiledStructureModel(
            [new Cartesian3(5.0, 0.0, 0.0), new Cartesian3(0.0, 5.0, 0.0), new Cartesian3(0.0, 0.0, 5.0)],
            125.0,
            [
                new CompiledSite(0, siteAId, "A", new Fractional3(0.0, 0.0, 0.0), SiteStability.Stable, setA, 0, [0]),
                new CompiledSite(1, siteXId, "X", new Fractional3(0.5, 0.0, 0.0), SiteStability.Stable, setX, 1, [1]),
                new CompiledSite(2, siteUId, "U", new Fractional3(0.5, 0.5, 0.5), SiteStability.Unstable, setU, 2, [2]),
            ],
            [],
            [
                CompiledPositionAt(0, siteAId, SiteStability.Stable, 0, 0b11110UL),
                CompiledPositionAt(1, siteXId, SiteStability.Stable, 1, 0b1100000UL),
                CompiledPositionAt(2, siteUId, SiteStability.Unstable, 2, 0b10UL),
            ],
            [0, 0, 0]),
    };

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

    private sealed record Scenario(
        Project Project,
        CompiledModel Model,
        SimulationId KmcSimulationId,
        SimulationId MmcSimulationId);
}
