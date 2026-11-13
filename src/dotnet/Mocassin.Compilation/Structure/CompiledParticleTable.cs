// SPDX-License-Identifier: MIT

using System.Collections.Frozen;
using System.Collections.Immutable;
using Mocassin.Core.Ids;

namespace Mocassin.Compilation.Structure;

/// <summary>
/// One compiled particle species. Index <c>0</c> is always the implicit void particle.
/// </summary>
/// <param name="Index">The dense compiled particle index.</param>
/// <param name="Id">The stable particle identifier.</param>
/// <param name="Name">The user-visible particle name.</param>
/// <param name="Symbol">The short mass-conservation symbol.</param>
/// <param name="ChargeElementary">The formal charge in elementary-charge units.</param>
/// <param name="IsVacancy">Whether the particle represents an authored vacancy.</param>
public sealed record CompiledParticle(
    int Index,
    ParticleId Id,
    string Name,
    string Symbol,
    double ChargeElementary,
    bool IsVacancy);

/// <summary>
/// The dense compiled particle table.
/// </summary>
/// <remarks>
/// The implicit void particle is index <c>0</c>. Authored particles follow in ascending
/// <see cref="ParticleId"/> order. This is deliberately not the transition-preview particle order, which puts
/// the implicit void last; compiled state codes always use this table.
/// </remarks>
public sealed record CompiledParticleTable
{
    /// <summary>
    /// Initializes a compiled particle table.
    /// </summary>
    /// <param name="particles">The particles in dense index order.</param>
    public CompiledParticleTable(ImmutableArray<CompiledParticle> particles)
    {
        Particles = particles.IsDefault ? [] : particles;
        var indexById = new Dictionary<ParticleId, int>(Particles.Length);
        for (var index = 0; index < Particles.Length; index++)
        {
            var particle = Particles[index];
            if (particle.Index != index)
            {
                throw new ArgumentException("Compiled particle indices must be dense and match array order.", nameof(particles));
            }

            if (!indexById.TryAdd(particle.Id, index))
            {
                throw new ArgumentException($"Duplicate compiled particle '{particle.Id:D}'.", nameof(particles));
            }
        }

        IndexById = indexById.ToFrozenDictionary();
    }

    /// <summary>
    /// Gets the empty particle table.
    /// </summary>
    public static CompiledParticleTable Empty { get; } = new([]);

    /// <summary>
    /// Gets the particles in dense index order.
    /// </summary>
    public ImmutableArray<CompiledParticle> Particles { get; }

    /// <summary>
    /// Gets the particle lookup by stable identifier.
    /// </summary>
    public FrozenDictionary<ParticleId, int> IndexById { get; }

    /// <summary>
    /// Gets the number of compiled particles, including the implicit void particle.
    /// </summary>
    public int Count => Particles.Length;

    /// <summary>
    /// Gets the implicit void particle index.
    /// </summary>
    public static int VoidIndex => 0;

    /// <summary>
    /// Tries to resolve a stable particle identifier to its compiled index.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <param name="index">The compiled index when found.</param>
    /// <returns><see langword="true"/> when the particle is compiled.</returns>
    public bool TryGetIndex(ParticleId id, out int index) => IndexById.TryGetValue(id, out index);
}
