// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;
using Mocassin.Core.Units;

namespace Mocassin.Core.Model.Energy;

/// <summary>
/// Removes pair interactions whose centre-to-partner distance lies in the inclusive
/// <see cref="Start"/>..<see cref="End"/> range.
/// </summary>
/// <param name="Start">The inclusive lower distance bound in ångström.</param>
/// <param name="End">The inclusive upper distance bound in ångström.</param>
/// <param name="SiteAId">The centre site for an unstable environment, or the first site of a stable pair.</param>
/// <param name="SiteBId">The partner site.</param>
/// <remarks>
/// Stable-environment filters are symmetric: the pair <c>(SiteAId, SiteBId)</c> removes both interaction
/// directions. Unstable-environment filters are one-directional and require <see cref="SiteAId"/> to equal the
/// owning environment's site. Filter order is not semantic; canonical serialization sorts by
/// <c>(start, end, siteAId, siteBId)</c>.
/// </remarks>
public sealed record InteractionFilter(
    LengthAngstrom Start,
    LengthAngstrom End,
    CellSiteId SiteAId,
    CellSiteId SiteBId);
