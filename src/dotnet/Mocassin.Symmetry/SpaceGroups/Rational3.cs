// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Runtime.CompilerServices;
using Mocassin.Core.Geometry;

namespace Mocassin.Symmetry.SpaceGroups;

/// <summary>
/// An exact crystallographic translation with a fixed denominator of 24, normalized to <c>[0, 1)</c>.
/// </summary>
/// <remarks>
/// <para>
/// The numerator components are stored in <c>[0, <see cref="Denominator"/>)</c>. Denominator 24 covers every
/// translation denominator in the crystallographic catalog: 2, 3, 4, 6, 8, 12, and 24.
/// </para>
/// <para>
/// This is an exact arithmetic type. It is not a general-purpose rational; the denominator is part of the
/// crystallographic contract.
/// </para>
/// </remarks>
public readonly record struct Rational3
{
    /// <summary>
    /// Gets the fixed translation denominator.
    /// </summary>
    public const int Denominator = 24;

    /// <summary>
    /// Initializes a normalized translation from three numerators.
    /// </summary>
    /// <param name="a">The numerator of the first component.</param>
    /// <param name="b">The numerator of the second component.</param>
    /// <param name="c">The numerator of the third component.</param>
    public Rational3(int a, int b, int c)
    {
        A = Normalize(a);
        B = Normalize(b);
        C = Normalize(c);
    }

    /// <summary>
    /// Gets the first normalized numerator.
    /// </summary>
    public int A { get; }

    /// <summary>
    /// Gets the second normalized numerator.
    /// </summary>
    public int B { get; }

    /// <summary>
    /// Gets the third normalized numerator.
    /// </summary>
    public int C { get; }

    /// <summary>
    /// Gets the zero translation.
    /// </summary>
    public static Rational3 Zero => default;

    /// <summary>
    /// Adds two translations modulo the lattice.
    /// </summary>
    /// <param name="other">The other translation.</param>
    /// <returns>The normalized sum.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rational3 Add(Rational3 other) => new(A + other.A, B + other.B, C + other.C);

    /// <summary>
    /// Negates a translation modulo the lattice.
    /// </summary>
    /// <returns>The normalized negation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rational3 Negate() => new(-A, -B, -C);

    /// <summary>
    /// Converts the translation to fractional coordinates.
    /// </summary>
    /// <returns>The fractional translation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Fractional3 ToFractional3() => new((double)A / Denominator, (double)B / Denominator, (double)C / Denominator);

    /// <summary>
    /// Determines whether this is the zero translation.
    /// </summary>
    /// <returns><see langword="true"/> when all components are zero.</returns>
    public bool IsZero => A == 0 && B == 0 && C == 0;

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({A}/{Denominator}, {B}/{Denominator}, {C}/{Denominator})");

    private static int Normalize(int numerator)
    {
        var remainder = numerator % Denominator;
        return remainder < 0 ? remainder + Denominator : remainder;
    }
}
