// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.App.ViewModels.Editors.Usage;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Transitions;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors;

/// <summary>
/// Answers "how many objects reference this one" and "which objects are these" from one immutable project snapshot.
/// </summary>
/// <remarks>
/// <para>
/// The index is an affordance and a pre-check, not a validator. It calls the same Step 04–06 services the compiler
/// uses: KMC reference paths are resolved with <see cref="TransitionPathResolver"/> to the owning
/// <see cref="CellSiteId"/>, because the Core structural validator deliberately leaves resolved-path and MMC site
/// rules to the Symmetry pipeline. It never re-implements those rules.
/// </para>
/// <para>
/// The index is built lazily by the editor per immutable <see cref="Project"/> reference and is read-only after
/// construction.
/// </para>
/// </remarks>
internal sealed class ModelUsageIndex
{
    private readonly ProjectModel project;
    private readonly Dictionary<ModelObjectRef, List<ModelUsageEntry>> dependents = [];

    private ModelUsageIndex(ProjectModel project)
    {
        this.project = project;
        Build();
    }

    /// <summary>
    /// Creates the index for one project snapshot.
    /// </summary>
    /// <param name="project">The immutable project value.</param>
    /// <returns>The built index.</returns>
    public static ModelUsageIndex Create(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return new ModelUsageIndex(project);
    }

    /// <summary>
    /// Gets the dependent objects of one target.
    /// </summary>
    /// <param name="target">The target reference.</param>
    /// <returns>The dependents, or an empty array.</returns>
    public ImmutableArray<ModelUsageEntry> GetDependents(ModelObjectRef target) =>
        dependents.TryGetValue(target, out var list)
            ? [.. list]
            : ImmutableArray<ModelUsageEntry>.Empty;

    /// <summary>
    /// Gets the dependent count of one target.
    /// </summary>
    /// <param name="target">The target reference.</param>
    /// <returns>The distinct dependent count.</returns>
    public int Count(ModelObjectRef target) => GetDependents(target).Length;

    /// <summary>
    /// Determines whether any object references the target.
    /// </summary>
    /// <param name="target">The target reference.</param>
    /// <returns><see langword="true"/> when at least one dependent exists.</returns>
    public bool IsReferenced(ModelObjectRef target) => Count(target) > 0;

    private void Build()
    {
        AddParticleSetMembers();
        AddStateChanges();
        AddTransitionStateChangeUses();
        AddSetSiteUses();
        AddSiteUses();
        AddEnergyUses();
    }

    private void AddParticleSetMembers()
    {
        foreach (var set in project.Model.ParticleSets.IsDefault
                     ? ImmutableArray<Mocassin.Core.Model.Particles.ParticleSet>.Empty
                     : project.Model.ParticleSets)
        {
            var reference = ModelObjectRef.ParticleSet(set.Id);
            var entry = new ModelUsageEntry(reference, ModelUsageKind.ParticleSet, set.Name, "Particle set");
            foreach (var particleId in set.ParticleIds)
            {
                Add(ModelObjectRef.Particle(particleId), entry);
            }
        }
    }

    private void AddStateChanges()
    {
        foreach (var stateChange in project.StateChanges)
        {
            var entry = new ModelUsageEntry(
                ModelObjectRef.StateChange(stateChange.Id),
                ModelUsageKind.StateChange,
                stateChange.Name,
                "State change");
            AddParticle(stateChange.DonorParticleId, entry);
            AddParticle(stateChange.AcceptorParticleId, entry);
        }
    }

    private void AddTransitionStateChangeUses()
    {
        foreach (var transition in project.KineticTransitions)
        {
            AddTransitionUses(
                transition.Id.Value,
                transition.Name,
                transition.ChainId,
                "Kinetic transition");
        }

        foreach (var transition in project.MetropolisTransitions)
        {
            AddTransitionUses(
                transition.Id.Value,
                transition.Name,
                transition.ChainId,
                "Metropolis transition");
        }
    }

    private void AddTransitionUses(Guid id, string name, TransitionChainId chainId, string description)
    {
        var chain = project.Find(chainId);
        if (chain is null || chain.GroupIds.IsDefault)
        {
            return;
        }

        var entry = new ModelUsageEntry(ModelObjectRef.Transition(id), ModelUsageKind.Transition, name, description);
        foreach (var groupId in chain.GroupIds)
        {
            var group = project.Find(groupId);
            if (group is null || group.StateChangeIds.IsDefault)
            {
                continue;
            }

            foreach (var stateChangeId in group.StateChangeIds)
            {
                var stateChange = project.Find(stateChangeId);
                if (stateChange is null)
                {
                    continue;
                }

                AddParticle(stateChange.DonorParticleId, entry);
                AddParticle(stateChange.AcceptorParticleId, entry);
            }
        }
    }

    private void AddSetSiteUses()
    {
        foreach (var site in ReferenceSites())
        {
            if (site.ParticleSetId == ParticleSetId.Empty)
            {
                continue;
            }

            Add(
                ModelObjectRef.ParticleSet(site.ParticleSetId),
                new ModelUsageEntry(
                    ModelObjectRef.CellSite(site.Id),
                    ModelUsageKind.CellSite,
                    site.Name,
                    "Reference site"));
        }
    }

    private void AddSiteUses()
    {
        foreach (var transition in project.MetropolisTransitions)
        {
            var entry = new ModelUsageEntry(
                ModelObjectRef.Transition(transition.Id.Value),
                ModelUsageKind.Transition,
                transition.Name,
                "Metropolis transition");
            Add(ModelObjectRef.CellSite(transition.SiteAId), entry);
            Add(ModelObjectRef.CellSite(transition.SiteBId), entry);
        }

        AddResolvedKineticPathUses();
        AddOccupationExchangeUses();
    }

    private void AddResolvedKineticPathUses()
    {
        if (!TransitionPathResolver.TryCreate(project.Structure, catalog: null, out var resolver, out _)
            || resolver is null)
        {
            return;
        }

        foreach (var transition in project.KineticTransitions)
        {
            var resolved = resolver.TryResolve(transition, out _);
            if (resolved is null)
            {
                continue;
            }

            var entry = new ModelUsageEntry(
                ModelObjectRef.Transition(transition.Id.Value),
                ModelUsageKind.Transition,
                transition.Name,
                "Kinetic transition (resolved path)");
            foreach (var position in resolved.Positions)
            {
                Add(ModelObjectRef.CellSite(position.SiteId), entry);
            }
        }
    }

    private void AddOccupationExchangeUses()
    {
        foreach (var exchange in project.OccupationExchanges)
        {
            var reference = ModelObjectRef.OccupationExchange(exchange.Id);
            var entry = new ModelUsageEntry(
                reference,
                ModelUsageKind.OccupationExchange,
                exchange.Name,
                "Occupation exchange");
            Add(ModelObjectRef.CellSite(exchange.SiteId), entry);
            AddParticle(exchange.SourceParticleId, entry);
            AddParticle(exchange.TargetParticleId, entry);
        }

        foreach (var doping in project.Dopings)
        {
            var entry = new ModelUsageEntry(
                ModelObjectRef.Doping(doping.Id),
                ModelUsageKind.Doping,
                doping.Name,
                "Doping");
            AddDopingUse(doping.PrimaryExchangeId, entry);
            if (doping.DependentExchangeId is { } dependent)
            {
                AddDopingUse(dependent, entry);
            }
        }
    }

    private void AddDopingUse(OccupationExchangeId exchangeId, ModelUsageEntry doping)
    {
        Add(ModelObjectRef.OccupationExchange(exchangeId), doping);
        var exchange = project.Find(exchangeId);
        if (exchange is null)
        {
            return;
        }

        Add(ModelObjectRef.CellSite(exchange.SiteId), doping);
        AddParticle(exchange.SourceParticleId, doping);
        AddParticle(exchange.TargetParticleId, doping);
    }

    private void AddEnergyUses()
    {
        foreach (var filter in project.StableEnvironment.Filters.IsDefault
                     ? ImmutableArray<Mocassin.Core.Model.Energy.InteractionFilter>.Empty
                     : project.StableEnvironment.Filters)
        {
            AddEnergyFilterUse(filter, "Stable-environment filter");
        }

        foreach (var environment in project.Model.UnstableEnvironments.IsDefault
                     ? ImmutableArray<Mocassin.Core.Model.Energy.UnstableEnvironment>.Empty
                     : project.Model.UnstableEnvironments)
        {
            // m-15: the environment is itself a dependent through its SiteId, even with no filters.
            Add(
                ModelObjectRef.CellSite(environment.SiteId),
                EnergyEntry("Unstable environment", "Unstable environment at the site"));
            foreach (var filter in environment.Filters.IsDefault
                         ? ImmutableArray<Mocassin.Core.Model.Energy.InteractionFilter>.Empty
                         : environment.Filters)
            {
                AddEnergyFilterUse(filter, "Unstable-environment filter");
            }
        }

        foreach (var cluster in project.Clusters)
        {
            Add(
                ModelObjectRef.CellSite(cluster.CenterSiteId),
                EnergyEntry(cluster.Name, "Cluster centre site"));
        }

        foreach (var siteEnergies in project.SiteEnergies)
        {
            Add(
                ModelObjectRef.CellSite(siteEnergies.SiteId),
                EnergyEntry("Site energies", "Site-energy set"));
        }
    }

    private void AddEnergyFilterUse(Mocassin.Core.Model.Energy.InteractionFilter filter, string description)
    {
        var entry = EnergyEntry(description, description);
        Add(ModelObjectRef.CellSite(filter.SiteAId), entry);
        Add(ModelObjectRef.CellSite(filter.SiteBId), entry);
    }

    private static ModelUsageEntry EnergyEntry(string name, string description) =>
        new(ModelObjectRef.Energy(), ModelUsageKind.EnergyReference, name, description);

    private void AddParticle(ParticleId id, ModelUsageEntry entry)
    {
        if (id == ParticleId.Void)
        {
            return;
        }

        Add(ModelObjectRef.Particle(id), entry);
    }

    private void Add(ModelObjectRef target, ModelUsageEntry entry)
    {
        if (!dependents.TryGetValue(target, out var list))
        {
            list = [];
            dependents[target] = list;
        }

        if (!list.Contains(entry))
        {
            list.Add(entry);
        }
    }

    private ImmutableArray<CellSite> ReferenceSites() =>
        project.Structure.Sites.IsDefault ? ImmutableArray<CellSite>.Empty : project.Structure.Sites;
}
