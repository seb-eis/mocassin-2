// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.Symmetry.Energy;

/// <summary>
/// Advisory preview of one directed pair interaction geometry.
/// </summary>
/// <param name="GeometryId">The stable identifier of this directed geometry.</param>
/// <param name="EnergyId">The shared energy-table identity.</param>
/// <param name="SiteAId">The centre reference site.</param>
/// <param name="CenterP">The canonical P1 index of the centre position.</param>
/// <param name="SiteBId">The partner reference site.</param>
/// <param name="PartnerP">The canonical P1 index of the partner position.</param>
/// <param name="DistanceAngstrom">The minimum-image distance in ångström.</param>
/// <param name="IsChiral">Whether the inverse direction is a distinct chiral partner.</param>
/// <param name="ChiralPartnerGeometryId">The inverse geometry identifier when chiral.</param>
public sealed record PairInteractionPreview(
    string GeometryId,
    string EnergyId,
    CellSiteId SiteAId,
    int CenterP,
    CellSiteId SiteBId,
    int PartnerP,
    double DistanceAngstrom,
    bool IsChiral,
    string? ChiralPartnerGeometryId);
