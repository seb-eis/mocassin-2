// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Structure;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Symmetry.Energy;

/// <summary>
/// Validates the catalog, cell-geometry, periodic-encoding, and sampling energy rules.
/// </summary>
/// <remarks>
/// Core owns the definition-level rules; this validator adds <c>ENM002</c>, <c>ENM009</c>–<c>ENM013</c>,
/// <c>ENM017</c>, and <c>ENM019</c>. Invalid structures return an empty result because the structure validator
/// already reports the underlying catalog or encoder failure.
/// </remarks>
public sealed class EnergyGeometryValidator
{
    private readonly SpaceGroupCatalog catalog;

    /// <summary>
    /// Initializes an energy geometry validator.
    /// </summary>
    /// <param name="catalog">The optional catalog override. The default is <see cref="SpaceGroupCatalog.Default"/>.</param>
    public EnergyGeometryValidator(SpaceGroupCatalog? catalog = null)
    {
        this.catalog = catalog ?? SpaceGroupCatalog.Default;
    }

    /// <summary>
    /// Validates the model's energy geometry and sampling budgets.
    /// </summary>
    /// <param name="model">The model to validate.</param>
    /// <returns>The diagnostics in stable order.</returns>
    public ImmutableArray<Diagnostic> Validate(ModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!EnergyGeometryContext.TryCreate(model, catalog, out var context, out _) || context is null)
        {
            return [];
        }

        var bag = new DiagnosticBag();
        var sites = context.Sites;
        if (sites.Length > 0)
        {
            ValidatePeriodicAmbiguity(context, bag);
            ValidateSampling(context, bag);
        }

        ValidateClusters(context, bag);
        return bag.Sorted;
    }

    private static void ValidatePeriodicAmbiguity(
        EnergyGeometryContext context,
        DiagnosticBag bag)
    {
        var halfMinimum = Math.Min(
            Math.Min(context.Cell.A.Length, context.Cell.B.Length),
            context.Cell.C.Length) / 2.0;
        var environment = context.Model.StableEnvironment ?? StableEnvironment.Default;
        if (environment.Cutoff.Value > halfMinimum + GeometryTolerances.DistanceEqualityTolerance)
        {
            bag.Add(Warning(
                EnergyDiagnosticCodes.PeriodicAmbiguity,
                $"Stable environment cutoff {environment.Cutoff.Value} Å exceeds half the smallest cell " +
                $"dimension ({halfMinimum} Å).",
                "stableEnvironment",
                Guid.Empty,
                "cutoff"));
        }

        var unstableEnvironments = context.Model.UnstableEnvironments.IsDefault
            ? []
            : context.Model.UnstableEnvironments;
        foreach (var unstable in unstableEnvironments)
        {
            if (unstable.Cutoff.Value > halfMinimum + GeometryTolerances.DistanceEqualityTolerance)
            {
                bag.Add(Warning(
                    EnergyDiagnosticCodes.PeriodicAmbiguity,
                    $"Unstable environment cutoff {unstable.Cutoff.Value} Å exceeds half the smallest cell " +
                    $"dimension ({halfMinimum} Å).",
                    "unstableEnvironment",
                    unstable.SiteId.Value,
                    "cutoff"));
            }
        }
    }

    private static void ValidateSampling(EnergyGeometryContext context, DiagnosticBag bag)
    {
        var stable = context.Model.StableEnvironment ?? StableEnvironment.Default;
        var stableCenters = context.Sites
            .Where(static site => site.Stability == SiteStability.Stable)
            .SelectMany(site => context.GetSitePIndices(site.Id))
            .ToImmutableArray();
        var stableCount = context.CountPairImages(
            stableCenters,
            stable.Cutoff,
            stable.Filters,
            symmetric: true);
        AddSamplingDiagnostics(
            stableCount,
            EnergyLimits.StablePositionsWarning,
            EnergyLimits.StablePositionsHard,
            "stableEnvironment",
            Guid.Empty,
            bag);

        var unstableEnvironments = context.GetEffectiveUnstableEnvironments();
        foreach (var unstable in unstableEnvironments)
        {
            if (!context.TryGetSite(unstable.SiteId, out var site) ||
                site.Stability != SiteStability.Unstable)
            {
                continue;
            }

            var count = context.CountPairImages(
                context.GetSitePIndices(site.Id),
                unstable.Cutoff,
                unstable.Filters,
                symmetric: false);
            AddSamplingDiagnostics(
                count,
                EnergyLimits.UnstablePositionsWarning,
                EnergyLimits.UnstablePositionsHard,
                "unstableEnvironment",
                unstable.SiteId.Value,
                bag);
        }
    }

    private static void AddSamplingDiagnostics(
        long count,
        int warningLimit,
        int hardLimit,
        string domain,
        Guid objectId,
        DiagnosticBag bag)
    {
        if (count < 1 || count > warningLimit)
        {
            bag.Add(Warning(
                EnergyDiagnosticCodes.EnvironmentSamplingWarning,
                count < 1
                    ? "Environment samples no pair positions and defines no interaction."
                    : $"Environment samples {count} pair positions; more than {warningLimit} is discouraged.",
                domain,
                objectId,
                "cutoff"));
        }

        if (count > hardLimit)
        {
            bag.Add(Error(
                EnergyDiagnosticCodes.EnvironmentSamplingLimit,
                $"Environment samples {count} pair positions; the hard limit is {hardLimit}.",
                domain,
                objectId,
                "cutoff"));
        }
    }

    private static void ValidateClusters(EnergyGeometryContext context, DiagnosticBag bag)
    {
        var clusters = context.Model.Clusters.IsDefault ? [] : context.Model.Clusters;
        var unstableEnvironments = context.Model.UnstableEnvironments.IsDefault
            ? []
            : context.Model.UnstableEnvironments;
        var environmentsBySite = new Dictionary<CellSiteId, UnstableEnvironment>();
        foreach (var environment in unstableEnvironments)
        {
            environmentsBySite.TryAdd(environment.SiteId, environment);
        }

        foreach (var cluster in clusters)
        {
            if (!context.TryGetSite(cluster.CenterSiteId, out var center))
            {
                continue;
            }

            Core.Units.LengthAngstrom cutoff;
            ImmutableArray<InteractionFilter> filters;
            bool symmetric;
            if (center.Stability == SiteStability.Stable)
            {
                var stable = context.Model.StableEnvironment ?? StableEnvironment.Default;
                cutoff = stable.Cutoff;
                filters = stable.Filters;
                symmetric = true;
            }
            else
            {
                if (!environmentsBySite.TryGetValue(center.Id, out var unstable))
                {
                    continue;
                }

                cutoff = unstable.Cutoff;
                filters = unstable.Filters;
                symmetric = false;
            }

            if (!context.TryGetCenterP(center, out var centerP))
            {
                bag.Add(Error(
                    EnergyDiagnosticCodes.InvalidClusterPosition,
                    "Cluster centre cannot be resolved to a canonical P1 position.",
                    "cluster",
                    cluster.Id.Value,
                    "centerSiteId"));
                continue;
            }

            var offsets = cluster.OffsetVectors.IsDefault ? [] : cluster.OffsetVectors;
            var centerEncoding = new Vector4I(0, 0, 0, centerP);
            var seenEncodings = new HashSet<Vector4I>();
            var centerEquivalentReported = false;
            var targets = new List<CellSite>(offsets.Length);
            var geometryValid = true;
            foreach (var offset in offsets)
            {
                if (!context.TryResolveOffset(center, offset, out var encoding, out var targetP))
                {
                    bag.Add(Error(
                        EnergyDiagnosticCodes.InvalidClusterPosition,
                        "Cluster offset does not resolve to exactly one canonical P1 position.",
                        "cluster",
                        cluster.Id.Value,
                        "offsetVectors"));
                    geometryValid = false;
                    continue;
                }

                var target = context.GetSiteAt(targetP);
                if (target.Stability != SiteStability.Stable)
                {
                    bag.Add(Error(
                        EnergyDiagnosticCodes.InvalidClusterPosition,
                        $"Cluster surrounding position resolves to unstable site '{target.Id:D}'.",
                        "cluster",
                        cluster.Id.Value,
                        "offsetVectors"));
                    geometryValid = false;
                    continue;
                }

                var image = context.GetMinimumImage(centerP, encoding);
                if (image.DistanceAngstrom >
                    cutoff.Value + GeometryTolerances.DistanceEqualityTolerance)
                {
                    bag.Add(Error(
                        EnergyDiagnosticCodes.InvalidClusterPosition,
                        $"Cluster surrounding position is {image.DistanceAngstrom} Å from the centre; the " +
                        $"cutoff is {cutoff.Value} Å.",
                        "cluster",
                        cluster.Id.Value,
                        "offsetVectors"));
                    geometryValid = false;
                    continue;
                }

                if (EnergyGeometryContext.IsFiltered(
                        filters,
                        center,
                        target,
                        image.DistanceAngstrom,
                        symmetric))
                {
                    bag.Add(Error(
                        EnergyDiagnosticCodes.InvalidClusterPosition,
                        $"Cluster surrounding position is removed by a filter of the centre environment.",
                        "cluster",
                        cluster.Id.Value,
                        "offsetVectors"));
                    geometryValid = false;
                    continue;
                }

                if (!seenEncodings.Add(encoding))
                {
                    bag.Add(Error(
                        EnergyDiagnosticCodes.DuplicateClusterOffset,
                        "Cluster contains a duplicate surrounding position after periodic encoding.",
                        "cluster",
                        cluster.Id.Value,
                        "offsetVectors"));
                }

                if (encoding == centerEncoding && !centerEquivalentReported)
                {
                    centerEquivalentReported = true;
                    bag.Add(Error(
                        EnergyDiagnosticCodes.DuplicateClusterGeometry,
                        "Cluster contains a position equivalent to its centre.",
                        "cluster",
                        cluster.Id.Value,
                        "offsetVectors"));
                }

                targets.Add(target);
            }

            if (geometryValid)
            {
                ValidateClusterPermutations(context, cluster, center, targets, bag);
            }
        }
    }

    private static void ValidateClusterPermutations(
        EnergyGeometryContext context,
        Cluster cluster,
        CellSite center,
        List<CellSite> targets,
        DiagnosticBag bag)
    {
        var rawPermutations = GetParticleCount(context.Model, center);
        foreach (var target in targets)
        {
            rawPermutations = SaturatingMultiply(rawPermutations, GetParticleCount(context.Model, target));
        }

        if (rawPermutations > EnergyLimits.ClusterPermutationsHard)
        {
            bag.Add(Error(
                EnergyDiagnosticCodes.ClusterPermutationLimit,
                $"Cluster has {rawPermutations} raw permutations; the hard limit is " +
                $"{EnergyLimits.ClusterPermutationsHard}.",
                "cluster",
                cluster.Id.Value,
                "offsetVectors"));
        }
        else if (rawPermutations > EnergyLimits.ClusterPermutationsWarning)
        {
            bag.Add(Warning(
                EnergyDiagnosticCodes.ClusterPermutationWarning,
                $"Cluster has {rawPermutations} raw permutations; more than " +
                $"{EnergyLimits.ClusterPermutationsWarning} is discouraged.",
                "cluster",
                cluster.Id.Value,
                "offsetVectors"));
        }
    }

    private static long GetParticleCount(ModelDefinition model, CellSite site)
    {
        if (model.ParticleSets.IsDefault)
        {
            return 0;
        }

        foreach (var particleSet in model.ParticleSets)
        {
            if (particleSet.Id == site.ParticleSetId)
            {
                return particleSet.ParticleIds.IsDefault ? 0 : particleSet.ParticleIds.Length;
            }
        }

        return 0;
    }

    private static long SaturatingMultiply(long left, long right)
    {
        if (left == 0 || right == 0)
        {
            return 0;
        }

        if (left > long.MaxValue / right)
        {
            return long.MaxValue;
        }

        return left * right;
    }

    private static Diagnostic Error(
        string code,
        string message,
        string domain,
        Guid objectId,
        string? property = null) =>
        new(
            code,
            DiagnosticSeverity.Error,
            message,
            new ModelPath(domain, objectId, property),
            EnergyDiagnosticCodes.HelpFor(code));

    private static Diagnostic Warning(
        string code,
        string message,
        string domain,
        Guid objectId,
        string? property = null) =>
        new(
            code,
            DiagnosticSeverity.Warning,
            message,
            new ModelPath(domain, objectId, property),
            EnergyDiagnosticCodes.HelpFor(code));
}
