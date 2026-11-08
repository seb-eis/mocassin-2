// SPDX-License-Identifier: MIT

namespace Mocassin.Symmetry.Diagnostics;

/// <summary>
/// Structured diagnostic codes emitted by the exact symmetry grid.
/// </summary>
/// <remarks>
/// The complete diagnostic catalog is finalized in Step 07. These codes are stable within the symmetry
/// implementation and are documented with their severity at that point.
/// </remarks>
internal static class SymmetryDiagnosticCodes
{
    /// <summary>
    /// A fractional coordinate was NaN or infinite.
    /// </summary>
    public const string NonFiniteCoordinate = "SYM001";

    /// <summary>
    /// A fractional coordinate was rounded to the nearest exact-grid point.
    /// </summary>
    public const string CoordinateRoundedToGrid = "SYM002";

    /// <summary>
    /// A fractional coordinate is outside the representable exact-grid range.
    /// </summary>
    public const string CoordinateOutsideGridRange = "SYM003";
}
