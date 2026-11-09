// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;

namespace Mocassin.Core.Model;

/// <summary>
/// Root of the authored scientific model.
/// </summary>
/// <remarks>
/// The model is flat on disk: <c>model.particles</c>, <c>model.particleSets</c>, <c>model.structure</c>, the
/// transition sections <c>stateChanges</c>, <c>stateChangeGroups</c>, <c>chains</c>,
/// <c>kineticTransitions</c>, and <c>metropolisTransitions</c>, and the energy sections
/// <c>stableEnvironment</c>, <c>unstableEnvironments</c>, <c>clusters</c>, and <c>siteEnergies</c>. Later steps
/// add lattice and simulation sections directly at the model root. <see cref="ParticleModel"/> is an in-memory
/// grouping only and is not a persisted member.
/// </remarks>
[JsonConverter(typeof(ModelDefinitionJsonConverter))]
public sealed record ModelDefinition
{
    /// <summary>
    /// Initializes the empty model used when a project contains <c>"model": {}</c>.
    /// </summary>
    [JsonConstructor]
    public ModelDefinition()
    {
        Particles = [];
        ParticleSets = [];
        Structure = StructureModel.Default;
        StateChanges = [];
        StateChangeGroups = [];
        Chains = [];
        KineticTransitions = [];
        MetropolisTransitions = [];
        StableEnvironment = Energy.StableEnvironment.Default;
        UnstableEnvironments = [];
        Clusters = [];
        SiteEnergies = [];
    }

    /// <summary>
    /// Gets the empty model with no particles, particle sets, or transitions and the default structure.
    /// </summary>
    public static ModelDefinition Empty { get; } = new();

    /// <summary>
    /// Gets the authored particles.
    /// </summary>
    public ImmutableArray<Particle> Particles { get; init; }

    /// <summary>
    /// Gets the authored particle sets.
    /// </summary>
    public ImmutableArray<ParticleSet> ParticleSets { get; init; }

    /// <summary>
    /// Gets the authored crystal structure.
    /// </summary>
    public StructureModel Structure { get; init; }

    /// <summary>
    /// Gets the authored state changes.
    /// </summary>
    public ImmutableArray<StateChange> StateChanges { get; init; }

    /// <summary>
    /// Gets the authored state-change groups.
    /// </summary>
    public ImmutableArray<StateChangeGroup> StateChangeGroups { get; init; }

    /// <summary>
    /// Gets the authored transition chains.
    /// </summary>
    public ImmutableArray<TransitionChain> Chains { get; init; }

    /// <summary>
    /// Gets the authored kinetic transitions.
    /// </summary>
    public ImmutableArray<KineticTransition> KineticTransitions { get; init; }

    /// <summary>
    /// Gets the authored Metropolis transitions.
    /// </summary>
    public ImmutableArray<MetropolisTransition> MetropolisTransitions { get; init; }

    /// <summary>
    /// Gets the stable-site interaction environment.
    /// </summary>
    public StableEnvironment StableEnvironment { get; init; }

    /// <summary>
    /// Gets the per-unstable-site interaction environments.
    /// </summary>
    public ImmutableArray<UnstableEnvironment> UnstableEnvironments { get; init; }

    /// <summary>
    /// Gets the authored multi-body interaction clusters.
    /// </summary>
    public ImmutableArray<Cluster> Clusters { get; init; }

    /// <summary>
    /// Gets the authored sparse site-energy sets.
    /// </summary>
    public ImmutableArray<SiteEnergySet> SiteEnergies { get; init; }
}
