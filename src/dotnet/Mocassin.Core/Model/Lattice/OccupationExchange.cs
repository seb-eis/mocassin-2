// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Lattice;

/// <summary>
/// A reusable particle exchange at one stable site that a doping can reference.
/// </summary>
/// <param name="Id">The stable occupation-exchange identifier.</param>
/// <param name="Name">The user-visible occupation-exchange name.</param>
/// <param name="SiteId">The stable site at which the exchange is applied.</param>
/// <param name="SourceParticleId">The particle that is replaced.</param>
/// <param name="TargetParticleId">The particle that replaces the source.</param>
/// <remarks>
/// The exchange has no effect until a <see cref="Doping"/> references it. Its charge delta is derived as
/// <c>target.charge - source.charge</c> and is never persisted. Occupation-exchange names are unique
/// case-insensitively within the collection.
/// </remarks>
public sealed record OccupationExchange(
    OccupationExchangeId Id,
    string Name,
    CellSiteId SiteId,
    ParticleId SourceParticleId,
    ParticleId TargetParticleId);
