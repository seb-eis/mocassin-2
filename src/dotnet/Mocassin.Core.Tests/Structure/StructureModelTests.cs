// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Symmetry;

namespace Mocassin.Core.Tests.Structure;

[Trait("Tier", "PR-fast")]
public sealed class StructureModelTests
{
    [Fact]
    public void DefaultUsesP1UnitCellAndNoPositions()
    {
        var structure = StructureModel.Default;

        Assert.Equal("New Structure", structure.Name);
        Assert.Equal(new SpaceGroupRef(1, AxisSetting.None, OriginChoice.None), structure.SpaceGroup);
        Assert.Equal(1.0, structure.Cell.A.Value);
        Assert.Equal(1.0, structure.Cell.B.Value);
        Assert.Equal(1.0, structure.Cell.C.Value);
        Assert.Equal(90.0, structure.Cell.Alpha.Value);
        Assert.Equal(90.0, structure.Cell.Beta.Value);
        Assert.Equal(90.0, structure.Cell.Gamma.Value);
        Assert.Empty(structure.Sites);
        Assert.Empty(structure.DummyPositions);
    }

    [Fact]
    public void DefaultNameAndSiteLimitMatchTheDocumentedContract()
    {
        Assert.Equal("New Structure", StructureModel.DefaultName);
        Assert.Equal(1000, StructureModel.MaxReferenceSites);
    }

    [Fact]
    public void SpaceGroupRefPreservesValues()
    {
        var reference = new SpaceGroupRef(225, AxisSetting.None, OriginChoice.None);

        Assert.Equal(225, reference.Number);
        Assert.Equal(AxisSetting.None, reference.Axis);
        Assert.Equal(OriginChoice.None, reference.Origin);
    }

    [Fact]
    public void CellSitePreservesValues()
    {
        var site = CreateSite();

        Assert.NotEqual(default, site.Id);
        Assert.Equal("Ce", site.Name);
        Assert.Equal(new Fractional3(0.0, 0.0, 0.0), site.Fractional);
        Assert.NotEqual(ParticleSetId.Empty, site.ParticleSetId);
        Assert.Equal(SiteStability.Stable, site.Stability);
    }

    [Fact]
    public void CellDummyPositionPreservesValues()
    {
        var dummy = CreateDummy();

        Assert.NotEqual(default, dummy.Id);
        Assert.Equal("Center", dummy.Name);
        Assert.Equal(new Fractional3(0.5, 0.5, 0.5), dummy.Fractional);
    }

    [Fact]
    public void SiteStabilityHasStableAndUnstableValues()
    {
        Assert.NotEqual(SiteStability.Stable, SiteStability.Unstable);
        Assert.Equal(0, (int)SiteStability.Stable);
        Assert.Equal(1, (int)SiteStability.Unstable);
    }

    [Fact]
    public void CellDummyPositionIdJsonRoundTrips()
    {
        var id = CellDummyPositionId.New();

        var json = JsonSerializer.Serialize(id);
        var loaded = JsonSerializer.Deserialize<CellDummyPositionId>(json);

        Assert.Equal(id, loaded);
    }

    private static CellSite CreateSite() =>
        new(
            CellSiteId.New(),
            "Ce",
            Fractional3.Zero,
            ParticleSetId.New(),
            SiteStability.Stable);

    private static CellDummyPosition CreateDummy() =>
        new(CellDummyPositionId.New(), "Center", new Fractional3(0.5, 0.5, 0.5));
}
