// SPDX-License-Identifier: MIT

using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Structure;

/// <summary>
/// A visual-only helper position in the crystal structure.
/// </summary>
/// <param name="Id">The stable dummy-position identifier.</param>
/// <param name="Name">The user-visible dummy-position name.</param>
/// <param name="Fractional">The authored fractional position.</param>
/// <remarks>
/// Dummy positions are visual helpers. They may coincide with reference sites and are excluded from all
/// simulation compilation.
/// </remarks>
public sealed record CellDummyPosition(
    CellDummyPositionId Id,
    string Name,
    Fractional3 Fractional);
