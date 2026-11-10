// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;

namespace Mocassin.Core.Model.Energy;

/// <summary>
/// Validates the definition-level energy rules that need only the model graph.
/// </summary>
/// <remarks>
/// Catalog, cell-geometry, periodic-encoding, sampling, and permutation rules belong to
/// <c>Mocassin.Symmetry.Energy.EnergyGeometryValidator</c>. The validator never throws for user data.
/// </remarks>
internal static class EnergyModelValidator
{
    /// <summary>
    /// Validates environments, filters, clusters, and site energies.
    /// </summary>
    /// <param name="model">The model to validate.</param>
    /// <returns>The diagnostics in stable order.</returns>
    public static ImmutableArray<Diagnostic> Validate(ModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var bag = new DiagnosticBag();
        var sites = model.Structure.Sites.IsDefault ? [] : model.Structure.Sites;
        var particles = model.Particles.IsDefault ? [] : model.Particles;
        var particleSets = model.ParticleSets.IsDefault ? [] : model.ParticleSets;
        var siteById = new Dictionary<CellSiteId, CellSite>();
        foreach (var site in sites)
        {
            siteById.TryAdd(site.Id, site);
        }

        var particleById = new Dictionary<ParticleId, Particle>();
        foreach (var particle in particles)
        {
            particleById.TryAdd(particle.Id, particle);
        }

        particleById[ParticleId.Void] = Particle.Void;
        var particleSetById = new Dictionary<ParticleSetId, ParticleSet>();
        foreach (var particleSet in particleSets)
        {
            particleSetById.TryAdd(particleSet.Id, particleSet);
        }

        ValidateStableEnvironment(model.StableEnvironment, siteById, bag);
        ValidateUnstableEnvironments(model.UnstableEnvironments, siteById, bag);
        ValidateClusters(model.Clusters, model.UnstableEnvironments, siteById, bag);
        ValidateSiteEnergies(model.SiteEnergies, siteById, particleById, particleSetById, bag);
        return bag.Sorted;
    }

    private static void ValidateStableEnvironment(
        StableEnvironment environment,
        Dictionary<CellSiteId, CellSite> siteById,
        DiagnosticBag bag)
    {
        if (environment is null)
        {
            bag.Add(Error(
                EnergyDiagnosticCodes.InvalidCutoff,
                "A stable environment is required.",
                "stableEnvironment",
                Guid.Empty,
                "cutoff"));
            return;
        }

        ValidateCutoff(environment.Cutoff, "stableEnvironment", Guid.Empty, bag);
        ValidateFilters(
            environment.Filters,
            environment.Cutoff,
            environmentSite: null,
            symmetric: true,
            "stableEnvironment",
            Guid.Empty,
            siteById,
            bag);
    }

    private static void ValidateUnstableEnvironments(
        ImmutableArray<UnstableEnvironment> environments,
        Dictionary<CellSiteId, CellSite> siteById,
        DiagnosticBag bag)
    {
        if (environments.IsDefaultOrEmpty)
        {
            return;
        }

        var seenSites = new HashSet<CellSiteId>();
        foreach (var environment in environments)
        {
            ValidateCutoff(
                environment.Cutoff,
                "unstableEnvironment",
                environment.SiteId.Value,
                bag);

            if (!siteById.TryGetValue(environment.SiteId, out var site))
            {
                bag.Add(Error(
                    EnergyDiagnosticCodes.InvalidUnstableEnvironment,
                    $"Unstable environment references unknown site '{environment.SiteId:D}'.",
                    "unstableEnvironment",
                    environment.SiteId.Value,
                    "siteId"));
            }
            else if (site.Stability != SiteStability.Unstable)
            {
                bag.Add(Error(
                    EnergyDiagnosticCodes.InvalidUnstableEnvironment,
                    "Unstable environment must reference an unstable site.",
                    "unstableEnvironment",
                    environment.SiteId.Value,
                    "siteId"));
            }

            if (!seenSites.Add(environment.SiteId))
            {
                bag.Add(Error(
                    EnergyDiagnosticCodes.InvalidUnstableEnvironment,
                    $"Site '{environment.SiteId:D}' has more than one unstable environment.",
                    "unstableEnvironment",
                    environment.SiteId.Value,
                    "siteId"));
            }

            ValidateFilters(
                environment.Filters,
                environment.Cutoff,
                environment.SiteId,
                symmetric: false,
                "unstableEnvironment",
                environment.SiteId.Value,
                siteById,
                bag);
        }
    }

    private static void ValidateCutoff(
        Core.Units.LengthAngstrom cutoff,
        string domain,
        Guid objectId,
        DiagnosticBag bag)
    {
        if (!double.IsFinite(cutoff.Value) || cutoff.Value <= 0.0)
        {
            bag.Add(Error(
                EnergyDiagnosticCodes.InvalidCutoff,
                "Environment cutoff must be positive and finite.",
                domain,
                objectId,
                "cutoff"));
        }
    }

    private static void ValidateFilters(
        ImmutableArray<InteractionFilter> filters,
        Core.Units.LengthAngstrom cutoff,
        CellSiteId? environmentSite,
        bool symmetric,
        string domain,
        Guid objectId,
        Dictionary<CellSiteId, CellSite> siteById,
        DiagnosticBag bag)
    {
        if (filters.IsDefaultOrEmpty)
        {
            return;
        }

        var seen = new HashSet<(double Start, double End, Guid SiteA, Guid SiteB)>();
        foreach (var filter in filters)
        {
            if (!double.IsFinite(filter.Start.Value) ||
                !double.IsFinite(filter.End.Value) ||
                filter.Start.Value > filter.End.Value ||
                filter.End.Value > cutoff.Value &&
                filter.End.Value - cutoff.Value > GeometryTolerances.DistanceEqualityTolerance)
            {
                bag.Add(Error(
                    EnergyDiagnosticCodes.InvalidFilterRange,
                    "Filter range must satisfy 0 <= start <= end and must stay within the environment cutoff.",
                    "interactionFilter",
                    filter.SiteAId.Value,
                    "start"));
            }

            var startKnown = siteById.ContainsKey(filter.SiteAId);
            var endKnown = siteById.ContainsKey(filter.SiteBId);
            var sameSite = filter.SiteAId == filter.SiteBId;
            var wrongCenter = environmentSite is { } siteId && filter.SiteAId != siteId;

            if (!startKnown || !endKnown || sameSite || wrongCenter)
            {
                bag.Add(Error(
                    EnergyDiagnosticCodes.InvalidFilterSites,
                    sameSite
                        ? "Filter must reference two distinct sites."
                        : wrongCenter
                            ? "An unstable-environment filter must start at the environment site."
                            : "Filter references an unknown site.",
                    "interactionFilter",
                    filter.SiteAId.Value,
                    wrongCenter ? "siteAId" : "siteBId"));
            }

            var siteA = symmetric && filter.SiteAId.Value.CompareTo(filter.SiteBId.Value) > 0
                ? filter.SiteBId.Value
                : filter.SiteAId.Value;
            var siteB = symmetric && filter.SiteAId.Value.CompareTo(filter.SiteBId.Value) > 0
                ? filter.SiteAId.Value
                : filter.SiteBId.Value;
            if (!seen.Add((filter.Start.Value, filter.End.Value, siteA, siteB)))
            {
                bag.Add(Warning(
                    EnergyDiagnosticCodes.DuplicateFilter,
                    symmetric
                        ? "Filter duplicates or mirrors an earlier filter in the same environment."
                        : "Filter duplicates an earlier filter in the same environment.",
                    "interactionFilter",
                    filter.SiteAId.Value));
            }
        }
    }

    private static void ValidateClusters(
        ImmutableArray<Cluster> clusters,
        ImmutableArray<UnstableEnvironment> unstableEnvironments,
        Dictionary<CellSiteId, CellSite> siteById,
        DiagnosticBag bag)
    {
        if (clusters.IsDefaultOrEmpty)
        {
            return;
        }

        var environmentSites = unstableEnvironments.IsDefault
            ? []
            : unstableEnvironments.Select(static environment => environment.SiteId).ToHashSet();
        var clusterCounts = new Dictionary<CellSiteId, int>();
        foreach (var cluster in clusters)
        {
            clusterCounts[cluster.CenterSiteId] = clusterCounts.GetValueOrDefault(cluster.CenterSiteId) + 1;

            if (!siteById.TryGetValue(cluster.CenterSiteId, out var center))
            {
                bag.Add(Error(
                    EnergyDiagnosticCodes.InvalidClusterCenter,
                    $"Cluster references unknown centre site '{cluster.CenterSiteId:D}'.",
                    "cluster",
                    cluster.Id.Value,
                    "centerSiteId"));
            }
            else if (center.Stability == SiteStability.Unstable && !environmentSites.Contains(center.Id))
            {
                bag.Add(Error(
                    EnergyDiagnosticCodes.InvalidClusterCenter,
                    $"Unstable cluster centre '{center.Id:D}' has no unstable environment.",
                    "cluster",
                    cluster.Id.Value,
                    "centerSiteId"));
            }

            var offsetCount = cluster.OffsetVectors.IsDefault ? 0 : cluster.OffsetVectors.Length;
            if (offsetCount < EnergyLimits.ClusterOffsetsMin || offsetCount > EnergyLimits.ClusterOffsetsMax)
            {
                bag.Add(Error(
                    EnergyDiagnosticCodes.InvalidClusterSize,
                    $"Cluster must have between {EnergyLimits.ClusterOffsetsMin} and " +
                    $"{EnergyLimits.ClusterOffsetsMax} surrounding offsets; found {offsetCount}.",
                    "cluster",
                    cluster.Id.Value,
                    "offsetVectors"));
            }
        }

        foreach (var (siteId, count) in clusterCounts)
        {
            if (count > EnergyLimits.ClustersPerPositionHard)
            {
                bag.Add(Error(
                    EnergyDiagnosticCodes.ClustersPerCenterLimit,
                    $"Site '{siteId:D}' has {count} clusters; the limit is " +
                    $"{EnergyLimits.ClustersPerPositionHard}.",
                    "cluster",
                    siteId.Value,
                    "centerSiteId"));
            }
            else if (count > EnergyLimits.ClustersPerPositionWarning)
            {
                bag.Add(Warning(
                    EnergyDiagnosticCodes.ClustersPerCenterWarning,
                    $"Site '{siteId:D}' has {count} clusters; more than " +
                    $"{EnergyLimits.ClustersPerPositionWarning} is discouraged.",
                    "cluster",
                    siteId.Value,
                    "centerSiteId"));
            }
        }
    }

    private static void ValidateSiteEnergies(
        ImmutableArray<SiteEnergySet> siteEnergies,
        Dictionary<CellSiteId, CellSite> siteById,
        Dictionary<ParticleId, Particle> particleById,
        Dictionary<ParticleSetId, ParticleSet> particleSetById,
        DiagnosticBag bag)
    {
        if (siteEnergies.IsDefaultOrEmpty)
        {
            return;
        }

        var seenSets = new HashSet<CellSiteId>();
        var seenEntries = new HashSet<(CellSiteId SiteId, ParticleId ParticleId)>();
        foreach (var siteEnergySet in siteEnergies)
        {
            if (!seenSets.Add(siteEnergySet.SiteId))
            {
                bag.Add(Error(
                    EnergyDiagnosticCodes.InvalidSiteEnergy,
                    $"Site '{siteEnergySet.SiteId:D}' has more than one site-energy set.",
                    "siteEnergySet",
                    siteEnergySet.SiteId.Value,
                    "siteId"));
            }

            var siteKnown = siteById.TryGetValue(siteEnergySet.SiteId, out var site);
            if (!siteKnown)
            {
                bag.Add(Error(
                    EnergyDiagnosticCodes.InvalidSiteEnergy,
                    $"Site-energy set references unknown site '{siteEnergySet.SiteId:D}'.",
                    "siteEnergySet",
                    siteEnergySet.SiteId.Value,
                    "siteId"));
            }

            if (siteEnergySet.Entries.IsDefaultOrEmpty)
            {
                continue;
            }

            foreach (var entry in siteEnergySet.Entries)
            {
                var particleKnown = particleById.ContainsKey(entry.ParticleId);
                if (!particleKnown)
                {
                    bag.Add(Error(
                        EnergyDiagnosticCodes.InvalidSiteEnergy,
                        $"Site-energy entry references unknown particle '{entry.ParticleId:D}'.",
                        "siteEnergyEntry",
                        entry.ParticleId.Value,
                        "particleId"));
                }

                if (!seenEntries.Add((siteEnergySet.SiteId, entry.ParticleId)))
                {
                    bag.Add(Error(
                        EnergyDiagnosticCodes.InvalidSiteEnergy,
                        $"Site '{siteEnergySet.SiteId:D}' repeats the site energy for particle " +
                        $"'{entry.ParticleId:D}'.",
                        "siteEnergyEntry",
                        entry.ParticleId.Value,
                        "particleId"));
                }

                if (!double.IsFinite(entry.Energy.Value) ||
                    entry.Energy.Value < EnergyLimits.MinEnergy.Value ||
                    entry.Energy.Value > EnergyLimits.MaxEnergy.Value)
                {
                    bag.Add(Error(
                        EnergyDiagnosticCodes.SiteEnergyOutOfRange,
                        $"Site energy must be finite and within {EnergyLimits.MinEnergy.Value}.." +
                        $"{EnergyLimits.MaxEnergy.Value} eV.",
                        "siteEnergyEntry",
                        entry.ParticleId.Value,
                        "energyEv"));
                }

                if (siteKnown &&
                    particleKnown &&
                    site is not null &&
                    !CanOccupy(site, entry.ParticleId, particleSetById))
                {
                    bag.Add(Warning(
                        EnergyDiagnosticCodes.ForbiddenSiteEnergy,
                        $"Particle '{entry.ParticleId:D}' cannot occupy site '{site.Id:D}'.",
                        "siteEnergyEntry",
                        entry.ParticleId.Value,
                        "particleId"));
                }
            }
        }
    }

    private static bool CanOccupy(
        CellSite site,
        ParticleId particleId,
        Dictionary<ParticleSetId, ParticleSet> particleSetById) =>
        particleSetById.TryGetValue(site.ParticleSetId, out var particleSet) &&
        !particleSet.ParticleIds.IsDefault &&
        particleSet.ParticleIds.Contains(particleId);

    private static Diagnostic Error(
        string code,
        string message,
        string domain,
        Guid objectId,
        string? property = null) =>
        new(code, DiagnosticSeverity.Error, message, new ModelPath(domain, objectId, property), EnergyDiagnosticCodes.HelpFor(code));

    private static Diagnostic Warning(
        string code,
        string message,
        string domain,
        Guid objectId,
        string? property = null) =>
        new(code, DiagnosticSeverity.Warning, message, new ModelPath(domain, objectId, property), EnergyDiagnosticCodes.HelpFor(code));
}
