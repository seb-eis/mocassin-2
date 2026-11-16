// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Contracts.Packages;

/// <summary>
/// Stable diagnostic codes emitted by the read-only <c>.msl</c> inspector and verifier.
/// </summary>
/// <remarks>
/// The descriptors are owned by <c>Mocassin.Contracts</c> so that <c>Mocassin.Packages</c> can compose them into
/// its default catalog and <c>Mocassin.Results</c> can use them without referencing <c>Mocassin.Packages</c>.
/// </remarks>
public static class PackageDiagnosticCodes
{
    /// <summary>The package is not a readable SQLite database or failed its integrity check.</summary>
    public const string UnreadableDatabase = "PKG001";

    /// <summary>The application id, schema version, or format version is missing or unsupported.</summary>
    public const string UnsupportedSchema = "PKG002";

    /// <summary>A required table, index, column, or schema-info key is missing or malformed.</summary>
    public const string MissingSchemaElement = "PKG003";

    /// <summary>A payload is not a valid Zstd/MessagePack document or exceeds its size bound.</summary>
    public const string InvalidPayload = "PKG004";

    /// <summary>A parameterization or lattice signature does not match its re-encoded projection.</summary>
    public const string SignatureMismatch = "PKG005";

    /// <summary>A job, model, or lattice reference, job id, or unique triple is invalid.</summary>
    public const string InvalidReference = "PKG006";

    /// <summary>A payload-derived job column disagrees with its payload value.</summary>
    public const string ColumnMismatch = "PKG007";

    /// <summary>The package is not immutable: a results table or WAL sidecar is present.</summary>
    public const string NotImmutable = "PKG008";

    /// <summary>The package exceeds the advisory size limit.</summary>
    public const string AdvisorySize = "PKG009";

    /// <summary>A payload value is out of bounds for its declared dimensions.</summary>
    public const string OutOfBounds = "PKG010";
}

/// <summary>
/// Registers the Step 11 <c>.msl</c> diagnostic descriptors.
/// </summary>
public static class PackageDiagnosticDescriptors
{
    /// <summary>
    /// Gets the registered Step 11 descriptors, sorted by ordinal code.
    /// </summary>
    public static ImmutableArray<DiagnosticDescriptor> All { get; } =
    [
        new(
            PackageDiagnosticCodes.UnreadableDatabase,
            DiagnosticSeverity.Error,
            "Step 11 — simulation package",
            "The package is not a readable SQLite database",
            "Rebuild the package and confirm that the file is a published .msl package; run SQLite integrity_check.",
            "#pkg001"),
        new(
            PackageDiagnosticCodes.UnsupportedSchema,
            DiagnosticSeverity.Error,
            "Step 11 — simulation package",
            "The package schema or format version is missing or unsupported",
            "Rebuild the package with a compatible mocassin release.",
            "#pkg002"),
        new(
            PackageDiagnosticCodes.MissingSchemaElement,
            DiagnosticSeverity.Error,
            "Step 11 — simulation package",
            "A required schema element is missing or malformed",
            "Rebuild the package; a published package must contain every required table, index, column, and schema-info row.",
            "#pkg003"),
        new(
            PackageDiagnosticCodes.InvalidPayload,
            DiagnosticSeverity.Error,
            "Step 11 — simulation package",
            "A package payload cannot be decoded",
            "Rebuild the package. The payload must be a standard Zstd frame containing a MessagePack map within its size bound.",
            "#pkg004"),
        new(
            PackageDiagnosticCodes.SignatureMismatch,
            DiagnosticSeverity.Error,
            "Step 11 — simulation package",
            "A stored signature does not match its payload projection",
            "Rebuild the package with the same compiler and parameterization; the stored payload is inconsistent.",
            "#pkg005"),
        new(
            PackageDiagnosticCodes.InvalidReference,
            DiagnosticSeverity.Error,
            "Step 11 — simulation package",
            "A package reference or identifier is invalid",
            "Rebuild the package and confirm unique ascending job ids and valid model/lattice foreign keys.",
            "#pkg006"),
        new(
            PackageDiagnosticCodes.ColumnMismatch,
            DiagnosticSeverity.Error,
            "Step 11 — simulation package",
            "A payload-derived job column disagrees with the payload",
            "Rebuild the package; the denormalized columns must be copied from the resolved job payload.",
            "#pkg007"),
        new(
            PackageDiagnosticCodes.NotImmutable,
            DiagnosticSeverity.Error,
            "Step 11 — simulation package",
            "The package is not immutable",
            "Publish results to a separate .msr store and republish the package without a results table or WAL sidecar.",
            "#pkg008"),
        new(
            PackageDiagnosticCodes.AdvisorySize,
            DiagnosticSeverity.Warning,
            "Step 11 — simulation package",
            "The package exceeds the advisory size limit",
            "Reduce duplicate jobs or supercell sizes, or rebuild with --compact compression.",
            "#pkg009"),
        new(
            PackageDiagnosticCodes.OutOfBounds,
            DiagnosticSeverity.Error,
            "Step 11 — simulation package",
            "A package payload value is out of bounds",
            "Rebuild the package; table lengths, indices, tracker order, and mappings must match the declared dimensions.",
            "#pkg010"),
    ];
}
