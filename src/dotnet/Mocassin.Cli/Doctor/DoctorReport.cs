// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Contracts;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Cli.Doctor;

/// <summary>
/// The <c>doctor-v1</c> JSON report emitted by <c>mocassin doctor</c>.
/// </summary>
/// <param name="SchemaVersion">The report schema version.</param>
/// <param name="Kind">The report kind.</param>
/// <param name="Ok">Whether the exit code is zero.</param>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="Summary">The diagnostic counts.</param>
/// <param name="Diagnostics">The diagnostics.</param>
/// <param name="Healthy">Whether every check is <c>ok</c> or <c>warn</c>.</param>
/// <param name="Checks">The ordered check results.</param>
/// <param name="Environment">The environment projection.</param>
internal sealed record DoctorReport(
    int SchemaVersion,
    string Kind,
    bool Ok,
    int ExitCode,
    ReportSummary Summary,
    IReadOnlyList<ReportDiagnostic> Diagnostics,
    bool Healthy,
    IReadOnlyList<DoctorReportCheck> Checks,
    DoctorReportEnvironment? Environment);

/// <summary>
/// One check result in the doctor report.
/// </summary>
/// <param name="Name">The stable check name.</param>
/// <param name="Status">The lower-case status: <c>ok</c>, <c>warn</c>, or <c>fail</c>.</param>
/// <param name="Message">The short result.</param>
/// <param name="Detail">Optional detail.</param>
internal sealed record DoctorReportCheck(
    string Name,
    string Status,
    string Message,
    string? Detail);

/// <summary>
/// The environment projection in the doctor report.
/// </summary>
/// <param name="Framework">The runtime description.</param>
/// <param name="Os">The operating-system description.</param>
/// <param name="Architecture">The process architecture.</param>
/// <param name="SqliteVersion">The SQLite library version.</param>
/// <param name="CatalogVersion">The embedded catalog version.</param>
/// <param name="CatalogSha256">The embedded catalog SHA-256.</param>
/// <param name="SolverPath">The solver path, or <see langword="null"/>.</param>
/// <param name="SolverVersion">The solver version, or <see langword="null"/>.</param>
internal sealed record DoctorReportEnvironment(
    string? Framework,
    string? Os,
    string? Architecture,
    string? SqliteVersion,
    string? CatalogVersion,
    string? CatalogSha256,
    string? SolverPath,
    string? SolverVersion);

/// <summary>
/// Writes the <c>doctor-v1</c> report in its JSON and human forms.
/// </summary>
internal static class DoctorReportWriter
{
    /// <summary>
    /// Writes the report as exactly one camelCase JSON object.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteJson(TextWriter writer, DoctorReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        JsonReportWriter.Write(writer, report);
    }

    /// <summary>
    /// Writes the human diagnosis table and environment.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteHuman(TextWriter writer, DoctorReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        writer.WriteLine("CHECK                 STATUS  MESSAGE");
        foreach (var check in report.Checks)
        {
            var detail = string.IsNullOrWhiteSpace(check.Detail) ? string.Empty : $" ({check.Detail})";
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{check.Name,-20}  {check.Status,-6}  {check.Message}{detail}"));
        }

        if (report.Environment is { } environment)
        {
            writer.WriteLine();
            writer.WriteLine("Environment:");
            writer.WriteLine($"  framework: {Format(environment.Framework)}");
            writer.WriteLine($"  os: {Format(environment.Os)}");
            writer.WriteLine($"  architecture: {Format(environment.Architecture)}");
            writer.WriteLine($"  sqlite: {Format(environment.SqliteVersion)}");
            writer.WriteLine($"  catalog: {Format(environment.CatalogVersion)} ({Format(environment.CatalogSha256)})");
            writer.WriteLine($"  solver: {Format(environment.SolverPath)} {Format(environment.SolverVersion)}");
        }

        writer.WriteLine();
        writer.WriteLine(
            $"Summary: {report.Summary.Errors} error(s), {report.Summary.Warnings} warning(s), " +
            $"{report.Summary.Infos} info(s)");
        writer.WriteLine($"Result: {(report.Healthy ? "healthy" : "unhealthy")} (exit {report.ExitCode})");
    }

    /// <summary>
    /// Creates the report of a usage error before any check.
    /// </summary>
    /// <param name="diagnostics">The usage diagnostics.</param>
    /// <returns>The schema-valid usage report.</returns>
    public static DoctorReport UsageError(IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(diagnostics);
        return new DoctorReport(
            ContractVersions.DoctorReportVersion,
            "doctor",
            Ok: false,
            CliExitCode.Usage,
            ReportEnvelope.Count(reportDiagnostics),
            reportDiagnostics,
            Healthy: false,
            Checks: [],
            Environment: null);
    }

    private static string Format(string? value) => string.IsNullOrEmpty(value) ? "-" : value;
}
