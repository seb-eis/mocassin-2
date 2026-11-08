// SPDX-License-Identifier: MIT

using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;

namespace Mocassin.Symmetry.Geometry;

/// <summary>
/// Describes two reference sites whose P1 orbits contain the same position.
/// </summary>
/// <param name="FirstSiteId">The first site in expansion order.</param>
/// <param name="SecondSiteId">The site that produced the overlapping position.</param>
/// <param name="Position">The shared canonical P1 position.</param>
public sealed record OrbitOverlap(
    CellSiteId FirstSiteId,
    CellSiteId SecondSiteId,
    Fractional3 Position);
