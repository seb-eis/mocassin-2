// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Jobs;

/// <summary>
/// A KMC start-option optimizer that removes a particle from a reference site's selection pool.
/// </summary>
/// <param name="SiteId">The stable reference site.</param>
/// <param name="ParticleId">The particle removed from the start-option pool.</param>
public sealed record SelectionOptimizer(CellSiteId SiteId, ParticleId ParticleId);
