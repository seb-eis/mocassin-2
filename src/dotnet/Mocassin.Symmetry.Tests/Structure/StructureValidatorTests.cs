// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using Mocassin.Symmetry.Structure;

namespace Mocassin.Symmetry.Tests.Structure;

[Trait("Tier", "PR-fast")]
public sealed class StructureValidatorTests
{
    private static readonly StructureValidator validator = new();

    [Fact]
    public void ValidCeriaProducesNoDiagnostics()
    {
        Assert.Empty(validator.Validate(CreateStructure()));
    }

    [Fact]
    public void InvalidSpaceGroupProducesStr001()
    {
        var structure = CreateStructure() with
        {
            SpaceGroup = new SpaceGroupRef(999, AxisSetting.None, OriginChoice.None),
        };

        var diagnostics = validator.Validate(structure);

        var diagnostic = Assert.Single(diagnostics.Where(static item => item.Code == "STR001"));
        Assert.Equal("structure", diagnostic.Path?.Domain);
        Assert.Equal("spaceGroup", diagnostic.Path?.Property);
    }

    [Fact]
    public void ReservedUniqueAxisAProducesStr001()
    {
        var structure = CreateStructure() with
        {
            SpaceGroup = new SpaceGroupRef(3, AxisSetting.UniqueAxisA, OriginChoice.None),
        };

        Assert.Contains(
            validator.Validate(structure),
            static diagnostic => diagnostic.Code == "STR001");
    }

    [Fact]
    public void InvalidCellLengthProducesStr002()
    {
        var structure = CreateStructure() with
        {
            Cell = CreateStructure().Cell with { A = new LengthAngstrom(0.0) },
        };

        Assert.Contains(
            validator.Validate(structure),
            static diagnostic => diagnostic.Code == "STR002");
    }

    [Fact]
    public void CrystalSystemConstraintProducesStr003()
    {
        var structure = CreateStructure() with
        {
            Cell = CreateStructure().Cell with { A = new LengthAngstrom(5.0) },
        };

        Assert.Contains(
            validator.Validate(structure),
            static diagnostic => diagnostic.Code == "STR003");
    }

    [Fact]
    public void OverlappingSitesProduceStr007WithCellSitePaths()
    {
        var firstId = CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e93"));
        var secondId = CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e94"));
        var structure = CreateStructure() with
        {
            SpaceGroup = new SpaceGroupRef(1, AxisSetting.None, OriginChoice.None),
            Sites =
            [
                CreateSite("A", new Fractional3(0.25, 0.25, 0.25), firstId),
                CreateSite("B", new Fractional3(0.25, 0.25, 0.25), secondId),
            ],
        };

        var diagnostic = Assert.Single(
            validator.Validate(structure).Where(static item => item.Code == "STR007"));

        Assert.Equal("cellSite", diagnostic.Path?.Domain);
        Assert.Equal(secondId.Value, diagnostic.Path?.ObjectId);
        Assert.Contains(diagnostic.Related, path => path.ObjectId == firstId.Value);
    }

    [Fact]
    public void P1BudgetProducesStr009Warning()
    {
        var sites = Enumerable.Range(0, 100)
            .Select(index => CreateSite(
                $"Site {index}",
                new Fractional3(0.001 * (index + 1), 0.137, 0.271)))
            .ToArray();
        var structure = CreateStructure() with
        {
            SpaceGroup = new SpaceGroupRef(225, AxisSetting.None, OriginChoice.None),
            Sites = [.. sites],
        };

        var diagnostic = Assert.Single(
            validator.Validate(structure).Where(static item => item.Code == "STR009"));

        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    private static StructureModel CreateStructure() =>
        StructureModel.Default with
        {
            Name = "Ceria",
            SpaceGroup = new SpaceGroupRef(225, AxisSetting.None, OriginChoice.None),
            Cell = new CellParameters(
                new LengthAngstrom(5.41),
                new LengthAngstrom(5.41),
                new LengthAngstrom(5.41),
                new AngleDegrees(90.0),
                new AngleDegrees(90.0),
                new AngleDegrees(90.0)),
            Sites =
            [
                CreateSite("Ce", Fractional3.Zero),
                CreateSite("O", new Fractional3(0.25, 0.25, 0.25)),
                CreateSite("O transition", new Fractional3(0.5, 0.25, 0.25), stability: SiteStability.Unstable),
            ],
        };

    private static CellSite CreateSite(
        string name,
        Fractional3 position,
        CellSiteId? id = null,
        SiteStability stability = SiteStability.Stable) =>
        new(id ?? CellSiteId.New(), name, position, ParticleSetId.New(), stability);
}
