// SPDX-License-Identifier: MIT

namespace Mocassin.Symmetry.Diagnostics;

/// <summary>
/// Structured diagnostic codes emitted by symmetry-side structure validation.
/// </summary>
/// <remarks>
/// The Core-side codes such as <c>STR004</c>, <c>STR005</c>, <c>STR006</c>, <c>STR010</c>, <c>STR012</c>,
/// <c>STR013</c>, and <c>STR015</c> are defined by <c>Mocassin.Core.Diagnostics.CoreStructureDiagnosticCodes</c>.
/// </remarks>
internal static class StructureDiagnosticCodes
{
    /// <summary>
    /// The structured space-group reference does not identify a catalog setting.
    /// </summary>
    public const string SpaceGroupReference = "STR001";

    /// <summary>
    /// A cell length is outside the supported range.
    /// </summary>
    public const string CellLength = "STR002";

    /// <summary>
    /// Cell angles, determinant, conditioning, or crystal-system constraints are invalid.
    /// </summary>
    public const string CellGeometry = "STR003";

    /// <summary>
    /// Two reference sites expand to the same P1 orbit position.
    /// </summary>
    public const string SiteOrbitOverlap = "STR007";

    /// <summary>
    /// The total P1 position count exceeds the performance budget.
    /// </summary>
    public const string P1PositionBudgetExceeded = "STR009";

    /// <summary>
    /// The requested supercell exceeds the signed 32-bit linear-index limit.
    /// </summary>
    public const string LinearIndexLimitExceeded = "STR014";
}
