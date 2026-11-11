// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Lattice;

/// <summary>
/// One derived default occupation of a reference site.
/// </summary>
/// <param name="SiteId">The reference-site identifier.</param>
/// <param name="ParticleId">The default particle, or <see cref="ParticleId.Void"/> for an unstable site.</param>
/// <remarks>
/// Building blocks are derived from the immutable project snapshot and are never persisted.
/// </remarks>
public sealed record BuildingBlockEntry(
    CellSiteId SiteId,
    ParticleId ParticleId);
