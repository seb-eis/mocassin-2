// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Mocassin.Core.Geometry;

/// <summary>
/// Encodes an integer unit-cell offset together with a per-cell canonical P1 position index.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="A"/>, <see cref="B"/>, and <see cref="C"/> are cell offsets, not linear indices. Negative
/// offsets are relative-path coordinates and must be converted to an explicit in-supercell offset before
/// linearization.
/// </para>
/// <para>
/// <see cref="P"/> is the per-cell canonical position index in <c>[0, Pcount)</c>. Base P1 positions use
/// <c>(0, 0, 0, P)</c>. This is an encoding type, not a generic spatial vector; it deliberately does not
/// share the floating-point vector kernel.
/// </para>
/// </remarks>
public readonly record struct Vector4I(int A, int B, int C, int P)
{
    /// <summary>
    /// Gets the zero encoding <c>(0, 0, 0, 0)</c>.
    /// </summary>
    public static Vector4I Zero => new(0, 0, 0, 0);

    /// <summary>
    /// Adds two encodings component-wise.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The component-wise sum.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4I operator +(Vector4I left, Vector4I right) =>
        new(left.A + right.A, left.B + right.B, left.C + right.C, left.P + right.P);

    /// <summary>
    /// Subtracts two encodings component-wise.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The component-wise difference.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4I operator -(Vector4I left, Vector4I right) =>
        new(left.A - right.A, left.B - right.B, left.C - right.C, left.P - right.P);

    /// <summary>
    /// Negates an encoding component-wise.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns>The negated encoding.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4I operator -(Vector4I value) => new(-value.A, -value.B, -value.C, -value.P);

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({A}, {B}, {C}, {P})");
}
