// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Caching;
using Mocassin.Symmetry.Contracts;
using Mocassin.Symmetry.Geometry;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Symmetry.Transitions;

/// <summary>
/// Builds cached, deterministic transition previews from a project query.
/// </summary>
/// <remarks>
/// The preview cache is scoped to the service, bounded, and thread-safe. Its key includes the transition
/// closure hash, the structure hash, the requested cell counts, the transition reference, and the catalog and
/// symmetry-contract versions.
/// </remarks>
public sealed class TransitionPreviewService : ITransitionPreviewService
{
    private readonly SpaceGroupCatalog catalog;
    private readonly SymmetryVersionInfo versions;
    private readonly SymmetryCache cache;
    private readonly TransitionPathValidator pathValidator;

    /// <summary>
    /// Initializes a transition-preview service.
    /// </summary>
    /// <param name="catalog">The optional catalog override.</param>
    /// <param name="versions">The optional version override used by tests and version-injection scenarios.</param>
    /// <param name="cacheCapacity">The maximum number of cached previews.</param>
    public TransitionPreviewService(
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
        pathValidator = new TransitionPathValidator(resolvedCatalog);
    }

    /// <summary>
    /// Gets the preview cache. Intended for diagnostics and tests.
    /// </summary>
    internal SymmetryCache Cache => cache;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">A cell count is non-positive.</exception>
    public TransitionPreview? TryCreatePreview(
        KineticTransition transition,
        IProjectQuery project,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(transition);
        ArgumentNullException.ThrowIfNull(project);
        ValidateCellCounts(cellCountA, cellCountB, cellCountC);

        var reference = TransitionRef.From(transition.Id);
        var cacheKey = CreateCacheKey(project, reference, cellCountA, cellCountB, cellCountC);
        if (cache.TryGet<PreviewCacheEntry>(cacheKey, out var cached) && cached is not null)
        {
            diagnostics = cached.Diagnostics;
            return cached.Preview;
        }

        var bag = new DiagnosticBag();
        if (!TryGetChain(project, transition.ChainId, kinetic: true, bag, out var chain))
        {
            return CacheResult(cacheKey, null, bag, out diagnostics);
        }

        var reduced = BuildReducedModel(project, kinetic: transition, metropolis: null);
        bag.AddRange(pathValidator.Validate(reduced));
        if (bag.HasErrors)
        {
            return CacheResult(cacheKey, null, bag, out diagnostics);
        }

        if (!TransitionPathResolver.TryCreate(project.Structure, catalog, out var resolver, out _) || resolver is null)
        {
            bag.Add(Error(
                TransitionDiagnosticCodes.UnmatchedPathPosition,
                "The transition path cannot be resolved against the structure.",
                "kineticTransition",
                transition.Id.Value));
            return CacheResult(cacheKey, null, bag, out diagnostics);
        }

        var resolved = resolver.TryResolve(transition, out var pathDiagnostics);
        bag.AddRange(pathDiagnostics);
        if (resolved is null)
        {
            return CacheResult(cacheKey, null, bag, out diagnostics);
        }

        if (!TryBuildMatrices(project, chain, resolved.Positions, bag, out var matrices))
        {
            return CacheResult(cacheKey, null, bag, out diagnostics);
        }

        var expandedPaths = ExpandPath(resolver.Encoder.Setting, transition.ReferencePath);
        var preview = new TransitionPreview(
            reference,
            chain,
            GetParticleOrder(project),
            transition.ReferencePath.IsDefault ? [] : transition.ReferencePath,
            resolved.Positions,
            expandedPaths,
            matrices,
            ComputeExpectedRuleCount(project, chain, expandedPaths.Length),
            cellCountA,
            cellCountB,
            cellCountC);
        return CacheResult(cacheKey, preview, bag, out diagnostics);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">A cell count is non-positive.</exception>
    public TransitionPreview? TryCreatePreview(
        MetropolisTransition transition,
        IProjectQuery project,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(transition);
        ArgumentNullException.ThrowIfNull(project);
        ValidateCellCounts(cellCountA, cellCountB, cellCountC);

        var reference = TransitionRef.From(transition.Id);
        var cacheKey = CreateCacheKey(project, reference, cellCountA, cellCountB, cellCountC);
        if (cache.TryGet<PreviewCacheEntry>(cacheKey, out var cached) && cached is not null)
        {
            diagnostics = cached.Diagnostics;
            return cached.Preview;
        }

        var bag = new DiagnosticBag();
        if (!TryGetChain(project, transition.ChainId, kinetic: false, bag, out var chain))
        {
            return CacheResult(cacheKey, null, bag, out diagnostics);
        }

        var reduced = BuildReducedModel(project, kinetic: null, metropolis: transition);
        bag.AddRange(pathValidator.Validate(reduced));
        if (bag.HasErrors)
        {
            return CacheResult(cacheKey, null, bag, out diagnostics);
        }

        if (!TransitionPathResolver.TryCreate(project.Structure, catalog, out var resolver, out _) || resolver is null)
        {
            bag.Add(Error(
                TransitionDiagnosticCodes.InvalidMetropolisSite,
                "The Metropolis sites cannot be resolved against the structure.",
                "metropolisTransition",
                transition.Id.Value));
            return CacheResult(cacheKey, null, bag, out diagnostics);
        }

        var siteA = project.Find(transition.SiteAId);
        var siteB = project.Find(transition.SiteBId);
        if (siteA is null || siteB is null)
        {
            bag.Add(Error(
                TransitionDiagnosticCodes.InvalidMetropolisSite,
                "The Metropolis transition references an unknown site.",
                "metropolisTransition",
                transition.Id.Value));
            return CacheResult(cacheKey, null, bag, out diagnostics);
        }

        if (!TryResolveSite(resolver.Encoder, siteA, out var resolvedA) ||
            !TryResolveSite(resolver.Encoder, siteB, out var resolvedB))
        {
            bag.Add(Error(
                TransitionDiagnosticCodes.UnmatchedPathPosition,
                "A Metropolis site does not match a canonical P1 position.",
                "metropolisTransition",
                transition.Id.Value));
            return CacheResult(cacheKey, null, bag, out diagnostics);
        }

        var resolvedPositions = ImmutableArray.Create(resolvedA!, resolvedB!);
        if (!TryBuildMatrices(project, chain, resolvedPositions, bag, out var matrices))
        {
            return CacheResult(cacheKey, null, bag, out diagnostics);
        }

        var referencePath = ImmutableArray.Create(siteA.Fractional, siteB.Fractional);
        var expandedSites = ExpandSites(resolver.Encoder.Setting, siteA, siteB);
        var preview = new TransitionPreview(
            reference,
            chain,
            GetParticleOrder(project),
            referencePath,
            resolvedPositions,
            expandedSites,
            matrices,
            ComputeExpectedRuleCount(project, chain, expandedSites.Length),
            cellCountA,
            cellCountB,
            cellCountC);
        return CacheResult(cacheKey, preview, bag, out diagnostics);
    }

    private static bool TryGetChain(
        IProjectQuery project,
        TransitionChainId chainId,
        bool kinetic,
        DiagnosticBag bag,
        out TransitionChain chain)
    {
        var found = project.Find(chainId);
        if (found is not null)
        {
            chain = found;
            return true;
        }

        chain = null!;
        bag.Add(Error(
            kinetic
                ? TransitionDiagnosticCodes.KineticTransitionUsesMetropolisChain
                : TransitionDiagnosticCodes.InvalidMetropolisChain,
            $"Transition references unknown chain '{chainId:D}'.",
            kinetic ? "kineticTransition" : "metropolisTransition",
            Guid.Empty,
            "chainId"));
        return false;
    }

    private static ModelDefinition BuildReducedModel(
        IProjectQuery project,
        KineticTransition? kinetic,
        MetropolisTransition? metropolis) =>
        new()
        {
            Particles = project.Particles,
            Structure = project.Structure,
            StateChanges = project.StateChanges,
            StateChangeGroups = project.StateChangeGroups,
            Chains = project.Chains,
            KineticTransitions = kinetic is null ? [] : [kinetic],
            MetropolisTransitions = metropolis is null ? [] : [metropolis],
        };

    private static bool TryBuildMatrices(
        IProjectQuery project,
        TransitionChain chain,
        ImmutableArray<ResolvedPathPosition> resolvedPositions,
        DiagnosticBag bag,
        out ImmutableArray<ImmutableArray<int>> matrices)
    {
        matrices = [];
        var particleOrder = GetParticleOrder(project);
        var particleIndex = new Dictionary<ParticleId, int>();
        for (var index = 0; index < particleOrder.Length; index++)
        {
            particleIndex[particleOrder[index]] = index;
        }

        var groupIds = chain.GroupIds.IsDefault ? ImmutableArray<StateChangeGroupId>.Empty : chain.GroupIds;
        if (groupIds.Length == 0)
        {
            bag.Add(Error(
                TransitionDiagnosticCodes.InvalidPatternPositionCount,
                "Transition chain has no positions.",
                "chain",
                chain.Id.Value,
                "groupIds"));
            return false;
        }

        var builders = ImmutableArray.CreateBuilder<ImmutableArray<int>>(groupIds.Length);
        for (var position = 0; position < groupIds.Length; position++)
        {
            var group = project.Find(groupIds[position]);
            if (group is null)
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.UnknownChainGroup,
                    $"Transition chain references unknown group '{groupIds[position]:D}'.",
                    "chain",
                    chain.Id.Value,
                    "groupIds"));
                return false;
            }

            var matrix = new int[particleOrder.Length * particleOrder.Length];
            foreach (var stateChangeId in group.StateChangeIds.IsDefault ? [] : group.StateChangeIds)
            {
                var stateChange = project.Find(stateChangeId);
                if (stateChange is null)
                {
                    bag.Add(Error(
                        TransitionDiagnosticCodes.InvalidGroup,
                        $"State-change group references unknown state change '{stateChangeId:D}'.",
                        "stateChangeGroup",
                        group.Id.Value,
                        "stateChangeIds"));
                    return false;
                }

                if (!particleIndex.TryGetValue(stateChange.DonorParticleId, out var donorIndex) ||
                    !particleIndex.TryGetValue(stateChange.AcceptorParticleId, out var acceptorIndex))
                {
                    bag.Add(Error(
                        TransitionDiagnosticCodes.UnknownParticle,
                        "State change references an unknown particle.",
                        "stateChange",
                        stateChange.Id.Value));
                    return false;
                }

                matrix[(donorIndex * particleOrder.Length) + acceptorIndex] = 1;
            }

            if (position < resolvedPositions.Length)
            {
                var stability = resolvedPositions[position].Stability;
                var occupied = matrix.Count(static value => value != 0);
                if (stability == SiteStability.Unstable && occupied != 1)
                {
                    bag.Add(Error(
                        TransitionDiagnosticCodes.InvalidUnstablePositionGroup,
                        $"Unstable position {position} must have exactly one donor/acceptor entry.",
                        "chain",
                        chain.Id.Value,
                        "groupIds"));
                    return false;
                }

                if (stability == SiteStability.Stable && occupied < 1)
                {
                    bag.Add(Error(
                        TransitionDiagnosticCodes.GroupStabilityMismatch,
                        $"Stable position {position} must have at least one donor/acceptor entry.",
                        "chain",
                        chain.Id.Value,
                        "groupIds"));
                    return false;
                }
            }

            builders.Add([.. matrix]);
        }

        matrices = builders.ToImmutable();
        return true;
    }

    private static ImmutableArray<ParticleId> GetParticleOrder(IProjectQuery project) =>
    [
        .. project.Particles.OrderBy(static particle => particle.Id.Value).Select(static particle => particle.Id),
        project.VoidParticle.Id,
    ];

    private static int ComputeExpectedRuleCount(IProjectQuery project, TransitionChain chain, int expandedCount)
    {
        long count = 1;
        foreach (var groupId in chain.GroupIds.IsDefault ? [] : chain.GroupIds)
        {
            var group = project.Find(groupId);
            var stateChanges = group is null || group.StateChangeIds.IsDefault ? 0 : group.StateChangeIds.Length;
            if (stateChanges == 0)
            {
                return 0;
            }

            count *= stateChanges;
            if (count > int.MaxValue)
            {
                break;
            }
        }

        count *= expandedCount;
        return count > int.MaxValue ? int.MaxValue : (int)count;
    }

    private static ImmutableArray<ImmutableArray<Fractional3>> ExpandPath(
        SpaceGroupSetting setting,
        ImmutableArray<Fractional3> path)
    {
        var points = path.IsDefault ? ImmutableArray<Fractional3>.Empty : path;
        var result = ImmutableArray.CreateBuilder<ImmutableArray<Fractional3>>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var operation in setting.Operations)
        {
            var expanded = points
                .Select(point => operation.Transform(point).TrimToUnitCell())
                .ToImmutableArray();
            var key = string.Join('|', expanded.Select(static point => point.ToString()));
            if (seen.Add(key))
            {
                result.Add(expanded);
            }
        }

        return result.ToImmutable();
    }

    private static ImmutableArray<ImmutableArray<Fractional3>> ExpandSites(
        SpaceGroupSetting setting,
        CellSite siteA,
        CellSite siteB)
    {
        var result = ImmutableArray.CreateBuilder<ImmutableArray<Fractional3>>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var operation in setting.Operations)
        {
            var first = operation.Transform(siteA.Fractional).TrimToUnitCell();
            var second = operation.Transform(siteB.Fractional).TrimToUnitCell();
            var key = first + "|" + second;
            if (seen.Add(key))
            {
                result.Add([first, second]);
            }
        }

        return result.ToImmutable();
    }

    private static bool TryResolveSite(
        UnitCellVectorEncoder encoder,
        CellSite site,
        out ResolvedPathPosition? position)
    {
        if (!encoder.TryIndexOf(site.Fractional, out var p))
        {
            position = null;
            return false;
        }

        position = new ResolvedPathPosition(
            site.Id,
            p,
            new Vector4I(0, 0, 0, p),
            site.Stability,
            encoder.Positions[p]);
        return true;
    }

    private string CreateCacheKey(
        IProjectQuery project,
        TransitionRef reference,
        int cellCountA,
        int cellCountB,
        int cellCountC) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"transition-preview|{versions.SpaceGroupDataVersion}|{versions.SpaceGroupDataSha256}|" +
            $"{versions.SymmetryContractVersion}|{reference.Kind}|{reference.Id:D}|" +
            $"{ProjectHasher.ComputeTransition(project, reference)}|{ProjectHasher.ComputeStructure(project.Structure)}|" +
            $"{cellCountA}|{cellCountB}|{cellCountC}");

    private TransitionPreview? CacheResult(
        string cacheKey,
        TransitionPreview? preview,
        DiagnosticBag bag,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        var entry = new PreviewCacheEntry(preview, bag.Sorted);
        var stored = cache.GetOrAdd(cacheKey, () => entry);
        diagnostics = stored.Diagnostics;
        return stored.Preview;
    }

    private static void ValidateCellCounts(int cellCountA, int cellCountB, int cellCountC)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCountA);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCountB);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCountC);
    }

    private static Diagnostic Error(
        string code,
        string message,
        string domain,
        Guid objectId,
        string? property = null) =>
        new(code, DiagnosticSeverity.Error, message, new ModelPath(domain, objectId, property));

    private sealed record PreviewCacheEntry(
        TransitionPreview? Preview,
        ImmutableArray<Diagnostic> Diagnostics);
}
