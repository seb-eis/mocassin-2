// SPDX-License-Identifier: MIT

using BenchmarkDotNet.Attributes;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using Mocassin.Symmetry.Energy;

namespace Mocassin.Benchmarks.Energy;

/// <summary>
/// Energy cost-preview benchmarks for ceria-like and barium-zirconate-like P1 fixtures.
/// </summary>
/// <remarks>
/// Each invocation creates a fresh preview service so the measurement covers the cache-miss path.
/// </remarks>
[MemoryDiagnoser]
public class EnergyPreviewBenchmarks
{
    private ModelDefinition ceria = null!;
    private ModelDefinition bariumZirconate = null!;

    /// <summary>
    /// Builds and validates the benchmark fixtures.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        ceria = CreateFixture("Ceria-like", 5.41, 3.0);
        bariumZirconate = CreateFixture("Barium-zirconate-like", 4.19, 2.0);

        ValidateFixture(ceria, "ceria");
        ValidateFixture(bariumZirconate, "barium zirconate");
    }

    /// <summary>
    /// Builds the ceria-like energy preview.
    /// </summary>
    /// <returns>A checksum that prevents elimination.</returns>
    [Benchmark]
    public long EnergyPreviewCeria() => CreateChecksum(ceria);

    /// <summary>
    /// Builds the barium-zirconate-like energy preview.
    /// </summary>
    /// <returns>A checksum that prevents elimination.</returns>
    [Benchmark]
    public long EnergyPreviewBariumZirconate() => CreateChecksum(bariumZirconate);

    private static long CreateChecksum(ModelDefinition model)
    {
        var service = new EnergyPreviewService();
        var preview = service.TryCreatePreview(model, 1, 1, 1, out var diagnostics);
        if (preview is null)
        {
            throw new InvalidOperationException(
                $"The energy-preview fixture was rejected: {string.Join("; ", diagnostics.Select(static diagnostic => diagnostic.ToString()))}");
        }

        return preview.PairImageCount + preview.UniquePairInteractionCount + preview.EstimatedMemoryBytes;
    }

    private static void ValidateFixture(ModelDefinition model, string name)
    {
        var service = new EnergyPreviewService();
        var preview = service.TryCreatePreview(model, 1, 1, 1, out var diagnostics);
        if (preview is null)
        {
            throw new InvalidOperationException(
                $"The {name} energy fixture is invalid: {string.Join("; ", diagnostics.Select(static diagnostic => diagnostic.ToString()))}");
        }

        if (preview.UniquePairInteractionCount == 0)
        {
            throw new InvalidOperationException($"The {name} energy fixture has no pair interactions.");
        }
    }

    private static ModelDefinition CreateFixture(string name, double length, double cutoff)
    {
        var oxygen = new Particle(
            ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9f10")),
            "O2-",
            "O",
            new ChargeElementary(-2.0),
            IsVacancy: false);
        var vacancy = new Particle(
            ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9f11")),
            "Vacancy",
            "Vc",
            new ChargeElementary(0.0),
            IsVacancy: true);
        var cation = new Particle(
            ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9f12")),
            "Cation",
            "C",
            new ChargeElementary(4.0),
            IsVacancy: false);
        var oxygenSet = new ParticleSet(
            ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9f13")),
            "Oxygen",
            [oxygen.Id, vacancy.Id]);
        var cationSet = new ParticleSet(
            ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9f14")),
            "Cation",
            [cation.Id]);
        var centerId = CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9f15"));
        var unstableId = CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9f16"));
        var endId = CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9f17"));
        var cationId = CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9f18"));

        return new ModelDefinition
        {
            Particles = [oxygen, vacancy, cation],
            ParticleSets = [oxygenSet, cationSet],
            Structure = new StructureModel(
                name,
                new SpaceGroupRef(1, AxisSetting.None, OriginChoice.None),
                new CellParameters(
                    new LengthAngstrom(length),
                    new LengthAngstrom(length),
                    new LengthAngstrom(length),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0)),
                [
                    new CellSite(centerId, "O", new Fractional3(0.25, 0.25, 0.25), oxygenSet.Id, SiteStability.Stable),
                    new CellSite(unstableId, "O transition", new Fractional3(0.5, 0.25, 0.25), oxygenSet.Id, SiteStability.Unstable),
                    new CellSite(endId, "O end", new Fractional3(0.75, 0.25, 0.25), oxygenSet.Id, SiteStability.Stable),
                    new CellSite(cationId, "Cation", Fractional3.Zero, cationSet.Id, SiteStability.Stable),
                ],
                []),
            StableEnvironment = new StableEnvironment(new LengthAngstrom(cutoff), []),
            UnstableEnvironments =
            [
                new UnstableEnvironment(unstableId, new LengthAngstrom(cutoff), []),
            ],
            Clusters =
            [
                new Cluster(
                    ClusterId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9f40")),
                    "Migration edge",
                    unstableId,
                    [
                        new Fractional3(-0.25, 0.0, 0.0),
                        new Fractional3(0.25, 0.0, 0.0),
                    ]),
            ],
            SiteEnergies =
            [
                new SiteEnergySet(
                    centerId,
                    [
                        new SiteEnergyEntry(oxygen.Id, new EnergyEv(-0.5)),
                        new SiteEnergyEntry(vacancy.Id, new EnergyEv(0.0)),
                    ]),
                new SiteEnergySet(
                    cationId,
                    [new SiteEnergyEntry(cation.Id, new EnergyEv(1.2))]),
            ],
        };
    }
}
