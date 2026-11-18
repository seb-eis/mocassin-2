// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.CommandLine.Reports;

/// <summary>
/// The fields shared by every report in the <c>build-v1</c> report family.
/// </summary>
/// <remarks>
/// Command reports add their own payload fields after the envelope. The envelope is a composition type, not a
/// base class: the JSON property order of each report matches its registered schema, and validation keeps its
/// separate <c>validation-v1</c> shape.
/// </remarks>
/// <param name="SchemaVersion">The report schema version.</param>
/// <param name="Kind">The report kind registered in the contract manifest.</param>
/// <param name="Ok">Whether <paramref name="ExitCode"/> is zero.</param>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="Summary">The deduplicated diagnostic counts.</param>
/// <param name="Diagnostics">The report diagnostics.</param>
internal sealed record ReportEnvelope(
    int SchemaVersion,
    string Kind,
    bool Ok,
    int ExitCode,
    ReportSummary Summary,
    IReadOnlyList<ReportDiagnostic> Diagnostics)
{
    /// <summary>
    /// Creates an envelope whose <see cref="Ok"/> value follows the exit code.
    /// </summary>
    /// <param name="schemaVersion">The report schema version.</param>
    /// <param name="kind">The report kind.</param>
    /// <param name="exitCode">The process exit code.</param>
    /// <param name="diagnostics">The report diagnostics.</param>
    /// <returns>The envelope.</returns>
    public static ReportEnvelope Create(
        int schemaVersion,
        string kind,
        int exitCode,
        IReadOnlyList<ReportDiagnostic> diagnostics) =>
        new(
            schemaVersion,
            kind,
            exitCode == 0,
            exitCode,
            Count(diagnostics),
            diagnostics);

    /// <summary>
    /// Counts diagnostics by severity.
    /// </summary>
    /// <param name="diagnostics">The report diagnostics.</param>
    /// <returns>The deduplicated counts.</returns>
    public static ReportSummary Count(IReadOnlyList<ReportDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        return new ReportSummary(
            diagnostics.Count(static diagnostic => diagnostic.Severity == "error"),
            diagnostics.Count(static diagnostic => diagnostic.Severity == "warning"),
            diagnostics.Count(static diagnostic => diagnostic.Severity == "info"));
    }
}
