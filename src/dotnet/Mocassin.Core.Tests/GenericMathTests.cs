// SPDX-License-Identifier: MIT

using FsCheck.Xunit;
using Mocassin.Core.Geometry;

namespace Mocassin.Core.Tests;

public sealed class GenericMathTests
{
    [Fact]
    public void Vector3MatchesScalarReferenceForDoubleAndFloat()
    {
        var left = new Vector3<double>(1.5, -2.25, 3.0);
        var right = new Vector3<double>(-0.5, 4.0, 0.125);

        Assert.Equal(new Vector3<double>(1.0, 1.75, 3.125), left + right);
        Assert.Equal(new Vector3<double>(2.0, -6.25, 2.875), left - right);
        Assert.Equal(new Vector3<double>(-1.5, 2.25, -3.0), -left);
        Assert.Equal(new Vector3<double>(3.0, -4.5, 6.0), left * 2.0);
        Assert.Equal(new Vector3<double>(0.75, -1.125, 1.5), left / 2.0);
        Assert.Equal(-9.375, Vector3<double>.Dot(left, right));
        Assert.Equal(new Vector3<double>(-12.28125, -1.6875, 4.875), Vector3<double>.Cross(left, right));
        Assert.Equal(16.3125, left.LengthSquared);

        var floatLeft = new Vector3<float>(1.5f, -2.25f, 3.0f);
        var floatRight = new Vector3<float>(-0.5f, 4.0f, 0.125f);
        Assert.Equal(new Vector3<float>(1.0f, 1.75f, 3.125f), floatLeft + floatRight);
        Assert.Equal(Vector3<float>.Dot(floatLeft, floatRight), Vector3<float>.Dot(floatLeft, floatRight));
        Assert.Equal(new Vector3<float>(-12.28125f, -1.6875f, 4.875f), Vector3<float>.Cross(floatLeft, floatRight));
    }

    [Fact]
    public void Vector4ArithmeticMatchesScalarReference()
    {
        var left = new Vector4<double>(1.0, 2.0, 3.0, 4.0);
        var right = new Vector4<double>(0.5, -1.0, 2.0, 3.0);

        Assert.Equal(new Vector4<double>(1.5, 1.0, 5.0, 7.0), left + right);
        Assert.Equal(new Vector4<double>(0.5, 3.0, 1.0, 1.0), left - right);
        Assert.Equal(new Vector4<double>(-1.0, -2.0, -3.0, -4.0), -left);
        Assert.Equal(new Vector4<double>(2.0, 4.0, 6.0, 8.0), left * 2.0);
        Assert.Equal(new Vector4<double>(0.5, 1.0, 1.5, 2.0), left / 2.0);
        Assert.Equal(16.5, Vector4<double>.Dot(left, right));
        Assert.Equal(30.0, left.LengthSquared);
        Assert.Equal(Vector4<double>.Zero, left - left);
    }

    [Fact]
    public void Matrix3IdentityAndFromColumnsFollowTheDocumentedConvention()
    {
        var identity = Matrix3<double>.Identity;
        var matrix = Matrix3<double>.FromColumns(
            new Vector3<double>(1.0, 2.0, 3.0),
            new Vector3<double>(4.0, 5.0, 6.0),
            new Vector3<double>(7.0, 8.0, 9.0));

        Assert.Equal(new Vector3<double>(1.0, 2.0, 3.0), matrix.Column0);
        Assert.Equal(new Vector3<double>(4.0, 5.0, 6.0), matrix.Column1);
        Assert.Equal(new Vector3<double>(7.0, 8.0, 9.0), matrix.Column2);
        Assert.Equal(new Vector3<double>(1.0, 2.0, 3.0), matrix.Transform(new Vector3<double>(1.0, 0.0, 0.0)));
        Assert.Equal(matrix, matrix * identity);
        Assert.Equal(matrix, identity * matrix);
        Assert.Equal(new Vector3<double>(1.0, 4.0, 7.0), matrix.Row0);
    }

    [Fact]
    public void Matrix3TransformMatchesRowDotReference()
    {
        var matrix = Matrix3<double>.FromRows(
            new Vector3<double>(1.0, 2.0, 3.0),
            new Vector3<double>(4.0, 5.0, 6.0),
            new Vector3<double>(7.0, 8.0, 9.0));
        var vector = new Vector3<double>(0.25, -0.5, 2.0);

        Assert.Equal(new Vector3<double>(5.25, 10.5, 15.75), matrix.Transform(vector));
        Assert.Equal(24.0, matrix.InfinityNorm);
        Assert.Equal(0.0, matrix.Determinant);
    }

    [Fact]
    public void Matrix4ConstructionTransformAndConditionNumber()
    {
        var matrix = Matrix4<double>.FromColumns(
            new Vector4<double>(1.0, 2.0, 3.0, 4.0),
            new Vector4<double>(5.0, 6.0, 7.0, 8.0),
            new Vector4<double>(9.0, 10.0, 11.0, 12.0),
            new Vector4<double>(13.0, 14.0, 15.0, 16.0));
        var vector = new Vector4<double>(1.0, 0.0, 0.0, 0.0);

        Assert.Equal(new Vector4<double>(1.0, 2.0, 3.0, 4.0), matrix.Transform(vector));
        Assert.Equal(new Vector4<double>(1.0, 2.0, 3.0, 4.0), matrix.Column0);
        Assert.Equal(0.0, matrix.Determinant);
        Assert.Throws<InvalidOperationException>(() => matrix.Inverse());
        Assert.Throws<InvalidOperationException>(() => matrix.ConditionNumber);

        var identity = Matrix4<double>.Identity;
        Assert.Equal(identity, Matrix4<double>.FromRows(identity.Row0, identity.Row1, identity.Row2, identity.Row3));
        Assert.Equal(identity, identity.Transpose());
        Assert.Equal(identity.InfinityNorm, identity.ConditionNumber);
    }

    [Property(MaxTest = 100)]
    public bool Matrix3InverseSatisfiesLeftAndRightInverse(
        double m11,
        double m12,
        double m13,
        double m21,
        double m22,
        double m23,
        double m31,
        double m32,
        double m33)
    {
        if (!AllFinite(m11, m12, m13, m21, m22, m23, m31, m32, m33)
            || Math.Max(Math.Abs(m11), Math.Max(Math.Abs(m12), Math.Abs(m13))) > 2.0
            || Math.Max(Math.Abs(m21), Math.Max(Math.Abs(m22), Math.Abs(m23))) > 2.0
            || Math.Max(Math.Abs(m31), Math.Max(Math.Abs(m32), Math.Abs(m33))) > 2.0)
        {
            return true;
        }

        var matrix = new Matrix3<double>(
            new Vector3<double>(m11 + 3.0, m12, m13),
            new Vector3<double>(m21, m22 + 3.0, m23),
            new Vector3<double>(m31, m32, m33 + 3.0));
        if (Math.Abs(matrix.Determinant) < 1e-6)
        {
            return true;
        }

        var inverse = matrix.Inverse();
        return Near(matrix * inverse, Matrix3<double>.Identity)
            && Near(inverse * matrix, Matrix3<double>.Identity);
    }

    [Property(MaxTest = 100)]
    public bool Matrix4InverseSatisfiesLeftAndRightInverse(
        double m11,
        double m12,
        double m13,
        double m14,
        double m21,
        double m22,
        double m23,
        double m24,
        double m31,
        double m32,
        double m33,
        double m34,
        double m41,
        double m42,
        double m43,
        double m44)
    {
        if (!AllFinite(m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, m41, m42, m43, m44)
            || Math.Max(Math.Abs(m11), Math.Max(Math.Abs(m12), Math.Max(Math.Abs(m13), Math.Abs(m14)))) > 2.0
            || Math.Max(Math.Abs(m21), Math.Max(Math.Abs(m22), Math.Max(Math.Abs(m23), Math.Abs(m24)))) > 2.0
            || Math.Max(Math.Abs(m31), Math.Max(Math.Abs(m32), Math.Max(Math.Abs(m33), Math.Abs(m34)))) > 2.0
            || Math.Max(Math.Abs(m41), Math.Max(Math.Abs(m42), Math.Max(Math.Abs(m43), Math.Abs(m44)))) > 2.0)
        {
            return true;
        }

        var matrix = new Matrix4<double>(
            new Vector4<double>(m11 + 4.0, m12, m13, m14),
            new Vector4<double>(m21, m22 + 4.0, m23, m24),
            new Vector4<double>(m31, m32, m33 + 4.0, m34),
            new Vector4<double>(m41, m42, m43, m44 + 4.0));
        if (Math.Abs(matrix.Determinant) < 1e-6)
        {
            return true;
        }

        var inverse = matrix.Inverse();
        return Near(matrix * inverse, Matrix4<double>.Identity)
            && Near(inverse * matrix, Matrix4<double>.Identity);
    }

    [Fact]
    public void FloatMatrixInverseRoundTrips()
    {
        var matrix = Matrix3<float>.FromRows(
            new Vector3<float>(2.0f, 1.0f, 0.0f),
            new Vector3<float>(1.0f, 3.0f, 1.0f),
            new Vector3<float>(0.0f, 1.0f, 2.0f));

        Assert.True(Near(matrix * matrix.Inverse(), Matrix3<float>.Identity, 1e-4f));
    }

    [Fact]
    public void SingularMatricesRejectInverseAndConditionNumber()
    {
        var singular3 = Matrix3<double>.FromRows(
            new Vector3<double>(1.0, 2.0, 3.0),
            new Vector3<double>(2.0, 4.0, 6.0),
            new Vector3<double>(0.0, 1.0, 1.0));
        var singular4 = Matrix4<double>.FromRows(
            new Vector4<double>(1.0, 2.0, 3.0, 4.0),
            new Vector4<double>(2.0, 4.0, 6.0, 8.0),
            new Vector4<double>(0.0, 1.0, 1.0, 0.0),
            new Vector4<double>(1.0, 0.0, 0.0, 1.0));

        Assert.Throws<InvalidOperationException>(() => singular3.Inverse());
        Assert.Throws<InvalidOperationException>(() => singular3.ConditionNumber);
        Assert.Throws<InvalidOperationException>(() => singular4.Inverse());
        Assert.Throws<InvalidOperationException>(() => singular4.ConditionNumber);
    }

    private static bool AllFinite(params double[] values) => values.All(double.IsFinite);

    private static bool Near(Matrix3<double> actual, Matrix3<double> expected) =>
        Near(actual.Row0, expected.Row0)
        && Near(actual.Row1, expected.Row1)
        && Near(actual.Row2, expected.Row2);

    private static bool Near(Vector3<double> actual, Vector3<double> expected) =>
        Math.Abs(actual.X - expected.X) < 1e-6
        && Math.Abs(actual.Y - expected.Y) < 1e-6
        && Math.Abs(actual.Z - expected.Z) < 1e-6;

    private static bool Near(Matrix3<float> actual, Matrix3<float> expected, float tolerance) =>
        Math.Abs(actual.M11 - expected.M11) < tolerance
        && Math.Abs(actual.M12 - expected.M12) < tolerance
        && Math.Abs(actual.M13 - expected.M13) < tolerance
        && Math.Abs(actual.M21 - expected.M21) < tolerance
        && Math.Abs(actual.M22 - expected.M22) < tolerance
        && Math.Abs(actual.M23 - expected.M23) < tolerance
        && Math.Abs(actual.M31 - expected.M31) < tolerance
        && Math.Abs(actual.M32 - expected.M32) < tolerance
        && Math.Abs(actual.M33 - expected.M33) < tolerance;

    private static bool Near(Matrix4<double> actual, Matrix4<double> expected) =>
        Near(actual.Row0, expected.Row0)
        && Near(actual.Row1, expected.Row1)
        && Near(actual.Row2, expected.Row2)
        && Near(actual.Row3, expected.Row3);

    private static bool Near(Vector4<double> actual, Vector4<double> expected) =>
        Math.Abs(actual.X - expected.X) < 1e-6
        && Math.Abs(actual.Y - expected.Y) < 1e-6
        && Math.Abs(actual.Z - expected.Z) < 1e-6
        && Math.Abs(actual.W - expected.W) < 1e-6;
}
