// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;

namespace Mocassin.Core.Project;

/// <summary>
/// Immutable root value of one <c>.mocprj</c> project file.
/// </summary>
#pragma warning disable CA1724 // The type intentionally names the project domain and the namespace follows the plan.
public sealed record Project : IProjectQuery
{
#pragma warning restore CA1724
    /// <summary>
    /// Initializes a project value.
    /// </summary>
    /// <param name="schema">The optional JSON Schema URI.</param>
    /// <param name="schemaVersion">The project schema version.</param>
    /// <param name="metadata">The project metadata.</param>
    /// <param name="model">The scientific model.</param>
    /// <param name="parameterizations">The parameterization templates.</param>
    /// <param name="jobTemplates">The job templates.</param>
    /// <param name="buildTemplates">The build templates.</param>
    /// <param name="viewSettings">The persisted view state.</param>
    public Project(
        string? schema,
        int schemaVersion,
        ProjectMetadata metadata,
        ModelDefinition model,
        ImmutableArray<ParameterizationTemplate> parameterizations,
        ImmutableArray<JobTemplate> jobTemplates,
        ImmutableArray<BuildTemplate> buildTemplates,
        ViewSettings viewSettings)
        : this(
            schema,
            schemaVersion,
            metadata,
            model,
            parameterizations,
            jobTemplates,
            buildTemplates,
            viewSettings,
            symmetry: null)
    {
    }

    /// <summary>
    /// Initializes a project value with optional symmetry metadata.
    /// </summary>
    /// <param name="schema">The optional JSON Schema URI.</param>
    /// <param name="schemaVersion">The project schema version.</param>
    /// <param name="metadata">The project metadata.</param>
    /// <param name="model">The scientific model.</param>
    /// <param name="parameterizations">The parameterization templates.</param>
    /// <param name="jobTemplates">The job templates.</param>
    /// <param name="buildTemplates">The build templates.</param>
    /// <param name="viewSettings">The persisted view state.</param>
    /// <param name="symmetry">The optional symmetry metadata.</param>
    public Project(
        string? schema,
        int schemaVersion,
        ProjectMetadata metadata,
        ModelDefinition model,
        ImmutableArray<ParameterizationTemplate> parameterizations,
        ImmutableArray<JobTemplate> jobTemplates,
        ImmutableArray<BuildTemplate> buildTemplates,
        ViewSettings viewSettings,
        ProjectSymmetryMetadata? symmetry)
    {
        Schema = schema;
        SchemaVersion = schemaVersion;
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Parameterizations = parameterizations.IsDefault ? [] : parameterizations;
        JobTemplates = jobTemplates.IsDefault ? [] : jobTemplates;
        BuildTemplates = buildTemplates.IsDefault ? [] : buildTemplates;
        ViewSettings = viewSettings ?? Mocassin.Core.Model.ViewSettings.Default;
        Symmetry = symmetry;
    }

    /// <summary>
    /// Gets the optional JSON Schema URI.
    /// </summary>
    public string? Schema { get; init; }

    /// <summary>
    /// Gets the project schema version.
    /// </summary>
    public int SchemaVersion { get; init; }

    /// <summary>
    /// Gets the project metadata.
    /// </summary>
    public ProjectMetadata Metadata { get; init; }

    /// <summary>
    /// Gets the scientific model.
    /// </summary>
    public ModelDefinition Model { get; init; }

    /// <summary>
    /// Gets the optional symmetry metadata.
    /// </summary>
    public ProjectSymmetryMetadata? Symmetry { get; init; }

    /// <summary>
    /// Gets the authored particles. The implicit void particle is not included.
    /// </summary>
    public ImmutableArray<Particle> Particles => Model.Particles;

    /// <summary>
    /// Gets the implicit void particle.
    /// </summary>
    public Particle VoidParticle => Particle.Void;

    /// <summary>
    /// Gets the authored crystal structure.
    /// </summary>
    public StructureModel Structure => Model.Structure;

    /// <summary>
    /// Gets the authored state changes.
    /// </summary>
    public ImmutableArray<StateChange> StateChanges => Model.StateChanges;

    /// <summary>
    /// Gets the authored state-change groups.
    /// </summary>
    public ImmutableArray<StateChangeGroup> StateChangeGroups => Model.StateChangeGroups;

    /// <summary>
    /// Gets the authored transition chains.
    /// </summary>
    public ImmutableArray<TransitionChain> Chains => Model.Chains;

    /// <summary>
    /// Gets the authored kinetic transitions.
    /// </summary>
    public ImmutableArray<KineticTransition> KineticTransitions => Model.KineticTransitions;

    /// <summary>
    /// Gets the authored Metropolis transitions.
    /// </summary>
    public ImmutableArray<MetropolisTransition> MetropolisTransitions => Model.MetropolisTransitions;

    /// <summary>
    /// Gets the stable-site interaction environment.
    /// </summary>
    public StableEnvironment StableEnvironment => Model.StableEnvironment;

    /// <summary>
    /// Gets the per-unstable-site interaction environments.
    /// </summary>
    public ImmutableArray<UnstableEnvironment> UnstableEnvironments => Model.UnstableEnvironments;

    /// <summary>
    /// Gets the authored multi-body interaction clusters.
    /// </summary>
    public ImmutableArray<Cluster> Clusters => Model.Clusters;

    /// <summary>
    /// Gets the authored sparse site-energy sets.
    /// </summary>
    public ImmutableArray<SiteEnergySet> SiteEnergies => Model.SiteEnergies;

    /// <summary>
    /// Gets the authored occupation exchanges.
    /// </summary>
    public ImmutableArray<OccupationExchange> OccupationExchanges => Model.OccupationExchanges;

    /// <summary>
    /// Gets the authored dopings in ascending priority order.
    /// </summary>
    public ImmutableArray<Doping> Dopings => Model.Dopings;

    /// <summary>
    /// Gets the derived default building block.
    /// </summary>
    public ImmutableArray<BuildingBlockEntry> DefaultBuildingBlock =>
        DefaultBuildingBlockBuilder.Build(Model);

    /// <summary>
    /// Gets the authored simulations in stable identifier order.
    /// </summary>
    public ImmutableArray<Simulation> Simulations => Model.Simulations;

    /// <summary>
    /// Finds the environment of one unstable site.
    /// </summary>
    /// <param name="siteId">The unstable-site identifier.</param>
    /// <returns>The matching environment, or <see langword="null"/> when none is authored.</returns>
    public UnstableEnvironment? FindEnvironment(CellSiteId siteId)
    {
        if (Model.UnstableEnvironments.IsDefault)
        {
            return null;
        }

        foreach (var environment in Model.UnstableEnvironments)
        {
            if (environment.SiteId == siteId)
            {
                return environment;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an authored cluster.
    /// </summary>
    /// <param name="id">The cluster identifier.</param>
    /// <returns>The matching cluster, or <see langword="null"/> when the identifier is unknown.</returns>
    public Cluster? FindCluster(ClusterId id)
    {
        if (Model.Clusters.IsDefault)
        {
            return null;
        }

        foreach (var cluster in Model.Clusters)
        {
            if (cluster.Id == id)
            {
                return cluster;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the site-energy set of one reference site.
    /// </summary>
    /// <param name="siteId">The reference-site identifier.</param>
    /// <returns>The matching set, or <see langword="null"/> when no entry is authored.</returns>
    public SiteEnergySet? FindSiteEnergies(CellSiteId siteId)
    {
        if (Model.SiteEnergies.IsDefault)
        {
            return null;
        }

        foreach (var siteEnergies in Model.SiteEnergies)
        {
            if (siteEnergies.SiteId == siteId)
            {
                return siteEnergies;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an authored occupation exchange.
    /// </summary>
    /// <param name="id">The occupation-exchange identifier.</param>
    /// <returns>The matching exchange, or <see langword="null"/> when the identifier is unknown.</returns>
    public OccupationExchange? Find(OccupationExchangeId id)
    {
        if (Model.OccupationExchanges.IsDefault)
        {
            return null;
        }

        foreach (var exchange in Model.OccupationExchanges)
        {
            if (exchange.Id == id)
            {
                return exchange;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an authored doping.
    /// </summary>
    /// <param name="id">The doping identifier.</param>
    /// <returns>The matching doping, or <see langword="null"/> when the identifier is unknown.</returns>
    public Doping? Find(DopingId id)
    {
        if (Model.Dopings.IsDefault)
        {
            return null;
        }

        foreach (var doping in Model.Dopings)
        {
            if (doping.Id == id)
            {
                return doping;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an authored simulation.
    /// </summary>
    /// <param name="id">The simulation identifier.</param>
    /// <returns>The matching simulation, or <see langword="null"/> when the identifier is unknown.</returns>
    public Simulation? Find(SimulationId id)
    {
        if (Model.Simulations.IsDefault)
        {
            return null;
        }

        foreach (var simulation in Model.Simulations)
        {
            if (simulation.Id == id)
            {
                return simulation;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an authored particle or the implicit void particle.
    /// </summary>
    /// <param name="id">The particle identifier.</param>
    /// <returns>The matching particle, or <see langword="null"/> when the identifier is unknown.</returns>
    public Particle? Find(ParticleId id)
    {
        if (id == ParticleId.Void)
        {
            return Particle.Void;
        }

        if (Model.Particles.IsDefault)
        {
            return null;
        }

        foreach (var particle in Model.Particles)
        {
            if (particle.Id == id)
            {
                return particle;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an authored particle set or the implicit empty particle set.
    /// </summary>
    /// <param name="id">The particle-set identifier.</param>
    /// <returns>The matching particle set, or <see langword="null"/> when the identifier is unknown.</returns>
    public ParticleSet? Find(ParticleSetId id)
    {
        if (id == ParticleSetId.Empty)
        {
            return ParticleSet.Empty;
        }

        if (Model.ParticleSets.IsDefault)
        {
            return null;
        }

        foreach (var particleSet in Model.ParticleSets)
        {
            if (particleSet.Id == id)
            {
                return particleSet;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an authored cell site.
    /// </summary>
    /// <param name="id">The cell-site identifier.</param>
    /// <returns>The matching site, or <see langword="null"/> when the identifier is unknown.</returns>
    public CellSite? Find(CellSiteId id)
    {
        if (Model.Structure.Sites.IsDefault)
        {
            return null;
        }

        foreach (var site in Model.Structure.Sites)
        {
            if (site.Id == id)
            {
                return site;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an authored state change.
    /// </summary>
    /// <param name="id">The state-change identifier.</param>
    /// <returns>The matching state change, or <see langword="null"/> when the identifier is unknown.</returns>
    public StateChange? Find(StateChangeId id)
    {
        if (Model.StateChanges.IsDefault)
        {
            return null;
        }

        foreach (var stateChange in Model.StateChanges)
        {
            if (stateChange.Id == id)
            {
                return stateChange;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an authored state-change group.
    /// </summary>
    /// <param name="id">The state-change-group identifier.</param>
    /// <returns>The matching group, or <see langword="null"/> when the identifier is unknown.</returns>
    public StateChangeGroup? Find(StateChangeGroupId id)
    {
        if (Model.StateChangeGroups.IsDefault)
        {
            return null;
        }

        foreach (var group in Model.StateChangeGroups)
        {
            if (group.Id == id)
            {
                return group;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an authored transition chain.
    /// </summary>
    /// <param name="id">The chain identifier.</param>
    /// <returns>The matching chain, or <see langword="null"/> when the identifier is unknown.</returns>
    public TransitionChain? Find(TransitionChainId id)
    {
        if (Model.Chains.IsDefault)
        {
            return null;
        }

        foreach (var chain in Model.Chains)
        {
            if (chain.Id == id)
            {
                return chain;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an authored kinetic transition.
    /// </summary>
    /// <param name="id">The kinetic-transition identifier.</param>
    /// <returns>The matching transition, or <see langword="null"/> when the identifier is unknown.</returns>
    public KineticTransition? Find(KineticTransitionId id)
    {
        if (Model.KineticTransitions.IsDefault)
        {
            return null;
        }

        foreach (var transition in Model.KineticTransitions)
        {
            if (transition.Id == id)
            {
                return transition;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an authored Metropolis transition.
    /// </summary>
    /// <param name="id">The Metropolis-transition identifier.</param>
    /// <returns>The matching transition, or <see langword="null"/> when the identifier is unknown.</returns>
    public MetropolisTransition? Find(MetropolisTransitionId id)
    {
        if (Model.MetropolisTransitions.IsDefault)
        {
            return null;
        }

        foreach (var transition in Model.MetropolisTransitions)
        {
            if (transition.Id == id)
            {
                return transition;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the parameterization templates.
    /// </summary>
    public ImmutableArray<ParameterizationTemplate> Parameterizations { get; init; }

    /// <summary>
    /// Gets the job templates.
    /// </summary>
    public ImmutableArray<JobTemplate> JobTemplates { get; init; }

    /// <summary>
    /// Gets the build templates.
    /// </summary>
    public ImmutableArray<BuildTemplate> BuildTemplates { get; init; }

    /// <summary>
    /// Gets the persisted view state.
    /// </summary>
    public ViewSettings ViewSettings { get; init; }

    /// <summary>
    /// Returns a copy with a new modified timestamp.
    /// </summary>
    /// <param name="modifiedUtc">The new UTC timestamp.</param>
    /// <returns>The updated project.</returns>
    public Project WithModifiedUtc(DateTimeOffset modifiedUtc) => this with
    {
        Metadata = Metadata.WithModifiedUtc(modifiedUtc),
    };
}
