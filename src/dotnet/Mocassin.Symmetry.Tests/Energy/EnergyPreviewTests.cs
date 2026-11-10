// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using Mocassin.Symmetry.Energy;

namespace Mocassin.Symmetry.Tests.Energy;

[Trait("Tier", "PR-medium")]
public sealed class EnergyPreviewTests
{
    private static readonly EnergyPreviewService service = new();

    [Fact]
    public void ValidModelProducesCostAndPairPreviews()
    {
        var model = CreateModel();

        var cost = service.TryCreatePreview(model, 1, 1, 1, out var costDiagnostics);
        var pairs = service.TryCreatePairPreview(model, 1, 1, 1, out var pairDiagnostics);

        Assert.NotNull(cost);
        Assert.NotNull(pairs);
        var pairArray = pairs.GetValueOrDefault();
        Assert.DoesNotContain(costDiagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(pairDiagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(3, cost.StableP1PositionCount);
        Assert.True(cost.PairImageCount > 0);
        Assert.Equal(pairArray.Length, cost.UniquePairInteractionCount);
        Assert.Equal(cost.PairImageCount, cost.UniquePairInteractionCount);
        var cluster = Assert.Single(cost.ClusterCosts);
        Assert.Equal(12, cluster.RawPermutationCount);
        Assert.Equal(12, cluster.UniquePermutationCount);
        Assert.Equal(96, cluster.EstimatedBytes);
        Assert.True(cost.EstimatedMemoryBytes >= cluster.EstimatedBytes);
    }

    [Fact]
    public void ChiralDirectionsShareEnergyIdAndPartnerGeometry()
    {
        var model = CreateModel();

        var pairs = service.TryCreatePairPreview(model, 1, 1, 1, out var diagnostics);

        Assert.NotNull(pairs);
        Assert.Empty(diagnostics);
        var pairArray = pairs.GetValueOrDefault();
        var forward = pairArray.First(static pair => pair.SiteAId == siteAId && pair.SiteBId == siteBId);
        var reverse = pairArray.Single(
            pair => pair.SiteAId == siteBId &&
                pair.SiteBId == siteAId &&
                pair.CenterP == forward.PartnerP &&
                pair.PartnerP == forward.CenterP &&
                Math.Abs(pair.DistanceAngstrom - forward.DistanceAngstrom) < 1e-9);

        Assert.True(forward.IsChiral);
        Assert.True(reverse.IsChiral);
        Assert.Equal(forward.EnergyId, reverse.EnergyId);
        Assert.Equal(reverse.GeometryId, forward.ChiralPartnerGeometryId);
        Assert.Equal(forward.GeometryId, reverse.ChiralPartnerGeometryId);
        Assert.Equal(
            string.CompareOrdinal(forward.GeometryId, reverse.GeometryId) <= 0
                ? forward.GeometryId
                : reverse.GeometryId,
            forward.EnergyId);
    }

    [Fact]
    public void PairImageCountMatchesBruteForceSampling()
    {
        var model = CreateModel();
        var cost = service.TryCreatePreview(model, 1, 1, 1, out _);

        Assert.NotNull(cost);
        Assert.True(EnergyGeometryContext.TryCreate(model, null, out var context, out _));
        Assert.NotNull(context);
        long bruteForce = 0;
        var stable = model.StableEnvironment;
        foreach (var site in context.Sites.Where(static site => site.Stability == SiteStability.Stable))
        {
            foreach (var centerP in context.GetSitePIndices(site.Id))
            {
                foreach (var image in context.Geometry.RadialSearch(new Vector4I(0, 0, 0, centerP), stable.Cutoff.Value))
                {
                    var target = context.GetSiteAt(image.Offset.P);
                    if (target.Stability == SiteStability.Stable &&
                        !EnergyGeometryContext.IsFiltered(stable.Filters, site, target, image.DistanceAngstrom, true))
                    {
                        bruteForce++;
                    }
                }
            }
        }

        Assert.Equal(bruteForce, cost.PairImageCount);
    }

    [Fact]
    public void CeriaPairGeometryIdsAreUniqueAcrossP1Shells()
    {
        var project = ProjectJson.Deserialize(
            File.ReadAllBytes(RepositoryPaths.Combine("samples", "ceria", "ceria.mocprj")));

        var pairs = service.TryCreatePairPreview(project.Model, 1, 1, 1, out var diagnostics);

        Assert.NotNull(pairs);
        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var previews = pairs.GetValueOrDefault();
        Assert.NotEmpty(previews);
        Assert.Equal(previews.Length, previews.Select(static pair => pair.GeometryId).Distinct().Count());
        Assert.Contains(
            previews.GroupBy(static pair => (pair.SiteAId, pair.SiteBId)),
            static group => group.Select(static pair => pair.CenterP).Distinct().Count() > 1);
    }

    [Fact]
    public void InvalidModelReturnsNullWithoutThrowing()
    {
        var model = CreateModel() with
        {
            StableEnvironment = new StableEnvironment(new LengthAngstrom(0.0), []),
        };

        Assert.Null(service.TryCreatePreview(model, 1, 1, 1, out var costDiagnostics));
        Assert.Contains(
            costDiagnostics,
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.InvalidCutoff);
        Assert.Null(service.TryCreatePairPreview(model, 1, 1, 1, out var pairDiagnostics));
        Assert.Contains(
            pairDiagnostics,
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.InvalidCutoff);
    }

    [Fact]
    public void LargePermutationCaseReturnsLimitDiagnosticWithoutOverflow()
    {
        var model = CreateLargePermutationModel();

        var preview = service.TryCreatePreview(model, 1, 1, 1, out var diagnostics);

        Assert.Null(preview);
        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.ClusterPermutationLimit);
    }

    [Fact]
    public void CostPreviewIsCachedByEnergyAndCellCounts()
    {
        var local = new EnergyPreviewService();
        var model = CreateModel();

        Assert.NotNull(local.TryCreatePreview(model, 1, 1, 1, out _));
        Assert.NotNull(local.TryCreatePreview(model, 1, 1, 1, out _));
        Assert.Equal(1, local.Cache.Count);
        Assert.NotNull(local.TryCreatePreview(model, 2, 1, 1, out _));
        Assert.Equal(2, local.Cache.Count);
        var changed = model with
        {
            SiteEnergies =
            [
                new SiteEnergySet(siteAId, [new SiteEnergyEntry(Particles[0].Id, new EnergyEv(0.5))]),
            ],
        };
        Assert.NotNull(local.TryCreatePreview(changed, 1, 1, 1, out _));
        Assert.Equal(3, local.Cache.Count);
    }

    private static readonly CellSiteId siteAId =
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f10"));

    private static readonly CellSiteId siteBId =
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f11"));

    private static readonly CellSiteId siteCId =
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f12"));

    private static ImmutableArray<Particle> Particles { get; } =
    [
        new Particle(ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f20")), "P0", "P0", new ChargeElementary(0.0), false),
        new Particle(ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f21")), "P1", "P1", new ChargeElementary(0.0), false),
        new Particle(ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f22")), "P2", "P2", new ChargeElementary(0.0), false),
        new Particle(ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f23")), "P3", "P3", new ChargeElementary(0.0), false),
        new Particle(ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f24")), "P4", "P4", new ChargeElementary(0.0), false),
    ];

    private static ModelDefinition CreateModel()
    {
        var centerSet = new ParticleSet(
            ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f30")),
            "Center set",
            [Particles[0].Id, Particles[1].Id, Particles[2].Id]);
        var otherSet = new ParticleSet(
            ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f31")),
            "Other set",
            [Particles[3].Id, Particles[4].Id]);

        return new ModelDefinition
        {
            Particles = Particles,
            ParticleSets = [centerSet, otherSet],
            Structure = new StructureModel(
                "Preview fixture",
                new SpaceGroupRef(1, AxisSetting.None, OriginChoice.None),
                new CellParameters(
                    new LengthAngstrom(10.0),
                    new LengthAngstrom(10.0),
                    new LengthAngstrom(10.0),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0)),
                [
                    new CellSite(siteAId, "A", new Fractional3(0.0, 0.0, 0.0), centerSet.Id, SiteStability.Stable),
                    new CellSite(siteBId, "B", new Fractional3(0.1, 0.0, 0.0), otherSet.Id, SiteStability.Stable),
                    new CellSite(siteCId, "C", new Fractional3(0.2, 0.0, 0.0), otherSet.Id, SiteStability.Stable),
                ],
                []),
            StableEnvironment = new StableEnvironment(new LengthAngstrom(2.5), []),
            Clusters =
            [
                new Cluster(
                    ClusterId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f40")),
                    "Preview cluster",
                    siteAId,
                    [new Fractional3(0.1, 0.0, 0.0), new Fractional3(0.2, 0.0, 0.0)]),
            ],
        };
    }

    private static ModelDefinition CreateLargePermutationModel()
    {
        var particles = Enumerable.Range(0, 63)
            .Select(index => new Particle(
                ParticleId.Create(Guid.Parse($"018f4c9a-7f6b-7c3a-9d2e-{(uint)(0x8000 + index):x12}")),
                $"P{index}",
                $"P{index}",
                new ChargeElementary(0.0),
                false))
            .ToImmutableArray();
        var particleSet = new ParticleSet(
            ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f50")),
            "Large set",
            [.. particles.Select(static particle => particle.Id)]);
        var sites = Enumerable.Range(0, 9)
            .Select(index => new CellSite(
                CellSiteId.Create(Guid.Parse($"018f4c9a-7f6b-7c3a-9d2e-{(uint)(0x8100 + index):x12}")),
                $"S{index}",
                new Fractional3(index * 0.1, 0.0, 0.0),
                particleSet.Id,
                SiteStability.Stable))
            .ToImmutableArray();

        return new ModelDefinition
        {
            Particles = particles,
            ParticleSets = [particleSet],
            Structure = new StructureModel(
                "Large cluster fixture",
                new SpaceGroupRef(1, AxisSetting.None, OriginChoice.None),
                new CellParameters(
                    new LengthAngstrom(10.0),
                    new LengthAngstrom(10.0),
                    new LengthAngstrom(10.0),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0)),
                sites,
                []),
            StableEnvironment = new StableEnvironment(new LengthAngstrom(8.1), []),
            Clusters =
            [
                new Cluster(
                    ClusterId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f51")),
                    "Large cluster",
                    sites[0].Id,
                    [.. Enumerable.Range(1, 8).Select(index => new Fractional3(index * 0.1, 0.0, 0.0))]),
            ],
        };
    }
}
