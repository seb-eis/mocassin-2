// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Geometry;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Symmetry.Reduction;

/// <summary>
/// One equivalent occupation tuple in a cluster's expansion map.
/// </summary>
/// <param name="OccupationCode">The encoded occupation tuple.</param>
/// <param name="PermutationIndex">The induced-permutation index that produced it.</param>
public sealed record ClusterExpansion(ulong OccupationCode, int PermutationIndex);

/// <summary>
/// One symmetry-reduced cluster occupation.
/// </summary>
/// <param name="CanonicalCode">The encoded canonical tuple.</param>
/// <param name="CanonicalTuple">The lexicographically smallest tuple in site order.</param>
/// <param name="Expansions">Every equivalent tuple code and its permutation.</param>
public sealed record ClusterOccupation(
    ulong CanonicalCode,
    ImmutableArray<int> CanonicalTuple,
    ImmutableArray<ClusterExpansion> Expansions);

/// <summary>
/// The symmetry-reduced cluster occupations for a center and ordered cluster geometry.
/// </summary>
/// <param name="PointGroup">The exact point-group decomposition.</param>
/// <param name="Clusters">The canonical occupation clusters.</param>
public sealed record ClusterReductionResult(
    PointGroup PointGroup,
    ImmutableArray<ClusterOccupation> Clusters);

/// <summary>
/// Exact cluster occupation reduction and 6-bit occupation codes.
/// </summary>
public static class ClusterReduction
{
    /// <summary>
    /// Gets the maximum number of sites in one cluster.
    /// </summary>
    public const int MaxClusterSites = 9;

    /// <summary>
    /// Gets the number of bits used for one site occupation.
    /// </summary>
    public const int ParticleBits = 6;

    /// <summary>
    /// Gets the maximum encodable particle index.
    /// </summary>
    public const int MaxParticleIndex = (1 << ParticleBits) - 1;

    /// <summary>
    /// Gets the bound on enumerated occupation tuples before a bounded failure.
    /// </summary>
    public const int MaxTupleCount = 1_000_000;

    /// <summary>
    /// Encodes an occupation tuple as 6 bits per site, site 0 in the least significant bits.
    /// </summary>
    /// <param name="occupation">The particle indices per site.</param>
    /// <returns>The occupation code with unused high bits zero.</returns>
    /// <exception cref="ArgumentException">
    /// The tuple has more than 9 sites or contains an index outside 0..63.
    /// </exception>
    public static ulong EncodeOccupation(ImmutableArray<int> occupation)
    {
        if (occupation.IsDefault || occupation.Length > MaxClusterSites)
        {
            throw new ArgumentException("A cluster occupation must contain at most 9 sites.", nameof(occupation));
        }

        ulong code = 0;
        for (var site = 0; site < occupation.Length; site++)
        {
            var particle = occupation[site];
            if (particle is < 0 or > MaxParticleIndex)
            {
                throw new ArgumentException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Particle index {particle} at site {site} is outside 0..{MaxParticleIndex}."),
                    nameof(occupation));
            }

            code |= (ulong)particle << (site * ParticleBits);
        }

        return code;
    }

    /// <summary>
    /// Decodes an occupation code.
    /// </summary>
    /// <param name="code">The occupation code.</param>
    /// <param name="siteCount">The number of sites to decode.</param>
    /// <returns>The particle indices per site.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="siteCount"/> is outside 0..9 or the code has non-zero unused bits.
    /// </exception>
    public static ImmutableArray<int> DecodeOccupation(ulong code, int siteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(siteCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(siteCount, MaxClusterSites);

        if (code >> (siteCount * ParticleBits) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(code), "The occupation code has non-zero unused bits.");
        }

        var occupation = ImmutableArray.CreateBuilder<int>(siteCount);
        for (var site = 0; site < siteCount; site++)
        {
            occupation.Add((int)((code >> (site * ParticleBits)) & MaxParticleIndex));
        }

        return occupation.MoveToImmutable();
    }

    /// <summary>
    /// Reduces allowed occupation tuples into canonical clusters.
    /// </summary>
    /// <param name="setting">The resolved space-group setting.</param>
    /// <param name="center">The center site.</param>
    /// <param name="clusterSites">The ordered cluster site sequence.</param>
    /// <param name="allowedParticles">The allowed particle indices per site.</param>
    /// <returns>The reduced clusters and point group.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="setting"/> or <paramref name="allowedParticles"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">The input lengths or occupations are invalid.</exception>
    /// <exception cref="InvalidOperationException">The tuple enumeration exceeds the bound.</exception>
    public static ClusterReductionResult Reduce(
        SpaceGroupSetting setting,
        Fractional3 center,
        ImmutableArray<Fractional3> clusterSites,
        ImmutableArray<ImmutableArray<int>> allowedParticles)
    {
        ArgumentNullException.ThrowIfNull(setting);
        if (clusterSites.IsDefault || allowedParticles.IsDefault || clusterSites.Length != allowedParticles.Length)
        {
            throw new ArgumentException("Cluster sites and allowed particles must have the same length.");
        }

        if (clusterSites.Length > MaxClusterSites)
        {
            throw new ArgumentException("A cluster may contain at most 9 sites.", nameof(clusterSites));
        }

        var pointGroup = SpaceGroupOperations.GetPointGroup(setting, center, clusterSites);
        var allowed = allowedParticles
            .Select(static particles => particles
                .Distinct()
                .Order()
                .ToImmutableArray())
            .ToArray();
        if (allowed.Any(static particles => particles.Length == 0))
        {
            return new ClusterReductionResult(pointGroup, ImmutableArray<ClusterOccupation>.Empty);
        }

        var builders = new Dictionary<ulong, ClusterBuilder>();
        var tuple = new int[clusterSites.Length];
        var tupleCount = 0;

        void Visit(int site)
        {
            if (site == tuple.Length)
            {
                tupleCount++;
                if (tupleCount > MaxTupleCount)
                {
                    throw new InvalidOperationException(
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"The cluster exceeds the bound of {MaxTupleCount} occupation tuples."));
                }

                AddTuple(builders, pointGroup, tuple);
                return;
            }

            foreach (var particle in allowed[site])
            {
                tuple[site] = particle;
                Visit(site + 1);
            }
        }

        Visit(0);

        var clusters = builders.Values
            .Select(static builder => builder.ToCluster())
            .OrderBy(static cluster => cluster.CanonicalCode)
            .ToImmutableArray();
        return new ClusterReductionResult(pointGroup, clusters);
    }

    private static void AddTuple(
        Dictionary<ulong, ClusterBuilder> builders,
        PointGroup pointGroup,
        int[] tuple)
    {
        var canonical = tuple;
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

        var canonicalCode = EncodeOccupation([.. canonical]);
        if (!builders.TryGetValue(canonicalCode, out var builder))
        {
            builder = new ClusterBuilder(canonicalCode, [.. canonical]);
            builders.Add(canonicalCode, builder);
        }

        for (var index = 0; index < pointGroup.InducedPermutations.Length; index++)
        {
            var permutation = pointGroup.InducedPermutations[index];
            var permuted = new int[tuple.Length];
            for (var site = 0; site < tuple.Length; site++)
            {
                permuted[site] = canonical[permutation[site]];
            }

            builder.Add(new ClusterExpansion(EncodeOccupation([.. permuted]), index));
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

    private sealed class ClusterBuilder(ulong canonicalCode, ImmutableArray<int> canonicalTuple)
    {
        private readonly HashSet<ulong> codes = [];

        public List<ClusterExpansion> Expansions { get; } = [];

        public void Add(ClusterExpansion expansion)
        {
            if (codes.Add(expansion.OccupationCode))
            {
                Expansions.Add(expansion);
            }
        }

        public ClusterOccupation ToCluster() =>
            new(canonicalCode, canonicalTuple, [.. Expansions.OrderBy(static expansion => expansion.OccupationCode)]);
    }
}
