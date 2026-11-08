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
        var tuples = new List<int[]>();
        var tuple = new int[sites.Length];

        void Collect(int index)
        {
            if (index == tuple.Length)
            {
                tuples.Add(tuple.ToArray());
                return;
            }

            foreach (var particle in lists[index])
            {
                tuple[index] = particle;
                Collect(index + 1);
            }
        }

        Collect(0);

        var groups = new Dictionary<ulong, (int[] Canonical, HashSet<ulong> Expansions)>();
        foreach (var original in tuples)
        {
            var orbit = EnumerateOrbit(pointGroup, original)
                .OrderBy(static item => item, Comparer<int[]>.Create(Compare))
                .ToArray();
            var canonical = orbit[0];
            var canonicalCode = Encode(canonical);
            if (!groups.TryGetValue(canonicalCode, out var entry))
            {
                entry = (canonical, []);
                groups.Add(canonicalCode, entry);
            }

            foreach (var member in orbit)
            {
                entry.Expansions.Add(Encode(member));
            }
        }

        return groups;
    }

    private static IEnumerable<int[]> EnumerateOrbit(PointGroup pointGroup, int[] tuple)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var permutation in pointGroup.InducedPermutations)
        {
            var permuted = new int[tuple.Length];
            for (var site = 0; site < tuple.Length; site++)
            {
                permuted[site] = tuple[permutation[site]];
            }

            if (seen.Add(string.Join(",", permuted)))
            {
                yield return permuted;
            }
        }
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

        var indexByOffset = candidates
            .Select((candidate, index) => (candidate.Offset, index))
            .ToDictionary(static entry => entry.Offset, static entry => entry.index);
        var parent = Enumerable.Range(0, candidates.Count).ToArray();

        int Find(int index)
        {
            while (parent[index] != index)
            {
                parent[index] = parent[parent[index]];
                index = parent[index];
            }

            return index;
        }

        void Union(int left, int right)
        {
            var leftRoot = Find(left);
            var rightRoot = Find(right);
            if (leftRoot != rightRoot)
            {
                parent[rightRoot] = leftRoot;
            }
        }

        for (var index = 0; index < candidates.Count; index++)
        {
            foreach (var operation in stabilizer)
            {
                var mapped = Map(encoder, centerPosition, operation, candidates[index].Offset);
                if (mapped is { } value
                    && indexByOffset.TryGetValue(value, out var mappedIndex)
                    && candidates[mappedIndex].Bucket == candidates[index].Bucket)
                {
                    Union(index, mappedIndex);
                }
            }
        }

        var shells = new List<ExpectedShell>();
        foreach (var group in Enumerable.Range(0, candidates.Count).GroupBy(Find))
        {
            var offsets = group.Select(index => candidates[index].Offset).ToHashSet();
            var representative = offsets.OrderBy(static offset => offset, Comparer<Vector4I>.Create(CompareOffset)).First();
            shells.Add(new ExpectedShell(
                representative,
                candidates[group.First()].Bucket,
                offsets,
                null));
        }

        shells.Sort(static (left, right) =>
        {
            var comparison = left.Bucket.CompareTo(right.Bucket);
            return comparison != 0 ? comparison : CompareOffset(left.Representative, right.Representative);
        });

        for (var index = 0; index < shells.Count; index++)
        {
            var shell = shells[index];
            var displacement = Displacement(encoder, centerPosition, shell.Representative);
            var forwardSignature = OrbitSignature(stabilizer, displacement);
            var inverseSignature = OrbitSignature(
                stabilizer,
                new Fractional3(-displacement.A, -displacement.B, -displacement.C));
            if (forwardSignature == inverseSignature)
            {
                continue;
            }

            for (var partner = 0; partner < shells.Count; partner++)
            {
                if (partner == index || shells[partner].Bucket != shell.Bucket)
                {
                    continue;
                }

                var partnerDisplacement = Displacement(encoder, centerPosition, shells[partner].Representative);
                if (OrbitSignature(stabilizer, partnerDisplacement) == inverseSignature)
                {
                    shells[index] = shell with { ChiralPartner = partner };
                    break;
                }
            }
        }

        return shells;
    }

    private static int CompareOffset(Vector4I left, Vector4I right)
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
    }

    private static Vector4I? Map(
        UnitCellVectorEncoder encoder,
        Fractional3 center,
        SymmetryOperation operation,
        Vector4I image)
    {
        var target = encoder.Positions[image.P];
        var absolute = new Fractional3(
            target.A + image.A,
            target.B + image.B,
            target.C + image.C);
        var mappedCenter = operation.Transform(center);
        var mappedTarget = operation.Transform(absolute);
        var relative = new Fractional3(
            mappedTarget.A - mappedCenter.A,
            mappedTarget.B - mappedCenter.B,
            mappedTarget.C - mappedCenter.C);
        var absoluteImage = new Fractional3(
            center.A + relative.A,
            center.B + relative.B,
            center.C + relative.C);
        var trimmed = absoluteImage.TrimToUnitCell();
        if (!encoder.TryIndexOf(trimmed, out var p))
        {
            return null;
        }

        var basePosition = encoder.Positions[p];
        return new Vector4I(
            (int)Math.Round(absoluteImage.A - basePosition.A, MidpointRounding.AwayFromZero),
            (int)Math.Round(absoluteImage.B - basePosition.B, MidpointRounding.AwayFromZero),
            (int)Math.Round(absoluteImage.C - basePosition.C, MidpointRounding.AwayFromZero),
            p);
    }

    private static Fractional3 Displacement(
        UnitCellVectorEncoder encoder,
        Fractional3 center,
        Vector4I image)
    {
        var target = encoder.Positions[image.P];
        return new Fractional3(
            target.A + image.A - center.A,
            target.B + image.B - center.B,
            target.C + image.C - center.C);
    }

    private static string OrbitSignature(
        ImmutableArray<SymmetryOperation> stabilizer,
        Fractional3 displacement)
    {
        var signatures = new List<string>(stabilizer.Length);
        foreach (var operation in stabilizer)
        {
            var linear = operation.Linear;
            var x = (linear.M11 * displacement.A) + (linear.M12 * displacement.B) + (linear.M13 * displacement.C);
            var y = (linear.M21 * displacement.A) + (linear.M22 * displacement.B) + (linear.M23 * displacement.C);
            var z = (linear.M31 * displacement.A) + (linear.M32 * displacement.B) + (linear.M33 * displacement.C);
            signatures.Add(string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{NormalizeComponent(x):F9},{NormalizeComponent(y):F9},{NormalizeComponent(z):F9}"));
        }

        signatures.Sort(StringComparer.Ordinal);
        return signatures[0];
    }

    private static double NormalizeComponent(double value)
    {
        var normalized = value - Math.Floor(value);
        return normalized == 1.0 ? 0.0 : normalized;
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
