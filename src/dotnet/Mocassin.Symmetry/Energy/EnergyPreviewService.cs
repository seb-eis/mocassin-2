// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Caching;
using Mocassin.Symmetry.Contracts;
using Mocassin.Symmetry.Reduction;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Symmetry.Energy;

/// <summary>
/// Cached energy cost and pair-geometry preview service.
/// </summary>
/// <remarks>
/// The cache key contains the canonical energy and structure hashes, the requested cell counts, and the catalog
/// and symmetry contract versions. Invalid user data returns <see langword="null"/> with diagnostics and is not
/// cached.
/// </remarks>
public sealed class EnergyPreviewService : IEnergyPreviewService
{
    private const long bytesPerEnergyEntry = sizeof(double);
    private readonly SpaceGroupCatalog catalog;
    private readonly SymmetryVersionInfo versions;
    private readonly SymmetryCache cache;
    private readonly EnergyGeometrySampler sampler;

    /// <summary>
    /// Initializes an energy-preview service.
    /// </summary>
    /// <param name="catalog">The optional catalog override. The default is <see cref="SpaceGroupCatalog.Default"/>.</param>
    /// <param name="versions">The optional version override used by tests and version-injection scenarios.</param>
    /// <param name="cacheCapacity">The maximum number of cached previews.</param>
    public EnergyPreviewService(
        SpaceGroupCatalog? catalog = null,
        SymmetryVersionInfo? versions = null,
        int cacheCapacity = 256)
    {
        var resolvedCatalog = catalog ?? SpaceGroupCatalog.Default;
        this.catalog = resolvedCatalog;
        this.versions = versions ?? SymmetryVersionInfo.FromCatalog(resolvedCatalog);
        cache = new SymmetryCache(
            this.versions.SpaceGroupDataVersion,
            this.versions.SymmetryContractVersion,
            cacheCapacity);
        sampler = new EnergyGeometrySampler(resolvedCatalog);
    }

    /// <summary>
    /// Gets the preview cache. Intended for diagnostics and tests.
    /// </summary>
    internal SymmetryCache Cache => cache;

    /// <inheritdoc />
    public EnergyCostPreview? TryCreatePreview(
        ModelDefinition model,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.StableEnvironment is null)
        {
            diagnostics = [NullStableEnvironment()];
            return null;
        }

        if (!AreCellCountsPositive(cellCountA, cellCountB, cellCountC, out diagnostics))
        {
            return null;
        }

        var key = CreateCacheKey("cost", model, cellCountA, cellCountB, cellCountC);
        if (cache.TryGet<CostCacheEntry>(key, out var cached) && cached is not null)
        {
            diagnostics = cached.Diagnostics;
            return cached.Preview;
        }

        var validation = Validate(model);
        if (validation.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            diagnostics = validation;
            return null;
        }

        if (!EnergyGeometryContext.TryCreate(model, catalog, out var context, out var contextDiagnostics) ||
            context is null)
        {
            diagnostics = [.. validation, .. contextDiagnostics];
            return null;
        }

        if (!sampler.TrySamplePairs(
                model,
                cellCountA,
                cellCountB,
                cellCountC,
                out var pairs,
                out var samplerDiagnostics))
        {
            diagnostics = [.. validation, .. samplerDiagnostics];
            return null;
        }

        if (!TryCreateClusterCosts(context, out var clusterCosts, out var clusterDiagnostics))
        {
            diagnostics = [.. validation, .. clusterDiagnostics];
            return null;
        }

        var stableP1PositionCount = context.Sites
            .Where(static site => site.Stability == SiteStability.Stable)
            .Sum(site => (long)context.GetSitePIndices(site.Id).Length);
        var pairImageCount = CountPairImages(context);
        var uniquePairInteractionCount = pairs
            .Select(static pair => pair.EnergyId)
            .Distinct(StringComparer.Ordinal)
            .LongCount();
        var particleCount = (model.Particles.IsDefault ? 0 : model.Particles.Length) + 1L;
        var pairBytes = SaturatingMultiply(
            SaturatingMultiply(uniquePairInteractionCount, particleCount),
            SaturatingMultiply(particleCount, bytesPerEnergyEntry));
        var clusterBytes = clusterCosts.Aggregate(
            0L,
            static (total, cluster) => SaturatingAdd(total, cluster.EstimatedBytes));
        var preview = new EnergyCostPreview(
            stableP1PositionCount,
            pairImageCount,
            uniquePairInteractionCount,
            clusterCosts,
            SaturatingAdd(pairBytes, clusterBytes));
        diagnostics = validation;
        var entry = new CostCacheEntry(preview, diagnostics);
        var stored = cache.GetOrAdd(key, () => entry);
        diagnostics = stored.Diagnostics;
        return stored.Preview;
    }

    /// <inheritdoc />
    public ImmutableArray<PairInteractionPreview>? TryCreatePairPreview(
        ModelDefinition model,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.StableEnvironment is null)
        {
            diagnostics = [NullStableEnvironment()];
            return null;
        }

        if (!AreCellCountsPositive(cellCountA, cellCountB, cellCountC, out diagnostics))
        {
            return null;
        }

        var key = CreateCacheKey("pairs", model, cellCountA, cellCountB, cellCountC);
        if (cache.TryGet<PairCacheEntry>(key, out var cached) && cached is not null)
        {
            diagnostics = cached.Diagnostics;
            return cached.Pairs;
        }

        var validation = Validate(model);
        if (validation.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            diagnostics = validation;
            return null;
        }

        if (!sampler.TrySamplePairs(
                model,
                cellCountA,
                cellCountB,
                cellCountC,
                out var pairs,
                out var samplerDiagnostics))
        {
            diagnostics = [.. validation, .. samplerDiagnostics];
            return null;
        }

        diagnostics = validation;
        var entry = new PairCacheEntry(pairs, diagnostics);
        var stored = cache.GetOrAdd(key, () => entry);
        diagnostics = stored.Diagnostics;
        return stored.Pairs;
    }

    private ImmutableArray<Diagnostic> Validate(ModelDefinition model) =>
    [
        .. EnergyModelValidator.Validate(model),
        .. new EnergyGeometryValidator(catalog).Validate(model),
    ];

    private static Diagnostic NullStableEnvironment() =>
        new(
            EnergyDiagnosticCodes.InvalidCutoff,
            DiagnosticSeverity.Error,
            "A stable environment is required.",
            new ModelPath("stableEnvironment", Guid.Empty, "cutoff"),
            EnergyDiagnosticCodes.HelpFor(EnergyDiagnosticCodes.InvalidCutoff));

    private static long CountPairImages(EnergyGeometryContext context)
    {
        var stable = context.Model.StableEnvironment ?? StableEnvironment.Default;
        var stableCenters = context.Sites
            .Where(static site => site.Stability == SiteStability.Stable)
            .SelectMany(site => context.GetSitePIndices(site.Id));
        var count = context.CountPairImages(stableCenters, stable.Cutoff, stable.Filters, symmetric: true);

        var unstableEnvironments = context.GetEffectiveUnstableEnvironments();
        foreach (var environment in unstableEnvironments)
        {
            if (!context.TryGetSite(environment.SiteId, out var site) ||
                site.Stability != SiteStability.Unstable)
            {
                continue;
            }

            count = SaturatingAdd(
                count,
                context.CountPairImages(
                    context.GetSitePIndices(site.Id),
                    environment.Cutoff,
                    environment.Filters,
                    symmetric: false));
        }

        return count;
    }

    private static bool TryCreateClusterCosts(
        EnergyGeometryContext context,
        out ImmutableArray<ClusterCostPreview> costs,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        var bag = new DiagnosticBag();
        var clusters = context.Model.Clusters.IsDefault ? [] : context.Model.Clusters;
        var builder = ImmutableArray.CreateBuilder<ClusterCostPreview>(clusters.Length);
        foreach (var cluster in clusters)
        {
            if (!context.TryGetSite(cluster.CenterSiteId, out var center) ||
                !context.TryGetCenterP(center, out var centerP))
            {
                bag.Add(new Diagnostic(
                    EnergyDiagnosticCodes.InvalidClusterCenter,
                    DiagnosticSeverity.Error,
                    $"Cluster '{cluster.Id:D}' centre cannot be resolved.",
                    new ModelPath("cluster", cluster.Id.Value, "centerSiteId"),
                    EnergyDiagnosticCodes.HelpFor(EnergyDiagnosticCodes.InvalidClusterCenter)));
                continue;
            }

            var offsets = cluster.OffsetVectors.IsDefault ? [] : cluster.OffsetVectors;
            var geometry = ImmutableArray.CreateBuilder<Fractional3>(offsets.Length + 1);
            var allowed = ImmutableArray.CreateBuilder<ImmutableArray<ParticleId>>(offsets.Length + 1);
            geometry.Add(center.Fractional.TrimToUnitCell());
            allowed.Add(GetParticleIds(context.Model, center));
            var valid = true;
            foreach (var offset in offsets)
            {
                if (!context.TryResolveOffset(center, offset, out _, out var targetP))
                {
                    valid = false;
                    break;
                }

                var target = context.GetSiteAt(targetP);
                geometry.Add(context.Encoder.Positions[targetP]);
                allowed.Add(GetParticleIds(context.Model, target));
            }

            if (!valid)
            {
                bag.Add(new Diagnostic(
                    EnergyDiagnosticCodes.InvalidClusterPosition,
                    DiagnosticSeverity.Error,
                    $"Cluster '{cluster.Id:D}' contains an unresolved surrounding position.",
                    new ModelPath("cluster", cluster.Id.Value, "offsetVectors"),
                    EnergyDiagnosticCodes.HelpFor(EnergyDiagnosticCodes.InvalidClusterPosition)));
                continue;
            }

            var raw = GetParticleCount(context.Model, center);
            foreach (var targetP in offsets
                         .Select(offset => context.TryResolveOffset(center, offset, out _, out var p) ? p : -1)
                         .Where(static p => p >= 0))
            {
                raw = SaturatingMultiply(raw, GetParticleCount(context.Model, context.GetSiteAt(targetP)));
            }

            PointGroup pointGroup;
            try
            {
                pointGroup = SpaceGroupOperations.GetPointGroup(
                    context.Setting,
                    center.Fractional.TrimToUnitCell(),
                    geometry.MoveToImmutable());
            }
            catch (ArgumentException exception)
            {
                bag.Add(new Diagnostic(
                    EnergyDiagnosticCodes.DuplicateClusterGeometry,
                    DiagnosticSeverity.Error,
                    $"Cluster '{cluster.Id:D}' geometry is invalid: {exception.Message}",
                    new ModelPath("cluster", cluster.Id.Value, "offsetVectors"),
                    EnergyDiagnosticCodes.HelpFor(EnergyDiagnosticCodes.DuplicateClusterGeometry)));
                continue;
            }

            var unique = BurnsideCount(pointGroup.InducedPermutations, allowed.MoveToImmutable());
            builder.Add(new ClusterCostPreview(
                cluster.Id,
                raw,
                unique,
                SaturatingMultiply(unique, bytesPerEnergyEntry)));
        }

        costs = builder.MoveToImmutable();
        diagnostics = bag.Sorted;
        return !bag.HasErrors;
    }

    private static long BurnsideCount(
        ImmutableArray<SitePermutation> permutations,
        ImmutableArray<ImmutableArray<ParticleId>> allowedParticles)
    {
        if (permutations.IsDefaultOrEmpty)
        {
            return 0;
        }

        var sum = 0L;
        foreach (var permutation in permutations)
        {
            sum = SaturatingAdd(sum, FixedCount(permutation, allowedParticles));
        }

        return sum / permutations.Length;
    }

    private static long FixedCount(
        SitePermutation permutation,
        ImmutableArray<ImmutableArray<ParticleId>> allowedParticles)
    {
        var visited = new bool[permutation.Count];
        var count = 1L;
        for (var start = 0; start < permutation.Count; start++)
        {
            if (visited[start])
            {
                continue;
            }

            HashSet<ParticleId>? intersection = null;
            var index = start;
            do
            {
                visited[index] = true;
                var allowed = allowedParticles[index].ToHashSet();
                if (intersection is null)
                {
                    intersection = allowed;
                }
                else
                {
                    intersection.IntersectWith(allowed);
                }

                index = permutation[index];
            }
            while (index != start);

            var choices = intersection?.Count ?? 0;
            count = SaturatingMultiply(count, choices);
        }

        return count;
    }

    private static ImmutableArray<ParticleId> GetParticleIds(ModelDefinition model, CellSite site)
    {
        if (model.ParticleSets.IsDefault)
        {
            return [];
        }

        foreach (var particleSet in model.ParticleSets)
        {
            if (particleSet.Id == site.ParticleSetId)
            {
                return particleSet.ParticleIds.IsDefault ? [] : particleSet.ParticleIds;
            }
        }

        return [];
    }

    private static long GetParticleCount(ModelDefinition model, CellSite site) =>
        GetParticleIds(model, site).Length;

    private static bool AreCellCountsPositive(
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        if (cellCountA > 0 && cellCountB > 0 && cellCountC > 0)
        {
            diagnostics = [];
            return true;
        }

        diagnostics =
        [
            new Diagnostic(
                ProjectDiagnosticCodes.InvalidDocument,
                DiagnosticSeverity.Error,
                "Energy preview requires positive cell counts.",
                new ModelPath("model", Guid.Empty, "cellCounts")),
        ];
        return false;
    }

    private string CreateCacheKey(
        string kind,
        ModelDefinition model,
        int cellCountA,
        int cellCountB,
        int cellCountC) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"energy-{kind}|{versions.SpaceGroupDataVersion}|{versions.SpaceGroupDataSha256}|" +
            $"{versions.SymmetryContractVersion}|{ProjectHasher.ComputeEnergy(model)}|" +
            $"{ProjectHasher.ComputeStructure(model.Structure)}|{cellCountA}|{cellCountB}|{cellCountC}");

    private static long SaturatingAdd(long left, long right)
    {
        if (left > long.MaxValue - right)
        {
            return long.MaxValue;
        }

        return left + right;
    }

    private static long SaturatingMultiply(long left, long right)
    {
        if (left == 0 || right == 0)
        {
            return 0;
        }

        return left > long.MaxValue / right ? long.MaxValue : left * right;
    }

    private sealed record CostCacheEntry(
        EnergyCostPreview Preview,
        ImmutableArray<Diagnostic> Diagnostics);

    private sealed record PairCacheEntry(
        ImmutableArray<PairInteractionPreview> Pairs,
        ImmutableArray<Diagnostic> Diagnostics);
}
