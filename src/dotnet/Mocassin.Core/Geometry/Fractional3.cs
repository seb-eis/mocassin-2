// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Mocassin.Core.Geometry;

/// <summary>
/// A point or vector in the fractional-coordinate basis of a unit cell.
/// </summary>
/// <remarks>
/// Fractional coordinates are stored as <see cref="double"/> values. Discrete symmetry work converts them to
/// the internal exact grid before deduplication or ordering; this type never implies a Cartesian space.
/// </remarks>
public readonly record struct Fractional3(double A, double B, double C)
{
    /// <summary>
    /// Gets the fractional origin <c>(0, 0, 0)</c>.
    /// </summary>
    public static Fractional3 Zero => new(0.0, 0.0, 0.0);

    /// <summary>
    /// Determines whether every component is finite.
    /// </summary>
    public bool IsFinite => double.IsFinite(A) && double.IsFinite(B) && double.IsFinite(C);

    /// <summary>
    /// Adds two fractional vectors component-wise.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The component-wise sum.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fractional3 operator +(Fractional3 left, Fractional3 right) =>
        Rewrap(Unwrap(left) + Unwrap(right));

    /// <summary>
    /// Subtracts two fractional vectors component-wise.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The component-wise difference.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fractional3 operator -(Fractional3 left, Fractional3 right) =>
        Rewrap(Unwrap(left) - Unwrap(right));

    /// <summary>
    /// Negates a fractional vector.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns>The negated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fractional3 operator -(Fractional3 value) => Rewrap(-Unwrap(value));

    /// <summary>
    /// Scales a fractional vector by a scalar.
    /// </summary>
    /// <param name="value">The vector.</param>
    /// <param name="factor">The scalar factor.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fractional3 operator *(Fractional3 value, double factor) => Rewrap(Unwrap(value) * factor);

    /// <summary>
    /// Scales a fractional vector by a scalar.
    /// </summary>
    /// <param name="factor">The scalar factor.</param>
    /// <param name="value">The vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fractional3 operator *(double factor, Fractional3 value) => value * factor;

    /// <summary>
    /// Divides a fractional vector by a scalar.
    /// </summary>
    /// <param name="value">The vector.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The divided vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fractional3 operator /(Fractional3 value, double divisor) => Rewrap(Unwrap(value) / divisor);

    /// <summary>
    /// Computes the dot product of two fractional vectors.
    /// </summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The dot product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Dot(Fractional3 other) => Vector3<double>.Dot(Unwrap(this), Unwrap(other));

    /// <summary>
    /// Computes the cross product of two fractional vectors.
    /// </summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The cross product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Fractional3 Cross(Fractional3 other) => Rewrap(Vector3<double>.Cross(Unwrap(this), Unwrap(other)));

    /// <summary>
    /// Gets the squared Euclidean length of the vector.
    /// </summary>
    public double LengthSquared
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unwrap(this).LengthSquared;
    }

    /// <summary>
    /// Gets the Euclidean length of the vector.
    /// </summary>
    public double Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Math.Sqrt(LengthSquared);
    }

    /// <summary>
    /// Maps every component into the half-open unit interval <c>[0, 1)</c>, snapping components within
    /// <see cref="GeometryTolerances.FractionalTrimTolerance"/> of an integer to that integer first.
    /// </summary>
    /// <returns>The trimmed fractional vector.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A component is not finite.</exception>
    public Fractional3 TrimToUnitCell() => new(TrimComponent(A), TrimComponent(B), TrimComponent(C));

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({A}, {B}, {C})");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Fractional3 Rewrap(Vector3<double> value) => new(value.X, value.Y, value.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3<double> Unwrap(Fractional3 value) => new(value.A, value.B, value.C);

    private static double TrimComponent(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Fractional coordinates must be finite before trimming.");
        }

        var nearestInteger = Math.Round(value, MidpointRounding.AwayFromZero);
        if (Math.Abs(value - nearestInteger) <= GeometryTolerances.FractionalTrimTolerance)
        {
            value = nearestInteger;
        }

        var trimmed = value - Math.Floor(value);
        return trimmed == 0.0 ? 0.0 : trimmed;
    }
}
