// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Mocassin.Core.Geometry;

/// <summary>
/// A generic four-component vector.
/// </summary>
/// <typeparam name="T">The numeric component type.</typeparam>
/// <remarks>
/// This is a general math primitive for rows, homogeneous coordinates, and future rendering/authoring
/// transforms. It does not define homogeneous point/direction semantics; callers own that convention.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1000:Do not declare static members on generic types",
    Justification = "Generic math primitives require static operators, identity values, and factories.")]
public readonly record struct Vector4<T>(T X, T Y, T Z, T W)
    where T : unmanaged, INumber<T>
{
    /// <summary>
    /// Gets the zero vector.
    /// </summary>
    public static Vector4<T> Zero => new(T.Zero, T.Zero, T.Zero, T.Zero);

    /// <summary>
    /// Computes the dot product with another vector.
    /// </summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The dot product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Dot(Vector4<T> other) => Dot(this, other);

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
    public static Vector4<T> operator +(Vector4<T> left, Vector4<T> right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);

    /// <summary>
    /// Subtracts two vectors component-wise.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The component-wise difference.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<T> operator -(Vector4<T> left, Vector4<T> right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);

    /// <summary>
    /// Negates a vector.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns>The negated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<T> operator -(Vector4<T> value) =>
        new(-value.X, -value.Y, -value.Z, -value.W);

    /// <summary>
    /// Scales a vector by a scalar.
    /// </summary>
    /// <param name="value">The vector.</param>
    /// <param name="factor">The scalar factor.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<T> operator *(Vector4<T> value, T factor) =>
        new(value.X * factor, value.Y * factor, value.Z * factor, value.W * factor);

    /// <summary>
    /// Scales a vector by a scalar.
    /// </summary>
    /// <param name="factor">The scalar factor.</param>
    /// <param name="value">The vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<T> operator *(T factor, Vector4<T> value) => value * factor;

    /// <summary>
    /// Divides a vector by a scalar.
    /// </summary>
    /// <param name="value">The vector.</param>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The divided vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<T> operator /(Vector4<T> value, T divisor) =>
        new(value.X / divisor, value.Y / divisor, value.Z / divisor, value.W / divisor);

    /// <summary>
    /// Computes the dot product of two vectors.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The dot product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Dot(Vector4<T> left, Vector4<T> right) =>
        (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z) + (left.W * right.W);

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X}, {Y}, {Z}, {W})");
}
