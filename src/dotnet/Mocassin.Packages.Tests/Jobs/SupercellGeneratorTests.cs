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
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Core.Units;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Jobs.Optimization;

namespace Mocassin.Packages.Tests.Jobs;

[Trait("Tier", "PR-fast")]
public sealed class SupercellGeneratorTests
{
    private static readonly ParticleId particleA =
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8a10", CultureInfo.InvariantCulture));

    private static readonly ParticleId particleB =
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8a11", CultureInfo.InvariantCulture));

    private static readonly ParticleSetId setA = ParticleSetId.Create(
        Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8a20", CultureInfo.InvariantCulture));

    private static readonly ParticleSetId setB = ParticleSetId.Create(
        Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8a21", CultureInfo.InvariantCulture));

    private static readonly CellSiteId stableAId = CellSiteId.Create(
        Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8a30", CultureInfo.InvariantCulture));

    private static readonly CellSiteId unstableId = CellSiteId.Create(
        Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8a31", CultureInfo.InvariantCulture));

    private static readonly CellSiteId stableBId = CellSiteId.Create(
        Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8a32", CultureInfo.InvariantCulture));

    [Fact]
    public void DefaultOccupationsUseTheFirstSetParticleAndTheVoidForUnstableSites()
    {
        var project = CreateProject();
        var model = CreateCompiledModel();
        var job = CreateJob(new LatticeRequest(1, 1, 1));

        var result = SupercellGenerator.Generate(project, model, job);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Lattice);
        Assert.Equal(new byte[] { 1, 0, 2 }, result.Lattice.Particles.ToArray());
        Assert.Equal(1, result.Lattice.SizeA);
        Assert.Equal(1, result.Lattice.SizeB);
        Assert.Equal(1, result.Lattice.SizeC);
        Assert.Equal(3, result.Lattice.PositionCount);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void ReplicationUsesRowMajorCellOrderAndTheCanonicalPositionOrder()
    {
        var result = SupercellGenerator.Generate(
            CreateProject(),
            CreateCompiledModel(),
            CreateJob(new LatticeRequest(2, 1, 2)));

        Assert.True(result.IsSuccess);
        var pattern = new byte[] { 1, 0, 2 };
        byte[] expected = [.. Enumerable.Range(0, 4).SelectMany(_ => pattern)];
        Assert.Equal(expected, result.Lattice!.Particles.ToArray());
    }

    [Fact]
    public void GenerationIsDeterministicForTheSameDescriptor()
    {
        var project = CreateProject();
        var model = CreateCompiledModel();
        var job = CreateJob(new LatticeRequest(2, 2, 2));

        var first = SupercellGenerator.Generate(project, model, job);
        var second = SupercellGenerator.Generate(project, model, job);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal<byte>(first.Lattice!.Particles, second.Lattice!.Particles);
        Assert.Equal(first.LatticeRequestHash, second.LatticeRequestHash);
        Assert.Equal<byte>(first.LatticeSeed, second.LatticeSeed);
    }

    [Fact]
    public void CompiledSelectionOptimizersNeverChangeTheLatticeParticles()
    {
        var project = CreateProject();
        var model = CreateCompiledModel();
        var request = new LatticeRequest(2, 2, 2);
        var without = SupercellGenerator.Generate(project, model, CreateJob(request));
        var with = SupercellGenerator.Generate(
            project,
            model,
            CreateJob(request, compiledOptimizers: [new CompiledSelectionOptimizer(0, 1)]));

        Assert.True(without.IsSuccess);
        Assert.True(with.IsSuccess);
        Assert.Equal<byte>(without.Lattice!.Particles, with.Lattice!.Particles);
        Assert.Equal(without.Lattice.Report.Applications.ToArray(), with.Lattice.Report.Applications.ToArray());
    }

    [Fact]
    public void ResultExposesTheRequestHashAndDerivedLatticeSeed()
    {
        var project = CreateProject();
        var model = CreateCompiledModel();
        var request = new LatticeRequest(2, 3, 4);
        var job = CreateJob(request);

        var result = SupercellGenerator.Generate(project, model, job);

        Assert.True(result.IsSuccess);
        var expectedHash = LatticeRequestHasher.Compute(project, model, request, []);
        Assert.Equal(expectedHash, result.LatticeRequestHash);
        Assert.Equal(
            Convert.ToHexStringLower(
                LatticeSeedDeriver.DeriveLatticeSeed(
                    job.Seed.AsSpan(),
                    Convert.FromHexString(expectedHash)).AsSpan()),
            Convert.ToHexStringLower(result.LatticeSeed.AsSpan()));
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(257, 1, 1)]
    [InlineData(1, 257, 1)]
    [InlineData(1, 1, 257)]
    public void CellSizeOutsideOneTo256IsJob004AndNeverPartial(int a, int b, int c)
    {
        var result = SupercellGenerator.Generate(
            CreateProject(),
            CreateCompiledModel(),
            CreateJob(new LatticeRequest(a, b, c)));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Lattice);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidCellSize &&
            diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void TotalPositionsAboveTheHardLimitIsJob004()
    {
        var result = SupercellGenerator.Generate(
            CreateProject(),
            CreateCompiledModel(),
            CreateJob(new LatticeRequest(256, 256, 3)));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Lattice);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidCellSize);
    }

    [Fact]
    public void TotalPositionsAboveTheWarningThresholdIsJob014ButStillGenerates()
    {
        var result = SupercellGenerator.Generate(
            CreateProject(),
            CreateCompiledModel(),
            CreateJob(new LatticeRequest(256, 256, 1)));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Lattice);
        Assert.Equal(196_608, result.Lattice.Particles.Length);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.LargeSupercell &&
            diagnostic.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void UnknownDopingReturnsJob006AndNoLattice()
    {
        var result = SupercellGenerator.Generate(
            CreateProject(),
            CreateCompiledModel(),
            CreateJob(new LatticeRequest(1, 1, 1), [new DopingValue(DopingId.New(), 0.1)]));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Lattice);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.UnknownDoping);
    }

    [Fact]
    public void PreCancelledGenerationThrows()
    {
        Assert.Throws<OperationCanceledException>(() => SupercellGenerator.Generate(
            CreateProject(),
            CreateCompiledModel(),
            CreateJob(new LatticeRequest(2, 2, 2)),
            new CancellationToken(canceled: true)));
    }

    private static JobDescriptor CreateJob(
        LatticeRequest request,
        ImmutableArray<DopingValue>? dopings = null,
        ImmutableArray<CompiledSelectionOptimizer>? compiledOptimizers = null) => new(
        1,
        0,
        0,
        0,
        "collection",
        "config",
        SimulationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8a40", CultureInfo.InvariantCulture)),
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
        [.. Enumerable.Repeat((byte)0x5a, LatticeSeedDeriver.SeedByteLength)],
        compiledOptimizers ?? []);

    private static Project CreateProject() => new(
        null,
        ProjectSchema.CurrentVersion,
        new ProjectMetadata(
            Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8a00", CultureInfo.InvariantCulture),
            new string('a', 64),
            "Supercell test",
            null,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch),
        new ModelDefinition
        {
            Particles =
            [
                new Particle(particleA, "A", "A", new ChargeElementary(1.0), IsVacancy: false),
                new Particle(particleB, "B", "B", new ChargeElementary(2.0), IsVacancy: false),
            ],
            ParticleSets =
            [
                new ParticleSet(setA, "A site", [particleA, particleB]),
                new ParticleSet(setB, "B site", [particleB]),
            ],
            Structure = StructureModel.Default with
            {
                Name = "Supercell test",
                Sites =
                [
                    new CellSite(stableAId, "A", new Fractional3(0.0, 0.0, 0.0), setA, SiteStability.Stable),
                    new CellSite(unstableId, "U", new Fractional3(0.5, 0.5, 0.5), setA, SiteStability.Unstable),
                    new CellSite(stableBId, "B", new Fractional3(0.25, 0.25, 0.25), setB, SiteStability.Stable),
                ],
            },
        },
        [],
        [],
        [],
        ViewSettings.Default);

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
        ]),
        Structure = new CompiledStructureModel(
            [new Cartesian3(5.0, 0.0, 0.0), new Cartesian3(0.0, 5.0, 0.0), new Cartesian3(0.0, 0.0, 5.0)],
            125.0,
            [
                new CompiledSite(0, stableAId, "A", new Fractional3(0.0, 0.0, 0.0), SiteStability.Stable, setA, 0, [0]),
                new CompiledSite(1, unstableId, "U", new Fractional3(0.5, 0.5, 0.5), SiteStability.Unstable, setA, 1, [1]),
                new CompiledSite(2, stableBId, "B", new Fractional3(0.25, 0.25, 0.25), SiteStability.Stable, setB, 2, [2]),
            ],
            [],
            [
                new CompiledPosition(
                    0,
                    new Vector4I(0, 0, 0, 0),
                    new Fractional3(0.0, 0.0, 0.0),
                    Cartesian3.Zero,
                    0,
                    stableAId,
                    SiteStability.Stable,
                    0b10UL,
                    0,
                    0,
                    [],
                    []),
                new CompiledPosition(
                    1,
                    new Vector4I(0, 0, 0, 1),
                    new Fractional3(0.5, 0.5, 0.5),
                    Cartesian3.Zero,
                    1,
                    unstableId,
                    SiteStability.Unstable,
                    0b10UL,
                    1,
                    1,
                    [],
                    []),
                new CompiledPosition(
                    2,
                    new Vector4I(0, 0, 0, 2),
                    new Fractional3(0.25, 0.25, 0.25),
                    Cartesian3.Zero,
                    2,
                    stableBId,
                    SiteStability.Stable,
                    0b100UL,
                    2,
                    2,
                    [],
                    []),
            ],
            [0, 0, 0, 0]),
    };
}
