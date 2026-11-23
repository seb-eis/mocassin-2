// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Core.Model;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;

namespace Mocassin.App.ViewModels.Editors.Structure;

/// <summary>
/// The six cell-parameter fields in display order.
/// </summary>
internal enum CellFieldKind
{
    /// <summary>The a-axis length.</summary>
    A,

    /// <summary>The b-axis length.</summary>
    B,

    /// <summary>The c-axis length.</summary>
    C,

    /// <summary>The alpha angle.</summary>
    Alpha,

    /// <summary>The beta angle.</summary>
    Beta,

    /// <summary>The gamma angle.</summary>
    Gamma,
}

/// <summary>
/// One cell-field lock: fixed to a value or mirrored from another field.
/// </summary>
/// <param name="IsLocked">Whether the field is locked.</param>
/// <param name="FixedValue">The fixed value for a fixed lock.</param>
/// <param name="Source">The source field for a mirror lock.</param>
/// <param name="Reason">The user-visible reason.</param>
internal readonly record struct CellParameterLock(
    bool IsLocked,
    double? FixedValue,
    CellFieldKind? Source,
    string? Reason)
{
    /// <summary>
    /// Gets an unlocked field.
    /// </summary>
    public static CellParameterLock Free => default;

    /// <summary>
    /// Creates a fixed-value lock.
    /// </summary>
    /// <param name="value">The fixed value.</param>
    /// <param name="reason">The reason.</param>
    /// <returns>The lock.</returns>
    public static CellParameterLock Fixed(double value, string reason) => new(true, value, null, reason);

    /// <summary>
    /// Creates a mirror lock.
    /// </summary>
    /// <param name="source">The source field.</param>
    /// <param name="reason">The reason.</param>
    /// <returns>The lock.</returns>
    public static CellParameterLock Mirror(CellFieldKind source, string reason) => new(true, null, source, reason);
}

/// <summary>
/// The lock table for one resolved crystal system and axis setting. It is an affordance only:
/// <c>CellGeometry</c> remains the validity authority.
/// </summary>
internal sealed class CellParameterLockTable
{
    private readonly Dictionary<CellFieldKind, CellParameterLock> locks;

    /// <summary>
    /// Initializes a lock table.
    /// </summary>
    /// <param name="locks">The locks by field.</param>
    public CellParameterLockTable(IReadOnlyDictionary<CellFieldKind, CellParameterLock> locks)
    {
        this.locks = new Dictionary<CellFieldKind, CellParameterLock>(locks);
    }

    /// <summary>
    /// Gets the lock for one field.
    /// </summary>
    /// <param name="kind">The field kind.</param>
    /// <returns>The lock, or a free lock.</returns>
    public CellParameterLock this[CellFieldKind kind] =>
        locks.TryGetValue(kind, out var value) ? value : CellParameterLock.Free;

    /// <summary>
    /// Determines whether a field is locked.
    /// </summary>
    /// <param name="kind">The field kind.</param>
    /// <returns><see langword="true"/> when locked.</returns>
    public bool IsLocked(CellFieldKind kind) => this[kind].IsLocked;

    /// <summary>
    /// Applies every lock to the supplied parameters.
    /// </summary>
    /// <param name="parameters">The raw parameters.</param>
    /// <returns>The parameters with locks enforced.</returns>
    public CellParameters Apply(CellParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var value = new Dictionary<CellFieldKind, double>
        {
            [CellFieldKind.A] = parameters.A.Value,
            [CellFieldKind.B] = parameters.B.Value,
            [CellFieldKind.C] = parameters.C.Value,
            [CellFieldKind.Alpha] = parameters.Alpha.Value,
            [CellFieldKind.Beta] = parameters.Beta.Value,
            [CellFieldKind.Gamma] = parameters.Gamma.Value,
        };

        foreach (var (kind, cellLock) in locks)
        {
            if (!cellLock.IsLocked)
            {
                continue;
            }

            value[kind] = cellLock.FixedValue ?? value[cellLock.Source!.Value];
        }

        return new CellParameters(
            new LengthAngstrom(value[CellFieldKind.A]),
            new LengthAngstrom(value[CellFieldKind.B]),
            new LengthAngstrom(value[CellFieldKind.C]),
            new AngleDegrees(value[CellFieldKind.Alpha]),
            new AngleDegrees(value[CellFieldKind.Beta]),
            new AngleDegrees(value[CellFieldKind.Gamma]));
    }
}

/// <summary>
/// The crystal-system lock table. It mirrors the constraints enforced by <c>CellGeometry</c>; the
/// <c>CellParameterTests</c> cross-check keeps the two from drifting.
/// </summary>
internal static class CellParameterLocks
{
    private const string rightAngleReason = "The crystal system requires a 90° angle.";
    private const string equalLengthReason = "The crystal system requires equal cell lengths.";
    private const string equalAngleReason = "The rhombohedral cell requires α = β = γ.";
    private const string hexagonalReason = "The hexagonal description requires α = β = 90° and γ = 120°.";
    private const string monoclinicBReason = "The monoclinic unique b axis fixes α and γ at 90°.";
    private const string monoclinicCReason = "The monoclinic unique c axis fixes α and β at 90°.";

    /// <summary>
    /// Gets the lock table for a crystal system and axis setting.
    /// </summary>
    /// <param name="system">The crystal system.</param>
    /// <param name="axis">The axis setting.</param>
    /// <returns>The lock table.</returns>
    public static CellParameterLockTable For(CrystalSystem system, AxisSetting axis)
    {
        var locks = new Dictionary<CellFieldKind, CellParameterLock>();
        switch (system)
        {
            case CrystalSystem.Triclinic:
                break;

            case CrystalSystem.Monoclinic:
                if (axis == AxisSetting.UniqueAxisC)
                {
                    Fix(locks, CellFieldKind.Alpha, 90d, monoclinicCReason);
                    Fix(locks, CellFieldKind.Beta, 90d, monoclinicCReason);
                }
                else if (axis == AxisSetting.UniqueAxisB)
                {
                    Fix(locks, CellFieldKind.Alpha, 90d, monoclinicBReason);
                    Fix(locks, CellFieldKind.Gamma, 90d, monoclinicBReason);
                }

                break;

            case CrystalSystem.Orthorhombic:
                FixAngles(locks);
                break;

            case CrystalSystem.Tetragonal:
                locks[CellFieldKind.B] = CellParameterLock.Mirror(CellFieldKind.A, equalLengthReason);
                FixAngles(locks);
                break;

            case CrystalSystem.Trigonal when axis == AxisSetting.Rhombohedral:
                locks[CellFieldKind.B] = CellParameterLock.Mirror(CellFieldKind.A, equalLengthReason);
                locks[CellFieldKind.C] = CellParameterLock.Mirror(CellFieldKind.A, equalLengthReason);
                locks[CellFieldKind.Beta] = CellParameterLock.Mirror(CellFieldKind.Alpha, equalAngleReason);
                locks[CellFieldKind.Gamma] = CellParameterLock.Mirror(CellFieldKind.Alpha, equalAngleReason);
                break;

            case CrystalSystem.Trigonal when axis == AxisSetting.Hexagonal:
            case CrystalSystem.Hexagonal:
                locks[CellFieldKind.B] = CellParameterLock.Mirror(CellFieldKind.A, equalLengthReason);
                Fix(locks, CellFieldKind.Alpha, 90d, hexagonalReason);
                Fix(locks, CellFieldKind.Beta, 90d, hexagonalReason);
                Fix(locks, CellFieldKind.Gamma, 120d, hexagonalReason);
                break;

            case CrystalSystem.Cubic:
                locks[CellFieldKind.B] = CellParameterLock.Mirror(CellFieldKind.A, equalLengthReason);
                locks[CellFieldKind.C] = CellParameterLock.Mirror(CellFieldKind.A, equalLengthReason);
                FixAngles(locks);
                break;

            case CrystalSystem.Trigonal:
                // A trigonal setting without an axis is invalid; CellGeometry reports it. No lock hides that.
                break;

            default:
                break;
        }

        return new CellParameterLockTable(locks);
    }

    /// <summary>
    /// Formats a number for a cell field using invariant culture.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The formatted value.</returns>
    public static string Format(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static void FixAngles(Dictionary<CellFieldKind, CellParameterLock> locks)
    {
        Fix(locks, CellFieldKind.Alpha, 90d, rightAngleReason);
        Fix(locks, CellFieldKind.Beta, 90d, rightAngleReason);
        Fix(locks, CellFieldKind.Gamma, 90d, rightAngleReason);
    }

    private static void Fix(
        Dictionary<CellFieldKind, CellParameterLock> locks,
        CellFieldKind kind,
        double value,
        string reason) => locks[kind] = CellParameterLock.Fixed(value, reason);
}
