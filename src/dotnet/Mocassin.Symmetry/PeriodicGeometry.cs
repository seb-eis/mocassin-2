// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Geometry;

namespace Mocassin.Symmetry;

/// <summary>
/// Periodic minimum-image, radial-search, and box-query operations over a cell and its canonical P1 positions.
/// </summary>
/// <remarks>
/// Every result carries an explicit image offset; no API wraps or reinterprets an offset implicitly. The
/// search boxes use interplanar spacings <c>d_a = V/|b×c|</c>, <c>d_b = V/|c×a|</c>, and
/// <c>d_c = V/|a×b|</c>, plus one extra cell for the center's position and cutoff-boundary candidates.
/// </remarks>
public sealed class PeriodicGeometry
{
    /// <summary>
    /// Initializes periodic geometry over a validated cell and encoder.
    /// </summary>
    /// <param name="cell">The validated cell geometry.</param>
    /// <param name="encoder">The canonical P1 encoder.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public PeriodicGeometry(CellGeometry cell, UnitCellVectorEncoder encoder)
    {
        ArgumentNullException.ThrowIfNull(cell);
        ArgumentNullException.ThrowIfNull(encoder);
        Cell = cell;
        Encoder = encoder;
    }

    /// <summary>
    /// Gets the validated cell geometry.
    /// </summary>
    public CellGeometry Cell { get; }

    /// <summary>
    /// Gets the canonical P1 encoder.
    /// </summary>
    public UnitCellVectorEncoder Encoder { get; }

    /// <summary>
    /// Gets exactly one shortest periodic image of a target position.
    /// </summary>
    /// <param name="center">The center encoding.</param>
    /// <param name="target">The target encoding.</param>
    /// <returns>The shortest image, with its relative offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A P index is invalid.</exception>
    public MinimumImageResult GetMinimumImage(Vector4I center, Vector4I target)
    {
        var centerPosition = Encoder.GetPosition(center.P);
        var targetPosition = Encoder.GetPosition(target.P);
        var coveringRadius = 0.5 * (Cell.A.Length + Cell.B.Length + Cell.C.Length);
        GetBoxSize(coveringRadius, out var maxA, out var maxB, out var maxC);

        var deltaA = checked(target.A - center.A);
        var deltaB = checked(target.B - center.B);
        var deltaC = checked(target.C - center.C);

        MinimumImageResult best = default;
        var hasBest = false;

        for (var a = -maxA; a <= maxA; a++)
        {
            for (var b = -maxB; b <= maxB; b++)
            {
                for (var c = -maxC; c <= maxC; c++)
                {
                    var offsetA = checked(deltaA + a);
                    var offsetB = checked(deltaB + b);
                    var offsetC = checked(deltaC + c);
                    var distance = Distance(centerPosition, targetPosition, offsetA, offsetB, offsetC);
                    var bucket = ToBucket(distance);
                    var candidate = new MinimumImageResult(
                        new Vector4I(offsetA, offsetB, offsetC, target.P),
                        distance,
                        bucket);

                    if (!hasBest || IsBefore(candidate, best))
                    {
                        best = candidate;
                        hasBest = true;
                    }
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Searches every periodic image within a cutoff, excluding the zero-offset self image.
    /// </summary>
    /// <param name="center">The center encoding; results are relative to its cell.</param>
    /// <param name="cutoffAngstrom">The cutoff radius in ångström.</param>
    /// <returns>Neighbor images sorted by <c>(bucket, P, cellA, cellB, cellC)</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The cutoff is negative or non-finite, or the center P index is invalid.
    /// </exception>
    public ImmutableArray<NeighborImage> RadialSearch(Vector4I center, double cutoffAngstrom)
    {
        if (!double.IsFinite(cutoffAngstrom) || cutoffAngstrom < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(cutoffAngstrom));
        }

        var centerPosition = Encoder.GetPosition(center.P);
        var cutoffBucket = ToBucket(cutoffAngstrom);
        GetBoxSize(cutoffAngstrom + GeometryTolerances.PairDistanceBucket, out var maxA, out var maxB, out var maxC);
        var results = new List<NeighborImage>();

        for (var p = 0; p < Encoder.PositionCount; p++)
        {
            var targetPosition = Encoder.Positions[p];
            for (var a = -maxA; a <= maxA; a++)
            {
                for (var b = -maxB; b <= maxB; b++)
                {
                    for (var c = -maxC; c <= maxC; c++)
                    {
                        if (a == 0 && b == 0 && c == 0 && p == center.P)
                        {
                            continue;
                        }

                        var distance = Distance(centerPosition, targetPosition, a, b, c);
                        var bucket = ToBucket(distance);
                        if (bucket <= cutoffBucket)
                        {
                            results.Add(new NeighborImage(
                                new Vector4I(a, b, c, p),
                                distance,
                                bucket));
                        }
                    }
                }
            }
        }

        results.Sort(static (left, right) => Compare(left.Offset, right.Offset, left.DistanceBucketFm, right.DistanceBucketFm));
        return results.ToImmutableArray();
    }

    /// <summary>
    /// Returns every periodic image whose Cartesian point lies in a half-open box.
    /// </summary>
    /// <param name="min">The inclusive lower corner.</param>
    /// <param name="max">The exclusive upper corner.</param>
    /// <returns>
    /// Image encodings sorted by <c>(P, cellA, cellB, cellC)</c>. Periodic duplicates with different offsets
    /// remain distinct.
    /// </returns>
    /// <exception cref="ArgumentException">A corner is not finite or has no positive extent.</exception>
    public ImmutableArray<Vector4I> GetPositionsInBox(Cartesian3 min, Cartesian3 max)
    {
        if (!min.IsFinite || !max.IsFinite)
        {
            throw new ArgumentException("Box corners must be finite.");
        }

        if (min.X >= max.X || min.Y >= max.Y || min.Z >= max.Z)
        {
            return ImmutableArray<Vector4I>.Empty;
        }

        var fractionalMinimum = new Fractional3(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
        var fractionalMaximum = new Fractional3(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);
        foreach (var x in new[] { min.X, max.X })
        {
            foreach (var y in new[] { min.Y, max.Y })
            {
                foreach (var z in new[] { min.Z, max.Z })
                {
                    var fractional = Cell.ToFractional(new Cartesian3(x, y, z));
                    fractionalMinimum = new Fractional3(
                        Math.Min(fractionalMinimum.A, fractional.A),
                        Math.Min(fractionalMinimum.B, fractional.B),
                        Math.Min(fractionalMinimum.C, fractional.C));
                    fractionalMaximum = new Fractional3(
                        Math.Max(fractionalMaximum.A, fractional.A),
                        Math.Max(fractionalMaximum.B, fractional.B),
                        Math.Max(fractionalMaximum.C, fractional.C));
                }
            }
        }

        var minA = (int)Math.Floor(fractionalMinimum.A) - 1;
        var maxA = (int)Math.Ceiling(fractionalMaximum.A) + 1;
        var minB = (int)Math.Floor(fractionalMinimum.B) - 1;
        var maxB = (int)Math.Ceiling(fractionalMaximum.B) + 1;
        var minC = (int)Math.Floor(fractionalMinimum.C) - 1;
        var maxC = (int)Math.Ceiling(fractionalMaximum.C) + 1;

        var results = new List<Vector4I>();
        for (var p = 0; p < Encoder.PositionCount; p++)
        {
            var position = Encoder.Positions[p];
            for (var a = minA; a <= maxA; a++)
            {
                for (var b = minB; b <= maxB; b++)
                {
                    for (var c = minC; c <= maxC; c++)
                    {
                        var cartesian = Cell.ToCartesian(
                            new Fractional3(position.A + a, position.B + b, position.C + c));
                        if (cartesian.X >= min.X && cartesian.X < max.X
                            && cartesian.Y >= min.Y && cartesian.Y < max.Y
                            && cartesian.Z >= min.Z && cartesian.Z < max.Z)
                        {
                            results.Add(new Vector4I(a, b, c, p));
                        }
                    }
                }
            }
        }

        results.Sort(static (left, right) =>
        {
            var comparison = left.P.CompareTo(right.P);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.A.CompareTo(right.A);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.B.CompareTo(right.B);
            return comparison != 0 ? comparison : left.C.CompareTo(right.C);
        });

        return results.ToImmutableArray();
    }

    private double Distance(
        Fractional3 center,
        Fractional3 target,
        int offsetA,
        int offsetB,
        int offsetC) =>
        Cell.GetDistance(
            center,
            new Fractional3(target.A + offsetA, target.B + offsetB, target.C + offsetC));

    private void GetBoxSize(double radius, out int maxA, out int maxB, out int maxC)
    {
        if (!double.IsFinite(radius) || radius < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }

        var volume = Cell.VolumeAngstrom3;
        var spacingA = volume / Cell.B.Cross(Cell.C).Length;
        var spacingB = volume / Cell.C.Cross(Cell.A).Length;
        var spacingC = volume / Cell.A.Cross(Cell.B).Length;

        maxA = Bound(radius, spacingA);
        maxB = Bound(radius, spacingB);
        maxC = Bound(radius, spacingC);
    }

    private static int Bound(double radius, double spacing)
    {
        var value = Math.Ceiling(radius / spacing) + 1.0;
        if (!double.IsFinite(value) || value > int.MaxValue / 2.0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "The search radius is too large for the cell.");
        }

        return (int)value;
    }

    private static bool IsBefore(MinimumImageResult candidate, MinimumImageResult current)
    {
        if (candidate.DistanceBucketFm != current.DistanceBucketFm)
        {
            return candidate.DistanceBucketFm < current.DistanceBucketFm;
        }

        var comparison = candidate.Offset.A.CompareTo(current.Offset.A);
        if (comparison != 0)
        {
            return comparison < 0;
        }

        comparison = candidate.Offset.B.CompareTo(current.Offset.B);
        if (comparison != 0)
        {
            return comparison < 0;
        }

        comparison = candidate.Offset.C.CompareTo(current.Offset.C);
        return comparison != 0 ? comparison < 0 : candidate.Offset.P < current.Offset.P;
    }

    private static int Compare(
        Vector4I left,
        Vector4I right,
        int leftBucket,
        int rightBucket)
    {
        var comparison = leftBucket.CompareTo(rightBucket);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.P.CompareTo(right.P);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.A.CompareTo(right.A);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.B.CompareTo(right.B);
        return comparison != 0 ? comparison : left.C.CompareTo(right.C);
    }

    /// <summary>
    /// Converts a distance to the fixed 1 fm bucket.
    /// </summary>
    /// <param name="distance">The distance in ångström.</param>
    /// <returns>The rounded femtometre bucket.</returns>
    internal static int ToBucket(double distance) =>
        checked((int)Math.Round(
            distance / GeometryTolerances.PairDistanceBucket,
            MidpointRounding.AwayFromZero));
}
