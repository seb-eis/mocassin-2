// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using FsCheck.Xunit;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;

namespace Mocassin.Symmetry.Tests;

[Trait("Tier", "PR-fast")]
public sealed class BruteForceReductionTests
{
    [Fact]
    public void PairReductionMatchesExplicitEnumeratedOrbitsInP2()
    {
        var geometry = CreateGeometry(
            3,
            AxisSetting.UniqueAxisB,
            OriginChoice.None,
            5.0,
            6.0,
            7.0,
            [new Fractional3(0.0, 0.2, 0.0), new Fractional3(0.1, 0.3, 0.4)]);
        var center = geometry.Encoder.Encode(new Fractional3(0.0, 0.2, 0.0)).P;

        AssertMatchesBruteForce(geometry, center, 4.0);
    }

    [Fact]
    public void PairReductionMatchesExplicitEnumeratedOrbitsInPmmm()
    {
        var geometry = CreateGeometry(
            47,
            AxisSetting.None,
            OriginChoice.None,
            5.0,
            6.0,
            7.0,
            [new Fractional3(0.0, 0.0, 0.0), new Fractional3(0.1, 0.2, 0.3)]);
        var center = geometry.Encoder.Encode(new Fractional3(0.0, 0.0, 0.0)).P;

        AssertMatchesBruteForce(geometry, center, 4.5);
    }

    [Property(MaxTest = 20)]
    public bool PairReductionMatchesExplicitEnumerationForRandomP2Targets(double x, double y, double z)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z))
        {
            return true;
        }

        x -= Math.Floor(x);
        y -= Math.Floor(y);
        z -= Math.Floor(z);
        if (x is < 0.05 or > 0.95 || z is < 0.05 or > 0.95)
        {
            return true;
        }

        var geometry = CreateGeometry(
            3,
            AxisSetting.UniqueAxisB,
            OriginChoice.None,
            5.0,
            6.0,
            7.0,
            [new Fractional3(0.0, 0.2, 0.0), new Fractional3(x, y, z)]);
        var center = geometry.Encoder.Encode(new Fractional3(0.0, 0.2, 0.0)).P;
        return MatchesBruteForce(geometry, center, 4.0);
    }

    [Fact]
    public void ClusterReductionMatchesOracleForRandomAllowedOccupations()
    {
        var random = new Random(20261108);
        ImmutableArray<Fractional3> sites =
        [
            new Fractional3(0.1, 0.3, 0.4),
            new Fractional3(0.9, 0.3, 0.6),
        ];

        for (var trial = 0; trial < 20; trial++)
        {
            var allowed = ImmutableArray.CreateBuilder<ImmutableArray<int>>(2);
            for (var site = 0; site < 2; site++)
            {
                var particles = Enumerable.Range(0, 4)
                    .Where(_ => random.Next(2) == 0)
                    .ToImmutableArray();
                allowed.Add(particles.IsEmpty ? [0] : particles);
            }

            AssertMatchesClusterOracle(
                3,
                AxisSetting.UniqueAxisB,
                OriginChoice.None,
                new Fractional3(0.0, 0.2, 0.0),
                sites,
                allowed.MoveToImmutable());
        }
    }

    [Fact]
    public void ClusterReductionMatchesExplicitPermutationOracleInP2AndPmmm()
    {
        AssertMatchesClusterOracle(
            3,
            AxisSetting.UniqueAxisB,
            OriginChoice.None,
            new Fractional3(0.0, 0.2, 0.0),
            [new Fractional3(0.1, 0.3, 0.4), new Fractional3(0.9, 0.3, 0.6)],
            [[0, 1, 2], [0, 1, 2]]);
        AssertMatchesClusterOracle(
            47,
            AxisSetting.None,
            OriginChoice.None,
            new Fractional3(0.0, 0.0, 0.0),
            [new Fractional3(0.1, 0.2, 0.3), new Fractional3(0.4, 0.5, 0.6)],
            [[0, 1], [0, 1, 2]]);
    }

    private static void AssertMatchesClusterOracle(
        int number,
        AxisSetting axis,
        OriginChoice origin,
        Fractional3 center,
        ImmutableArray<Fractional3> sites,
        ImmutableArray<ImmutableArray<int>> allowed)
    {
        var setting = SpaceGroupCatalog.Default.GetSetting(number, axis, origin)
            ?? throw new InvalidOperationException($"Missing setting ({number}, {axis}, {origin}).");
        var expected = EnumerateClusters(setting, center, sites, allowed);
        var actual = ClusterReduction.Reduce(setting, center, sites, allowed);

        Assert.Equal(expected.Count, actual.Clusters.Length);
        foreach (var cluster in actual.Clusters)
        {
            Assert.True(expected.TryGetValue(cluster.CanonicalCode, out var oracle));
            Assert.Equal(oracle.Canonical, cluster.CanonicalTuple.ToArray());
            Assert.Equal(
                oracle.Expansions.Order(),
                cluster.Expansions.Select(static expansion => expansion.OccupationCode).Order());
        }
    }

    private static Dictionary<ulong, (int[] Canonical, HashSet<ulong> Expansions)> EnumerateClusters(
        SpaceGroupSetting setting,
        Fractional3 center,
        ImmutableArray<Fractional3> sites,
        ImmutableArray<ImmutableArray<int>> allowed)
    {
        var pointGroup = SpaceGroupOperations.GetPointGroup(setting, center, sites);
        var lists = allowed.Select(static particles => particles.Distinct().Order().ToArray()).ToArray();
        var clusters = new Dictionary<ulong, (int[] Canonical, HashSet<ulong> Expansions)>();
        var tuple = new int[sites.Length];

        void Visit(int index)
        {
            if (index == tuple.Length)
            {
                var canonical = tuple.ToArray();
                foreach (var permutation in pointGroup.InducedPermutations)
                {
                    var permuted = new int[tuple.Length];
                    for (var site = 0; site < tuple.Length; site++)
                    {
                        permuted[site] = tuple[permutation[site]];
                    }

                    if (Compare(permuted, canonical) < 0)
                    {
                        canonical = permuted;
                    }
                }

                var code = Encode(canonical);
                if (!clusters.TryGetValue(code, out var entry))
                {
                    entry = (canonical, []);
                    clusters.Add(code, entry);
                }

                foreach (var permutation in pointGroup.InducedPermutations)
                {
                    var permuted = new int[canonical.Length];
                    for (var site = 0; site < canonical.Length; site++)
                    {
                        permuted[site] = canonical[permutation[site]];
                    }

                    entry.Expansions.Add(Encode(permuted));
                }

                return;
            }

            foreach (var particle in lists[index])
            {
                tuple[index] = particle;
                Visit(index + 1);
            }
        }

        Visit(0);
        return clusters;
    }

    private static int Compare(int[] left, int[] right)
    {
        for (var index = 0; index < left.Length; index++)
        {
            var comparison = left[index].CompareTo(right[index]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }

    private static ulong Encode(int[] tuple)
    {
        ulong code = 0;
        for (var site = 0; site < tuple.Length; site++)
        {
            code |= (ulong)tuple[site] << (site * 6);
        }

        return code;
    }

    private static void AssertMatchesBruteForce(PeriodicGeometry geometry, int center, double cutoff)
    {
        Assert.True(MatchesBruteForce(geometry, center, cutoff));
    }

    private static bool MatchesBruteForce(PeriodicGeometry geometry, int center, double cutoff)
    {
        var expected = EnumerateShells(geometry, center, cutoff);
        var actual = PairReduction.Reduce(geometry, center, cutoff);
        if (expected.Count != actual.Shells.Length)
        {
            return false;
        }

        for (var index = 0; index < expected.Count; index++)
        {
            var expectedShell = expected[index];
            var actualShell = actual.Shells[index];
            if (expectedShell.Representative != actualShell.Representative
                || expectedShell.Bucket != actualShell.DistanceBucketFm
                || !expectedShell.Equivalents.SetEquals(actualShell.EquivalentImages)
                || expectedShell.ChiralPartner != actualShell.ChiralPartnerIndex)
            {
                return false;
            }
        }

        return true;
    }

    private static List<ExpectedShell> EnumerateShells(
        PeriodicGeometry geometry,
        int center,
        double cutoff)
    {
        var encoder = geometry.Encoder;
        var centerPosition = encoder.GetPosition(center);
        var centerGrid = SpaceGroupOperations.ToGrid(centerPosition, nameof(center));
        var stabilizer = SpaceGroupOperations.GetStabilizer(encoder.Setting, centerPosition);
        var cutoffBucket = PeriodicGeometry.ToBucket(cutoff);
        var candidates = new List<(Vector4I Offset, int Bucket)>();

        for (var p = 0; p < encoder.PositionCount; p++)
        {
            var target = encoder.Positions[p];
            for (var a = -6; a <= 6; a++)
            {
                for (var b = -6; b <= 6; b++)
                {
                    for (var c = -6; c <= 6; c++)
                    {
                        if (a == 0 && b == 0 && c == 0 && p == center)
                        {
                            continue;
                        }

                        var distance = geometry.Cell.GetDistance(
                            centerPosition,
                            new Fractional3(target.A + a, target.B + b, target.C + c));
                        var bucket = PeriodicGeometry.ToBucket(distance);
                        if (bucket <= cutoffBucket)
                        {
                            candidates.Add((new Vector4I(a, b, c, p), bucket));
                        }
                    }
                }
            }
        }

        candidates.Sort(static (left, right) =>
        {
            var comparison = left.Bucket.CompareTo(right.Bucket);
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

        var byOffset = candidates.ToDictionary(static candidate => candidate.Offset);
        var assigned = new HashSet<Vector4I>();
        var shells = new List<ExpectedShell>();

        foreach (var candidate in candidates)
        {
            if (!assigned.Add(candidate.Offset))
            {
                continue;
            }

            var equivalents = new HashSet<Vector4I> { candidate.Offset };
            foreach (var operation in stabilizer)
            {
                var mapped = Map(encoder, centerGrid, operation, candidate.Offset);
                if (mapped is { } value
                    && byOffset.TryGetValue(value, out var image)
                    && image.Bucket == candidate.Bucket)
                {
                    equivalents.Add(value);
                    assigned.Add(value);
                }
            }

            shells.Add(new ExpectedShell(
                candidate.Offset,
                candidate.Bucket,
                equivalents,
                null));
        }

        for (var index = 0; index < shells.Count; index++)
        {
            var shell = shells[index];
            var displacement = Displacement(encoder, centerGrid, shell.Representative);
            var inverse = new GridFractional3(-displacement.A, -displacement.B, -displacement.C);
            if (IsSameOrbit(stabilizer, inverse, displacement))
            {
                continue;
            }

            for (var partner = 0; partner < shells.Count; partner++)
            {
                if (partner == index || shells[partner].Bucket != shell.Bucket)
                {
                    continue;
                }

                if (IsSameOrbit(
                    stabilizer,
                    inverse,
                    Displacement(encoder, centerGrid, shells[partner].Representative)))
                {
                    shells[index] = shell with { ChiralPartner = partner };
                    break;
                }
            }
        }

        return shells;
    }

    private static Vector4I? Map(
        UnitCellVectorEncoder encoder,
        GridFractional3 center,
        SymmetryOperation operation,
        Vector4I image)
    {
        var target = SpaceGroupOperations.ToGrid(encoder.Positions[image.P], nameof(image));
        var absolute = target
            + new GridFractional3(
                checked(image.A * GridFractional3.Denominator),
                checked(image.B * GridFractional3.Denominator),
                checked(image.C * GridFractional3.Denominator));
        var centerShift = operation.Transform(center) - center;
        var mapped = operation.Transform(absolute) - centerShift;
        var cellA = FloorDiv(mapped.A);
        var cellB = FloorDiv(mapped.B);
        var cellC = FloorDiv(mapped.C);
        var baseGrid = new GridFractional3(
            mapped.A - (cellA * GridFractional3.Denominator),
            mapped.B - (cellB * GridFractional3.Denominator),
            mapped.C - (cellC * GridFractional3.Denominator));
        if (!encoder.TryGetPositionIndex(baseGrid, out var p))
        {
            return null;
        }

        return new Vector4I(checked((int)cellA), checked((int)cellB), checked((int)cellC), p);
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
        return value < 0 && value % GridFractional3.Denominator != 0 ? quotient - 1 : quotient;
    }

    private static PeriodicGeometry CreateGeometry(
        int number,
        AxisSetting axis,
        OriginChoice origin,
        double a,
        double b,
        double c,
        ImmutableArray<Fractional3> references)
    {
        var parameters = new CellParameters(
            new LengthAngstrom(a),
            new LengthAngstrom(b),
            new LengthAngstrom(c),
            new AngleDegrees(90.0),
            new AngleDegrees(90.0),
            new AngleDegrees(90.0));
        Assert.True(CellGeometry.TryCreate(parameters, out var cell, out _));
        Assert.NotNull(cell);
        var setting = SpaceGroupCatalog.Default.GetSetting(number, axis, origin)
            ?? throw new InvalidOperationException($"Missing setting ({number}, {axis}, {origin}).");
        Assert.True(UnitCellVectorEncoder.TryCreate(setting, references, out var encoder, out _));
        Assert.NotNull(encoder);
        return new PeriodicGeometry(cell, encoder);
    }

    private sealed record ExpectedShell(
        Vector4I Representative,
        int Bucket,
        HashSet<Vector4I> Equivalents,
        int? ChiralPartner);
}
