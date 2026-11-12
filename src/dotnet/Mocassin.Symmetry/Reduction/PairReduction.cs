// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Symmetry.Geometry;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Symmetry.Reduction;

/// <summary>
/// One symmetry-reduced pair shell around a center position.
/// </summary>
/// <param name="DistanceBucketFm">The shell distance bucket in femtometres.</param>
/// <param name="DistanceAngstrom">The representative distance in ångström.</param>
/// <param name="Representative">The canonical representative image encoding.</param>
/// <param name="EquivalentImages">Every equivalent image in the shell, sorted canonically.</param>
/// <param name="ChiralPartnerIndex">The linked inverse shell index, or <see langword="null"/> when not chiral.</param>
public sealed record PairShell(
    int DistanceBucketFm,
    double DistanceAngstrom,
    Vector4I Representative,
    ImmutableArray<Vector4I> EquivalentImages,
    int? ChiralPartnerIndex)
{
    /// <summary>
    /// Gets whether the inverse direction is outside the center-stabilizer orbit.
    /// </summary>
    /// <remarks>
    /// This is an orbit property, not physical chirality. It is independent of whether the inverse shell is
    /// present in the current reduction, which matters for structures where the inverse direction is not a
    /// sampled shell around the same centre.
    /// </remarks>
    public bool IsChiral { get; init; }
}

/// <summary>
/// The symmetry-reduced pair shells for one center position.
/// </summary>
/// <param name="CenterP">The center P index.</param>
/// <param name="Shells">The shells in canonical pair order.</param>
public sealed record PairReductionResult(
    int CenterP,
    ImmutableArray<PairShell> Shells);

/// <summary>
/// Exact pair-shell reduction for periodic neighbor lists.
/// </summary>
public static class PairReduction
{
    /// <summary>
    /// Reduces periodic pair images into symmetry-equivalent shells.
    /// </summary>
    /// <param name="geometry">The periodic geometry and encoder.</param>
    /// <param name="centerP">The center P index.</param>
    /// <param name="cutoffAngstrom">The pair cutoff radius in ångström.</param>
    /// <returns>The reduced shells in canonical pair order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="geometry"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The cutoff or center P index is invalid.</exception>
    public static PairReductionResult Reduce(
        PeriodicGeometry geometry,
        int centerP,
        double cutoffAngstrom)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var encoder = geometry.Encoder;
        var centerPosition = encoder.GetPosition(centerP);
        var centerGrid = SpaceGroupOperations.ToGrid(centerPosition, nameof(centerP));
        var stabilizer = SpaceGroupOperations.GetStabilizer(encoder.Setting, centerPosition);
        var images = geometry.RadialSearch(new Vector4I(0, 0, 0, centerP), cutoffAngstrom);
        var byOffset = new Dictionary<Vector4I, NeighborImage>();
        foreach (var image in images)
        {
            byOffset[image.Offset] = image;
        }

        var assigned = new HashSet<Vector4I>();
        var shells = new List<PairShell>();
        foreach (var candidate in images)
        {
            if (!assigned.Add(candidate.Offset))
            {
                continue;
            }

            var equivalent = new HashSet<Vector4I> { candidate.Offset };
            foreach (var operation in stabilizer)
            {
                if (!TryMapImage(encoder, centerGrid, operation, candidate.Offset, out var mapped))
                {
                    continue;
                }

                if (byOffset.TryGetValue(mapped, out var mappedImage)
                    && mappedImage.DistanceBucketFm == candidate.DistanceBucketFm)
                {
                    equivalent.Add(mapped);
                    assigned.Add(mapped);
                }
            }

            var equivalents = equivalent
                .OrderBy(static image => image.P)
                .ThenBy(static image => image.A)
                .ThenBy(static image => image.B)
                .ThenBy(static image => image.C)
                .ToImmutableArray();
            var displacement = Displacement(encoder, centerGrid, candidate.Offset);
            var inverse = new GridFractional3(-displacement.A, -displacement.B, -displacement.C);
            var isChiral = !IsSameOrbit(stabilizer, inverse, displacement);
            shells.Add(new PairShell(
                candidate.DistanceBucketFm,
                candidate.DistanceAngstrom,
                candidate.Offset,
                equivalents,
                null)
            {
                IsChiral = isChiral,
            });
        }

        var withPartners = shells.ToArray();
        for (var index = 0; index < shells.Count; index++)
        {
            var shell = shells[index];
            if (!shell.IsChiral)
            {
                continue;
            }

            var displacement = Displacement(encoder, centerGrid, shell.Representative);
            var inverse = new GridFractional3(-displacement.A, -displacement.B, -displacement.C);
            for (var partner = 0; partner < shells.Count; partner++)
            {
                if (partner == index
                    || shells[partner].DistanceBucketFm != shell.DistanceBucketFm)
                {
                    continue;
                }

                var partnerDisplacement = Displacement(encoder, centerGrid, shells[partner].Representative);
                if (IsSameOrbit(stabilizer, inverse, partnerDisplacement))
                {
                    withPartners[index] = shell with { ChiralPartnerIndex = partner };
                    break;
                }
            }
        }

        return new PairReductionResult(centerP, [.. withPartners]);
    }

    private static GridFractional3 Displacement(
        UnitCellVectorEncoder encoder,
        GridFractional3 center,
        Vector4I image)
    {
        var target = SpaceGroupOperations.ToGrid(encoder.Positions[image.P], nameof(image));
        return target
            + new GridFractional3(
                checked(image.A * GridFractional3.Denominator),
                checked(image.B * GridFractional3.Denominator),
                checked(image.C * GridFractional3.Denominator))
            - center;
    }

    private static bool TryMapImage(
        UnitCellVectorEncoder encoder,
        GridFractional3 center,
        SymmetryOperation operation,
        Vector4I image,
        out Vector4I mapped)
    {
        mapped = default;
        var target = SpaceGroupOperations.ToGrid(encoder.Positions[image.P], nameof(image));
        var absolute = target
            + new GridFractional3(
                checked(image.A * GridFractional3.Denominator),
                checked(image.B * GridFractional3.Denominator),
                checked(image.C * GridFractional3.Denominator));
        var centerShift = operation.Transform(center) - center;
        var mappedAbsolute = operation.Transform(absolute) - centerShift;

        var cellA = FloorDiv(mappedAbsolute.A);
        var cellB = FloorDiv(mappedAbsolute.B);
        var cellC = FloorDiv(mappedAbsolute.C);
        var baseGrid = new GridFractional3(
            mappedAbsolute.A - (cellA * GridFractional3.Denominator),
            mappedAbsolute.B - (cellB * GridFractional3.Denominator),
            mappedAbsolute.C - (cellC * GridFractional3.Denominator));

        if (!encoder.TryGetPositionIndex(baseGrid, out var p))
        {
            return false;
        }

        mapped = new Vector4I(checked((int)cellA), checked((int)cellB), checked((int)cellC), p);
        return true;
    }

    private static bool IsSameOrbit(
        ImmutableArray<SymmetryOperation> stabilizer,
        GridFractional3 vector,
        GridFractional3 target)
    {
        var normalizedTarget = Normalize(target);
        foreach (var operation in stabilizer)
        {
            var (x, y, z) = operation.Linear.Transform(vector.A, vector.B, vector.C);
            if (Normalize(new GridFractional3(x, y, z)) == normalizedTarget)
            {
                return true;
            }
        }

        return false;
    }

    private static GridFractional3 Normalize(GridFractional3 value) =>
        new(Modulo(value.A), Modulo(value.B), Modulo(value.C));

    private static long Modulo(long value)
    {
        var remainder = value % GridFractional3.Denominator;
        return remainder < 0 ? remainder + GridFractional3.Denominator : remainder;
    }

    private static long FloorDiv(long value)
    {
        var quotient = value / GridFractional3.Denominator;
        if (value < 0 && value % GridFractional3.Denominator != 0)
        {
            quotient--;
        }

        return quotient;
    }
}
