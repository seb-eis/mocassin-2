// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Contracts.Cli;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Cli.Diagnostics;

/// <summary>
/// Creates and converts the <c>CLI001</c>–<c>CLI009</c> diagnostics owned by the command-line interface.
/// </summary>
internal static class CliDiagnosticFactory
{
    /// <summary>
    /// Creates a <c>CLI001</c> usage diagnostic.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <param name="hint">An optional actionable hint.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic Usage(string message, string? hint = null) =>
        new(CliDiagnosticCodes.Usage, DiagnosticSeverity.Error, message, hint: hint);

    /// <summary>
    /// Creates a <c>CLI002</c> I/O diagnostic.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic Io(string message) =>
        new(CliDiagnosticCodes.Io, DiagnosticSeverity.Error, message);

    /// <summary>
    /// Creates a <c>CLI003</c> output-exists diagnostic.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic OutputExists(string message) =>
        new(CliDiagnosticCodes.OutputExists, DiagnosticSeverity.Error, message);

    /// <summary>
    /// Creates a <c>CLI004</c> selection diagnostic.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <param name="hint">An optional actionable hint.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic Selection(string message, string? hint = null) =>
        new(CliDiagnosticCodes.Selection, DiagnosticSeverity.Error, message, hint: hint);

    /// <summary>
    /// Creates a <c>CLI005</c> job-selection diagnostic.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic JobSelection(string message) =>
        new(CliDiagnosticCodes.JobSelection, DiagnosticSeverity.Error, message);

    /// <summary>
    /// Creates a <c>CLI006</c> cancellation diagnostic.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic Cancelled(string message) =>
        new(CliDiagnosticCodes.Cancelled, DiagnosticSeverity.Info, message);

    /// <summary>
    /// Creates a <c>CLI007</c> internal-error diagnostic.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic Internal(string message) =>
        new(CliDiagnosticCodes.Internal, DiagnosticSeverity.Error, message);

    /// <summary>
    /// Creates a <c>CLI008</c> reserved-command diagnostic.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic Reserved(string message) =>
        new(CliDiagnosticCodes.Reserved, DiagnosticSeverity.Error, message);

    /// <summary>
    /// Creates a <c>CLI009</c> not-a-package diagnostic.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <param name="hint">An optional actionable hint.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic NotAPackage(string message, string? hint = null) =>
        new(CliDiagnosticCodes.NotAPackage, DiagnosticSeverity.Error, message, hint: hint);

    /// <summary>
    /// Converts a domain diagnostic to its report form.
    /// </summary>
    /// <param name="diagnostic">The diagnostic.</param>
    /// <returns>The report diagnostic.</returns>
    public static ReportDiagnostic ToReportDiagnostic(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return new ReportDiagnostic(
            diagnostic.Code,
            SeverityName(diagnostic.Severity),
            diagnostic.Message,
            ToReportPath(diagnostic.Path),
            diagnostic.Hint,
            diagnostic.Related.Select(static path => ToReportPath(path)!).ToArray());
    }

    /// <summary>
    /// Converts a set of domain diagnostics to their report forms, dropping exact duplicates.
    /// </summary>
    /// <param name="diagnostics">The diagnostics.</param>
    /// <returns>The deduplicated report diagnostics.</returns>
    public static IReadOnlyList<ReportDiagnostic> ToReportDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        var seen = new HashSet<(string Code, ReportPath? Path, string Message)>();
        var result = new List<ReportDiagnostic>();
        foreach (var diagnostic in diagnostics)
        {
            var report = ToReportDiagnostic(diagnostic);
            if (seen.Add((report.Code, report.Path, report.Message)))
            {
                result.Add(report);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the stable lower-case name of a severity.
    /// </summary>
    /// <param name="severity">The severity.</param>
    /// <returns><c>error</c>, <c>warning</c>, or <c>info</c>.</returns>
    public static string SeverityName(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => "error",
        DiagnosticSeverity.Warning => "warning",
        _ => "info",
    };

    private static ReportPath? ToReportPath(ModelPath? path) =>
        path is null
            ? null
            : new ReportPath(
                path.Domain,
                path.ObjectId.ToString("D", CultureInfo.InvariantCulture),
                path.Property);
}
