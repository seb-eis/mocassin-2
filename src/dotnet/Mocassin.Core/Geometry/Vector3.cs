// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Mocassin.Core.Geometry;

/// <summary>
/// Internal generic three-component arithmetic kernel shared by the semantic coordinate types.
/// </summary>
/// <typeparam name="T">The numeric component type.</typeparam>
/// <remarks>
/// This type exists so the public <see cref="Fractional3"/> and <see cref="Cartesian3"/> wrappers do not
/// duplicate add/subtract/scale/dot/cross arithmetic. It is not a public coordinate type: the semantic
/// wrappers keep fractional and Cartesian spaces distinct at compile time.
/// </remarks>
internal readonly record struct Vector3<T>(T X, T Y, T Z)
    where T : unmanaged, INumber<T>
{
    /// <summary>
    /// Gets the zero vector.
    /// </summary>
    public static Vector3<T> Zero => new(T.Zero, T.Zero, T.Zero);

    /// <summary>
    /// Gets the sum of the squared components.
    /// </summary>
    public T LengthSquared
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Dot(this, this);
    }

    /// <summary>
    /// Adds two vectors component-wise.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The component-wise sum.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<T> operator +(Vector3<T> left, Vector3<T> right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    /// <summary>
    /// Subtracts two vectors component-wise.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The component-wise difference.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<T> operator -(Vector3<T> left, Vector3<T> right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    /// <summary>
    /// Negates a vector.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns>The negated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<T> operator -(Vector3<T> value) => new(-value.X, -value.Y, -value.Z);

    /// <summary>
    /// Scales a vector by a scalar.
    /// </summary>
    /// <param name="value">The vector.</param>
    /// <param name="factor">The scalar factor.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<T> operator *(Vector3<T> value, T factor) =>
        new(value.X * factor, value.Y * factor, value.Z * factor);

    /// <summary>
    /// Scales a vector by a scalar.
    /// </summary>
    /// <param name="factor">The scalar factor.</param>
    /// <param name="value">The vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<T> operator *(T factor, Vector3<T> value) => value * factor;

    /// <summary>
    /// Divides a vector by a scalar.
    /// </summary>
    /// <param name="value">The vector.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The divided vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<T> operator /(Vector3<T> value, T divisor) =>
        new(value.X / divisor, value.Y / divisor, value.Z / divisor);

    /// <summary>
    /// Computes the dot product of two vectors.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The dot product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Dot(Vector3<T> left, Vector3<T> right) =>
        (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);

    /// <summary>
    /// Computes the cross product of two vectors.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The cross product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<T> Cross(Vector3<T> left, Vector3<T> right) =>
        new(
            (left.Y * right.Z) - (left.Z * right.Y),
            (left.Z * right.X) - (left.X * right.Z),
            (left.X * right.Y) - (left.Y * right.X));

    /// <summary>
    /// Returns the invariant-culture representation of the vector.
    /// </summary>
    /// <returns>A tuple-style textual representation.</returns>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X}, {Y}, {Z})");
}
