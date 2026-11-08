// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;

namespace Mocassin.Core.Model;

/// <summary>
/// Root of the authored scientific model.
/// </summary>
/// <remarks>
/// The model is flat on disk: <c>model.particles</c>, <c>model.particleSets</c>, and
/// <c>model.structure</c>. Later steps add transition, energy, lattice, and simulation sections directly at
/// the model root. <see cref="ParticleModel"/> is an in-memory grouping only and is not a persisted member.
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
    }

    /// <summary>
    /// Gets the empty model with no particles or particle sets and the default structure.
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
}
