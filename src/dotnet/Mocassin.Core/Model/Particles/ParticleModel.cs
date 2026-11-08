// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Particles;

/// <summary>
/// In-memory grouping of authored particles and particle sets.
/// </summary>
/// <remarks>
/// This grouping is not a persisted member of the project model; the project JSON stores particles and sets
/// directly on the model root. The implicit void particle and empty particle set are exposed separately and are
/// never part of <see cref="Particles"/> or <see cref="ParticleSets"/>.
/// </remarks>
public sealed record ParticleModel
{
    /// <summary>
    /// The maximum number of authored particles. The 64th slot is reserved for the implicit void particle.
    /// </summary>
    public const int MaxCustomParticles = 63;

    /// <summary>
    /// The maximum number of authored particle sets.
    /// </summary>
    public const int MaxParticleSets = 100;

    /// <summary>
    /// Initializes an in-memory particle grouping.
    /// </summary>
    /// <param name="particles">The authored particles.</param>
    /// <param name="particleSets">The authored particle sets.</param>
    public ParticleModel(
        ImmutableArray<Particle> particles,
        ImmutableArray<ParticleSet> particleSets)
    {
        Particles = particles.IsDefault ? [] : particles;
        ParticleSets = particleSets.IsDefault ? [] : particleSets;
    }

    /// <summary>
    /// Gets the authored particles.
    /// </summary>
    public ImmutableArray<Particle> Particles { get; init; }

    /// <summary>
    /// Gets the authored particle sets.
    /// </summary>
    public ImmutableArray<ParticleSet> ParticleSets { get; init; }

    /// <summary>
    /// Gets the implicit void particle.
    /// </summary>
    public Particle VoidParticle { get; } = Particle.Void;

    /// <summary>
    /// Gets the implicit empty particle set.
    /// </summary>
    public ParticleSet EmptyParticleSet { get; } = ParticleSet.Empty;

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

        foreach (var particle in Particles)
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

        foreach (var particleSet in ParticleSets)
        {
            if (particleSet.Id == id)
            {
                return particleSet;
            }
        }

        return null;
    }
}
