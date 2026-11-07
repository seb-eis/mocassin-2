// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Geometry;

/// <summary>
/// Named, unit-specific tolerances used by coordinate and cell geometry.
/// </summary>
/// <remarks>
/// Tolerances are deliberately explicit and unit-qualified. Discrete symmetry work uses the exact
/// fractional grid instead of any tolerance comparer.
/// </remarks>
public static class GeometryTolerances
{
    /// <summary>
    /// Maximum distance from an integer for boundary snapping before exact grid conversion, in fractional
    /// coordinate units.
    /// </summary>
    /// <remarks>
    /// This is a user-input convenience only. It is not the deduplication criterion and may move a point by
    /// many grid steps when the input is close to a cell boundary.
    /// </remarks>
    public const double FractionalTrimTolerance = 1e-10;
}
