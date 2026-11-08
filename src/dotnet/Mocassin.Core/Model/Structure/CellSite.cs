// SPDX-License-Identifier: MIT

using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Structure;

/// <summary>
/// A reference site in the crystal structure.
/// </summary>
/// <param name="Id">The stable cell-site identifier.</param>
/// <param name="Name">The user-visible site name.</param>
/// <param name="Fractional">The authored fractional position.</param>
/// <param name="ParticleSetId">The particle set allowed to occupy the site.</param>
/// <param name="Stability">Whether the site is stable or unstable.</param>
/// <remarks>
/// The fractional position remains a double-based user value in the project. Symmetry converts it to the
/// exact internal grid for P1 expansion, overlap detection, and canonical ordering.
/// </remarks>
public sealed record CellSite(
    CellSiteId Id,
    string Name,
    Fractional3 Fractional,
    ParticleSetId ParticleSetId,
    SiteStability Stability);
