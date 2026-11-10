// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Energy;

/// <summary>
/// Read-only access to the authored energy domain of a project snapshot.
/// </summary>
public interface IEnergyQuery
{
    /// <summary>
    /// Gets the stable-site interaction environment.
    /// </summary>
    public StableEnvironment StableEnvironment { get; }

    /// <summary>
    /// Gets the per-unstable-site interaction environments.
    /// </summary>
    public ImmutableArray<UnstableEnvironment> UnstableEnvironments { get; }

    /// <summary>
    /// Gets the authored multi-body interaction clusters.
    /// </summary>
    public ImmutableArray<Cluster> Clusters { get; }

    /// <summary>
    /// Gets the authored sparse site-energy sets.
    /// </summary>
    public ImmutableArray<SiteEnergySet> SiteEnergies { get; }

    /// <summary>
    /// Finds the environment of one unstable site.
    /// </summary>
    /// <param name="siteId">The unstable-site identifier.</param>
    /// <returns>The matching environment, or <see langword="null"/> when none is authored.</returns>
    public UnstableEnvironment? FindEnvironment(CellSiteId siteId);

    /// <summary>
    /// Finds an authored cluster.
    /// </summary>
    /// <param name="id">The cluster identifier.</param>
    /// <returns>The matching cluster, or <see langword="null"/> when the identifier is unknown.</returns>
    public Cluster? FindCluster(ClusterId id);

    /// <summary>
    /// Finds the site-energy set of one reference site.
    /// </summary>
    /// <param name="siteId">The reference-site identifier.</param>
    /// <returns>The matching set, or <see langword="null"/> when no entry is authored.</returns>
    public SiteEnergySet? FindSiteEnergies(CellSiteId siteId);
}
