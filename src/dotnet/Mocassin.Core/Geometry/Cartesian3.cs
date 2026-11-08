// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Mocassin.Core.Geometry;

/// <summary>
/// A point or vector in a Cartesian coordinate system, in ångström.
/// </summary>
/// <remarks>
/// This is the continuous geometry type used for cell vectors, distances, and radial search. It remains
/// distinct from <see cref="Fractional3"/> so the compiler can catch coordinate-space mistakes.
/// </remarks>
public readonly record struct Cartesian3(double X, double Y, double Z)
{
    /// <summary>
    /// Gets the Cartesian origin <c>(0, 0, 0)</c>.
    /// </summary>
    public static Cartesian3 Zero => new(0.0, 0.0, 0.0);

    /// <summary>
    /// Determines whether every component is finite.
    /// </summary>
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y) && double.IsFinite(Z);

    /// <summary>
    /// Adds two Cartesian vectors component-wise.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The component-wise sum.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Cartesian3 operator +(Cartesian3 left, Cartesian3 right) =>
        Rewrap(Unwrap(left) + Unwrap(right));

    /// <summary>
    /// Subtracts two Cartesian vectors component-wise.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The component-wise difference.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Cartesian3 operator -(Cartesian3 left, Cartesian3 right) =>
        Rewrap(Unwrap(left) - Unwrap(right));

    /// <summary>
    /// Negates a Cartesian vector.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns>The negated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Cartesian3 operator -(Cartesian3 value) => Rewrap(-Unwrap(value));

    /// <summary>
    /// Scales a Cartesian vector by a scalar.
    /// </summary>
    /// <param name="value">The vector.</param>
    /// <param name="factor">The scalar factor.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Cartesian3 operator *(Cartesian3 value, double factor) => Rewrap(Unwrap(value) * factor);

    /// <summary>
    /// Scales a Cartesian vector by a scalar.
    /// </summary>
    /// <param name="factor">The scalar factor.</param>
    /// <param name="value">The vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Cartesian3 operator *(double factor, Cartesian3 value) => value * factor;

    /// <summary>
    /// Divides a Cartesian vector by a scalar.
    /// </summary>
    /// <param name="value">The vector.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The divided vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Cartesian3 operator /(Cartesian3 value, double divisor) => Rewrap(Unwrap(value) / divisor);

    /// <summary>
    /// Computes the dot product of two Cartesian vectors.
    /// </summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The dot product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Dot(Cartesian3 other) => Vector3<double>.Dot(Unwrap(this), Unwrap(other));

    /// <summary>
    /// Computes the cross product of two Cartesian vectors.
    /// </summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The cross product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cartesian3 Cross(Cartesian3 other) => Rewrap(Vector3<double>.Cross(Unwrap(this), Unwrap(other)));

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
    /// Computes the Euclidean distance to another Cartesian point.
    /// </summary>
    /// <param name="other">The other point.</param>
    /// <returns>The distance in ångström.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double DistanceTo(Cartesian3 other) => (this - other).Length;

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X}, {Y}, {Z})");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Cartesian3 Rewrap(Vector3<double> value) => new(value.X, value.Y, value.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3<double> Unwrap(Cartesian3 value) => new(value.X, value.Y, value.Z);
}
