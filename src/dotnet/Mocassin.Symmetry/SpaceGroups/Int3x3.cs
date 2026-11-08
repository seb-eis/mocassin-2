// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Mocassin.Symmetry;

/// <summary>
/// An exact 3×3 integer matrix used for the linear part of a crystallographic symmetry operation.
/// </summary>
/// <remarks>
/// Components use row-major naming: <see cref="M11"/> is row 1, column 1. The matrix is deliberately a value
/// type with integer arithmetic so composition, inversion, and closure remain exact.
/// </remarks>
public readonly record struct Int3x3(
    int M11,
    int M12,
    int M13,
    int M21,
    int M22,
    int M23,
    int M31,
    int M32,
    int M33)
{
    /// <summary>
    /// Gets the identity matrix.
    /// </summary>
    public static Int3x3 Identity => new(1, 0, 0, 0, 1, 0, 0, 0, 1);

    /// <summary>
    /// Gets the determinant of the matrix.
    /// </summary>
    public int Determinant =>
        (M11 * ((M22 * M33) - (M23 * M32)))
        - (M12 * ((M21 * M33) - (M23 * M31)))
        + (M13 * ((M21 * M32) - (M22 * M31)));

    /// <summary>
    /// Determines whether the matrix has a negative determinant and therefore flips orientation.
    /// </summary>
    public bool FlipsOrientation => Determinant < 0;

    /// <summary>
    /// Multiplies two matrices.
    /// </summary>
    /// <param name="left">The left matrix.</param>
    /// <param name="right">The right matrix.</param>
    /// <returns>The matrix product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3x3 operator *(Int3x3 left, Int3x3 right) =>
        new(
            (left.M11 * right.M11) + (left.M12 * right.M21) + (left.M13 * right.M31),
            (left.M11 * right.M12) + (left.M12 * right.M22) + (left.M13 * right.M32),
            (left.M11 * right.M13) + (left.M12 * right.M23) + (left.M13 * right.M33),
            (left.M21 * right.M11) + (left.M22 * right.M21) + (left.M23 * right.M31),
            (left.M21 * right.M12) + (left.M22 * right.M22) + (left.M23 * right.M32),
            (left.M21 * right.M13) + (left.M22 * right.M23) + (left.M23 * right.M33),
            (left.M31 * right.M11) + (left.M32 * right.M21) + (left.M33 * right.M31),
            (left.M31 * right.M12) + (left.M32 * right.M22) + (left.M33 * right.M32),
            (left.M31 * right.M13) + (left.M32 * right.M23) + (left.M33 * right.M33));

    /// <summary>
    /// Transforms an integer vector.
    /// </summary>
    /// <param name="x">The x component.</param>
    /// <param name="y">The y component.</param>
    /// <param name="z">The z component.</param>
    /// <returns>The transformed components.</returns>
    public (int X, int Y, int Z) Transform(int x, int y, int z) =>
        (
            checked((M11 * x) + (M12 * y) + (M13 * z)),
            checked((M21 * x) + (M22 * y) + (M23 * z)),
            checked((M31 * x) + (M32 * y) + (M33 * z)));

    /// <summary>
    /// Transforms a long integer vector.
    /// </summary>
    /// <param name="x">The x component.</param>
    /// <param name="y">The y component.</param>
    /// <param name="z">The z component.</param>
    /// <returns>The transformed components.</returns>
    public (long X, long Y, long Z) Transform(long x, long y, long z) =>
        (
            checked((M11 * x) + (M12 * y) + (M13 * z)),
            checked((M21 * x) + (M22 * y) + (M23 * z)),
            checked((M31 * x) + (M32 * y) + (M33 * z)));

    /// <summary>
    /// Transforms an exact crystallographic translation numerator vector.
    /// </summary>
    /// <param name="translation">The translation.</param>
    /// <returns>The normalized transformed translation.</returns>
    public Rational3 Transform(Rational3 translation)
    {
        var (x, y, z) = Transform(translation.A, translation.B, translation.C);
        return new Rational3(x, y, z);
    }

    /// <summary>
    /// Computes the exact inverse of a crystallographic linear part.
    /// </summary>
    /// <returns>The inverse matrix.</returns>
    /// <exception cref="InvalidOperationException">The determinant is not <c>+1</c> or <c>-1</c>.</exception>
    public Int3x3 Inverse()
    {
        var determinant = Determinant;
        if (determinant is not (1 or -1))
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"A crystallographic linear part must have determinant +1 or -1, but was {determinant}."));
        }

        var c11 = (M22 * M33) - (M23 * M32);
        var c12 = -((M21 * M33) - (M23 * M31));
        var c13 = (M21 * M32) - (M22 * M31);
        var c21 = -((M12 * M33) - (M13 * M32));
        var c22 = (M11 * M33) - (M13 * M31);
        var c23 = -((M11 * M32) - (M12 * M31));
        var c31 = (M12 * M23) - (M13 * M22);
        var c32 = -((M11 * M23) - (M13 * M21));
        var c33 = (M11 * M22) - (M12 * M21);

        return new Int3x3(
            determinant * c11,
            determinant * c21,
            determinant * c31,
            determinant * c12,
            determinant * c22,
            determinant * c32,
            determinant * c13,
            determinant * c23,
            determinant * c33);
    }

    /// <inheritdoc />
    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"[{M11},{M12},{M13}; {M21},{M22},{M23}; {M31},{M32},{M33}]");
}
