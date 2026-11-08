// SPDX-License-Identifier: MIT

namespace Mocassin.Symmetry;

/// <summary>
/// Version constants that define the symmetry contract recorded in compiled packages.
/// </summary>
/// <remarks>
/// Changing the exact-grid denominator or conversion rule, canonical P1 order, pair-shell order, the
/// femtometre bucket, cluster canonicalization, or reduction semantics requires a new
/// <see cref="Version"/> and updated golden fixtures. The catalog version is independent and changes only
/// when the catalog payload, selection rule, or encoding changes.
/// </remarks>
public static class SymmetryContract
{
    /// <summary>
    /// Gets the symmetry contract version.
    /// </summary>
    public const string Version = "symmetry-1";

    /// <summary>
    /// Gets the exact fractional-grid denominator <c>24 × 10^12</c>.
    /// </summary>
    public const long FractionalGridDenominator = 24_000_000_000_000L;
}
