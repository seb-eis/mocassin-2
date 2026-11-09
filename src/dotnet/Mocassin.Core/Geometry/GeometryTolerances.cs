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

    /// <summary>
    /// Maximum Cartesian distance or vector-component difference for geometry equality, in ångström.
    /// </summary>
    public const double GeometricEqualityTolerance = 1e-6;

    /// <summary>
    /// Maximum fractional component difference for matching a kinetic reference-path point to a P1 position.
    /// </summary>
    public const double PathMatchTolerance = 1e-6;

    /// <summary>
    /// Maximum distance difference for continuous distance predicates outside symmetry reduction, in ångström.
    /// </summary>
    public const double DistanceEqualityTolerance = 1e-5;

    /// <summary>
    /// Maximum difference for cell-length equality, in ångström.
    /// </summary>
    public const double CellLengthTolerance = 1e-3;

    /// <summary>
    /// Maximum difference for cell-angle equality, in degrees.
    /// </summary>
    public const double CellAngleTolerance = 1e-3;

    /// <summary>
    /// Minimum positive Gram determinant for a non-degenerate cell.
    /// </summary>
    public const double CellDeterminantTolerance = 1e-12;

    /// <summary>
    /// Maximum accepted infinity-norm condition number of the cell matrix.
    /// </summary>
    /// <remarks>
    /// Within the v1 length range and the positive-determinant cutoff, realizable cells stay below this
    /// bound; it is a defensive guard for future extensions and extreme inputs.
    /// </remarks>
    public const double CellConditionLimit = 1e12;

    /// <summary>
    /// Discrete distance bucket for pair and cluster reduction, in ångström (1 fm).
    /// </summary>
    public const double PairDistanceBucket = 1e-5;
}
