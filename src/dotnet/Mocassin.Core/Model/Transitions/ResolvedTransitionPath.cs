// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;

namespace Mocassin.Core.Model.Transitions;

/// <summary>
/// One kinetic reference-path point resolved against the structure's P1 positions and owning reference site.
/// </summary>
/// <param name="SiteId">The reference site whose P1 orbit owns the position.</param>
/// <param name="P">The canonical per-cell position index.</param>
/// <param name="Encoding">The full cell offset plus P encoding.</param>
/// <param name="Stability">The stability of the owning reference site.</param>
/// <param name="CanonicalPosition">The exact canonical P1 fractional coordinate.</param>
public sealed record ResolvedPathPosition(
    CellSiteId SiteId,
    int P,
    Vector4I Encoding,
    SiteStability Stability,
    Fractional3 CanonicalPosition);

/// <summary>
/// A kinetic reference path resolved to canonical P1 positions.
/// </summary>
/// <param name="Positions">The resolved positions in path order.</param>
public sealed record ResolvedTransitionPath(ImmutableArray<ResolvedPathPosition> Positions);
