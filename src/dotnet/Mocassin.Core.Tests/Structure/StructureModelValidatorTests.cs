// SPDX-License-Identifier: MIT

using FsCheck.Xunit;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;

namespace Mocassin.Core.Tests.Structure;

[Trait("Tier", "PR-fast")]
public sealed class StructureModelValidatorTests
{
    [Fact]
    public void ValidStructureProducesNoDiagnostics()
    {
        var structure = CreateStructure(siteCount: 3, dummyCount: 2);

        Assert.Empty(StructureModelValidator.Validate(structure));
    }

    [Fact]
    public void DefaultStructureProducesNoDiagnostics()
    {
        Assert.Empty(StructureModelValidator.Validate(StructureModel.Default));
    }

    [Fact]
    public void EmptyStructureNameProducesStr013()
    {
        var structure = StructureModel.Default with { Name = " " };

        var diagnostic = Assert.Single(StructureModelValidator.Validate(structure));

        Assert.Equal(CoreStructureDiagnosticCodes.InvalidStructureName, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("structure", diagnostic.Path?.Domain);
        Assert.Equal("name", diagnostic.Path?.Property);
    }

    [Fact]
    public void OneThousandSitesAreAccepted()
    {
        var structure = CreateStructure(siteCount: 1000);

        Assert.DoesNotContain(
            StructureModelValidator.Validate(structure),
            static diagnostic => diagnostic.Code == CoreStructureDiagnosticCodes.SiteLimitExceeded);
    }

    [Fact]
    public void OneThousandAndOneSitesProduceStr012()
    {
        var structure = CreateStructure(siteCount: 1001);

        Assert.Contains(
            StructureModelValidator.Validate(structure),
            static diagnostic => diagnostic.Code == CoreStructureDiagnosticCodes.SiteLimitExceeded);
    }

    [Fact]
    public void EmptySiteNameProducesStr005()
    {
        var structure = WithSites(CreateSite(" "));

        var diagnostic = Assert.Single(StructureModelValidator.Validate(structure));

        Assert.Equal(CoreStructureDiagnosticCodes.InvalidSiteName, diagnostic.Code);
        Assert.Equal("name", diagnostic.Path?.Property);
    }

    [Fact]
    public void DuplicateSiteNameIsCaseInsensitiveAndProducesStr005()
    {
        var structure = WithSites(CreateSite("Ce"), CreateSite("ce"));

        Assert.Contains(
            StructureModelValidator.Validate(structure),
            static diagnostic => diagnostic.Code == CoreStructureDiagnosticCodes.InvalidSiteName);
    }

    [Fact]
    public void MissingParticleSetProducesStr006()
    {
        var defaultSet = CreateSite("Ce", particleSetId: default(ParticleSetId));
        var reservedSet = CreateSite("O", particleSetId: ParticleSetId.Empty);
        var structure = WithSites(defaultSet, reservedSet);

        var diagnostics = StructureModelValidator.Validate(structure);

        Assert.Equal(
            2,
            diagnostics.Count(static diagnostic => diagnostic.Code == CoreStructureDiagnosticCodes.MissingParticleSet));
    }

    [Fact]
    public void OutOfRangeSitePositionProducesStr004()
    {
        var structure = WithSites(
            CreateSite("A", new Fractional3(1.1, 0.5, 0.5)),
            CreateSite("B", new Fractional3(-0.1, 0.5, 0.5)),
            CreateSite("C", new Fractional3(0.5, 2.0, 0.5)));

        var diagnostics = StructureModelValidator.Validate(structure);

        Assert.Equal(
            3,
            diagnostics.Count(static diagnostic => diagnostic.Code == CoreStructureDiagnosticCodes.InvalidSitePosition));
    }

    [Fact]
    public void NonFiniteSitePositionProducesStr004()
    {
        var structure = WithSites(
            CreateSite("A", new Fractional3(double.NaN, 0.5, 0.5)),
            CreateSite("B", new Fractional3(0.5, double.PositiveInfinity, 0.5)));

        var diagnostics = StructureModelValidator.Validate(structure);

        Assert.Equal(
            2,
            diagnostics.Count(static diagnostic => diagnostic.Code == CoreStructureDiagnosticCodes.InvalidSitePosition));
    }

    [Fact]
    public void PositionsWithinFractionalTrimToleranceAreAccepted()
    {
        var structure = WithSites(
            CreateSite("A", new Fractional3(1.0, 0.0, 0.0)),
            CreateSite("B", new Fractional3(1.0 + 5e-11, 0.0, 0.0)),
            CreateSite("C", new Fractional3(-5e-11, 0.0, 0.0)),
            CreateSite("D", new Fractional3(0.99999999995, 0.0, 0.0)));

        Assert.Empty(StructureModelValidator.Validate(structure));
    }

    [Fact]
    public void PositionsBeyondFractionalTrimToleranceAreRejected()
    {
        var structure = WithSites(
            CreateSite("A", new Fractional3(1.0 + 2e-10, 0.0, 0.0)),
            CreateSite("B", new Fractional3(-2e-10, 0.0, 0.0)));

        var diagnostics = StructureModelValidator.Validate(structure);

        Assert.Equal(
            2,
            diagnostics.Count(static diagnostic => diagnostic.Code == CoreStructureDiagnosticCodes.InvalidSitePosition));
    }

    [Fact]
    public void InvalidDummyPositionProducesStr010()
    {
        var structure = WithDummies(
            CreateDummy("A", new Fractional3(1.1, 0.5, 0.5)),
            CreateDummy("B", new Fractional3(double.NaN, 0.5, 0.5)));

        var diagnostics = StructureModelValidator.Validate(structure);

        Assert.Equal(
            2,
            diagnostics.Count(static diagnostic => diagnostic.Code == CoreStructureDiagnosticCodes.InvalidDummyPosition));
    }

    [Fact]
    public void EmptyDummyNameProducesStr015()
    {
        var structure = WithDummies(CreateDummy(" "));

        var diagnostic = Assert.Single(StructureModelValidator.Validate(structure));

        Assert.Equal(CoreStructureDiagnosticCodes.InvalidDummyName, diagnostic.Code);
        Assert.Equal("name", diagnostic.Path?.Property);
    }

    [Fact]
    public void DuplicateDummyNameIsCaseInsensitiveAndProducesStr015()
    {
        var structure = WithDummies(CreateDummy("Center"), CreateDummy("center"));

        Assert.Contains(
            StructureModelValidator.Validate(structure),
            static diagnostic => diagnostic.Code == CoreStructureDiagnosticCodes.InvalidDummyName);
    }

    [Fact]
    public void DummyPositionMayCoincideWithSite()
    {
        var position = new Fractional3(0.25, 0.25, 0.25);
        var structure = StructureModel.Default with
        {
            Sites = [CreateSite("O", position)],
            DummyPositions = [CreateDummy("O helper", position)],
        };

        Assert.Empty(StructureModelValidator.Validate(structure));
    }

    [Property(MaxTest = 100)]
    public bool ValidRandomStructuresHaveNoDiagnostics(byte siteCountSeed, byte dummyCountSeed)
    {
        var structure = CreateStructure(siteCountSeed % 20, dummyCountSeed % 20);

        return StructureModelValidator.Validate(structure).IsEmpty;
    }

    private static StructureModel CreateStructure(int siteCount = 0, int dummyCount = 0)
    {
        var sites = Enumerable.Range(0, siteCount)
            .Select(index => CreateSite($"Site {index}", Position(index)))
            .ToArray();
        var dummies = Enumerable.Range(0, dummyCount)
            .Select(index => CreateDummy($"Dummy {index}", Position(index + 1)))
            .ToArray();

        return StructureModel.Default with
        {
            Sites = [.. sites],
            DummyPositions = [.. dummies],
        };
    }

    private static StructureModel WithSites(params CellSite[] sites) =>
        StructureModel.Default with { Sites = [.. sites] };

    private static StructureModel WithDummies(params CellDummyPosition[] dummies) =>
        StructureModel.Default with { DummyPositions = [.. dummies] };

    private static Fractional3 Position(int index) =>
        new((index % 10) / 10.0, (index / 10 % 10) / 10.0, 0.0);

    private static CellSite CreateSite(
        string name,
        Fractional3? fractional = null,
        ParticleSetId? particleSetId = null,
        SiteStability stability = SiteStability.Stable) =>
        new(
            CellSiteId.New(),
            name,
            fractional ?? Fractional3.Zero,
            particleSetId ?? ParticleSetId.New(),
            stability);

    private static CellDummyPosition CreateDummy(string name, Fractional3? fractional = null) =>
        new(CellDummyPositionId.New(), name, fractional ?? new Fractional3(0.5, 0.5, 0.5));
}
