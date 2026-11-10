// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Units;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Editing;

public sealed partial class ProjectEditor
{
    /// <summary>
    /// Replaces the stable-site interaction environment.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="environment">The replacement environment.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult SetStableEnvironment(ProjectModel project, StableEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(environment);
        return Apply(project, "Set stable environment", current => current with
        {
            Model = current.Model with { StableEnvironment = environment },
        });
    }

    /// <summary>
    /// Sets the stable-environment cutoff while preserving its filters.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="cutoff">The new cutoff.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult SetStableEnvironmentCutoff(ProjectModel project, LengthAngstrom cutoff)
    {
        ArgumentNullException.ThrowIfNull(project);
        return SetStableEnvironment(project, project.Model.StableEnvironment with { Cutoff = cutoff });
    }

    /// <summary>
    /// Sets the stable-environment filters while preserving its cutoff.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="filters">The new filters.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult SetStableEnvironmentFilters(
        ProjectModel project,
        ImmutableArray<InteractionFilter> filters)
    {
        ArgumentNullException.ThrowIfNull(project);
        return SetStableEnvironment(project, project.Model.StableEnvironment with { Filters = filters });
    }

    /// <summary>
    /// Adds an unstable-site environment.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="environment">The environment to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddUnstableEnvironment(ProjectModel project, UnstableEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(environment);
        return Apply(project, "Add unstable environment", current => current with
        {
            Model = current.Model with
            {
                UnstableEnvironments = Append(current.Model.UnstableEnvironments, environment),
            },
        });
    }

    /// <summary>
    /// Updates an unstable-site environment by site identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="environment">The replacement environment.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateUnstableEnvironment(ProjectModel project, UnstableEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(environment);
        if (!Contains(
                project.Model.UnstableEnvironments,
                candidate => candidate.SiteId == environment.SiteId))
        {
            return Unknown(
                project,
                "unstableEnvironment",
                environment.SiteId.Value,
                $"Unstable environment for site '{environment.SiteId:D}' does not exist.");
        }

        return Apply(project, "Update unstable environment", current =>
        {
            var model = current.Model;
            return TryReplace(
                model.UnstableEnvironments,
                candidate => candidate.SiteId == environment.SiteId,
                environment,
                out var environments)
                ? current with { Model = model with { UnstableEnvironments = environments } }
                : current;
        });
    }

    /// <summary>
    /// Removes the unstable-site environment of one site.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="siteId">The unstable-site identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveUnstableEnvironment(ProjectModel project, CellSiteId siteId)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.UnstableEnvironments, candidate => candidate.SiteId == siteId))
        {
            return Unknown(
                project,
                "unstableEnvironment",
                siteId.Value,
                $"Unstable environment for site '{siteId:D}' does not exist.");
        }

        return Apply(project, "Remove unstable environment", current => current with
        {
            Model = current.Model with
            {
                UnstableEnvironments = RemoveWhere(
                    current.Model.UnstableEnvironments,
                    candidate => candidate.SiteId == siteId),
            },
        });
    }

    /// <summary>
    /// Adds a multi-body interaction cluster.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="cluster">The cluster to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddCluster(ProjectModel project, Cluster cluster)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(cluster);
        return Apply(project, "Add cluster", current => current with
        {
            Model = current.Model with { Clusters = Append(current.Model.Clusters, cluster) },
        });
    }

    /// <summary>
    /// Updates a cluster by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="cluster">The replacement cluster.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateCluster(ProjectModel project, Cluster cluster)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(cluster);
        if (!Contains(project.Model.Clusters, candidate => candidate.Id == cluster.Id))
        {
            return Unknown(project, "cluster", cluster.Id.Value, $"Cluster '{cluster.Id:D}' does not exist.");
        }

        return Apply(project, "Update cluster", current =>
        {
            var model = current.Model;
            return TryReplace(model.Clusters, candidate => candidate.Id == cluster.Id, cluster, out var clusters)
                ? current with { Model = model with { Clusters = clusters } }
                : current;
        });
    }

    /// <summary>
    /// Replaces the surrounding offsets of a cluster.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The cluster identifier.</param>
    /// <param name="offsetVectors">The new surrounding offsets in definition order.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult SetClusterOffsets(
        ProjectModel project,
        ClusterId id,
        ImmutableArray<Geometry.Fractional3> offsetVectors)
    {
        ArgumentNullException.ThrowIfNull(project);
        var cluster = FirstOrDefault(project.Model.Clusters, candidate => candidate.Id == id);
        if (cluster is null)
        {
            return Unknown(project, "cluster", id.Value, $"Cluster '{id:D}' does not exist.");
        }

        return UpdateCluster(project, cluster with { OffsetVectors = offsetVectors });
    }

    /// <summary>
    /// Removes a cluster by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The cluster identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveCluster(ProjectModel project, ClusterId id)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.Clusters, candidate => candidate.Id == id))
        {
            return Unknown(project, "cluster", id.Value, $"Cluster '{id:D}' does not exist.");
        }

        return Apply(project, "Remove cluster", current => current with
        {
            Model = current.Model with
            {
                Clusters = RemoveWhere(current.Model.Clusters, candidate => candidate.Id == id),
            },
        });
    }

    /// <summary>
    /// Sets the sparse site-energy entries of one reference site.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="siteEnergies">The replacement site-energy set.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult SetSiteEnergies(ProjectModel project, SiteEnergySet siteEnergies)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(siteEnergies);
        if (!Contains(project.Model.Structure.Sites, site => site.Id == siteEnergies.SiteId))
        {
            return Unknown(
                project,
                "cellSite",
                siteEnergies.SiteId.Value,
                $"Cell site '{siteEnergies.SiteId:D}' does not exist.");
        }

        return Apply(project, "Set site energies", current =>
        {
            var model = current.Model;
            return TryReplace(
                model.SiteEnergies,
                candidate => candidate.SiteId == siteEnergies.SiteId,
                siteEnergies,
                out var sets)
                ? current with { Model = model with { SiteEnergies = sets } }
                : current with
                {
                    Model = model with { SiteEnergies = Append(model.SiteEnergies, siteEnergies) },
                };
        });
    }

    /// <summary>
    /// Clears all site energies of one reference site.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="siteId">The reference-site identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult ClearSiteEnergies(ProjectModel project, CellSiteId siteId)
    {
        ArgumentNullException.ThrowIfNull(project);
        return Apply(project, "Clear site energies", current => current with
        {
            Model = current.Model with
            {
                SiteEnergies = RemoveWhere(
                    current.Model.SiteEnergies,
                    candidate => candidate.SiteId == siteId),
            },
        });
    }

    /// <summary>
    /// Removes one cell site together with every energy object that names it.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The cell-site identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveCellSiteAndEnergyReferences(ProjectModel project, CellSiteId id)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.Structure.Sites, site => site.Id == id))
        {
            return Unknown(project, "cellSite", id.Value, $"Cell site '{id:D}' does not exist.");
        }

        return Apply(project, "Remove cell site and energy references", current =>
        {
            var model = RemoveEnergyReferences(current.Model, id);
            return current with
            {
                Model = model with
                {
                    Structure = model.Structure with
                    {
                        Sites = RemoveWhere(model.Structure.Sites, site => site.Id == id),
                    },
                },
            };
        });
    }

    /// <summary>
    /// Rebases every energy reference from one cell site to another.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="oldId">The existing reference-site identifier to replace.</param>
    /// <param name="newId">The replacement reference-site identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RebaseEnergyReferences(
        ProjectModel project,
        CellSiteId oldId,
        CellSiteId newId)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.Structure.Sites, site => site.Id == oldId) ||
            !Contains(project.Model.Structure.Sites, site => site.Id == newId))
        {
            return Unknown(
                project,
                "cellSite",
                oldId.Value,
                "Both the source and target cell sites must exist.");
        }

        return Apply(project, "Rebase energy references", current => current with
        {
            Model = RebaseEnergyReferences(current.Model, oldId, newId),
        });
    }

    private static ModelDefinition RemoveEnergyReferences(ModelDefinition model, CellSiteId siteId)
    {
        var stable = model.StableEnvironment ?? StableEnvironment.Default;
        var stableFilters = (stable.Filters.IsDefault ? [] : stable.Filters)
            .Where(filter => filter.SiteAId != siteId && filter.SiteBId != siteId)
            .ToImmutableArray();
        var unstable = (model.UnstableEnvironments.IsDefault ? [] : model.UnstableEnvironments)
            .Where(environment => environment.SiteId != siteId)
            .Select(environment => environment with
            {
                Filters = (environment.Filters.IsDefault ? [] : environment.Filters)
                    .Where(filter => filter.SiteAId != siteId && filter.SiteBId != siteId)
                    .ToImmutableArray(),
            })
            .ToImmutableArray();
        var clusters = (model.Clusters.IsDefault ? [] : model.Clusters)
            .Where(cluster => cluster.CenterSiteId != siteId)
            .ToImmutableArray();
        var siteEnergies = (model.SiteEnergies.IsDefault ? [] : model.SiteEnergies)
            .Where(siteEnergySet => siteEnergySet.SiteId != siteId)
            .ToImmutableArray();

        return model with
        {
            StableEnvironment = stable with { Filters = stableFilters },
            UnstableEnvironments = unstable,
            Clusters = clusters,
            SiteEnergies = siteEnergies,
        };
    }

    private static ModelDefinition RebaseEnergyReferences(
        ModelDefinition model,
        CellSiteId oldId,
        CellSiteId newId)
    {
        var stable = model.StableEnvironment ?? StableEnvironment.Default;
        var stableFilters = (stable.Filters.IsDefault ? [] : stable.Filters)
            .Select(filter => filter with
            {
                SiteAId = filter.SiteAId == oldId ? newId : filter.SiteAId,
                SiteBId = filter.SiteBId == oldId ? newId : filter.SiteBId,
            })
            .ToImmutableArray();
        var unstable = (model.UnstableEnvironments.IsDefault ? [] : model.UnstableEnvironments)
            .Select(environment => environment with
            {
                SiteId = environment.SiteId == oldId ? newId : environment.SiteId,
                Filters = (environment.Filters.IsDefault ? [] : environment.Filters)
                    .Select(filter => filter with
                    {
                        SiteAId = filter.SiteAId == oldId ? newId : filter.SiteAId,
                        SiteBId = filter.SiteBId == oldId ? newId : filter.SiteBId,
                    })
                    .ToImmutableArray(),
            })
            .ToImmutableArray();
        var clusters = (model.Clusters.IsDefault ? [] : model.Clusters)
            .Select(cluster => cluster.CenterSiteId == oldId
                ? cluster with { CenterSiteId = newId }
                : cluster)
            .ToImmutableArray();
        var siteEnergies = (model.SiteEnergies.IsDefault ? [] : model.SiteEnergies)
            .Select(siteEnergySet => siteEnergySet.SiteId == oldId
                ? siteEnergySet with { SiteId = newId }
                : siteEnergySet)
            .ToImmutableArray();

        return model with
        {
            StableEnvironment = stable with { Filters = stableFilters },
            UnstableEnvironments = unstable,
            Clusters = clusters,
            SiteEnergies = siteEnergies,
        };
    }
}
