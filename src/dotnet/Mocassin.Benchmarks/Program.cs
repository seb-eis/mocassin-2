// SPDX-License-Identifier: MIT

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using Mocassin.Core.Geometry;
using Mocassin.Core.Symmetry;
using Mocassin.Symmetry;

namespace Mocassin.Benchmarks;

/// <summary>
/// Benchmark entry point.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the symmetry benchmarks.
    /// </summary>
    /// <param name="args">BenchmarkDotNet arguments.</param>
    public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}

/// <summary>
/// Symmetry performance fixtures.
/// </summary>
[MemoryDiagnoser]
public class SymmetryBenchmarks
{
    private const int cellCount = 20;
    private UnitCellVectorEncoder encoder = null!;
    private int checksum;

    /// <summary>
    /// Builds the 64-position P1 fixture.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var positions = new Fractional3[64];
        var index = 0;
        for (var a = 0; a < 4; a++)
        {
            for (var b = 0; b < 4; b++)
            {
                for (var c = 0; c < 4; c++)
                {
                    positions[index++] = new Fractional3(a / 4.0, b / 4.0, c / 4.0);
                }
            }
        }

        var setting = SpaceGroupCatalog.Default.GetSetting(1, AxisSetting.None, OriginChoice.None)
            ?? throw new InvalidOperationException("The P1 setting is missing.");
        if (!UnitCellVectorEncoder.TryCreate(setting, positions, out var built, out var diagnostics)
            || built is null)
        {
            throw new InvalidOperationException(
                $"The benchmark fixture is invalid: {string.Join("; ", diagnostics.Select(static diagnostic => diagnostic.ToString()))}");
        }

        if (built.PositionCount != 64)
        {
            throw new InvalidOperationException($"Expected 64 P1 positions, found {built.PositionCount}.");
        }

        encoder = built;
    }

    /// <summary>
    /// Expands 64 P1 positions to 20×20×20 = 512,000 lattice positions.
    /// </summary>
    /// <returns>A checksum that prevents elimination.</returns>
    [Benchmark]
    public int P1Expansion20Cubed()
    {
        var sum = 0;
        for (var a = 0; a < cellCount; a++)
        {
            for (var b = 0; b < cellCount; b++)
            {
                for (var c = 0; c < cellCount; c++)
                {
                    for (var p = 0; p < encoder.PositionCount; p++)
                    {
                        sum += encoder.GetLinearIndex(new Vector4I(a, b, c, p), cellCount, cellCount, cellCount);
                    }
                }
            }
        }

        checksum = sum;
        return sum;
    }
}

/// <summary>
/// Matrix arithmetic benchmarks for the public generic math layer.
/// </summary>
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 2)]
public class MatrixMathBenchmarks
{
    private Matrix3<double> matrix;
    private Vector3<double> vector;

    /// <summary>
    /// Builds the fixed matrix fixture.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        matrix = Matrix3<double>.FromRows(
            new Vector3<double>(2.0, 0.5, 0.25),
            new Vector3<double>(-0.5, 3.0, 0.75),
            new Vector3<double>(0.25, -0.75, 4.0));
        vector = new Vector3<double>(0.5, -1.0, 2.0);
    }

    /// <summary>
    /// Transforms one vector.
    /// </summary>
    /// <returns>The transformed vector.</returns>
    [Benchmark]
    public Vector3<double> Matrix3Transform() => matrix.Transform(vector);

    /// <summary>
    /// Inverts the matrix.
    /// </summary>
    /// <returns>The inverse matrix.</returns>
    [Benchmark]
    public Matrix3<double> Matrix3Inverse() => matrix.Inverse();
}
