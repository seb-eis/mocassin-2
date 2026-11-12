// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Symmetry.Diagnostics;

/// <summary>
/// Registers the stable structure and symmetry diagnostic codes.
/// </summary>
public static class SymmetryDiagnosticCatalog
{
    /// <summary>
    /// Gets the registered descriptors, sorted by ordinal code.
    /// </summary>
    public static ImmutableArray<DiagnosticDescriptor> Descriptors { get; } =
    [
        new("STR001", DiagnosticSeverity.Error, "Step 02 — symmetry and geometry", "The structured space-group reference does not identify a catalog setting", "Review the referenced model object, correct the reported condition, and revalidate.", "#str001"),
        new("STR002", DiagnosticSeverity.Error, "Step 02 — symmetry and geometry", "A cell length is outside the supported range", "Review the referenced model object, correct the reported condition, and revalidate.", "#str002"),
        new("STR003", DiagnosticSeverity.Error, "Step 02 — symmetry and geometry", "Cell angles, determinant, conditioning, or crystal-system constraints are invalid", "Review the referenced model object, correct the reported condition, and revalidate.", "#str003"),
        new("STR007", DiagnosticSeverity.Error, "Step 02 — symmetry and geometry", "Two reference sites expand to the same P1 orbit position", "Review the referenced model object, correct the reported condition, and revalidate.", "#str007"),
        new("STR009", DiagnosticSeverity.Warning, "Step 02 — symmetry and geometry", "The total P1 position count exceeds the performance budget", "Review the referenced model object, correct the reported condition, and revalidate.", "#str009"),
        new("STR014", DiagnosticSeverity.Error, "Step 02 — symmetry and geometry", "The requested supercell exceeds the signed 32-bit linear-index limit", "Review the referenced model object, correct the reported condition, and revalidate.", "#str014"),
        new("SYM001", DiagnosticSeverity.Error, "Step 02 — symmetry and geometry", "A fractional coordinate was NaN or infinite", "Review the referenced model object, correct the reported condition, and revalidate.", "#sym001"),
        new("SYM002", DiagnosticSeverity.Warning, "Step 02 — symmetry and geometry", "A fractional coordinate was rounded to the nearest exact-grid point", "Review the referenced model object, correct the reported condition, and revalidate.", "#sym002"),
        new("SYM003", DiagnosticSeverity.Error, "Step 02 — symmetry and geometry", "A fractional coordinate is outside the representable exact-grid range", "Review the referenced model object, correct the reported condition, and revalidate.", "#sym003"),
    ];
}
