// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using FsCheck.Xunit;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;

namespace Mocassin.Symmetry.Tests;

[Trait("Tier", "PR-fast")]
public sealed class ReductionTests
{
    private static readonly int[] identityMapping = [0, 1, 2];
    private static readonly int[] swappedMapping = [1, 0];
    private static readonly int[] mixedTuple = [0, 1];

    [Fact]
    public void GenericOrbitHasFullMultiplicityForEverySetting()
    {
        var generic = new Fractional3(0.137123456789, 0.271234567891, 0.419345678912);
        foreach (var setting in SpaceGroupCatalog.Default.Settings)
        {
            var orbit = SpaceGroupOperations.ExpandOrbit(setting, generic);
            Assert.Equal(setting.Operations.Length, orbit.Length);
        }
    }

    [Fact]
    public void OrbitExpansionMatchesExplicitGridEnumerationForEverySetting()
    {
        var points = new[]
        {
            new Fractional3(0.137123456789, 0.271234567891, 0.419345678912),
            new Fractional3(0.25, 0.5, 0.75),
            new Fractional3(0.0, 0.0, 0.0),
        };

        foreach (var setting in SpaceGroupCatalog.Default.Settings)
        {
            foreach (var point in points)
            {
                var expected = new SortedSet<GridFractional3>();
                var grid = SpaceGroupOperations.ToGrid(point, nameof(point));
                foreach (var operation in setting.Operations)
                {
                    expected.Add(operation.Transform(grid).TrimToUnitCell());
                }

                var actual = SpaceGroupOperations.ExpandOrbit(setting, point);
                Assert.Equal(
                    expected.Select(static entry => entry.ToFractional3()).ToArray(),
                    actual.Select(static entry => SpaceGroupOperations.ToGrid(entry, nameof(point))).Select(static entry => entry.ToFractional3()).ToArray());
            }
        }
    }

    [Fact]
    public void GridOperationsStayOnTheExactGridForEverySetting()
    {
        var points = new[]
        {
            new Fractional3(0.0, 0.0, 0.0),
            new Fractional3(0.25, 0.5, 0.75),
            new Fractional3(1.0 / 24.0, 3.0 / 24.0, 5.0 / 24.0),
        };

        foreach (var setting in SpaceGroupCatalog.Default.Settings)
        {
            foreach (var point in points)
            {
                var grid = SpaceGroupOperations.ToGrid(point, nameof(point));
                foreach (var operation in setting.Operations)
                {
                    var transformed = operation.Transform(grid);
                    Assert.True(GridFractional3.TryFromFractional(transformed.ToFractional3(), out var roundTripped, out _));
                    Assert.Equal(transformed, roundTripped);
                }
            }
        }
    }

    [Fact]
    public void P1PointGroupIsTrivial()
    {
        var setting = Setting(1, AxisSetting.None, OriginChoice.None);
        var pointGroup = SpaceGroupOperations.GetPointGroup(
            setting,
            new Fractional3(0.1, 0.2, 0.3),
            [new Fractional3(0.2, 0.3, 0.4)]);

        Assert.Single(pointGroup.CenterStabilizer);
        Assert.Single(pointGroup.SetwiseStabilizer);
        Assert.Single(pointGroup.PointwiseStabilizer);
        Assert.Single(pointGroup.InducedPermutations);
        Assert.True(pointGroup.InducedPermutations[0].IsIdentity);
        Assert.Equal(1, pointGroup.OrderedOrbitSize);
        Assert.Equal(1, pointGroup.SetOrbitSize);
    }

    [Fact]
    public void P2PointGroupDistinguishesSetwiseAndPointwiseStabilizers()
    {
        var setting = Setting(3, AxisSetting.UniqueAxisB, OriginChoice.None);
        var center = new Fractional3(0.0, 0.2, 0.0);
        ImmutableArray<Fractional3> geometry =
        [
            new Fractional3(0.1, 0.3, 0.4),
            new Fractional3(0.9, 0.3, 0.6),
        ];

        var pointGroup = SpaceGroupOperations.GetPointGroup(setting, center, geometry);

        Assert.Equal(2, pointGroup.CenterStabilizer.Length);
        Assert.Equal(2, pointGroup.SetwiseStabilizer.Length);
        Assert.Single(pointGroup.PointwiseStabilizer);
        Assert.Equal(2, pointGroup.InducedPermutations.Length);
        Assert.Equal(2, pointGroup.OrderedOrbitSize);
        Assert.Equal(1, pointGroup.SetOrbitSize);
        Assert.Contains(pointGroup.InducedPermutations, static permutation => permutation.IsIdentity);
        Assert.Contains(
            pointGroup.InducedPermutations,
            static permutation => permutation.Mapping.SequenceEqual(swappedMapping));
        Assert.Equal(2, SpaceGroupOperations.GetOrderedOrbit(setting, center, geometry).Length);
        Assert.Single(SpaceGroupOperations.GetSetOrbit(setting, center, geometry));
    }

    [Fact]
    public void FixedPointGeometryHasEqualSetwiseAndPointwiseStabilizers()
    {
        var setting = Setting(3, AxisSetting.UniqueAxisB, OriginChoice.None);
        var center = new Fractional3(0.0, 0.2, 0.0);
        ImmutableArray<Fractional3> geometry =
        [
            new Fractional3(0.0, 0.3, 0.0),
            new Fractional3(0.0, 0.5, 0.0),
        ];

        var pointGroup = SpaceGroupOperations.GetPointGroup(setting, center, geometry);

        Assert.Equal(2, pointGroup.CenterStabilizer.Length);
        Assert.Equal(2, pointGroup.SetwiseStabilizer.Length);
        Assert.Equal(2, pointGroup.PointwiseStabilizer.Length);
        Assert.Single(pointGroup.InducedPermutations);
        Assert.Equal(1, pointGroup.OrderedOrbitSize);
        Assert.Equal(1, pointGroup.SetOrbitSize);
        Assert.Single(SpaceGroupOperations.GetOrderedOrbit(setting, center, geometry));
        Assert.Single(SpaceGroupOperations.GetSetOrbit(setting, center, geometry));
    }

    [Fact]
    public void PmmmPointGroupHasEightCenterOperationsAndAGeneralOrbitOfEight()
    {
        var setting = Setting(47, AxisSetting.None, OriginChoice.None);
        var center = new Fractional3(0.0, 0.0, 0.0);
        ImmutableArray<Fractional3> geometry = [new Fractional3(0.1, 0.2, 0.3)];

        var pointGroup = SpaceGroupOperations.GetPointGroup(setting, center, geometry);

        Assert.Equal(8, pointGroup.CenterStabilizer.Length);
        Assert.Single(pointGroup.SetwiseStabilizer);
        Assert.Single(pointGroup.PointwiseStabilizer);
        Assert.Single(pointGroup.InducedPermutations);
        Assert.Equal(8, pointGroup.OrderedOrbitSize);
        Assert.Equal(8, pointGroup.SetOrbitSize);
        Assert.Equal(8, SpaceGroupOperations.GetOrderedOrbit(setting, center, geometry).Length);
        Assert.Equal(8, SpaceGroupOperations.GetSetOrbit(setting, center, geometry).Length);
    }

    [Fact]
    public void PointGroupRejectsDuplicateGeometry()
    {
        var setting = Setting(1, AxisSetting.None, OriginChoice.None);
        ImmutableArray<Fractional3> geometry =
        [
            new Fractional3(0.1, 0.2, 0.3),
            new Fractional3(0.1, 0.2, 0.3),
        ];

        Assert.Throws<ArgumentException>(
            () => SpaceGroupOperations.GetPointGroup(setting, new Fractional3(0.0, 0.0, 0.0), geometry));
    }

    [Fact]
    public void SitePermutationValidatesAndIdentifiesIdentity()
    {
        var identity = SitePermutation.Identity(3);
        Assert.True(identity.IsIdentity);
        Assert.Equal(identityMapping, identity.Mapping.ToArray());

        Assert.Throws<ArgumentException>(() => new SitePermutation([0, 0, 2]));
        Assert.Throws<ArgumentException>(() => new SitePermutation([0, 1, 3]));
    }

    [Fact]
    public void PairReductionInP1LinksChiralPartners()
    {
        var geometry = CreatePeriodicGeometry(
            1,
            AxisSetting.None,
            OriginChoice.None,
            5.0,
            5.0,
            5.0,
            [new Fractional3(0.0, 0.0, 0.0), new Fractional3(0.3, 0.0, 0.0), new Fractional3(0.7, 0.0, 0.0)]);
        var centerP = geometry.Encoder.Encode(new Fractional3(0.0, 0.0, 0.0)).P;

        var result = PairReduction.Reduce(geometry, centerP, 4.0);

        Assert.NotEmpty(result.Shells);
        Assert.All(result.Shells, static shell => Assert.Single(shell.EquivalentImages));
        Assert.All(result.Shells, static shell => Assert.NotNull(shell.ChiralPartnerIndex));
        for (var index = 0; index < result.Shells.Length; index++)
        {
            var partnerIndex = result.Shells[index].ChiralPartnerIndex!.Value;
            Assert.NotEqual(index, partnerIndex);
            Assert.Equal(index, result.Shells[partnerIndex].ChiralPartnerIndex);
            Assert.Equal(result.Shells[index].DistanceBucketFm, result.Shells[partnerIndex].DistanceBucketFm);
        }
        Assert.Equal(result.Shells[0].DistanceBucketFm, result.Shells[1].DistanceBucketFm);
    }

    [Fact]
    public void PairReductionGroupsSymmetryEquivalentImages()
    {
        var geometry = CreatePeriodicGeometry(
            3,
            AxisSetting.UniqueAxisB,
            OriginChoice.None,
            5.0,
            6.0,
            7.0,
            [new Fractional3(0.0, 0.2, 0.0), new Fractional3(0.1, 0.3, 0.4)]);
        var centerP = geometry.Encoder.Encode(new Fractional3(0.0, 0.2, 0.0)).P;

        var result = PairReduction.Reduce(geometry, centerP, 4.0);

        Assert.NotEmpty(result.Shells);
        Assert.All(result.Shells, static shell => Assert.Equal(2, shell.EquivalentImages.Length));
        for (var index = 1; index < result.Shells.Length; index++)
        {
            var previous = result.Shells[index - 1];
            var current = result.Shells[index];
            Assert.True(
                previous.DistanceBucketFm < current.DistanceBucketFm
                || (previous.DistanceBucketFm == current.DistanceBucketFm
                    && Compare(previous.Representative, current.Representative) < 0));
        }
    }

    [Fact]
    public void ClusterReductionInP1KeepsEveryTuple()
    {
        var setting = Setting(1, AxisSetting.None, OriginChoice.None);
        ImmutableArray<Fractional3> sites =
        [
            new Fractional3(0.2, 0.0, 0.0),
            new Fractional3(0.4, 0.0, 0.0),
        ];
        ImmutableArray<ImmutableArray<int>> allowed = [[0, 1], [0, 1]];

        var result = ClusterReduction.Reduce(setting, new Fractional3(0.0, 0.0, 0.0), sites, allowed);

        Assert.Equal(4, result.Clusters.Length);
        Assert.All(result.Clusters, static cluster => Assert.Single(cluster.Expansions));
        Assert.Equal(
            4,
            result.Clusters.SelectMany(static cluster => cluster.Expansions).Select(static expansion => expansion.OccupationCode).Distinct().Count());

        var reversed = ClusterReduction.Reduce(setting, new Fractional3(0.0, 0.0, 0.0), sites, [[1, 0], [1, 0]]);
        Assert.Equal(
            result.Clusters.Select(static cluster => cluster.CanonicalCode),
            reversed.Clusters.Select(static cluster => cluster.CanonicalCode));
    }

    [Property(MaxTest = 30)]
    public bool P2PairShellsAreClosedUnderTheCenterStabilizer(double x, double y, double z)
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

        var geometry = CreatePeriodicGeometry(
            3,
            AxisSetting.UniqueAxisB,
            OriginChoice.None,
            5.0,
            6.0,
            7.0,
            [new Fractional3(0.0, 0.2, 0.0), new Fractional3(x, y, z)]);
        var center = geometry.Encoder.Encode(new Fractional3(0.0, 0.2, 0.0)).P;
        var target = geometry.Encoder.Encode(new Fractional3(x, y, z)).P;
        var result = PairReduction.Reduce(geometry, center, 4.0);

        return result.Shells
            .Where(shell => shell.Representative.P == target)
            .All(shell => shell.EquivalentImages.Length == 2);
    }

    [Fact]
    public void ClusterReductionMergesPermutationEquivalentTuples()
    {
        var setting = Setting(3, AxisSetting.UniqueAxisB, OriginChoice.None);
        ImmutableArray<Fractional3> sites =
        [
            new Fractional3(0.1, 0.3, 0.4),
            new Fractional3(0.9, 0.3, 0.6),
        ];
        ImmutableArray<ImmutableArray<int>> allowed = [[0, 1], [0, 1]];

        var result = ClusterReduction.Reduce(setting, new Fractional3(0.0, 0.2, 0.0), sites, allowed);

        Assert.Equal(2, result.PointGroup.InducedPermutations.Length);
        Assert.Equal(3, result.Clusters.Length);
        Assert.Equal(
            4,
            result.Clusters.SelectMany(static cluster => cluster.Expansions).Select(static expansion => expansion.OccupationCode).Distinct().Count());
        var mixed = result.Clusters.Single(static cluster => cluster.CanonicalTuple.SequenceEqual(mixedTuple));
        Assert.Equal(2, mixed.Expansions.Length);
        Assert.Contains(mixed.Expansions, static expansion => expansion.OccupationCode == ClusterReduction.EncodeOccupation([1, 0]));
    }

    [Fact]
    public void ClusterReductionReturnsNothingWhenAnySiteIsEmpty()
    {
        var setting = Setting(1, AxisSetting.None, OriginChoice.None);
        ImmutableArray<Fractional3> sites =
        [
            new Fractional3(0.2, 0.0, 0.0),
            new Fractional3(0.4, 0.0, 0.0),
        ];
        ImmutableArray<ImmutableArray<int>> allowed = [[0], []];

        var result = ClusterReduction.Reduce(setting, new Fractional3(0.0, 0.0, 0.0), sites, allowed);

        Assert.Empty(result.Clusters);
    }

    [Fact]
    public void OccupationCodesRoundTripForNineSitesAndAllParticleBits()
    {
        ImmutableArray<int> occupation = [63, 0, 63, 1, 2, 3, 4, 5, 6];
        var code = ClusterReduction.EncodeOccupation(occupation);

        Assert.Equal(occupation.ToArray(), ClusterReduction.DecodeOccupation(code, occupation.Length).ToArray());
        Assert.Equal(0UL, code >> (occupation.Length * ClusterReduction.ParticleBits));
        Assert.Throws<ArgumentException>(() => ClusterReduction.EncodeOccupation([64]));
        Assert.Throws<ArgumentException>(() => ClusterReduction.EncodeOccupation([0, 0, 0, 0, 0, 0, 0, 0, 0, 0]));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ClusterReduction.DecodeOccupation(1UL << (9 * ClusterReduction.ParticleBits), 9));
    }

    private static SpaceGroupSetting Setting(int number, AxisSetting axis, OriginChoice origin) =>
        SpaceGroupCatalog.Default.GetSetting(number, axis, origin)
        ?? throw new InvalidOperationException($"Missing setting ({number}, {axis}, {origin}).");

    private static PeriodicGeometry CreatePeriodicGeometry(
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
        Assert.True(UnitCellVectorEncoder.TryCreate(
            Setting(number, axis, origin),
            references,
            out var encoder,
            out _));
        Assert.NotNull(encoder);
        return new PeriodicGeometry(cell, encoder);
    }

    private static int Compare(Vector4I left, Vector4I right)
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
}
