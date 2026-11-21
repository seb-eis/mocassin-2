// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using FsCheck.Xunit;
using Mocassin.Compilation.Energy;
using Mocassin.Contracts.Packages;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Symmetry;
using Mocassin.Packages.SimulationPackage.Payloads;
using Mocassin.Symmetry.Reduction;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Packages.Tests.SimulationPackage;

/// <summary>
/// Property tests for the cluster expansion map against the reduction oracle, including superset orbits and
/// asymmetric allowed sets.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ClusterExpansionPropertyTests
{
    private static readonly SpaceGroupSetting p1 =
        SpaceGroupCatalog.Default.GetSetting(1, AxisSetting.None, OriginChoice.None)
        ?? throw new InvalidOperationException("P1 is missing.");
    private static readonly SpaceGroupSetting pMinus1 =
        SpaceGroupCatalog.Default.GetSetting(2, AxisSetting.None, OriginChoice.None)
        ?? throw new InvalidOperationException("P-1 is missing.");
    private static readonly SpaceGroupSetting p2 =
        SpaceGroupCatalog.Default.GetSetting(3, AxisSetting.UniqueAxisB, OriginChoice.None)
        ?? throw new InvalidOperationException("P2 is missing.");
    private static readonly SpaceGroupSetting p3 =
        SpaceGroupCatalog.Default.GetSetting(5, AxisSetting.UniqueAxisB, OriginChoice.None)
        ?? throw new InvalidOperationException("P3 is missing.");

    [Property(MaxTest = 40)]
    public void TheMapCoversEveryEnumeratedTupleAndSelfMaps(int seed)
    {
        var random = new Random(seed);
        var (setting, centre, sites) = Geometry(random);
        ImmutableArray<ImmutableArray<int>> allowed =
        [
            AllowedSet(random),
            AllowedSet(random),
        ];
        var reduction = ClusterReduction.Reduce(setting, centre, sites, allowed);
        var centreParticle = random.Next(1, 4);
        var table = BuildTable(reduction, centreParticle);
        var map = EnergyPayloadMapper.MapCodeExpansions(table, particleCount: 4);

        var oracle = Oracle(setting, centre, sites, allowed);
        var canonicalIndexByCode = table.Occupations
            .Select(static (occupation, index) => (occupation.CanonicalCode, index))
            .ToDictionary(static item => item.CanonicalCode, static item => (uint)item.index);

        if (map is null)
        {
            Assert.All(reduction.Clusters, static cluster => Assert.Single(cluster.Expansions));
            foreach (var (canonicalCode, _) in canonicalIndexByCode)
            {
                Assert.Contains(table.Occupations, occupation => occupation.CanonicalCode == canonicalCode);
            }

            return;
        }

        var expanded = map.ExpandedCodes.ToArray();
        var indices = map.CanonicalIndices.ToArray();
        foreach (var memberTuple in EnumerateTuples(allowed))
        {
            var raw = ClusterReduction.EncodeOccupation([centreParticle, .. memberTuple]);
            var position = Array.IndexOf(expanded, raw);
            Assert.True(position >= 0, $"enumerated raw code {raw} is missing from the map");
            var canonical = table.Occupations[(int)indices[position]].CanonicalCode;
            Assert.True(
                canonical == raw || reduction.Clusters.Any(cluster =>
                    ClusterReduction.EncodeOccupation([centreParticle, .. cluster.CanonicalTuple]) == canonical
                    && cluster.Expansions.Any(expansion =>
                        ClusterReduction.EncodeOccupation([centreParticle, .. ClusterReduction.DecodeOccupation(expansion.OccupationCode, 2)]) == raw)),
                $"raw code {raw} does not resolve through the canonical orbit {canonical}");
        }

        foreach (var occupation in table.Occupations)
        {
            var position = Array.IndexOf(expanded, occupation.CanonicalCode);
            Assert.InRange(position, 0, expanded.Length - 1);
            Assert.Equal(
                canonicalIndexByCode[occupation.CanonicalCode],
                indices[position]);
        }

        var enumerated = EnumerateTuples(allowed)
            .Select(tuple => ClusterReduction.EncodeOccupation([centreParticle, .. tuple]))
            .ToHashSet();
        foreach (var code in expanded)
        {
            Assert.True((code >> 60) == 0);
            for (var slot = 0; slot < 4; slot++)
            {
                Assert.InRange(Field(code, slot), 0, 3);
            }

            if (enumerated.Contains(code))
            {
                continue;
            }

            var canonical = table.Occupations[(int)indices[Array.IndexOf(expanded, code)]].CanonicalCode;
            Assert.True(
                oracle.Values.Any(orbit =>
                    orbit.Contains(code >> 6) && orbit.Contains(canonical >> 6)),
                $"extra code {code} is not in the canonical orbit of {canonical}");
        }
    }

    [Fact]
    public void AnAsymmetricAllowedSetProducesASupersetThatStillResolvesEveryEnumeratedTuple()
    {
        ImmutableArray<Fractional3> sites =
        [
            new Fractional3(0.25, 0.0, 0.0),
            new Fractional3(0.75, 0.0, 0.0),
        ];
        ImmutableArray<ImmutableArray<int>> allowed =
        [
            [1, 2],
            [1, 2, 3],
        ];
        var reduction = ClusterReduction.Reduce(pMinus1, new Fractional3(0, 0, 0), sites, allowed);
        var table = BuildTable(reduction, centreParticle: 2);
        var map = Assert.IsType<CodeExpansionPayload>(
            EnergyPayloadMapper.MapCodeExpansions(table, particleCount: 4));

        var enumerated = EnumerateTuples(allowed)
            .Select(tuple => ClusterReduction.EncodeOccupation([2, .. tuple]))
            .ToHashSet();
        foreach (var raw in enumerated)
        {
            var position = map.ExpandedCodes.IndexOf(raw);
            Assert.InRange(position, 0, map.ExpandedCodes.Length - 1);
            var canonical = table.Occupations[(int)map.CanonicalIndices[position]].CanonicalCode;
            Assert.True(
                canonical == raw || reduction.Clusters.Any(cluster =>
                    ClusterReduction.EncodeOccupation([2, .. cluster.CanonicalTuple]) == canonical
                    && cluster.Expansions.Any(expansion =>
                        ClusterReduction.EncodeOccupation([2, .. ClusterReduction.DecodeOccupation(expansion.OccupationCode, 2)]) == raw)),
                $"raw code {raw} does not resolve through {canonical}");
        }

        var extras = map.ExpandedCodes.Where(code => !enumerated.Contains(code)).ToArray();
        Assert.NotEmpty(extras);
        Assert.Contains(extras, code => Field(code, slot: 1) == 3);
        foreach (var code in extras)
        {
            Assert.True((code >> 60) == 0);
            for (var slot = 0; slot < 4; slot++)
            {
                Assert.InRange(Field(code, slot), 0, 3);
            }
        }
    }

    private static (SpaceGroupSetting Setting, Fractional3 Centre, ImmutableArray<Fractional3> Sites) Geometry(
        Random random) =>
        random.Next(4) switch
        {
            0 => (p1, new Fractional3(0, 0, 0), [new Fractional3(0.2, 0, 0), new Fractional3(0.4, 0, 0)]),
            1 => (pMinus1, new Fractional3(0, 0, 0), [new Fractional3(0.25, 0, 0), new Fractional3(0.75, 0, 0)]),
            2 => (p2, new Fractional3(0, 0.2, 0), [new Fractional3(0.2, 0.1, 0.3), new Fractional3(0.8, 0.9, 0.7)]),
            _ => (p3, new Fractional3(0, 0, 0), [new Fractional3(0.2, 0.1, 0.3), new Fractional3(0.8, 0.9, 0.7)]),
        };

    private static ImmutableArray<int> AllowedSet(Random random) =>
        random.Next(4) switch
        {
            0 => [1, 2],
            1 => [2, 3],
            2 => [1, 2, 3],
            _ => [1],
        };

    private static CompiledClusterTable BuildTable(ClusterReductionResult reduction, int centreParticle)
    {
        var occupations = reduction.Clusters
            .Select(cluster =>
            {
                var canonicalTuple = ImmutableArray.CreateBuilder<int>(cluster.CanonicalTuple.Length + 1);
                canonicalTuple.Add(centreParticle);
                canonicalTuple.AddRange(cluster.CanonicalTuple);
                var fullCanonicalTuple = canonicalTuple.MoveToImmutable();
                var expansions = cluster.Expansions
                    .Select(expansion => new CompiledClusterExpansion(
                        ClusterReduction.EncodeOccupation(
                            [centreParticle, .. ClusterReduction.DecodeOccupation(expansion.OccupationCode, 2)]),
                        expansion.PermutationIndex))
                    .ToImmutableArray();
                return new CompiledClusterOccupation(
                    ClusterReduction.EncodeOccupation(fullCanonicalTuple),
                    fullCanonicalTuple,
                    expansions);
            })
            .OrderBy(static occupation => occupation.CanonicalCode)
            .ToImmutableArray();

        return new CompiledClusterTable(
            0,
            new string('a', 64),
            ClusterId.New(),
            0,
            0,
            [0, 1, 2],
            [Vector4I.Zero, Vector4I.Zero, Vector4I.Zero],
            occupations,
            [.. occupations.Select(static _ => 0.0)],
            [1, 2, 3]);
    }

    private static IEnumerable<int[]> EnumerateTuples(ImmutableArray<ImmutableArray<int>> allowed)
    {
        var lists = allowed.Select(static values => values.Distinct().Order().ToArray()).ToArray();
        var tuple = new int[lists.Length];
        return Enumerate(0);

        IEnumerable<int[]> Enumerate(int index)
        {
            if (index == tuple.Length)
            {
                yield return tuple.ToArray();
                yield break;
            }

            foreach (var particle in lists[index])
            {
                tuple[index] = particle;
                foreach (var value in Enumerate(index + 1))
                {
                    yield return value;
                }
            }
        }
    }

    private static Dictionary<ulong, HashSet<ulong>> Oracle(
        SpaceGroupSetting setting,
        Fractional3 centre,
        ImmutableArray<Fractional3> sites,
        ImmutableArray<ImmutableArray<int>> allowed)
    {
        var pointGroup = SpaceGroupOperations.GetPointGroup(setting, centre, sites);
        var groups = new Dictionary<ulong, HashSet<ulong>>();
        foreach (var tuple in EnumerateTuples(allowed))
        {
            var orbit = pointGroup.InducedPermutations
                .Select(permutation =>
                {
                    var permuted = new int[tuple.Length];
                    for (var site = 0; site < tuple.Length; site++)
                    {
                        permuted[site] = tuple[permutation[site]];
                    }

                    return permuted;
                })
                .OrderBy(static value => value, Comparer<int[]>.Create(Compare))
                .ToArray();
            var canonical = ClusterReduction.EncodeOccupation([.. orbit[0]]);
            if (!groups.TryGetValue(canonical, out var values))
            {
                values = [];
                groups.Add(canonical, values);
            }

            foreach (var member in orbit)
            {
                values.Add(ClusterReduction.EncodeOccupation([.. member]));
            }
        }

        return groups;
    }

    private static ulong FullCode(int centreParticle, ImmutableArray<int> members) =>
        ClusterReduction.EncodeOccupation([centreParticle, .. members]);

    private static int Field(ulong code, int slot) => (int)((code >> (6 * slot)) & 0x3F);

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
}
