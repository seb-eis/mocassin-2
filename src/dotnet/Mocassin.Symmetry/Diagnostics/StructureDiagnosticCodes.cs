// SPDX-License-Identifier: MIT

namespace Mocassin.Symmetry.Diagnostics;

/// <summary>
/// Structured diagnostic codes emitted by symmetry-side structure validation.
/// </summary>
/// <remarks>
/// The complete structure catalogue is finalized in Step 03/07. Only the cell codes needed by Step 02 are
/// declared here.
/// </remarks>
internal static class StructureDiagnosticCodes
{
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
}
