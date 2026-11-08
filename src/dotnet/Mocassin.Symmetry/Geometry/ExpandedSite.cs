// SPDX-License-Identifier: MIT

using Mocassin.Core.Geometry;

namespace Mocassin.Symmetry.Geometry;

/// <summary>
/// One canonical P1 position produced by symmetry expansion.
/// </summary>
/// <param name="P">The canonical per-cell position index.</param>
/// <param name="Encoding">The base-cell encoding <c>(0, 0, 0, P)</c>.</param>
/// <param name="Fractional">The exact-grid fractional coordinate.</param>
/// <param name="ReferenceSiteIndex">The zero-based index of the input reference position.</param>
/// <param name="OrbitMemberIndex">The zero-based index within the reference site's sorted orbit.</param>
public sealed record ExpandedSite(
    int P,
    Vector4I Encoding,
    Fractional3 Fractional,
    int ReferenceSiteIndex,
    int OrbitMemberIndex);
