// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Transitions;

/// <summary>
/// A Metropolis transition bound to an abstract chain and two reference sites.
/// </summary>
/// <param name="Id">The stable Metropolis-transition identifier.</param>
/// <param name="Name">The user-visible transition name.</param>
/// <param name="ChainId">The abstract chain used by the transition.</param>
/// <param name="SiteAId">The first reference site.</param>
/// <param name="SiteBId">The second reference site.</param>
/// <param name="ReverseTransitionId">The optional explicitly defined reverse transition.</param>
/// <remarks>
/// <see cref="SiteAId"/> and <see cref="SiteBId"/> may be equal, which represents an intra-sublattice exchange.
/// </remarks>
public sealed record MetropolisTransition(
    MetropolisTransitionId Id,
    string Name,
    TransitionChainId ChainId,
    CellSiteId SiteAId,
    CellSiteId SiteBId,
    MetropolisTransitionId? ReverseTransitionId);
