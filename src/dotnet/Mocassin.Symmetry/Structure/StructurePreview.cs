// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Symmetry.Geometry;

namespace Mocassin.Symmetry.Structure;

/// <summary>
/// A deterministic, non-compiled preview of a structure's symmetry expansion.
/// </summary>
/// <param name="PositionList">The canonical P1 positions in ascending <c>P</c> order.</param>
/// <param name="Encoder">The encoder that owns the fractional/encoding mapping.</param>
/// <param name="CartesianPositions">The Cartesian position of each canonical P1 position, in ångström.</param>
/// <param name="CellVolumeAngstrom3">The unit-cell volume in cubic ångström.</param>
/// <param name="Orbits">The per-reference-site orbit information.</param>
/// <param name="CellCountA">The requested supercell count along a.</param>
/// <param name="CellCountB">The requested supercell count along b.</param>
/// <param name="CellCountC">The requested supercell count along c.</param>
/// <remarks>
/// The preview is derived data for the UI and validation. It is not the compiled model; Step 08 produces the
/// authoritative compiled structure.
/// </remarks>
public sealed record StructurePreview(
    ImmutableArray<ExpandedSite> PositionList,
    UnitCellVectorEncoder Encoder,
    ImmutableArray<Cartesian3> CartesianPositions,
    double CellVolumeAngstrom3,
    ImmutableArray<SiteOrbitInfo> Orbits,
    int CellCountA,
    int CellCountB,
    int CellCountC);
