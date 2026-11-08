// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Core.Symmetry;

namespace Mocassin.Symmetry.Tests;

[Trait("Tier", "PR-fast")]
public sealed class UnitCellVectorEncoderTests
{
    private static readonly SpaceGroupSetting p1 = Setting(1, AxisSetting.None, OriginChoice.None);
    private static readonly SpaceGroupSetting p2B = Setting(3, AxisSetting.UniqueAxisB, OriginChoice.None);
    private static readonly SpaceGroupSetting pmmm = Setting(47, AxisSetting.None, OriginChoice.None);
    private static readonly SpaceGroupSetting fm3m = Setting(225, AxisSetting.None, OriginChoice.None);
    private static readonly SpaceGroupSetting r3mHexagonal = Setting(166, AxisSetting.Hexagonal, OriginChoice.None);

    [Fact]
    public void OrbitExpansionMatchesKnownMultiplicities()
    {
        Assert.Single(SpaceGroupOperations.ExpandOrbit(p1, new Fractional3(0.1, 0.2, 0.3)));
        Assert.Equal(2, SpaceGroupOperations.ExpandOrbit(p2B, new Fractional3(0.1, 0.2, 0.3)).Length);
        Assert.Equal(8, SpaceGroupOperations.ExpandOrbit(pmmm, new Fractional3(0.1, 0.2, 0.3)).Length);
        Assert.Equal(4, SpaceGroupOperations.ExpandOrbit(fm3m, new Fractional3(0.0, 0.0, 0.0)).Length);
        Assert.Equal(36, SpaceGroupOperations.ExpandOrbit(r3mHexagonal, new Fractional3(0.11, 0.23, 0.37)).Length);
    }

    [Fact]
    public void OrbitExpansionTrimsAndSortsCanonically()
    {
        var orbit = SpaceGroupOperations.ExpandOrbit(p2B, new Fractional3(1.1, 0.2, -0.7));

        Assert.Equal(
            new[] { new Fractional3(0.1, 0.2, 0.3), new Fractional3(0.9, 0.2, 0.7) },
            orbit.ToArray());
    }

    [Fact]
    public void OrbitExpansionRejectsNonFiniteSites()
    {
        Assert.Throws<ArgumentException>(
            () => SpaceGroupOperations.ExpandOrbit(p1, new Fractional3(double.NaN, 0.0, 0.0)));
    }

    [Fact]
    public void CanonicalOrderIsIndependentOfInputAndOperationOrder()
    {
        var references = new[]
        {
            new Fractional3(0.1, 0.2, 0.3),
            new Fractional3(0.25, 0.5, 0.75),
        };
        Assert.True(UnitCellVectorEncoder.TryCreate(p2B, references, out var forward, out _));
        Assert.True(UnitCellVectorEncoder.TryCreate(p2B, references.Reverse(), out var reversed, out _));

        var reorderedSetting = p2B with { Operations = p2B.Operations.Reverse().ToImmutableArray() };
        Assert.True(UnitCellVectorEncoder.TryCreate(reorderedSetting, references, out var reordered, out _));

        Assert.NotNull(forward);
        Assert.NotNull(reversed);
        Assert.NotNull(reordered);
        Assert.Equal(forward.Positions.ToArray(), reversed.Positions.ToArray());
        Assert.Equal(forward.Positions.ToArray(), reordered.Positions.ToArray());
        Assert.Equal(0, forward.Encode(new Fractional3(0.1, 0.2, 0.3)).P);
        Assert.Equal(1, forward.Encode(new Fractional3(0.25, 0.5, 0.75)).P);
        Assert.Equal(2, forward.Encode(new Fractional3(0.75, 0.5, 0.25)).P);
        Assert.Equal(3, forward.Encode(new Fractional3(0.9, 0.2, 0.7)).P);
    }

    [Fact]
    public void EncoderFreezesTheExpectedGoldenPIndices()
    {
        var references = new[]
        {
            new Fractional3(0.1, 0.2, 0.3),
            new Fractional3(0.25, 0.5, 0.75),
        };

        Assert.True(UnitCellVectorEncoder.TryCreate(p2B, references, out var encoder, out var diagnostics));
        Assert.NotNull(encoder);
        Assert.Empty(diagnostics);
        Assert.Equal(4, encoder.PositionCount);
        Assert.Equal(new Vector4I(0, 0, 0, 0), encoder.ExpandedSites[0].Encoding);
        Assert.Equal(new Vector4I(0, 0, 0, 3), encoder.ExpandedSites[3].Encoding);
        Assert.Equal(0, encoder.ExpandedSites[0].ReferenceSiteIndex);
        Assert.Equal(0, encoder.ExpandedSites[0].OrbitMemberIndex);
        Assert.Equal(1, encoder.ExpandedSites[1].ReferenceSiteIndex);
        Assert.Equal(0, encoder.ExpandedSites[1].OrbitMemberIndex);
        Assert.Equal(0, encoder.ExpandedSites[3].ReferenceSiteIndex);
        Assert.Equal(1, encoder.ExpandedSites[3].OrbitMemberIndex);

        Assert.Equal(2, encoder.Orbits.Length);
        Assert.Equal(2, encoder.Orbits[0].Multiplicity);
        Assert.Equal(2, encoder.Orbits[1].Multiplicity);
        Assert.Equal([0, 3], encoder.Orbits[0].PositionIndices.ToArray());
        Assert.Equal([1, 2], encoder.Orbits[1].PositionIndices.ToArray());
    }

    [Fact]
    public void DuplicateReferenceOrbitsAreRejectedAsSiteOverlap()
    {
        var references = new[]
        {
            new Fractional3(0.1, 0.2, 0.3),
            new Fractional3(0.9, 0.2, 0.7),
        };

        Assert.False(UnitCellVectorEncoder.TryCreate(p2B, references, out var encoder, out var diagnostics));
        Assert.Null(encoder);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "STR007");
    }

    [Fact]
    public void NearBoundaryReferencesAreTrimmedBeforeOverlapDetection()
    {
        var references = new[]
        {
            new Fractional3(0.0, 0.0, 0.0),
            new Fractional3(0.99999999999, 0.0, 0.0),
        };

        Assert.False(UnitCellVectorEncoder.TryCreate(p1, references, out _, out var diagnostics));
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "STR007");
    }

    [Fact]
    public void DistinctGridPositionsStayDistinct()
    {
        var references = new[]
        {
            new Fractional3(0.1, 0.0, 0.0),
            new Fractional3(0.10000000001, 0.0, 0.0),
        };

        Assert.True(UnitCellVectorEncoder.TryCreate(p1, references, out var encoder, out _));
        Assert.NotNull(encoder);
        Assert.Equal(2, encoder.PositionCount);
    }

    [Fact]
    public void NonFiniteReferenceIsRejected()
    {
        var references = new[] { new Fractional3(double.PositiveInfinity, 0.0, 0.0) };

        Assert.False(UnitCellVectorEncoder.TryCreate(p1, references, out _, out var diagnostics));
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "SYM001");
    }

    [Fact]
    public void EncoderMapsBaseAndNeighborImages()
    {
        Assert.True(UnitCellVectorEncoder.TryCreate(p2B, [new Fractional3(0.1, 0.2, 0.3)], out var encoder, out _));
        Assert.NotNull(encoder);

        Assert.Equal(0, encoder.Encode(new Fractional3(0.1, 0.2, 0.3)).P);
        Assert.Equal(1, encoder.Encode(new Fractional3(0.9, 0.2, 0.7)).P);
        Assert.Equal(new Fractional3(1.1, 4.2, -2.7), encoder.Decode(new Vector4I(1, 4, -3, 0)));
        Assert.Equal(new Vector4I(1, 0, 2, 0), encoder.ToSupercellOffset(new Vector4I(-1, 0, -2, 0), 2, 3, 4));
        Assert.Throws<ArgumentException>(() => encoder.Encode(new Fractional3(0.5, 0.5, 0.5)));
    }

    [Fact]
    public void LinearIndexRoundTripsAcrossASmallSupercell()
    {
        Assert.True(UnitCellVectorEncoder.TryCreate(
            p2B,
            [new Fractional3(0.1, 0.2, 0.3), new Fractional3(0.25, 0.5, 0.75)],
            out var encoder,
            out _));
        Assert.NotNull(encoder);
        Assert.Equal(4, encoder.PositionCount);

        Assert.Equal(94, encoder.GetLinearIndex(new Vector4I(1, 2, 3, 2), 2, 3, 4));
        for (var index = 0; index < 2 * 3 * 4 * encoder.PositionCount; index++)
        {
            var encoding = encoder.DecodeLinearIndex(index, 2, 3, 4);
            Assert.Equal(index, encoder.GetLinearIndex(encoding, 2, 3, 4));
        }
    }

    [Fact]
    public void LinearIndexRespectsTheSigned32BitCap()
    {
        Assert.Equal(int.MaxValue, UnitCellVectorEncoder.GetLatticePositionCount(1, 1, int.MaxValue, 1));
        Assert.True(UnitCellVectorEncoder.IsLatticeSizeSupported(1, 1, int.MaxValue, 1));
        Assert.False(UnitCellVectorEncoder.IsLatticeSizeSupported(1, 1, int.MaxValue, 2));
        Assert.Equal(
            long.MaxValue,
            UnitCellVectorEncoder.GetLatticePositionCount(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue));

        Assert.True(UnitCellVectorEncoder.TryCreate(p2B, [new Fractional3(0.1, 0.2, 0.3)], out var encoder, out _));
        Assert.NotNull(encoder);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => encoder.GetLinearIndex(new Vector4I(0, 0, 0, 0), 1, 1, int.MaxValue));
    }

    [Fact]
    public void OutOfRangeIndicesAndOffsetsAreRejected()
    {
        Assert.True(UnitCellVectorEncoder.TryCreate(p2B, [new Fractional3(0.1, 0.2, 0.3)], out var encoder, out _));
        Assert.NotNull(encoder);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => encoder.GetLinearIndex(new Vector4I(2, 0, 0, 0), 2, 3, 4));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => encoder.GetLinearIndex(new Vector4I(0, 0, 0, 4), 2, 3, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => encoder.DecodeLinearIndex(-1, 2, 3, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => encoder.DecodeLinearIndex(96, 2, 3, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => encoder.GetPosition(4));
    }

    private static SpaceGroupSetting Setting(int number, AxisSetting axis, OriginChoice origin) =>
        SpaceGroupCatalog.Default.GetSetting(number, axis, origin)
        ?? throw new InvalidOperationException($"Missing setting ({number}, {axis}, {origin}).");
}
