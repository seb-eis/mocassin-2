// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Runtime.CompilerServices;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;

using Mocassin.Symmetry.Diagnostics;
namespace Mocassin.Symmetry.Geometry;

/// <summary>
/// An exact fractional point scaled by the symmetry grid denominator.
/// </summary>
/// <remarks>
/// <para>
/// Components are <see cref="long"/> integers scaled by
/// <see cref="Denominator"/> = <c>24 × 10^12</c>. The factor 24 is divisible by every crystallographic
/// translation denominator (2, 3, 4, 6, 8, 12, and 24); the factor <c>10^12</c> preserves any input decimal
/// with up to 12 fractional digits exactly.
/// </para>
/// <para>
/// This type is internal: the public and persisted coordinate type remains <see cref="Fractional3"/> with
/// <see cref="double"/> components. The grid is the discrete path used for orbit expansion, deduplication,
/// ordering, and site-overlap detection.
/// </para>
/// </remarks>
internal readonly record struct GridFractional3(long A, long B, long C) : IComparable<GridFractional3>
{
    /// <summary>
    /// Gets the grid denominator <c>24 × 10^12</c>.
    /// </summary>
    public const long Denominator = 24_000_000_000_000L;

    /// <summary>
    /// Gets the number of grid steps in one crystallographic translation unit (<c>1/24</c>).
    /// </summary>
    public const long TranslationUnit = Denominator / 24L;

    /// <summary>
    /// Gets the zero grid point.
    /// </summary>
    public static GridFractional3 Zero => default;

    private const decimal denominatorDecimal = 24_000_000_000_000m;
    private const double halfGridStep = 0.5 / 24_000_000_000_000.0;

    /// <summary>
    /// Adds two grid points exactly.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The exact sum.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GridFractional3 operator +(GridFractional3 left, GridFractional3 right) =>
        new(checked(left.A + right.A), checked(left.B + right.B), checked(left.C + right.C));

    /// <summary>
    /// Subtracts two grid points exactly.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The exact difference.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GridFractional3 operator -(GridFractional3 left, GridFractional3 right) =>
        new(checked(left.A - right.A), checked(left.B - right.B), checked(left.C - right.C));

    /// <summary>
    /// Negates a grid point exactly.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns>The exact negation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GridFractional3 operator -(GridFractional3 value) =>
        new(checked(-value.A), checked(-value.B), checked(-value.C));

    /// <summary>
    /// Converts a public fractional coordinate to the exact grid.
    /// </summary>
    /// <param name="value">The fractional coordinate.</param>
    /// <param name="grid">The converted grid point when successful.</param>
    /// <param name="diagnostic">
    /// A structured error when conversion fails, or a warning when the input required rounding to the nearest
    /// grid point.
    /// </param>
    /// <returns><see langword="true"/> when conversion succeeded, including a rounded conversion.</returns>
    public static bool TryFromFractional(
        Fractional3 value,
        out GridFractional3 grid,
        out Diagnostic? diagnostic)
    {
        grid = default;
        diagnostic = null;

        if (!value.IsFinite)
        {
            diagnostic = new Diagnostic(
                SymmetryDiagnosticCodes.NonFiniteCoordinate,
                DiagnosticSeverity.Error,
                "Fractional coordinates must be finite (not NaN or infinity).");
            return false;
        }

        if (!TryScale(value.A, out var a, out var roundedA)
            || !TryScale(value.B, out var b, out var roundedB)
            || !TryScale(value.C, out var c, out var roundedC))
        {
            diagnostic = new Diagnostic(
                SymmetryDiagnosticCodes.CoordinateOutsideGridRange,
                DiagnosticSeverity.Error,
                "A fractional coordinate is outside the representable exact-grid range.");
            return false;
        }

        grid = new GridFractional3(a, b, c);
        if (roundedA || roundedB || roundedC)
        {
            diagnostic = new Diagnostic(
                SymmetryDiagnosticCodes.CoordinateRoundedToGrid,
                DiagnosticSeverity.Warning,
                "A fractional coordinate had more than 12 fractional digits and was rounded to the nearest exact-grid point.");
        }

        return true;
    }

    /// <summary>
    /// Converts the grid point back to public fractional coordinates.
    /// </summary>
    /// <returns>The fractional coordinate.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Fractional3 ToFractional3() =>
        new((double)A / Denominator, (double)B / Denominator, (double)C / Denominator);

    /// <summary>
    /// Maps the grid point into the half-open unit cell <c>[0, 1)</c>.
    /// </summary>
    /// <returns>The trimmed grid point.</returns>
    public GridFractional3 TrimToUnitCell() =>
        new(Modulo(A), Modulo(B), Modulo(C));

    /// <summary>
    /// Compares grid points in canonical order: first component most significant.
    /// </summary>
    /// <param name="other">The other grid point.</param>
    /// <returns>A signed comparison value.</returns>
    public int CompareTo(GridFractional3 other)
    {
        var comparison = A.CompareTo(other.A);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = B.CompareTo(other.B);
        return comparison != 0 ? comparison : C.CompareTo(other.C);
    }

    /// <inheritdoc />
    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"({A}/{Denominator}, {B}/{Denominator}, {C}/{Denominator})");

    private static bool TryScale(double value, out long scaled, out bool rounded)
    {
        scaled = 0;
        rounded = false;

        if (value == 0.0)
        {
            return true;
        }

        if (Math.Abs(value) < halfGridStep)
        {
            return true;
        }

        var text = value.ToString("R", CultureInfo.InvariantCulture);
        if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        var exact = parsed * denominatorDecimal;
        if (exact < long.MinValue || exact > long.MaxValue)
        {
            return false;
        }

        var nearest = decimal.Round(exact, 0, MidpointRounding.AwayFromZero);
        rounded = nearest != exact;
        scaled = (long)nearest;
        return true;
    }

    private static long Modulo(long value)
    {
        var remainder = value % Denominator;
        return remainder < 0 ? remainder + Denominator : remainder;
    }
}
