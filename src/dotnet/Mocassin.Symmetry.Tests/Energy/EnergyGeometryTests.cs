// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using Mocassin.Symmetry.Energy;
using Mocassin.Symmetry.Tests.TestSupport;

namespace Mocassin.Symmetry.Tests.Energy;

[Trait("Tier", "PR-medium")]
public sealed class EnergyGeometryTests
{
    private static readonly EnergyGeometryValidator validator = new();

    [Fact]
    public void ValidClusterProducesNoGeometryErrors()
    {
        var model = CreateValidModel();

        var diagnostics = validator.Validate(model);

        Assert.DoesNotContain(
            diagnostics,
            static diagnostic => diagnostic.Code is
                EnergyDiagnosticCodes.PeriodicAmbiguity or
                EnergyDiagnosticCodes.DuplicateClusterOffset or
                EnergyDiagnosticCodes.InvalidClusterPosition or
                EnergyDiagnosticCodes.DuplicateClusterGeometry or
                EnergyDiagnosticCodes.ClusterPermutationWarning or
                EnergyDiagnosticCodes.ClusterPermutationLimit);
    }

    [Fact]
    public void CutoffAboveHalfSmallestCellDimensionProducesEnm002()
    {
        var model = CreateValidModel() with
        {
            StableEnvironment = new StableEnvironment(new LengthAngstrom(2.6), []),
        };

        Assert.Contains(
            validator.Validate(model),
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.PeriodicAmbiguity);
    }

    [Fact]
    public void DuplicatePeriodicOffsetProducesEnm009()
    {
        var model = CreateValidModel() with
        {
            Clusters =
            [
                new Cluster(
                    ClusterId.New(),
                    "Duplicate",
                    TransitionTestModels.SiteId(TransitionTestModels.Site2),
                    [new Fractional3(0.5, 0.0, 0.0), new Fractional3(0.5, 0.0, 0.0)]),
            ],
        };

        Assert.Contains(
            validator.Validate(model),
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.DuplicateClusterOffset);
    }

    [Fact]
    public void UnstableUnknownFilteredAndOutOfRangeSurroundingsProduceEnm010()
    {
        var unstable = CreateValidModel() with
        {
            Clusters =
            [
                new Cluster(
                    ClusterId.New(),
                    "Unstable target",
                    TransitionTestModels.SiteId(TransitionTestModels.Site2),
                    [new Fractional3(0.25, 0.0, 0.0), new Fractional3(0.5, 0.0, 0.0)]),
            ],
        };
        var unknown = CreateValidModel() with
        {
            Clusters =
            [
                new Cluster(
                    ClusterId.New(),
                    "Unknown target",
                    TransitionTestModels.SiteId(TransitionTestModels.Site2),
                    [new Fractional3(0.123, 0.0, 0.0), new Fractional3(0.5, 0.0, 0.0)]),
            ],
        };
        var filtered = CreateValidModel() with
        {
            StableEnvironment = new StableEnvironment(
                new LengthAngstrom(2.5),
                [
                    new InteractionFilter(
                        new LengthAngstrom(2.4),
                        new LengthAngstrom(2.6),
                        TransitionTestModels.SiteId(TransitionTestModels.Site2),
                        TransitionTestModels.SiteId(TransitionTestModels.Site4)),
                ]),
        };
        var outOfRange = CreateValidModel() with
        {
            StableEnvironment = new StableEnvironment(new LengthAngstrom(2.4), []),
        };

        Assert.Contains(
            validator.Validate(unstable),
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.InvalidClusterPosition);
        Assert.Contains(
            validator.Validate(unknown),
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.InvalidClusterPosition);
        Assert.Contains(
            validator.Validate(filtered),
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.InvalidClusterPosition);
        Assert.Contains(
            validator.Validate(outOfRange),
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.InvalidClusterPosition);
    }

    [Fact]
    public void PeriodicCellShiftIsNotADuplicateOrCentreEquivalentOffset()
    {
        var model = CreateValidModel() with
        {
            Clusters =
            [
                new Cluster(
                    ClusterId.New(),
                    "Periodic images",
                    TransitionTestModels.SiteId(TransitionTestModels.Site2),
                    [new Fractional3(0.5, 0.0, 0.0), new Fractional3(1.5, 0.0, 0.0)]),
                new Cluster(
                    ClusterId.New(),
                    "Shifted centre image",
                    TransitionTestModels.SiteId(TransitionTestModels.Site2),
                    [new Fractional3(1.0, 0.0, 0.0), new Fractional3(0.5, 0.0, 0.0)]),
            ],
        };

        var diagnostics = validator.Validate(model);

        Assert.DoesNotContain(
            diagnostics,
            static diagnostic => diagnostic.Code is
                EnergyDiagnosticCodes.DuplicateClusterOffset or
                EnergyDiagnosticCodes.DuplicateClusterGeometry);
    }

    [Fact]
    public void TwoCentreEquivalentOffsetsReportOneEnm011()
    {
        var model = CreateValidModel() with
        {
            Clusters =
            [
                new Cluster(
                    ClusterId.New(),
                    "Repeated centre",
                    TransitionTestModels.SiteId(TransitionTestModels.Site2),
                    [new Fractional3(0.0, 0.0, 0.0), new Fractional3(0.0, 0.0, 0.0)]),
            ],
        };

        var diagnostics = validator.Validate(model);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.DuplicateClusterOffset);
        Assert.Single(
            diagnostics,
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.DuplicateClusterGeometry);
    }

    [Fact]
    public void CentreEquivalentOffsetProducesEnm011()
    {
        var model = CreateValidModel() with
        {
            Clusters =
            [
                new Cluster(
                    ClusterId.New(),
                    "Centre equivalent",
                    TransitionTestModels.SiteId(TransitionTestModels.Site2),
                    [new Fractional3(0.0, 0.0, 0.0), new Fractional3(0.5, 0.0, 0.0)]),
            ],
        };

        Assert.Contains(
            validator.Validate(model),
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.DuplicateClusterGeometry);
    }

    [Theory]
    [InlineData(2, EnergyDiagnosticCodes.ClusterPermutationWarning)]
    [InlineData(4, EnergyDiagnosticCodes.ClusterPermutationLimit)]
    public void ClusterPermutationLimitsProduceWarningsAndErrors(int particleCount, string expectedCode)
    {
        var model = CreateNineSiteModel(particleCount);

        Assert.Contains(
            validator.Validate(model),
            diagnostic => diagnostic.Code == expectedCode);
    }

    [Fact]
    public void EmptySamplingProducesEnm017()
    {
        var model = TransitionTestModels.CreateModel() with
        {
            StableEnvironment = new StableEnvironment(new LengthAngstrom(0.001), []),
        };

        Assert.Contains(
            validator.Validate(model),
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.EnvironmentSamplingWarning);
    }

    [Fact]
    public void ExcessiveSamplingProducesEnm019()
    {
        var baseModel = TransitionTestModels.CreateModel();
        var model = baseModel with
        {
            Structure = baseModel.Structure with
            {
                Cell = new CellParameters(
                    new LengthAngstrom(10.0),
                    new LengthAngstrom(10.0),
                    new LengthAngstrom(10.0),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0)),
            },
            StableEnvironment = new StableEnvironment(new LengthAngstrom(60.0), []),
        };

        Assert.Contains(
            validator.Validate(model),
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.EnvironmentSamplingLimit);
    }

    [Fact]
    public void SamplerReturnsPairsAndFailsWithoutThrowingOnInvalidInput()
    {
        var sampler = new EnergyGeometrySampler();
        var valid = CreateValidModel();

        Assert.True(sampler.TrySamplePairs(valid, 1, 1, 1, out var pairs, out var validDiagnostics));
        Assert.Empty(validDiagnostics);
        Assert.NotEmpty(pairs);
        Assert.All(pairs, static pair => Assert.False(string.IsNullOrWhiteSpace(pair.GeometryId)));

        var invalid = valid with
        {
            Structure = valid.Structure with
            {
                Cell = valid.Structure.Cell with { A = new LengthAngstrom(0.0) },
            },
        };
        Assert.False(sampler.TrySamplePairs(invalid, 1, 1, 1, out var invalidPairs, out var invalidDiagnostics));
        Assert.Empty(invalidPairs);
        Assert.NotEmpty(invalidDiagnostics);
    }

    private static ModelDefinition CreateValidModel() =>
        TransitionTestModels.CreateModel() with
        {
            StableEnvironment = new StableEnvironment(new LengthAngstrom(2.5), []),
            UnstableEnvironments =
            [
                new UnstableEnvironment(
                    TransitionTestModels.SiteId(TransitionTestModels.Site3),
                    new LengthAngstrom(2.5),
                    []),
            ],
            Clusters =
            [
                new Cluster(
                    ClusterId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6f71")),
                    "Migration edge",
                    TransitionTestModels.SiteId(TransitionTestModels.Site2),
                    [new Fractional3(0.5, 0.0, 0.0), new Fractional3(-0.25, -0.25, -0.25)]),
            ],
        };

    private static ModelDefinition CreateNineSiteModel(int particleCount)
    {
        var particles = Enumerable.Range(0, particleCount)
            .Select(index => new Particle(
                ParticleId.Create(Guid.Parse($"018f4c9a-7f6b-7c3a-9d2e-{(uint)(0x7000 + index):x12}")),
                $"P{index}",
                $"P{index}",
                new ChargeElementary(0.0),
                IsVacancy: false))
            .ToImmutableArray();
        var particleSet = new ParticleSet(
            ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f00")),
            "Nine-site set",
            [.. particles.Select(static particle => particle.Id)]);
        var sites = Enumerable.Range(0, 9)
            .Select(index => new CellSite(
                CellSiteId.Create(Guid.Parse($"018f4c9a-7f6b-7c3a-9d2e-{(uint)(0x7100 + index):x12}")),
                $"Site{index}",
                new Fractional3(index * 0.1, 0.0, 0.0),
                particleSet.Id,
                SiteStability.Stable))
            .ToImmutableArray();
        var offsets = Enumerable.Range(1, 8)
            .Select(index => new Fractional3(index * 0.1, 0.0, 0.0))
            .ToImmutableArray();

        return new ModelDefinition
        {
            Particles = particles,
            ParticleSets = [particleSet],
            Structure = new StructureModel(
                "Nine sites",
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
                    ClusterId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f01")),
                    "Large cluster",
                    sites[0].Id,
                    offsets),
            ],
        };
    }
}
