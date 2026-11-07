// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;

namespace Mocassin.Symmetry.Tests;

public sealed class PeriodicGeometryTests
{
    [Fact]
    public void MinimumImageMatchesBruteForceForASkewedCell()
    {
        var geometry = CreateTriclinic();
        var center = new Vector4I(0, 0, 0, 0);
        var target = new Vector4I(1, -2, 1, 1);

        var actual = geometry.GetMinimumImage(center, target);
        var expected = BruteForceMinimumImage(geometry, center, target, -8, 8);

        Assert.Equal(expected.Offset, actual.Offset);
        Assert.Equal(expected.DistanceBucketFm, actual.DistanceBucketFm);
        Assert.Equal(expected.DistanceAngstrom, actual.DistanceAngstrom, 12);
    }

    [Fact]
    public void MinimumImageUsesTheDocumentedTieBreak()
    {
        var geometry = CreateCubicPair();
        var center = new Vector4I(0, 0, 0, 0);
        var target = new Vector4I(0, 0, 0, 1);

        var actual = geometry.GetMinimumImage(center, target);

        Assert.Equal(new Vector4I(-1, 0, 0, 1), actual.Offset);
    }

    [Fact]
    public void RadialSearchMatchesBruteForceAndExcludesSelf()
    {
        var geometry = CreateTriclinic();
        var center = new Vector4I(0, 0, 0, 1);
        const double cutoff = 4.0;

        var actual = geometry.RadialSearch(center, cutoff);
        var expected = BruteForceRadialSearch(geometry, center, cutoff, -8, 8);

        Assert.Equal(expected.Length, actual.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index].Offset, actual[index].Offset);
            Assert.Equal(expected[index].DistanceBucketFm, actual[index].DistanceBucketFm);
            Assert.Equal(expected[index].DistanceAngstrom, actual[index].DistanceAngstrom, 12);
        }

        Assert.DoesNotContain(
            actual,
            image => image.Offset == new Vector4I(0, 0, 0, center.P));
    }

    [Fact]
    public void RadialSearchReturnsMultipleImagesOfTheSamePosition()
    {
        var geometry = CreateCubic();
        var center = new Vector4I(0, 0, 0, 0);

        var actual = geometry.RadialSearch(center, 6.0);

        Assert.True(actual.Count(image => image.Offset.P == 0) > 1);
    }

    [Fact]
    public void DistanceBucketsUseSymmetricFemtometreRounding()
    {
        Assert.Equal(0, PeriodicGeometry.ToBucket(0.0));
        Assert.Equal(1, PeriodicGeometry.ToBucket(1e-5));
        Assert.Equal(2, PeriodicGeometry.ToBucket(1.5e-5));
        Assert.Equal(2, PeriodicGeometry.ToBucket(2.4e-5));
    }

    [Fact]
    public void BoxQueriesAreHalfOpenAndKeepDistinctImages()
    {
        var geometry = CreateCubic();
        var onlyOrigin = geometry.GetPositionsInBox(new Cartesian3(0, 0, 0), new Cartesian3(5, 5, 5));

        Assert.Equal(new[] { new Vector4I(0, 0, 0, 0) }, onlyOrigin.ToArray());

        var leftImage = geometry.GetPositionsInBox(new Cartesian3(-5, 0, 0), new Cartesian3(0, 5, 5));
        Assert.Contains(new Vector4I(-1, 0, 0, 0), leftImage);

        var multiple = geometry.GetPositionsInBox(new Cartesian3(-5.1, -5.1, -5.1), new Cartesian3(5.1, 5.1, 5.1));
        Assert.Contains(new Vector4I(0, 0, 0, 0), multiple);
        Assert.Contains(new Vector4I(1, 0, 0, 0), multiple);
        Assert.Contains(new Vector4I(-1, 0, 0, 0), multiple);
    }

    [Fact]
    public void BoxQueriesReturnEmptyForEmptyOrInvertedBoxes()
    {
        var geometry = CreateCubic();

        Assert.Empty(geometry.GetPositionsInBox(new Cartesian3(1, 1, 1), new Cartesian3(1, 1, 1)));
        Assert.Empty(geometry.GetPositionsInBox(new Cartesian3(2, 0, 0), new Cartesian3(1, 1, 1)));
    }

    [Fact]
    public void ResultsAreDeterministic()
    {
        var geometry = CreateTriclinic();
        var first = geometry.RadialSearch(new Vector4I(0, 0, 0, 0), 4.0);
        var second = geometry.RadialSearch(new Vector4I(0, 0, 0, 0), 4.0);

        Assert.Equal(first.ToArray(), second.ToArray());
    }

    private static PeriodicGeometry CreateTriclinic()
    {
        var cell = CreateCell(5.0, 6.0, 7.0, 80.0, 85.0, 95.0);
        Assert.True(UnitCellVectorEncoder.TryCreate(
            Setting(1),
            [new Fractional3(0.0, 0.0, 0.0), new Fractional3(0.25, 0.25, 0.25)],
            out var encoder,
            out _));
        Assert.NotNull(encoder);
        return new PeriodicGeometry(cell, encoder);
    }

    private static PeriodicGeometry CreateCubic()
    {
        var cell = CreateCell(5.0, 5.0, 5.0, 90.0, 90.0, 90.0);
        Assert.True(UnitCellVectorEncoder.TryCreate(
            Setting(1),
            [new Fractional3(0.0, 0.0, 0.0)],
            out var encoder,
            out _));
        Assert.NotNull(encoder);
        return new PeriodicGeometry(cell, encoder);
    }

    private static PeriodicGeometry CreateCubicPair()
    {
        var cell = CreateCell(5.0, 5.0, 5.0, 90.0, 90.0, 90.0);
        Assert.True(UnitCellVectorEncoder.TryCreate(
            Setting(1),
            [new Fractional3(0.0, 0.0, 0.0), new Fractional3(0.5, 0.0, 0.0)],
            out var encoder,
            out _));
        Assert.NotNull(encoder);
        return new PeriodicGeometry(cell, encoder);
    }

    private static CellGeometry CreateCell(
        double a,
        double b,
        double c,
        double alpha,
        double beta,
        double gamma)
    {
        var parameters = new CellParameters(
            new LengthAngstrom(a),
            new LengthAngstrom(b),
            new LengthAngstrom(c),
            new AngleDegrees(alpha),
            new AngleDegrees(beta),
            new AngleDegrees(gamma));
        Assert.True(CellGeometry.TryCreate(parameters, out var cell, out _));
        Assert.NotNull(cell);
        return cell;
    }

    private static SpaceGroupSetting Setting(int number) =>
        SpaceGroupCatalog.Default.GetSetting(number, AxisSetting.None, OriginChoice.None)
        ?? throw new InvalidOperationException($"Missing setting {number}.");

    private static MinimumImageResult BruteForceMinimumImage(
        PeriodicGeometry geometry,
        Vector4I center,
        Vector4I target,
        int min,
        int max)
    {
        var centerPosition = geometry.Encoder.GetPosition(center.P);
        var targetPosition = geometry.Encoder.GetPosition(target.P);
        MinimumImageResult best = default;
        var hasBest = false;

        for (var a = min; a <= max; a++)
        {
            for (var b = min; b <= max; b++)
            {
                for (var c = min; c <= max; c++)
                {
                    var offset = new Vector4I(
                        target.A - center.A + a,
                        target.B - center.B + b,
                        target.C - center.C + c,
                        target.P);
                    var distance = geometry.Cell.GetDistance(
                        centerPosition,
                        new Fractional3(targetPosition.A + offset.A, targetPosition.B + offset.B, targetPosition.C + offset.C));
                    var candidate = new MinimumImageResult(offset, distance, PeriodicGeometry.ToBucket(distance));
                    if (!hasBest || candidate.DistanceBucketFm < best.DistanceBucketFm
                        || (candidate.DistanceBucketFm == best.DistanceBucketFm && IsSmallerOffset(candidate.Offset, best.Offset)))
                    {
                        best = candidate;
                        hasBest = true;
                    }
                }
            }
        }

        return best;
    }

    private static ImmutableArray<NeighborImage> BruteForceRadialSearch(
        PeriodicGeometry geometry,
        Vector4I center,
        double cutoff,
        int min,
        int max)
    {
        var centerPosition = geometry.Encoder.GetPosition(center.P);
        var cutoffBucket = PeriodicGeometry.ToBucket(cutoff);
        var results = new List<NeighborImage>();

        for (var p = 0; p < geometry.Encoder.PositionCount; p++)
        {
            var targetPosition = geometry.Encoder.Positions[p];
            for (var a = min; a <= max; a++)
            {
                for (var b = min; b <= max; b++)
                {
                    for (var c = min; c <= max; c++)
                    {
                        if (a == 0 && b == 0 && c == 0 && p == center.P)
                        {
                            continue;
                        }

                        var distance = geometry.Cell.GetDistance(
                            centerPosition,
                            new Fractional3(targetPosition.A + a, targetPosition.B + b, targetPosition.C + c));
                        var bucket = PeriodicGeometry.ToBucket(distance);
                        if (bucket <= cutoffBucket)
                        {
                            results.Add(new NeighborImage(new Vector4I(a, b, c, p), distance, bucket));
                        }
                    }
                }
            }
        }

        results.Sort(static (left, right) =>
        {
            var comparison = left.DistanceBucketFm.CompareTo(right.DistanceBucketFm);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.Offset.P.CompareTo(right.Offset.P);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.Offset.A.CompareTo(right.Offset.A);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.Offset.B.CompareTo(right.Offset.B);
            return comparison != 0 ? comparison : left.Offset.C.CompareTo(right.Offset.C);
        });

        return results.ToImmutableArray();
    }

    private static bool IsSmallerOffset(Vector4I candidate, Vector4I current)
    {
        var comparison = candidate.A.CompareTo(current.A);
        if (comparison != 0)
        {
            return comparison < 0;
        }

        comparison = candidate.B.CompareTo(current.B);
        return comparison != 0 ? comparison < 0 : candidate.C.CompareTo(current.C) < 0;
    }
}
