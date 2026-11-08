// SPDX-License-Identifier: MIT

using BenchmarkDotNet.Attributes;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using Mocassin.Symmetry.Structure;

namespace Mocassin.Benchmarks.Structure;

/// <summary>
/// Structure-preview benchmarks for the ceria and barium-zirconate reference structures.
/// </summary>
/// <remarks>
/// Each invocation creates a fresh preview service so the measurement covers the cache-miss path rather than a
/// warm cache lookup.
/// </remarks>
[MemoryDiagnoser]
public class StructurePreviewBenchmarks
{
    private static readonly int[] ceriaOrbitSizes = [4, 8, 24];
    private static readonly int[] bariumZirconateOrbitSizes = [1, 1, 3, 12];

    private StructureModel ceria = null!;
    private StructureModel bariumZirconate = null!;

    /// <summary>
    /// Builds and validates the benchmark fixtures.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        ceria = CreateCeria();
        bariumZirconate = CreateBariumZirconate();

        ValidateFixture(ceria, "ceria", expectedPositions: 36, expectedOrbits: ceriaOrbitSizes);
        ValidateFixture(
            bariumZirconate,
            "barium zirconate",
            expectedPositions: 17,
            expectedOrbits: bariumZirconateOrbitSizes);
    }

    /// <summary>
    /// Builds the ceria preview.
    /// </summary>
    /// <returns>A checksum that prevents elimination.</returns>
    [Benchmark]
    public int StructurePreviewCeria() => CreatePreviewChecksum(ceria);

    /// <summary>
    /// Builds the barium-zirconate preview.
    /// </summary>
    /// <returns>A checksum that prevents elimination.</returns>
    [Benchmark]
    public int StructurePreviewBariumZirconate() => CreatePreviewChecksum(bariumZirconate);

    private static int CreatePreviewChecksum(StructureModel structure)
    {
        var service = new StructurePreviewService();
        var preview = service.TryCreatePreview(structure, 1, 1, 1, out var diagnostics);
        if (preview is null)
        {
            throw new InvalidOperationException(
                $"The benchmark preview was rejected: {string.Join("; ", diagnostics.Select(static diagnostic => diagnostic.ToString()))}");
        }

        return preview.PositionList.Length;
    }

    private static void ValidateFixture(
        StructureModel structure,
        string name,
        int expectedPositions,
        int[] expectedOrbits)
    {
        var service = new StructurePreviewService();
        var preview = service.TryCreatePreview(structure, 1, 1, 1, out var diagnostics);
        if (preview is null)
        {
            throw new InvalidOperationException(
                $"The {name} fixture is invalid: {string.Join("; ", diagnostics.Select(static diagnostic => diagnostic.ToString()))}");
        }

        if (preview.PositionList.Length != expectedPositions)
        {
            throw new InvalidOperationException(
                $"The {name} fixture expands to {preview.PositionList.Length} P1 positions, expected {expectedPositions}.");
        }

        var orbits = preview.Orbits.Select(static orbit => orbit.Multiplicity).ToArray();
        if (!orbits.SequenceEqual(expectedOrbits))
        {
            throw new InvalidOperationException(
                $"The {name} fixture has orbit sizes [{string.Join(", ", orbits)}], expected [{string.Join(", ", expectedOrbits)}].");
        }
    }

    private static StructureModel CreateCeria() =>
        StructureModel.Default with
        {
            Name = "Ceria",
            SpaceGroup = new SpaceGroupRef(225, AxisSetting.None, OriginChoice.None),
            Cell = CubicCell(5.41),
            Sites =
            [
                CreateSite("Ce", Fractional3.Zero, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5ea0"),
                CreateSite("O", new Fractional3(0.25, 0.25, 0.25), "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5ea1"),
                CreateSite(
                    "O transition",
                    new Fractional3(0.5, 0.25, 0.25),
                    "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5ea2",
                    SiteStability.Unstable),
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
                CreateSite("Ba", Fractional3.Zero, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5ea3"),
                CreateSite("Zr", new Fractional3(0.5, 0.5, 0.5), "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5ea4"),
                CreateSite("O", new Fractional3(0.5, 0.5, 0.0), "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5ea5"),
                CreateSite("H interstitial", new Fractional3(0.734, 0.5, 0.0), "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5ea6"),
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
        string id,
        SiteStability stability = SiteStability.Stable) =>
        new(
            CellSiteId.Create(Guid.Parse(id)),
            name,
            position,
            ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5eaf")),
            stability);
}
