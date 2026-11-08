// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model;
using Mocassin.Core.Symmetry;

namespace Mocassin.Symmetry;

/// <summary>
/// The validated continuous geometry of a crystallographic unit cell.
/// </summary>
/// <remarks>
/// <para>
/// The cell matrix <c>M</c> has the vectors <see cref="A"/>, <see cref="B"/>, and <see cref="C"/> as columns.
/// Fractional coordinates convert with <c>x = M·f</c>; Cartesian coordinates convert with <c>f = M⁻¹·x</c>.
/// The metric tensor is <c>G = MᵀM</c>, so distances use <c>d² = dfᵀ G df</c>.
/// </para>
/// <para>
/// Geometry is immutable and thread-safe. Validate user input with <see cref="TryCreate(CellParameters, out CellGeometry?, out ImmutableArray{Diagnostic})"/>;
/// invalid parameters produce structured diagnostics and never silently normalize values.
/// </para>
/// </remarks>
public sealed class CellGeometry
{
    private readonly double g00;
    private readonly double g01;
    private readonly double g02;
    private readonly double g11;
    private readonly double g12;
    private readonly double g22;

    private CellGeometry(CellParameters parameters, Cartesian3 a, Cartesian3 b, Cartesian3 c)
    {
        Parameters = parameters;
        A = a;
        B = b;
        C = c;
        Matrix = Matrix3<double>.FromColumns(
            new Vector3<double>(a.X, a.Y, a.Z),
            new Vector3<double>(b.X, b.Y, b.Z),
            new Vector3<double>(c.X, c.Y, c.Z));
        Inverse = Matrix.Inverse();
        VolumeAngstrom3 = Math.Abs(Matrix.Determinant);
        ConditionNumber = Matrix.ConditionNumber;

        g00 = a.Dot(a);
        g01 = a.Dot(b);
        g02 = a.Dot(c);
        g11 = b.Dot(b);
        g12 = b.Dot(c);
        g22 = c.Dot(c);
    }

    /// <summary>
    /// Gets the validated input parameters.
    /// </summary>
    public CellParameters Parameters { get; }

    /// <summary>
    /// Gets the first cell vector (column a).
    /// </summary>
    public Cartesian3 A { get; }

    /// <summary>
    /// Gets the second cell vector (column b).
    /// </summary>
    public Cartesian3 B { get; }

    /// <summary>
    /// Gets the third cell vector (column c).
    /// </summary>
    public Cartesian3 C { get; }

    /// <summary>
    /// Gets the cell matrix with <see cref="A"/>, <see cref="B"/>, and <see cref="C"/> as columns.
    /// </summary>
    public Matrix3<double> Matrix { get; }

    /// <summary>
    /// Gets the inverse cell matrix.
    /// </summary>
    public Matrix3<double> Inverse { get; }

    /// <summary>
    /// Gets the cell volume in cubic ångström.
    /// </summary>
    public double VolumeAngstrom3 { get; }

    /// <summary>
    /// Gets the infinity-norm condition number <c>||M||∞ · ||M⁻¹||∞</c>.
    /// </summary>
    public double ConditionNumber { get; }

    /// <summary>
    /// Validates cell parameters without applying crystal-system constraints.
    /// </summary>
    /// <param name="parameters">The cell parameters.</param>
    /// <param name="geometry">The geometry when validation succeeds.</param>
    /// <param name="diagnostics">The structured validation diagnostics.</param>
    /// <returns><see langword="true"/> when the general cell geometry is valid.</returns>
    public static bool TryCreate(
        CellParameters parameters,
        out CellGeometry? geometry,
        out ImmutableArray<Diagnostic> diagnostics) =>
        TryCreateCore(parameters, null, AxisSetting.None, out geometry, out diagnostics);

    /// <summary>
    /// Validates cell parameters against a crystal system and axis setting.
    /// </summary>
    /// <param name="parameters">The cell parameters.</param>
    /// <param name="crystalSystem">The catalog-derived crystal system.</param>
    /// <param name="axis">The catalog axis setting.</param>
    /// <param name="geometry">The geometry when validation succeeds.</param>
    /// <param name="diagnostics">The structured validation diagnostics.</param>
    /// <returns><see langword="true"/> when the geometry matches the crystal system.</returns>
    public static bool TryCreate(
        CellParameters parameters,
        CrystalSystem crystalSystem,
        AxisSetting axis,
        out CellGeometry? geometry,
        out ImmutableArray<Diagnostic> diagnostics) =>
        TryCreateCore(parameters, crystalSystem, axis, out geometry, out diagnostics);

    /// <summary>
    /// Validates cell parameters against a resolved space-group setting.
    /// </summary>
    /// <param name="parameters">The cell parameters.</param>
    /// <param name="setting">The resolved catalog setting.</param>
    /// <param name="geometry">The geometry when validation succeeds.</param>
    /// <param name="diagnostics">The structured validation diagnostics.</param>
    /// <returns><see langword="true"/> when the geometry matches the setting's crystal system.</returns>
    public static bool TryCreate(
        CellParameters parameters,
        SpaceGroupSetting setting,
        out CellGeometry? geometry,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(setting);
        return TryCreateCore(parameters, setting.CrystalSystem, setting.Axis, out geometry, out diagnostics);
    }

    /// <summary>
    /// Converts a fractional coordinate to a Cartesian point.
    /// </summary>
    /// <param name="fractional">The fractional coordinate.</param>
    /// <returns>The Cartesian point in ångström.</returns>
    public Cartesian3 ToCartesian(Fractional3 fractional)
    {
        var transformed = Matrix.Transform(new Vector3<double>(fractional.A, fractional.B, fractional.C));
        return new Cartesian3(transformed.X, transformed.Y, transformed.Z);
    }

    /// <summary>
    /// Converts a Cartesian point to a fractional coordinate.
    /// </summary>
    /// <param name="cartesian">The Cartesian point in ångström.</param>
    /// <returns>The fractional coordinate.</returns>
    public Fractional3 ToFractional(Cartesian3 cartesian)
    {
        var transformed = Inverse.Transform(new Vector3<double>(cartesian.X, cartesian.Y, cartesian.Z));
        return new Fractional3(transformed.X, transformed.Y, transformed.Z);
    }

    /// <summary>
    /// Computes the direct, non-minimum-image distance between two fractional coordinates.
    /// </summary>
    /// <param name="left">The first coordinate.</param>
    /// <param name="right">The second coordinate.</param>
    /// <returns>The distance in ångström.</returns>
    public double GetDistance(Fractional3 left, Fractional3 right)
    {
        var da = left.A - right.A;
        var db = left.B - right.B;
        var dc = left.C - right.C;
        var squared =
            (g00 * da * da)
            + (g11 * db * db)
            + (g22 * dc * dc)
            + (2.0 * ((g01 * da * db) + (g02 * da * dc) + (g12 * db * dc)));

        return Math.Sqrt(Math.Max(0.0, squared));
    }

    /// <summary>
    /// Gets the cell volume in cubic ångström.
    /// </summary>
    /// <returns>The cell volume.</returns>
    public double GetCellVolume() => VolumeAngstrom3;

    /// <summary>
    /// Determines whether a condition number is finite and within the v1 limit.
    /// </summary>
    /// <param name="conditionNumber">The condition number to test.</param>
    /// <returns><see langword="true"/> when the value is accepted.</returns>
    internal static bool IsConditionWithinLimit(double conditionNumber) =>
        double.IsFinite(conditionNumber)
        && conditionNumber <= GeometryTolerances.CellConditionLimit;

    private static bool TryCreateCore(
        CellParameters parameters,
        CrystalSystem? crystalSystem,
        AxisSetting axis,
        out CellGeometry? geometry,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        geometry = null;
        var bag = new DiagnosticBag();

        ValidateLength(parameters.A.Value, "a", bag);
        ValidateLength(parameters.B.Value, "b", bag);
        ValidateLength(parameters.C.Value, "c", bag);
        ValidateAngle(parameters.Alpha.Value, "α", bag);
        ValidateAngle(parameters.Beta.Value, "β", bag);
        ValidateAngle(parameters.Gamma.Value, "γ", bag);

        if (bag.HasErrors)
        {
            diagnostics = bag.Sorted;
            return false;
        }

        var alpha = parameters.Alpha.Value * (Math.PI / 180.0);
        var beta = parameters.Beta.Value * (Math.PI / 180.0);
        var gamma = parameters.Gamma.Value * (Math.PI / 180.0);
        var cosAlpha = Math.Cos(alpha);
        var cosBeta = Math.Cos(beta);
        var cosGamma = Math.Cos(gamma);
        var sinGamma = Math.Sin(gamma);

        var determinantG =
            1.0
            - (cosAlpha * cosAlpha)
            - (cosBeta * cosBeta)
            - (cosGamma * cosGamma)
            + (2.0 * cosAlpha * cosBeta * cosGamma);
        if (!double.IsFinite(determinantG) || determinantG <= GeometryTolerances.CellDeterminantTolerance)
        {
            bag.Add(new Diagnostic(
                StructureDiagnosticCodes.CellGeometry,
                DiagnosticSeverity.Error,
                "The cell Gram determinant must be positive and above the near-degeneracy tolerance."));
            diagnostics = bag.Sorted;
            return false;
        }

        var a = parameters.A.Value;
        var b = parameters.B.Value;
        var c = parameters.C.Value;
        var vectorA = new Cartesian3(a, 0.0, 0.0);
        var vectorB = new Cartesian3(b * cosGamma, b * sinGamma, 0.0);
        var vectorC = new Cartesian3(
            c * cosBeta,
            c * (cosAlpha - (cosBeta * cosGamma)) / sinGamma,
            c * Math.Sqrt(determinantG) / sinGamma);

        var candidate = new CellGeometry(parameters, vectorA, vectorB, vectorC);
        if (!IsConditionWithinLimit(candidate.ConditionNumber))
        {
            bag.Add(new Diagnostic(
                StructureDiagnosticCodes.CellGeometry,
                DiagnosticSeverity.Error,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The cell matrix condition number {candidate.ConditionNumber} exceeds the limit {GeometryTolerances.CellConditionLimit}.")));
            diagnostics = bag.Sorted;
            return false;
        }

        if (crystalSystem is { } system)
        {
            ValidateCrystalSystem(parameters, system, axis, bag);
        }

        if (bag.HasErrors)
        {
            diagnostics = bag.Sorted;
            return false;
        }

        geometry = candidate;
        diagnostics = bag.Sorted;
        return true;
    }

    private static void ValidateLength(double value, string name, DiagnosticBag bag)
    {
        if (value is < 0.1 or > 1000.0)
        {
            bag.Add(new Diagnostic(
                StructureDiagnosticCodes.CellLength,
                DiagnosticSeverity.Error,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The cell length {name} must be between 0.1 and 1000 Å, but was {value} Å.")));
        }
    }

    private static void ValidateAngle(double value, string name, DiagnosticBag bag)
    {
        if (!double.IsFinite(value) || value <= 0.0 || value >= 180.0)
        {
            bag.Add(new Diagnostic(
                StructureDiagnosticCodes.CellGeometry,
                DiagnosticSeverity.Error,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The cell angle {name} must be strictly between 0° and 180°, but was {value}°.")));
        }
    }

    private static void ValidateCrystalSystem(
        CellParameters parameters,
        CrystalSystem crystalSystem,
        AxisSetting axis,
        DiagnosticBag bag)
    {
        switch (crystalSystem)
        {
            case CrystalSystem.Triclinic:
                RequireAxis(axis, AxisSetting.None, crystalSystem, bag);
                break;

            case CrystalSystem.Monoclinic:
                if (axis == AxisSetting.UniqueAxisB)
                {
                    RequireAngle(parameters.Alpha.Value, 90.0, "α", crystalSystem, bag);
                    RequireAngle(parameters.Gamma.Value, 90.0, "γ", crystalSystem, bag);
                }
                else if (axis == AxisSetting.UniqueAxisC)
                {
                    RequireAngle(parameters.Alpha.Value, 90.0, "α", crystalSystem, bag);
                    RequireAngle(parameters.Beta.Value, 90.0, "β", crystalSystem, bag);
                }
                else
                {
                    RequireAxis(axis, AxisSetting.UniqueAxisB, crystalSystem, bag);
                }

                break;

            case CrystalSystem.Orthorhombic:
                RequireAxis(axis, AxisSetting.None, crystalSystem, bag);
                RequireAllRightAngles(parameters, crystalSystem, bag);
                break;

            case CrystalSystem.Tetragonal:
                RequireAxis(axis, AxisSetting.None, crystalSystem, bag);
                RequireEqualLengths(parameters.A.Value, parameters.B.Value, "a", "b", crystalSystem, bag);
                RequireAllRightAngles(parameters, crystalSystem, bag);
                break;

            case CrystalSystem.Trigonal when axis == AxisSetting.Hexagonal:
                RequireHexagonalGeometry(parameters, crystalSystem, bag);
                break;

            case CrystalSystem.Trigonal when axis == AxisSetting.Rhombohedral:
                RequireEqualLengths(parameters.A.Value, parameters.B.Value, "a", "b", crystalSystem, bag);
                RequireEqualLengths(parameters.A.Value, parameters.C.Value, "a", "c", crystalSystem, bag);
                RequireEqualAngles(parameters, crystalSystem, bag);
                if (parameters.Alpha.Value >= 120.0)
                {
                    AddSystemDiagnostic(
                        crystalSystem,
                        "The rhombohedral angle must be below 120°.",
                        bag);
                }

                break;

            case CrystalSystem.Hexagonal:
                RequireAxis(axis, AxisSetting.None, crystalSystem, bag);
                RequireHexagonalGeometry(parameters, crystalSystem, bag);
                break;

            case CrystalSystem.Cubic:
                RequireAxis(axis, AxisSetting.None, crystalSystem, bag);
                RequireEqualLengths(parameters.A.Value, parameters.B.Value, "a", "b", crystalSystem, bag);
                RequireEqualLengths(parameters.A.Value, parameters.C.Value, "a", "c", crystalSystem, bag);
                RequireAllRightAngles(parameters, crystalSystem, bag);
                break;

            case CrystalSystem.Trigonal:
                AddSystemDiagnostic(
                    crystalSystem,
                    "Trigonal cells require the hexagonal or rhombohedral axis setting.",
                    bag);
                break;

            default:
                AddSystemDiagnostic(crystalSystem, "The crystal system is not supported.", bag);
                break;
        }
    }

    private static void RequireHexagonalGeometry(
        CellParameters parameters,
        CrystalSystem crystalSystem,
        DiagnosticBag bag)
    {
        RequireEqualLengths(parameters.A.Value, parameters.B.Value, "a", "b", crystalSystem, bag);
        RequireAngle(parameters.Alpha.Value, 90.0, "α", crystalSystem, bag);
        RequireAngle(parameters.Beta.Value, 90.0, "β", crystalSystem, bag);
        RequireAngle(parameters.Gamma.Value, 120.0, "γ", crystalSystem, bag);
    }

    private static void RequireAllRightAngles(
        CellParameters parameters,
        CrystalSystem crystalSystem,
        DiagnosticBag bag)
    {
        RequireAngle(parameters.Alpha.Value, 90.0, "α", crystalSystem, bag);
        RequireAngle(parameters.Beta.Value, 90.0, "β", crystalSystem, bag);
        RequireAngle(parameters.Gamma.Value, 90.0, "γ", crystalSystem, bag);
    }

    private static void RequireEqualAngles(CellParameters parameters, CrystalSystem crystalSystem, DiagnosticBag bag)
    {
        RequireAngle(parameters.Beta.Value, parameters.Alpha.Value, "β", crystalSystem, bag);
        RequireAngle(parameters.Gamma.Value, parameters.Alpha.Value, "γ", crystalSystem, bag);
    }

    private static void RequireEqualLengths(
        double actual,
        double expected,
        string actualName,
        string expectedName,
        CrystalSystem crystalSystem,
        DiagnosticBag bag)
    {
        if (Math.Abs(actual - expected) > GeometryTolerances.CellLengthTolerance)
        {
            AddSystemDiagnostic(
                crystalSystem,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The cell lengths {actualName} and {expectedName} must be equal, but were {actual} Å and {expected} Å."),
                bag);
        }
    }

    private static void RequireAngle(
        double actual,
        double expected,
        string name,
        CrystalSystem crystalSystem,
        DiagnosticBag bag)
    {
        if (Math.Abs(actual - expected) > GeometryTolerances.CellAngleTolerance)
        {
            AddSystemDiagnostic(
                crystalSystem,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The cell angle {name} must be {expected}°, but was {actual}°."),
                bag);
        }
    }

    private static void RequireAxis(
        AxisSetting actual,
        AxisSetting expected,
        CrystalSystem crystalSystem,
        DiagnosticBag bag)
    {
        if (actual != expected)
        {
            AddSystemDiagnostic(
                crystalSystem,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The crystal system requires the {expected} axis setting, but was {actual}."),
                bag);
        }
    }

    private static void AddSystemDiagnostic(CrystalSystem crystalSystem, string message, DiagnosticBag bag) =>
        bag.Add(new Diagnostic(
            StructureDiagnosticCodes.CellGeometry,
            DiagnosticSeverity.Error,
            string.Create(CultureInfo.InvariantCulture, $"{message} (Crystal system: {crystalSystem}.)")));
}
