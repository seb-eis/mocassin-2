// SPDX-License-Identifier: MIT

using FsCheck.Xunit;
using Mocassin.Core.Geometry;

namespace Mocassin.Symmetry.Tests;

public sealed class SymmetryOperationTests
{
    private static readonly Int3x3 identity = Int3x3.Identity;
    private static readonly Int3x3 inversion = new(-1, 0, 0, 0, -1, 0, 0, 0, -1);
    private static readonly Int3x3 twoFoldZ = new(-1, 0, 0, 0, -1, 0, 0, 0, 1);
    private static readonly Int3x3 mirrorX = new(-1, 0, 0, 0, 1, 0, 0, 0, 1);

    [Fact]
    public void IdentityHasUnitDeterminantAndTransformsExactly()
    {
        var operation = SymmetryOperation.Identity;
        var point = new Fractional3(0.25, 0.5, 0.75);

        Assert.Equal(1, operation.Determinant);
        Assert.False(operation.FlipsOrientation);
        Assert.Equal(point, operation.Transform(point));
    }

    [Fact]
    public void CompositionAppliesTheOtherOperationFirst()
    {
        var mirror = new SymmetryOperation(mirrorX, new Rational3(12, 0, 0));
        var rotation = new SymmetryOperation(twoFoldZ, new Rational3(0, 0, 0));
        var point = new Fractional3(0.2, 0.3, 0.4);

        var composed = mirror.Compose(rotation).Transform(point);
        var sequential = mirror.Transform(rotation.Transform(point));

        Assert.Equal(sequential.A, composed.A, 12);
        Assert.Equal(sequential.B, composed.B, 12);
        Assert.Equal(sequential.C, composed.C, 12);
    }

    [Fact]
    public void InverseComposesToIdentity()
    {
        var operation = new SymmetryOperation(mirrorX, new Rational3(12, 3, 8));

        Assert.Equal(SymmetryOperation.Identity, operation.Compose(operation.Inverse()));
        Assert.Equal(SymmetryOperation.Identity, operation.Inverse().Compose(operation));
    }

    [Fact]
    public void ScrewOperationHasTheExpectedOrder()
    {
        var screw = new SymmetryOperation(twoFoldZ, new Rational3(0, 0, 12));

        Assert.NotEqual(SymmetryOperation.Identity, screw);
        Assert.Equal(SymmetryOperation.Identity, screw.Compose(screw));
    }

    [Fact]
    public void DeterminantAndOrientationFollowTheLinearPart()
    {
        Assert.Equal(1, new SymmetryOperation(twoFoldZ, Rational3.Zero).Determinant);
        Assert.Equal(-1, new SymmetryOperation(inversion, Rational3.Zero).Determinant);
        Assert.Equal(-1, new SymmetryOperation(mirrorX, Rational3.Zero).Determinant);
        Assert.True(new SymmetryOperation(inversion, Rational3.Zero).FlipsOrientation);
        Assert.False(new SymmetryOperation(twoFoldZ, Rational3.Zero).FlipsOrientation);
    }

    [Fact]
    public void RationalTranslationNormalizesModuloTheLattice()
    {
        var translation = new Rational3(-1, 25, 49);

        Assert.Equal(23, translation.A);
        Assert.Equal(1, translation.B);
        Assert.Equal(1, translation.C);
        Assert.Equal(new Fractional3(23.0 / 24.0, 1.0 / 24.0, 1.0 / 24.0), translation.ToFractional3());
    }

    [Fact]
    public void ToAffineMatrixUsesRowMajorOrder()
    {
        var operation = new SymmetryOperation(mirrorX, new Rational3(12, 0, 6));

        var matrix = operation.ToAffineMatrix();

        Assert.Equal(16, matrix.Length);
        Assert.Equal(
            [
                -1.0, 0.0, 0.0, 0.5,
                0.0, 1.0, 0.0, 0.0,
                0.0, 0.0, 1.0, 0.25,
                0.0, 0.0, 0.0, 1.0,
            ],
            matrix.ToArray());
    }

    [Fact]
    public void GridTransformStaysOnTheExactGrid()
    {
        var operation = new SymmetryOperation(mirrorX, new Rational3(12, 1, 0));
        var point = new GridFractional3(6_000_000_000_000L, 12_000_000_000_000L, 18_000_000_000_000L);

        var transformed = operation.Transform(point);

        Assert.Equal(6_000_000_000_000L, transformed.A);
        Assert.Equal(13_000_000_000_000L, transformed.B);
        Assert.Equal(18_000_000_000_000L, transformed.C);
    }

    [Fact]
    public void InverseRejectsNonUnimodularLinearParts()
    {
        var singular = new SymmetryOperation(new Int3x3(1, 0, 0, 0, 2, 0, 0, 0, 1), Rational3.Zero);

        Assert.Throws<InvalidOperationException>(() => singular.Inverse());
    }

    [Property(MaxTest = 300)]
    public bool CompositionIsAssociative(
        int a1,
        int a2,
        int a3,
        int b1,
        int b2,
        int b3,
        int c1,
        int c2,
        int c3)
    {
        var a = new SymmetryOperation(mirrorX, new Rational3(a1, a2, a3));
        var b = new SymmetryOperation(twoFoldZ, new Rational3(b1, b2, b3));
        var c = new SymmetryOperation(inversion, new Rational3(c1, c2, c3));

        return a.Compose(b).Compose(c) == a.Compose(b.Compose(c));
    }

    [Property(MaxTest = 300)]
    public bool CompositionIsClosedOverUnimodularMatrices(
        int a1,
        int a2,
        int a3,
        int b1,
        int b2,
        int b3)
    {
        var a = new SymmetryOperation(mirrorX, new Rational3(a1, a2, a3));
        var b = new SymmetryOperation(twoFoldZ, new Rational3(b1, b2, b3));
        var composed = a.Compose(b);

        return Math.Abs(composed.Determinant) == 1
            && composed.Translation.A is >= 0 and < Rational3.Denominator
            && composed.Translation.B is >= 0 and < Rational3.Denominator
            && composed.Translation.C is >= 0 and < Rational3.Denominator;
    }

    [Property(MaxTest = 300)]
    public bool GridTransformMatchesExactIntegerArithmetic(
        int a1,
        int a2,
        int a3,
        int x,
        int y,
        int z)
    {
        var operation = new SymmetryOperation(twoFoldZ, new Rational3(a1, a2, a3));
        var point = new GridFractional3(x, y, z);

        var transformed = operation.Transform(point);
        var translation = operation.Translation;
        var expected = new GridFractional3(
            checked((-x) + (translation.A * GridFractional3.TranslationUnit)),
            checked((-y) + (translation.B * GridFractional3.TranslationUnit)),
            checked(z + (translation.C * GridFractional3.TranslationUnit)));

        return transformed == expected;
    }

    [Fact]
    public void IdentityLinearPartIsExact()
    {
        Assert.Equal(identity, Int3x3.Identity * Int3x3.Identity);
        Assert.Equal(inversion, inversion.Inverse());
        Assert.Equal(twoFoldZ, twoFoldZ.Inverse());
        Assert.Equal(mirrorX, mirrorX.Inverse());
    }
}
