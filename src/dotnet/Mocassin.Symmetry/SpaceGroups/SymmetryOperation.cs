// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using Mocassin.Core.Geometry;
using Mocassin.Symmetry.Geometry;

namespace Mocassin.Symmetry.SpaceGroups;

/// <summary>
/// An exact crystallographic symmetry operation <c>x ↦ R·x + t</c>.
/// </summary>
/// <remarks>
/// <para>
/// The linear part is a signed integer matrix and the translation is an exact rational with denominator 24.
/// Composition, inversion, and closure use integer arithmetic only; conversion to <see cref="double"/> happens
/// at the public geometry boundary.
/// </para>
/// <para>
/// <see cref="Compose"/> means “apply the other operation first, then this operation”:
/// <c>(this ∘ other)(x) = this(other(x))</c>.
/// </para>
/// </remarks>
public readonly record struct SymmetryOperation(Int3x3 Linear, Rational3 Translation)
{
    /// <summary>
    /// Gets the identity operation.
    /// </summary>
    public static SymmetryOperation Identity => new(Int3x3.Identity, Rational3.Zero);

    /// <summary>
    /// Gets the determinant of the linear part.
    /// </summary>
    public int Determinant => Linear.Determinant;

    /// <summary>
    /// Determines whether the operation flips orientation.
    /// </summary>
    public bool FlipsOrientation => Linear.FlipsOrientation;

    /// <summary>
    /// Composes this operation with another operation.
    /// </summary>
    /// <param name="other">The operation to apply first.</param>
    /// <returns>The composed operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SymmetryOperation Compose(SymmetryOperation other) =>
        new(Linear * other.Linear, Linear.Transform(other.Translation).Add(Translation));

    /// <summary>
    /// Computes the exact inverse operation.
    /// </summary>
    /// <returns>The inverse operation.</returns>
    /// <exception cref="InvalidOperationException">The linear determinant is not <c>+1</c> or <c>-1</c>.</exception>
    public SymmetryOperation Inverse()
    {
        var inverseLinear = Linear.Inverse();
        var inverseTranslation = inverseLinear.Transform(Translation).Negate();
        return new SymmetryOperation(inverseLinear, inverseTranslation);
    }

    /// <summary>
    /// Returns an operation with the translation normalized to <c>[0, 1)</c>.
    /// </summary>
    /// <returns>The normalized operation.</returns>
    public SymmetryOperation NormalizeTranslation() =>
        new(Linear, new Rational3(Translation.A, Translation.B, Translation.C));

    /// <summary>
    /// Transforms a public fractional coordinate.
    /// </summary>
    /// <param name="point">The fractional coordinate.</param>
    /// <returns>The transformed fractional coordinate.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Fractional3 Transform(Fractional3 point)
    {
        var translation = Translation.ToFractional3();
        return new Fractional3(
            (Linear.M11 * point.A) + (Linear.M12 * point.B) + (Linear.M13 * point.C) + translation.A,
            (Linear.M21 * point.A) + (Linear.M22 * point.B) + (Linear.M23 * point.C) + translation.B,
            (Linear.M31 * point.A) + (Linear.M32 * point.B) + (Linear.M33 * point.C) + translation.C);
    }

    /// <summary>
    /// Transforms an exact grid point without leaving integer arithmetic.
    /// </summary>
    /// <param name="point">The grid point.</param>
    /// <returns>The transformed grid point.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal GridFractional3 Transform(GridFractional3 point)
    {
        var (x, y, z) = Linear.Transform(point.A, point.B, point.C);
        var translationX = Translation.A * GridFractional3.TranslationUnit;
        var translationY = Translation.B * GridFractional3.TranslationUnit;
        var translationZ = Translation.C * GridFractional3.TranslationUnit;

        return new GridFractional3(
            checked(x + translationX),
            checked(y + translationY),
            checked(z + translationZ));
    }

    /// <summary>
    /// Converts the operation to a row-major 4×4 affine matrix.
    /// </summary>
    /// <returns>Sixteen <see cref="double"/> values in row-major order.</returns>
    public ImmutableArray<double> ToAffineMatrix()
    {
        var translation = Translation.ToFractional3();
        return
        [
            Linear.M11, Linear.M12, Linear.M13, translation.A,
            Linear.M21, Linear.M22, Linear.M23, translation.B,
            Linear.M31, Linear.M32, Linear.M33, translation.C,
            0.0, 0.0, 0.0, 1.0,
        ];
    }

    /// <inheritdoc />
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Linear} + {Translation}");
}
