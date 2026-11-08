// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Particles;

/// <summary>
/// Read-only access to the authored particle domain of a project snapshot.
/// </summary>
public interface IParticleQuery
{
    /// <summary>
    /// Gets the authored particles. The implicit void particle is not included.
    /// </summary>
    public ImmutableArray<Particle> Particles { get; }

    /// <summary>
    /// Gets the implicit void particle.
    /// </summary>
    public Particle VoidParticle { get; }

    /// <summary>
    /// Finds an authored particle or the implicit void particle.
    /// </summary>
    /// <param name="id">The particle identifier.</param>
    /// <returns>The matching particle, or <see langword="null"/> when the identifier is unknown.</returns>
    public Particle? Find(ParticleId id);

    /// <summary>
    /// Finds an authored particle set or the implicit empty particle set.
    /// </summary>
    /// <param name="id">The particle-set identifier.</param>
    /// <returns>The matching particle set, or <see langword="null"/> when the identifier is unknown.</returns>
    public ParticleSet? Find(ParticleSetId id);
}
