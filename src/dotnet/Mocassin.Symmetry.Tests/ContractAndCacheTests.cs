// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Core.Symmetry;

namespace Mocassin.Symmetry.Tests;

public sealed class ContractAndCacheTests
{
    [Fact]
    public void VersionInfoRecordsThePinnedContractAndCatalog()
    {
        var version = SymmetryVersionInfo.FromCatalog(SpaceGroupCatalog.Default);

        Assert.Equal("symmetry-1", version.SymmetryContractVersion);
        Assert.Equal("spglib-2.8.0/catalog-1", version.SpaceGroupDataVersion);
        Assert.Equal(SpaceGroupCatalog.Default.DataSha256, version.SpaceGroupDataSha256);
        Assert.Equal(GridFractional3.Denominator, SymmetryContract.FractionalGridDenominator);
    }

    [Fact]
    public void CacheReturnsTheStoredValueWithoutRerunningTheFactory()
    {
        var cache = new SymmetryCache("catalog", "contract");
        var calls = 6;

        Assert.Equal(7, cache.GetOrAdd("key", () => ++calls));
        Assert.Equal(7, cache.GetOrAdd("key", () => ++calls));
        Assert.Equal(7, calls);
        Assert.Equal(1, cache.Count);
        Assert.True(cache.Contains("key"));
    }

    [Fact]
    public void CacheKeysIncludeVersionsSettingAndOperationInputs()
    {
        var cache = new SymmetryCache("catalog-a", "contract-a");
        var otherCache = new SymmetryCache("catalog-b", "contract-a");
        var setting = Setting(3, AxisSetting.UniqueAxisB, OriginChoice.None);
        var otherSetting = Setting(3, AxisSetting.UniqueAxisC, OriginChoice.None);
        var center = new GridFractional3(1, 2, 3);
        var otherCenter = new GridFractional3(1, 2, 4);

        Assert.NotEqual(cache.OrbitKey(setting, center), otherCache.OrbitKey(setting, center));
        Assert.NotEqual(cache.OrbitKey(setting, center), cache.OrbitKey(otherSetting, center));
        Assert.NotEqual(cache.OrbitKey(setting, center), cache.OrbitKey(setting, otherCenter));
        Assert.Equal(cache.OrbitKey(setting, center), cache.OrbitKey(setting, center));

        Assert.NotEqual(cache.PairShellKey(setting, 1, 10, "cell-a"), cache.PairShellKey(setting, 1, 11, "cell-a"));
        Assert.NotEqual(cache.PairShellKey(setting, 1, 10, "cell-a"), cache.PairShellKey(setting, 2, 10, "cell-a"));
        Assert.NotEqual(cache.PairShellKey(setting, 1, 10, "cell-a"), cache.PairShellKey(setting, 1, 10, "cell-b"));
        Assert.NotEqual(
            cache.ClusterKey(setting, 1, "offset-a", "occupation-a"),
            cache.ClusterKey(setting, 1, "offset-a", "occupation-b"));
        Assert.NotEqual(cache.CanonicalP1Key("structure-a"), cache.CanonicalP1Key("structure-b"));
        Assert.NotEqual(cache.CellKey("cell-a"), cache.CellKey("cell-b"));
    }

    [Fact]
    public void CacheCapacityIsABoundedFailure()
    {
        var cache = new SymmetryCache("catalog", "contract", capacity: 1);
        cache.GetOrAdd("first", static () => 1);

        Assert.Throws<InvalidOperationException>(() => cache.GetOrAdd("second", static () => 2));
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void GridSequenceHashIsDeterministicAndOrderSensitive()
    {
        ImmutableArray<GridFractional3> first = [new GridFractional3(1, 2, 3), new GridFractional3(4, 5, 6)];
        ImmutableArray<GridFractional3> reversed = [new GridFractional3(4, 5, 6), new GridFractional3(1, 2, 3)];

        Assert.Equal(SymmetryCache.GridSequenceHash(first), SymmetryCache.GridSequenceHash(first));
        Assert.NotEqual(SymmetryCache.GridSequenceHash(first), SymmetryCache.GridSequenceHash(reversed));
    }

    [Fact]
    public void ConcurrentCacheAccessStoresOneValue()
    {
        var cache = new SymmetryCache("catalog", "contract");
        var values = new ConcurrentBag<int>();
        var calls = 0;

        Parallel.For(0, 32, _ => values.Add(cache.GetOrAdd("shared", () => Interlocked.Increment(ref calls))));

        Assert.Equal(1, calls);
        Assert.Single(values.Distinct());
    }

    [Fact]
    public void ParallelEncodingMatchesSequentialCanonicalOutput()
    {
        var setting = Setting(3, AxisSetting.UniqueAxisB, OriginChoice.None);
        var references = new[]
        {
            new Fractional3(0.1, 0.2, 0.3),
            new Fractional3(0.25, 0.5, 0.75),
        };
        var sequential = Encode(setting, references);
        var parallel = new ConcurrentBag<Fractional3[]>();

        Parallel.For(0, 16, _ => parallel.Add(Encode(setting, references)));

        Assert.All(parallel, positions => Assert.Equal(sequential, positions));
    }

    [Fact]
    public void BenchmarkFixtureExpandsTo512000LinearIndices()
    {
        var positions = new Fractional3[64];
        var index = 0;
        for (var a = 0; a < 4; a++)
        {
            for (var b = 0; b < 4; b++)
            {
                for (var c = 0; c < 4; c++)
                {
                    positions[index++] = new Fractional3(a / 4.0, b / 4.0, c / 4.0);
                }
            }
        }

        Assert.True(UnitCellVectorEncoder.TryCreate(
            Setting(1, AxisSetting.None, OriginChoice.None),
            positions,
            out var encoder,
            out _));
        Assert.NotNull(encoder);
        Assert.Equal(64, encoder.PositionCount);

        var last = new Vector4I(19, 19, 19, 63);
        Assert.Equal(511_999, encoder.GetLinearIndex(last, 20, 20, 20));
    }

    private static Fractional3[] Encode(SpaceGroupSetting setting, Fractional3[] references)
    {
        Assert.True(UnitCellVectorEncoder.TryCreate(setting, references, out var encoder, out _));
        Assert.NotNull(encoder);
        return encoder.Positions.ToArray();
    }

    private static SpaceGroupSetting Setting(int number, AxisSetting axis, OriginChoice origin) =>
        SpaceGroupCatalog.Default.GetSetting(number, axis, origin)
        ?? throw new InvalidOperationException($"Missing setting ({number}, {axis}, {origin}).");
}
