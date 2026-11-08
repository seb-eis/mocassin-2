// SPDX-License-Identifier: MIT

using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;

namespace Mocassin.Symmetry.Geometry;

/// <summary>
/// Identifies one authored reference site by its stable ID and fractional position.
/// </summary>
/// <param name="Id">The stable cell-site identifier.</param>
/// <param name="Position">The authored fractional position.</param>
public readonly record struct StructureSiteReference(CellSiteId Id, Fractional3 Position);
