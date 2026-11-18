// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Contracts;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Cli.Verify;

/// <summary>
/// The <c>verify-v1</c> JSON report emitted by <c>mocassin verify</c>.
/// </summary>
/// <param name="SchemaVersion">The report schema version.</param>
/// <param name="Kind">The report kind.</param>
/// <param name="Ok">Whether the exit code is zero.</param>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="Summary">The diagnostic counts.</param>
/// <param name="Diagnostics">The <c>PKG</c> diagnostics.</param>
/// <param name="Valid">Whether the package passed verification.</param>
/// <param name="Checks">The ordered verification checks.</param>
/// <param name="Package">The inspection projection, or <see langword="null"/> on failure.</param>
internal sealed record VerifyReport(
    int SchemaVersion,
    string Kind,
    bool Ok,
    int ExitCode,
    ReportSummary Summary,
    IReadOnlyList<ReportDiagnostic> Diagnostics,
    bool Valid,
    IReadOnlyList<VerifyReportCheck> Checks,
    Inspect.PackageReport? Package);

/// <summary>
/// One ordered verification check in the verify report.
/// </summary>
/// <param name="Code">The primary <c>PKG</c> code or check code.</param>
/// <param name="Passed">Whether the check passed.</param>
/// <param name="Message">The short result message.</param>
internal sealed record VerifyReportCheck(string Code, bool Passed, string Message);

/// <summary>
/// Writes the <c>verify-v1</c> report in its JSON and human forms.
/// </summary>
internal static class VerifyReportWriter
{
    /// <summary>
    /// Writes the report as exactly one camelCase JSON object.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteJson(TextWriter writer, VerifyReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        JsonReportWriter.Write(writer, report);
    }

    /// <summary>
    /// Writes the human verification summary.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    /// <param name="path">The verified path.</param>
    public static void WriteHuman(TextWriter writer, VerifyReport report, string path)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(path);
        writer.WriteLine($"Package: {path}");
        writer.WriteLine($"Valid: {(report.Valid ? "yes" : "no")}");
        writer.WriteLine("Checks:");
        foreach (var check in report.Checks)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"  [{(check.Passed ? "PASS" : "FAIL")}] {check.Code} {check.Message}"));
        }

        writer.WriteLine(
            $"Summary: {report.Summary.Errors} error(s), {report.Summary.Warnings} warning(s), " +
            $"{report.Summary.Infos} info(s)");
        writer.WriteLine($"Result: {(report.Ok ? "ok" : "failed")} (exit {report.ExitCode})");
    }

    /// <summary>
    /// Creates the report of a usage error before any work.
    /// </summary>
    /// <param name="diagnostics">The usage diagnostics.</param>
    /// <returns>The schema-valid usage report.</returns>
    public static VerifyReport UsageError(IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(diagnostics);
        return new VerifyReport(
            ContractVersions.VerifyReportVersion,
            "verify",
            Ok: false,
            CliExitCode.Usage,
            ReportEnvelope.Count(reportDiagnostics),
            reportDiagnostics,
            Valid: false,
            Checks: [],
            Package: null);
    }
}
