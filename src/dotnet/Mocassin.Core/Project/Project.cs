// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Particles;
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
