// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Particles;

/// <summary>
/// An ordered set of particles that may occupy a crystal site.
/// </summary>
/// <remarks>
/// The order of <see cref="ParticleIds"/> is semantic: the first particle is the default occupation of a stable
/// site. The implicit empty set is exposed as <see cref="Empty"/> and uses the reserved
/// <see cref="ParticleSetId.Empty"/> value. Authored sets must not reuse that identifier.
/// </remarks>
public sealed record ParticleSet
{
    /// <summary>
    /// Initializes an authored particle set.
    /// </summary>
    /// <param name="id">The stable set identifier.</param>
    /// <param name="name">The user-visible set name.</param>
    /// <param name="particleIds">The ordered particle identifiers.</param>
    public ParticleSet(ParticleSetId id, string name, ImmutableArray<ParticleId> particleIds)
    {
        Id = id;
        Name = name;
        ParticleIds = particleIds.IsDefault ? [] : particleIds;
    }

    /// <summary>
    /// Gets the implicit empty particle set.
    /// </summary>
    /// <remarks>
    /// The empty set is reserved for internal use and is never persisted as an authored particle set.
    /// </remarks>
    public static ParticleSet Empty { get; } = new(ParticleSetId.Empty, "Empty", []);

    /// <summary>
    /// Gets the stable set identifier.
    /// </summary>
    public ParticleSetId Id { get; init; }

    /// <summary>
    /// Gets the user-visible set name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the ordered particle identifiers.
    /// </summary>
    /// <remarks>
    /// The first identifier is the default occupation of a stable site.
    /// </remarks>
    public ImmutableArray<ParticleId> ParticleIds { get; init; }
}
