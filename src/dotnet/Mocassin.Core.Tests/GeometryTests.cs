// SPDX-License-Identifier: MIT

using FsCheck.Xunit;
using Mocassin.Core.Geometry;

namespace Mocassin.Core.Tests;

public sealed class GeometryTests
{
    [Fact]
    public void FractionalArithmeticMatchesGenericKernel()
    {
        var left = new Fractional3(1.5, -2.25, 3.0);
        var right = new Fractional3(-0.5, 4.0, 0.125);

        var leftKernel = new Vector3<double>(left.A, left.B, left.C);
        var rightKernel = new Vector3<double>(right.A, right.B, right.C);

        Assert.Equal(ToFractional(leftKernel + rightKernel), left + right);
        Assert.Equal(ToFractional(leftKernel - rightKernel), left - right);
        Assert.Equal(ToFractional(-leftKernel), -left);
        Assert.Equal(ToFractional(leftKernel * 2.5), left * 2.5);
        Assert.Equal(ToFractional(2.5 * leftKernel), 2.5 * left);
        Assert.Equal(ToFractional(leftKernel / 4.0), left / 4.0);
        Assert.Equal(Vector3<double>.Dot(leftKernel, rightKernel), left.Dot(right));
        Assert.Equal(ToFractional(Vector3<double>.Cross(leftKernel, rightKernel)), left.Cross(right));
        Assert.Equal(leftKernel.LengthSquared, left.LengthSquared);
        Assert.Equal(Math.Sqrt(leftKernel.LengthSquared), left.Length);
    }

    [Fact]
    public void CartesianArithmeticMatchesGenericKernel()
    {
        var left = new Cartesian3(1.5, -2.25, 3.0);
        var right = new Cartesian3(-0.5, 4.0, 0.125);

        var leftKernel = new Vector3<double>(left.X, left.Y, left.Z);
        var rightKernel = new Vector3<double>(right.X, right.Y, right.Z);

        Assert.Equal(ToCartesian(leftKernel + rightKernel), left + right);
        Assert.Equal(ToCartesian(leftKernel - rightKernel), left - right);
        Assert.Equal(ToCartesian(-leftKernel), -left);
        Assert.Equal(ToCartesian(leftKernel * 2.5), left * 2.5);
        Assert.Equal(ToCartesian(leftKernel / 4.0), left / 4.0);
        Assert.Equal(Vector3<double>.Dot(leftKernel, rightKernel), left.Dot(right));
        Assert.Equal(ToCartesian(Vector3<double>.Cross(leftKernel, rightKernel)), left.Cross(right));
        Assert.Equal(Math.Sqrt(leftKernel.LengthSquared), left.Length);
        Assert.Equal((left - right).Length, left.DistanceTo(right));
    }

    [Fact]
    public void CartesianCrossProductIsRightHanded()
    {
        var x = new Cartesian3(1.0, 0.0, 0.0);
        var y = new Cartesian3(0.0, 1.0, 0.0);

        Assert.Equal(new Cartesian3(0.0, 0.0, 1.0), x.Cross(y));
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.25, 0.25)]
    [InlineData(1.0, 0.0)]
    [InlineData(-0.25, 0.75)]
    [InlineData(-1.25, 0.75)]
    [InlineData(1.25, 0.25)]
    [InlineData(0.5, 0.5)]
    [InlineData(0.99999999995, 0.0)]
    [InlineData(-0.00000000005, 0.0)]
    [InlineData(5e-11, 0.0)]
    [InlineData(-1e-11, 0.0)]
    public void TrimMapsIntoHalfOpenUnitInterval(double input, double expected)
    {
        var trimmed = new Fractional3(input, input, input).TrimToUnitCell();

        Assert.Equal(expected, trimmed.A);
        Assert.Equal(expected, trimmed.B);
        Assert.Equal(expected, trimmed.C);
    }

    [Fact]
    public void TrimNormalizesNegativeZeroToPositiveZero()
    {
        var trimmed = new Fractional3(-0.0, -1e-11, 1.0).TrimToUnitCell();

        Assert.False(double.IsNegative(trimmed.A));
        Assert.False(double.IsNegative(trimmed.B));
        Assert.False(double.IsNegative(trimmed.C));
    }

    [Fact]
    public void TrimLeavesValuesAwayFromBoundariesUnchanged()
    {
        var value = new Fractional3(0.1234567890123, 0.4999999998, 0.5000000002);

        Assert.Equal(value, value.TrimToUnitCell());
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void TrimRejectsNonFiniteComponents(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Fractional3(value, 0.0, 0.0).TrimToUnitCell());
        Assert.Throws<ArgumentOutOfRangeException>(() => new Fractional3(0.0, value, 0.0).TrimToUnitCell());
        Assert.Throws<ArgumentOutOfRangeException>(() => new Fractional3(0.0, 0.0, value).TrimToUnitCell());
    }

    [Property(MaxTest = 500)]
    public bool TrimIsIdempotentAndInsideTheUnitInterval(double a, double b, double c)
    {
        if (!double.IsFinite(a) || !double.IsFinite(b) || !double.IsFinite(c))
        {
            return true;
        }

        var trimmed = new Fractional3(a, b, c).TrimToUnitCell();
        var trimmedTwice = trimmed.TrimToUnitCell();

        return IsInUnitInterval(trimmed.A)
            && IsInUnitInterval(trimmed.B)
            && IsInUnitInterval(trimmed.C)
            && trimmed == trimmedTwice;
    }

    [Fact]
    public void CoreAssemblyDoesNotReferenceSymmetry()
    {
        var referencedAssemblies = typeof(Fractional3).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name);

        Assert.DoesNotContain("Mocassin.Symmetry", referencedAssemblies);
    }

    [Fact]
    public void Vector4IKeepsCellOffsetAndPositionIndexDistinct()
    {
        var value = new Vector4I(-1, 2, -3, 7);

        Assert.Equal(-1, value.A);
        Assert.Equal(2, value.B);
        Assert.Equal(-3, value.C);
        Assert.Equal(7, value.P);
        Assert.Equal(new Vector4I(-2, 4, -6, 14), value + value);
        Assert.Equal(Vector4I.Zero, value - value);
        Assert.Equal(new Vector4I(1, -2, 3, -7), -value);
    }

    private static bool IsInUnitInterval(double value) =>
        value >= 0.0 && value < 1.0 && !(value == 0.0 && double.IsNegative(value));

    private static Fractional3 ToFractional(Vector3<double> value) => new(value.X, value.Y, value.Z);

    private static Cartesian3 ToCartesian(Vector3<double> value) => new(value.X, value.Y, value.Z);
}
