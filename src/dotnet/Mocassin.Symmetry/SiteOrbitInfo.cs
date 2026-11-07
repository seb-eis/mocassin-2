// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;

namespace Mocassin.Symmetry;

/// <summary>
/// Describes the canonical P1 orbit of one reference site.
/// </summary>
/// <param name="ReferenceSiteIndex">The zero-based index of the input reference position.</param>
/// <param name="Representative">The first canonical orbit position.</param>
/// <param name="Positions">The orbit positions in canonical P order.</param>
/// <param name="PositionIndices">The canonical P indices in the same order as <paramref name="Positions"/>.</param>
public sealed record SiteOrbitInfo(
    int ReferenceSiteIndex,
    Fractional3 Representative,
    ImmutableArray<Fractional3> Positions,
    ImmutableArray<int> PositionIndices)
{
    /// <summary>
    /// Gets the number of distinct positions in the orbit.
    /// </summary>
    public int Multiplicity => Positions.Length;
}
