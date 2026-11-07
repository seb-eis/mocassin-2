// SPDX-License-Identifier: MIT

using FsCheck.Xunit;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;

namespace Mocassin.Symmetry.Tests;

public sealed class GridFractional3Tests
{
    [Theory]
    [InlineData(0.0, 0L)]
    [InlineData(0.25, 6_000_000_000_000L)]
    [InlineData(0.1, 2_400_000_000_000L)]
    [InlineData(0.123456789012, 2_962_962_936_288L)]
    [InlineData(-0.25, -6_000_000_000_000L)]
    [InlineData(1.25, 30_000_000_000_000L)]
    public void ConvertsRepresentableValuesExactly(double input, long expected)
    {
        var success = GridFractional3.TryFromFractional(
            new Fractional3(input, input, input),
            out var grid,
            out var diagnostic);

        Assert.True(success);
        Assert.Null(diagnostic);
        Assert.Equal(expected, grid.A);
        Assert.Equal(expected, grid.B);
        Assert.Equal(expected, grid.C);
    }

    [Fact]
    public void NegativeZeroConvertsToGridZero()
    {
        var success = GridFractional3.TryFromFractional(
            new Fractional3(-0.0, -0.0, -0.0),
            out var grid,
            out var diagnostic);

        Assert.True(success);
        Assert.Null(diagnostic);
        Assert.Equal(GridFractional3.Zero, grid);
    }

    [Fact]
    public void WarnsWhenInputRequiresMoreThanTwelveFractionalDigits()
    {
        var success = GridFractional3.TryFromFractional(
            new Fractional3(0.1234567890123, 0.0, 0.0),
            out var grid,
            out var diagnostic);

        Assert.True(success);
        Assert.Equal(2_962_962_936_295L, grid.A);
        Assert.NotNull(diagnostic);
        Assert.Equal(SymmetryDiagnosticCodes.CoordinateRoundedToGrid, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RejectsNonFiniteCoordinates(double value)
    {
        var success = GridFractional3.TryFromFractional(
            new Fractional3(value, value, value),
            out _,
            out var diagnostic);

        Assert.False(success);
        Assert.NotNull(diagnostic);
        Assert.Equal(SymmetryDiagnosticCodes.NonFiniteCoordinate, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Theory]
    [InlineData(double.Epsilon)]
    [InlineData(-double.Epsilon)]
    [InlineData(1e-20)]
    [InlineData(-1e-20)]
    public void UnderflowsValuesBelowHalfAGridStepToZero(double value)
    {
        var success = GridFractional3.TryFromFractional(
            new Fractional3(value, value, value),
            out var grid,
            out var diagnostic);

        Assert.True(success);
        Assert.Null(diagnostic);
        Assert.Equal(GridFractional3.Zero, grid);
    }

    [Fact]
    public void RejectsCoordinatesOutsideTheRepresentableGridRange()
    {
        var success = GridFractional3.TryFromFractional(
            new Fractional3(1e300, 0.0, 0.0),
            out _,
            out var diagnostic);

        Assert.False(success);
        Assert.NotNull(diagnostic);
        Assert.Equal(SymmetryDiagnosticCodes.CoordinateOutsideGridRange, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void RoundTripsRepresentableCoordinates()
    {
        var original = new Fractional3(0.1, 0.123456789012, 0.25);

        Assert.True(GridFractional3.TryFromFractional(original, out var grid, out _));
        Assert.Equal(original, grid.ToFractional3());
    }

    [Fact]
    public void TrimsIntoTheHalfOpenUnitCell()
    {
        var negative = new GridFractional3(-6_000_000_000_000L, -6_000_000_000_000L, -6_000_000_000_000L);
        var positive = new GridFractional3(30_000_000_000_000L, 30_000_000_000_000L, 30_000_000_000_000L);

        Assert.Equal(
            new GridFractional3(18_000_000_000_000L, 18_000_000_000_000L, 18_000_000_000_000L),
            negative.TrimToUnitCell());
        Assert.Equal(
            new GridFractional3(6_000_000_000_000L, 6_000_000_000_000L, 6_000_000_000_000L),
            positive.TrimToUnitCell());
    }

    [Fact]
    public void ComparesInCanonicalComponentOrder()
    {
        var a = new GridFractional3(1, 2, 3);
        var b = new GridFractional3(1, 2, 4);
        var c = new GridFractional3(1, 3, 0);

        Assert.True(a.CompareTo(b) < 0);
        Assert.True(b.CompareTo(c) < 0);
        Assert.Equal(0, a.CompareTo(a));
    }

    [Property(MaxTest = 500)]
    public bool ConversionIsDeterministicForFiniteValues(double a, double b, double c)
    {
        if (!double.IsFinite(a) || !double.IsFinite(b) || !double.IsFinite(c))
        {
            return true;
        }

        var value = new Fractional3(a, b, c);
        var first = GridFractional3.TryFromFractional(value, out var firstGrid, out var firstDiagnostic);
        var second = GridFractional3.TryFromFractional(value, out var secondGrid, out var secondDiagnostic);

        return first == second && firstGrid == secondGrid && firstDiagnostic == secondDiagnostic;
    }
}
