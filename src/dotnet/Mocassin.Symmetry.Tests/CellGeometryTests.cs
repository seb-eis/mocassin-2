// SPDX-License-Identifier: MIT

using FsCheck.Xunit;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model;
using Mocassin.Core.Symmetry;

namespace Mocassin.Symmetry.Tests;

[Trait("Tier", "PR-fast")]
public sealed class CellGeometryTests
{
    [Fact]
    public void TriclinicRegressionMatchesIndependentVectorsAnglesAndVolume()
    {
        var parameters = Parameters(5.0, 6.0, 7.0, 80.0, 85.0, 95.0);

        Assert.True(CellGeometry.TryCreate(parameters, out var geometry, out var diagnostics));
        Assert.Empty(diagnostics);
        Assert.NotNull(geometry);

        var detG = GramDeterminant(80.0, 85.0, 95.0);
        var expectedVolume = 5.0 * 6.0 * 7.0 * Math.Sqrt(detG);

        Assert.Equal(expectedVolume, geometry.VolumeAngstrom3, 10);
        Assert.Equal(expectedVolume, geometry.GetCellVolume(), 10);
        Assert.Equal(expectedVolume, geometry.A.Dot(geometry.B.Cross(geometry.C)), 10);
        Assert.True(geometry.C.Z > 0.0);

        Assert.Equal(80.0, AngleBetween(geometry.B, geometry.C), 9);
        Assert.Equal(85.0, AngleBetween(geometry.A, geometry.C), 9);
        Assert.Equal(95.0, AngleBetween(geometry.A, geometry.B), 9);
    }

    [Fact]
    public void TriclinicCellHasTheExpectedCVectorComponents()
    {
        var parameters = Parameters(5.0, 6.0, 7.0, 80.0, 85.0, 95.0);
        Assert.True(CellGeometry.TryCreate(parameters, out var geometry, out _));
        Assert.NotNull(geometry);

        var cosAlpha = Math.Cos(80.0 * Math.PI / 180.0);
        var cosBeta = Math.Cos(85.0 * Math.PI / 180.0);
        var cosGamma = Math.Cos(95.0 * Math.PI / 180.0);
        var sinGamma = Math.Sin(95.0 * Math.PI / 180.0);
        var detG = GramDeterminant(80.0, 85.0, 95.0);

        Assert.Equal(7.0 * cosBeta, geometry.C.X, 12);
        Assert.Equal(7.0 * (cosAlpha - (cosBeta * cosGamma)) / sinGamma, geometry.C.Y, 12);
        Assert.Equal(7.0 * Math.Sqrt(detG) / sinGamma, geometry.C.Z, 12);
    }

    [Fact]
    public void CellMatrixColumnsAndInverseMatchTheGeometry()
    {
        var parameters = Parameters(5.0, 6.0, 7.0, 80.0, 85.0, 95.0);
        Assert.True(CellGeometry.TryCreate(parameters, out var geometry, out _));
        Assert.NotNull(geometry);

        Assert.Equal(new Vector3<double>(geometry.A.X, geometry.A.Y, geometry.A.Z), geometry.Matrix.Column0);
        Assert.Equal(new Vector3<double>(geometry.B.X, geometry.B.Y, geometry.B.Z), geometry.Matrix.Column1);
        Assert.Equal(new Vector3<double>(geometry.C.X, geometry.C.Y, geometry.C.Z), geometry.Matrix.Column2);
        Assert.Equal(Math.Abs(geometry.Matrix.Determinant), geometry.VolumeAngstrom3, 10);
        Assert.Equal(geometry.Matrix.ConditionNumber, geometry.ConditionNumber, 12);

        var vector = new Vector3<double>(0.25, -0.5, 1.25);
        var roundTripped = geometry.Inverse.Transform(geometry.Matrix.Transform(vector));
        Assert.Equal(vector.X, roundTripped.X, 12);
        Assert.Equal(vector.Y, roundTripped.Y, 12);
        Assert.Equal(vector.Z, roundTripped.Z, 12);
    }

    [Fact]
    public void AllSevenCrystalSystemsValidate()
    {
        AssertValid(Parameters(5.0, 6.0, 7.0, 80.0, 85.0, 95.0), CrystalSystem.Triclinic, AxisSetting.None);
        AssertValid(Parameters(5.0, 6.0, 7.0, 90.0, 100.0, 90.0), CrystalSystem.Monoclinic, AxisSetting.UniqueAxisB);
        AssertValid(Parameters(5.0, 6.0, 7.0, 90.0, 90.0, 100.0), CrystalSystem.Monoclinic, AxisSetting.UniqueAxisC);
        AssertValid(Parameters(5.0, 6.0, 7.0, 90.0, 90.0, 90.0), CrystalSystem.Orthorhombic, AxisSetting.None);
        AssertValid(Parameters(5.0, 5.0, 7.0, 90.0, 90.0, 90.0), CrystalSystem.Tetragonal, AxisSetting.None);
        AssertValid(Parameters(5.0, 5.0, 7.0, 90.0, 90.0, 120.0), CrystalSystem.Trigonal, AxisSetting.Hexagonal);
        AssertValid(Parameters(5.0, 5.0, 5.0, 75.0, 75.0, 75.0), CrystalSystem.Trigonal, AxisSetting.Rhombohedral);
        AssertValid(Parameters(5.0, 5.0, 7.0, 90.0, 90.0, 120.0), CrystalSystem.Hexagonal, AxisSetting.None);
        AssertValid(Parameters(5.0, 5.0, 5.0, 90.0, 90.0, 90.0), CrystalSystem.Cubic, AxisSetting.None);
    }

    [Theory]
    [InlineData(0.05)]
    [InlineData(1000.1)]
    public void CellLengthsOutsideTheSupportedRangeAreRejected(double length)
    {
        var parameters = Parameters(length, 5.0, 5.0, 90.0, 90.0, 90.0);

        Assert.False(CellGeometry.TryCreate(parameters, out var geometry, out var diagnostics));
        Assert.Null(geometry);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "STR002");
    }

    [Theory]
    [InlineData(0.0, 90.0, 90.0)]
    [InlineData(180.0, 90.0, 90.0)]
    [InlineData(90.0, 200.0, 90.0)]
    public void CellAnglesOutsideTheOpenRangeAreRejected(double alpha, double beta, double gamma)
    {
        var parameters = Parameters(5.0, 5.0, 5.0, alpha, beta, gamma);

        Assert.False(CellGeometry.TryCreate(parameters, out var geometry, out var diagnostics));
        Assert.Null(geometry);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "STR003");
    }

    [Fact]
    public void NonPositiveGramDeterminantIsRejected()
    {
        var parameters = Parameters(5.0, 5.0, 5.0, 30.0, 30.0, 150.0);

        Assert.False(CellGeometry.TryCreate(parameters, out var geometry, out var diagnostics));
        Assert.Null(geometry);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "STR003");
    }

    [Fact]
    public void NearDegenerateGramDeterminantIsRejected()
    {
        var parameters = Parameters(5.0, 5.0, 5.0, 45.00000000001, 90.0, 45.00000000001);

        Assert.False(CellGeometry.TryCreate(parameters, out _, out var diagnostics));
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "STR003");
    }

    [Fact]
    public void CrystalSystemConstraintsAreEnforced()
    {
        AssertViolatesSystem(Parameters(5.0, 6.0, 5.0, 90.0, 90.0, 90.0), CrystalSystem.Tetragonal, AxisSetting.None);
        AssertViolatesSystem(Parameters(5.0, 5.0, 5.0, 91.0, 90.0, 90.0), CrystalSystem.Cubic, AxisSetting.None);
        AssertViolatesSystem(Parameters(5.0, 6.0, 5.0, 90.0, 90.0, 90.0), CrystalSystem.Cubic, AxisSetting.None);
        AssertViolatesSystem(Parameters(5.0, 6.0, 7.0, 90.0, 91.0, 90.0), CrystalSystem.Orthorhombic, AxisSetting.None);
        AssertViolatesSystem(Parameters(5.0, 6.0, 7.0, 90.0, 100.0, 91.0), CrystalSystem.Monoclinic, AxisSetting.UniqueAxisB);
        AssertViolatesSystem(Parameters(5.0, 6.0, 7.0, 90.0, 91.0, 100.0), CrystalSystem.Monoclinic, AxisSetting.UniqueAxisC);
        AssertViolatesSystem(Parameters(5.0, 6.0, 7.0, 90.0, 100.0, 90.0), CrystalSystem.Monoclinic, AxisSetting.None);
        AssertViolatesSystem(Parameters(5.0, 5.0, 7.0, 90.0, 90.0, 119.0), CrystalSystem.Hexagonal, AxisSetting.None);
        AssertViolatesSystem(Parameters(5.0, 5.0, 7.0, 90.0, 90.0, 120.0), CrystalSystem.Trigonal, AxisSetting.None);
        AssertViolatesSystem(Parameters(5.0, 5.0, 5.0, 75.0, 75.0, 74.0), CrystalSystem.Trigonal, AxisSetting.Rhombohedral);
        AssertViolatesSystem(Parameters(5.0, 5.0, 5.0, 120.0, 120.0, 120.0), CrystalSystem.Trigonal, AxisSetting.Rhombohedral);
    }

    [Fact]
    public void ConditionNumberIsComputedAndBounded()
    {
        Assert.True(CellGeometry.IsConditionWithinLimit(1e11));
        Assert.False(CellGeometry.IsConditionWithinLimit(1e13));
        Assert.False(CellGeometry.IsConditionWithinLimit(double.PositiveInfinity));

        var parameters = Parameters(0.1, 1000.0, 1000.0, 45.0000001, 90.0, 45.0000001);
        Assert.True(CellGeometry.TryCreate(parameters, out var geometry, out var diagnostics));
        Assert.Empty(diagnostics);
        Assert.NotNull(geometry);
        Assert.True(geometry.ConditionNumber > 1e6);
        Assert.True(CellGeometry.IsConditionWithinLimit(geometry.ConditionNumber));
    }

    [Property(MaxTest = 300)]
    public bool FractionalCartesianRoundTrips(double a, double b, double c)
    {
        if (!double.IsFinite(a) || !double.IsFinite(b) || !double.IsFinite(c)
            || Math.Abs(a) > 1e6 || Math.Abs(b) > 1e6 || Math.Abs(c) > 1e6)
        {
            return true;
        }

        var parameters = Parameters(5.0, 6.0, 7.0, 80.0, 85.0, 95.0);
        Assert.True(CellGeometry.TryCreate(parameters, out var geometry, out _));
        Assert.NotNull(geometry);

        var point = new Fractional3(a, b, c);
        var roundTripped = geometry.ToFractional(geometry.ToCartesian(point));
        var scale = Math.Max(1.0, Math.Max(Math.Abs(a), Math.Max(Math.Abs(b), Math.Abs(c))));

        return Math.Abs(roundTripped.A - a) <= 1e-9 * scale
            && Math.Abs(roundTripped.B - b) <= 1e-9 * scale
            && Math.Abs(roundTripped.C - c) <= 1e-9 * scale;
    }

    [Property(MaxTest = 300)]
    public bool MetricDistanceMatchesCartesianDistance(
        double ax,
        double ay,
        double az,
        double bx,
        double by,
        double bz)
    {
        if (!double.IsFinite(ax) || !double.IsFinite(ay) || !double.IsFinite(az)
            || !double.IsFinite(bx) || !double.IsFinite(by) || !double.IsFinite(bz)
            || Math.Abs(ax) > 1e6 || Math.Abs(ay) > 1e6 || Math.Abs(az) > 1e6
            || Math.Abs(bx) > 1e6 || Math.Abs(by) > 1e6 || Math.Abs(bz) > 1e6)
        {
            return true;
        }

        var parameters = Parameters(5.0, 6.0, 7.0, 80.0, 85.0, 95.0);
        Assert.True(CellGeometry.TryCreate(parameters, out var geometry, out _));
        Assert.NotNull(geometry);

        var left = new Fractional3(ax, ay, az);
        var right = new Fractional3(bx, by, bz);
        var metricDistance = geometry.GetDistance(left, right);
        var cartesianDistance = geometry.ToCartesian(left).DistanceTo(geometry.ToCartesian(right));

        return Math.Abs(metricDistance - cartesianDistance) <= 1e-9 * Math.Max(1.0, cartesianDistance);
    }

    private static CellParameters Parameters(
        double a,
        double b,
        double c,
        double alpha,
        double beta,
        double gamma) =>
        new(
            new Mocassin.Core.Units.LengthAngstrom(a),
            new Mocassin.Core.Units.LengthAngstrom(b),
            new Mocassin.Core.Units.LengthAngstrom(c),
            new Mocassin.Core.Units.AngleDegrees(alpha),
            new Mocassin.Core.Units.AngleDegrees(beta),
            new Mocassin.Core.Units.AngleDegrees(gamma));

    private static void AssertValid(CellParameters parameters, CrystalSystem system, AxisSetting axis)
    {
        Assert.True(CellGeometry.TryCreate(parameters, system, axis, out var geometry, out var diagnostics));
        Assert.Empty(diagnostics);
        Assert.NotNull(geometry);
    }

    private static void AssertViolatesSystem(CellParameters parameters, CrystalSystem system, AxisSetting axis)
    {
        Assert.False(CellGeometry.TryCreate(parameters, system, axis, out var geometry, out var diagnostics));
        Assert.Null(geometry);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "STR003");
    }

    private static double GramDeterminant(double alpha, double beta, double gamma)
    {
        var cosAlpha = Math.Cos(alpha * Math.PI / 180.0);
        var cosBeta = Math.Cos(beta * Math.PI / 180.0);
        var cosGamma = Math.Cos(gamma * Math.PI / 180.0);
        return 1.0
            - (cosAlpha * cosAlpha)
            - (cosBeta * cosBeta)
            - (cosGamma * cosGamma)
            + (2.0 * cosAlpha * cosBeta * cosGamma);
    }

    private static double AngleBetween(Cartesian3 left, Cartesian3 right) =>
        Math.Acos(Math.Clamp(left.Dot(right) / (left.Length * right.Length), -1.0, 1.0)) * 180.0 / Math.PI;
}
