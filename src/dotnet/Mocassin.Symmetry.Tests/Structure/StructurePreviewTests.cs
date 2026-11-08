// SPDX-License-Identifier: MIT

using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using Mocassin.Symmetry.Structure;

namespace Mocassin.Symmetry.Tests.Structure;

[Trait("Tier", "PR-fast")]
public sealed class StructurePreviewTests
{
    private static readonly StructurePreviewService service = new();
    private static readonly int[] ceriaOrbitSizes = [4, 8, 24];
    private static readonly int[] bariumZirconateOrbitSizes = [1, 1, 3];

    [Fact]
    public void CeriaPreviewMatchesKnownOrbitSizes()
    {
        var preview = service.TryCreatePreview(CreateCeria(), 1, 1, 1, out var diagnostics);

        Assert.NotNull(preview);
        Assert.Empty(diagnostics);
        Assert.Equal(ceriaOrbitSizes, preview.Orbits.Select(static orbit => orbit.Multiplicity).ToArray());
        Assert.Equal(36, preview.PositionList.Length);
        Assert.Equal(36, preview.CartesianPositions.Length);
        Assert.Equal(Math.Pow(5.41, 3), preview.CellVolumeAngstrom3, 9);
        Assert.Equal(1, preview.CellCountA);
        Assert.Equal(1, preview.CellCountB);
        Assert.Equal(1, preview.CellCountC);
    }

    [Fact]
    public void BariumZirconatePreviewMatchesKnownOrbitSizes()
    {
        var preview = service.TryCreatePreview(CreateBariumZirconate(), 1, 1, 1, out var diagnostics);

        Assert.NotNull(preview);
        Assert.Empty(diagnostics);
        Assert.Equal(bariumZirconateOrbitSizes, preview.Orbits.Select(static orbit => orbit.Multiplicity).ToArray());
        Assert.Equal(5, preview.PositionList.Length);
        Assert.Equal(Math.Pow(4.19, 3), preview.CellVolumeAngstrom3, 9);
    }

    [Fact]
    public void PreviewEchoesRequestedCellCounts()
    {
        var preview = service.TryCreatePreview(CreateCeria(), 2, 3, 4, out _);

        Assert.NotNull(preview);
        Assert.Equal(2, preview.CellCountA);
        Assert.Equal(3, preview.CellCountB);
        Assert.Equal(4, preview.CellCountC);
    }

    [Fact]
    public void PreviewIsDeterministic()
    {
        var ceria = CreateCeria();
        var first = service.TryCreatePreview(ceria, 2, 2, 2, out _);
        var second = service.TryCreatePreview(ceria, 2, 2, 2, out _);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(
            first.PositionList.Select(static site => site.Fractional).ToArray(),
            second.PositionList.Select(static site => site.Fractional).ToArray());
        Assert.Equal(first.CartesianPositions.ToArray(), second.CartesianPositions.ToArray());
        Assert.Equal(
            first.Orbits.Select(static orbit => orbit.Multiplicity).ToArray(),
            second.Orbits.Select(static orbit => orbit.Multiplicity).ToArray());
    }

    [Fact]
    public void CacheSeparatesCellCountsAndStructureHashes()
    {
        var localService = new StructurePreviewService();
        var ceria = CreateCeria();

        Assert.NotNull(localService.TryCreatePreview(ceria, 1, 1, 1, out _));
        Assert.NotNull(localService.TryCreatePreview(ceria, 2, 2, 2, out _));
        Assert.NotNull(localService.TryCreatePreview(ceria with { Name = "Ceria copy" }, 1, 1, 1, out _));

        Assert.Equal(3, localService.Cache.Count);
    }

    [Fact]
    public void CacheSeparatesCatalogAndContractVersions()
    {
        var catalog = Mocassin.Symmetry.SpaceGroups.SpaceGroupCatalog.Default;
        var versions = Mocassin.Symmetry.Contracts.SymmetryVersionInfo.FromCatalog(catalog);
        var otherVersions = versions with
        {
            SpaceGroupDataSha256 = new string('0', 64),
            SymmetryContractVersion = "symmetry-2",
        };
        var cache = new Mocassin.Symmetry.Caching.SymmetryCache(
            versions.SpaceGroupDataVersion,
            versions.SymmetryContractVersion,
            16);
        var first = new StructurePreviewService(catalog, versions, cache);
        var second = new StructurePreviewService(catalog, otherVersions, cache);
        var ceria = CreateCeria();

        Assert.NotNull(first.TryCreatePreview(ceria, 1, 1, 1, out _));
        Assert.NotNull(second.TryCreatePreview(ceria, 1, 1, 1, out _));

        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void InvalidInputIsNotCached()
    {
        var localService = new StructurePreviewService();
        var ceria = CreateCeria();
        var invalid = ceria with
        {
            Cell = ceria.Cell with { A = new LengthAngstrom(0.0) },
        };

        Assert.Null(localService.TryCreatePreview(invalid, 1, 1, 1, out var firstDiagnostics));
        Assert.Null(localService.TryCreatePreview(invalid, 1, 1, 1, out var secondDiagnostics));

        Assert.Contains(firstDiagnostics, static diagnostic => diagnostic.Code == "STR002");
        Assert.Contains(secondDiagnostics, static diagnostic => diagnostic.Code == "STR002");
        Assert.Equal(0, localService.Cache.Count);
    }

    [Fact]
    public void NonFiniteCoordinateReturnsDiagnosticsAndDoesNotThrow()
    {
        var ceria = CreateCeria();
        var structure = ceria with
        {
            Sites = [.. ceria.Sites, CreateSite("Bad", new Fractional3(double.NaN, 0.0, 0.0))],
        };

        var preview = service.TryCreatePreview(structure, 1, 1, 1, out var diagnostics);

        Assert.Null(preview);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "SYM001");
    }

    [Fact]
    public void IndexCapReturnsStr014AndNoPreview()
    {
        var preview = service.TryCreatePreview(CreateCeria(), 10000, 10000, 10000, out var diagnostics);

        Assert.Null(preview);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "STR014");
    }

    [Fact]
    public void NonPositiveCellCountsAreProgrammerErrors()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.TryCreatePreview(CreateCeria(), 0, 1, 1, out _));
    }

    [Fact]
    public void TryResolveAcceptsValidSettingAndRejectsInvalidReferences()
    {
        var valid = service.TryResolve(
            new SpaceGroupRef(225, AxisSetting.None, OriginChoice.None),
            out var validDiagnostics);
        var outsideRange = service.TryResolve(
            new SpaceGroupRef(999, AxisSetting.None, OriginChoice.None),
            out var rangeDiagnostics);
        var reservedAxis = service.TryResolve(
            new SpaceGroupRef(3, AxisSetting.UniqueAxisA, OriginChoice.None),
            out var axisDiagnostics);

        Assert.NotNull(valid);
        Assert.Equal(225, valid.Number);
        Assert.Empty(validDiagnostics);
        Assert.Null(outsideRange);
        Assert.Contains(rangeDiagnostics, static diagnostic => diagnostic.Code == "STR001");
        Assert.Null(reservedAxis);
        Assert.Contains(axisDiagnostics, static diagnostic => diagnostic.Code == "STR001");
        Assert.Contains(
            axisDiagnostics,
            static diagnostic => diagnostic.Hint is not null && diagnostic.Hint.Contains("uniqueAxisB", StringComparison.Ordinal));
    }

    [Fact]
    public void StructuredEncoderReportsOverlapWithCellSitePaths()
    {
        var setting = Mocassin.Symmetry.SpaceGroups.SpaceGroupCatalog.Default.GetSetting(
            1,
            AxisSetting.None,
            OriginChoice.None);
        Assert.NotNull(setting);
        var firstId = CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e91"));
        var secondId = CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e92"));
        StructureSiteReference[] references =
        [
            new(firstId, new Fractional3(0.25, 0.25, 0.25)),
            new(secondId, new Fractional3(0.25, 0.25, 0.25)),
        ];

        var created = Mocassin.Symmetry.Geometry.UnitCellVectorEncoder.TryCreate(
            setting,
            references,
            out var encoder,
            out var overlaps,
            out var diagnostics);

        Assert.False(created);
        Assert.Null(encoder);
        var overlap = Assert.Single(overlaps);
        Assert.Equal(firstId, overlap.FirstSiteId);
        Assert.Equal(secondId, overlap.SecondSiteId);
        var diagnostic = Assert.Single(diagnostics.Where(static item => item.Code == "STR007"));
        Assert.Equal("cellSite", diagnostic.Path?.Domain);
        Assert.Equal(secondId.Value, diagnostic.Path?.ObjectId);
        Assert.Contains(diagnostic.Related, path => path.ObjectId == firstId.Value);
    }

    private static StructureModel CreateCeria() =>
        StructureModel.Default with
        {
            Name = "Ceria",
            SpaceGroup = new SpaceGroupRef(225, AxisSetting.None, OriginChoice.None),
            Cell = CubicCell(5.41),
            Sites =
            [
                CreateSite("Ce", Fractional3.Zero),
                CreateSite("O", new Fractional3(0.25, 0.25, 0.25)),
                CreateSite("O transition", new Fractional3(0.5, 0.25, 0.25), SiteStability.Unstable),
            ],
        };

    private static StructureModel CreateBariumZirconate() =>
        StructureModel.Default with
        {
            Name = "Barium zirconate",
            SpaceGroup = new SpaceGroupRef(221, AxisSetting.None, OriginChoice.None),
            Cell = CubicCell(4.19),
            Sites =
            [
                CreateSite("Ba", Fractional3.Zero),
                CreateSite("Zr", new Fractional3(0.5, 0.5, 0.5)),
                CreateSite("O", new Fractional3(0.5, 0.5, 0.0)),
            ],
        };

    private static CellParameters CubicCell(double length) =>
        new(
            new LengthAngstrom(length),
            new LengthAngstrom(length),
            new LengthAngstrom(length),
            new AngleDegrees(90.0),
            new AngleDegrees(90.0),
            new AngleDegrees(90.0));

    private static CellSite CreateSite(
        string name,
        Fractional3 position,
        SiteStability stability = SiteStability.Stable) =>
        new(CellSiteId.New(), name, position, ParticleSetId.New(), stability);
}
