// SPDX-License-Identifier: MIT

using Mocassin.Cli.CommandLine;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Contracts;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Cli.NewProject;

/// <summary>
/// The <c>new-project-v1</c> JSON report emitted by <c>mocassin new</c>.
/// </summary>
/// <param name="SchemaVersion">The report schema version.</param>
/// <param name="Kind">The report kind.</param>
/// <param name="Ok">Whether the exit code is zero.</param>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="Summary">The diagnostic counts.</param>
/// <param name="Diagnostics">The diagnostics.</param>
/// <param name="ProjectPath">The created project path, or <see langword="null"/>.</param>
/// <param name="ProjectId">The fresh project identity, or <see langword="null"/>.</param>
/// <param name="Template">The template name, or <see langword="null"/> on a usage error.</param>
internal sealed record NewProjectReport(
    int SchemaVersion,
    string Kind,
    bool Ok,
    int ExitCode,
    ReportSummary Summary,
    IReadOnlyList<ReportDiagnostic> Diagnostics,
    string? ProjectPath,
    Guid? ProjectId,
    string? Template);

/// <summary>
/// Writes the <c>new-project-v1</c> report in its JSON and human forms.
/// </summary>
internal static class NewProjectReportWriter
{
    /// <summary>
    /// Writes the report as exactly one camelCase JSON object.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteJson(TextWriter writer, NewProjectReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        JsonReportWriter.Write(writer, report);
    }

    /// <summary>
    /// Writes the human result.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteHuman(TextWriter writer, NewProjectReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        if (report.Ok)
        {
            writer.WriteLine($"Created project: {report.ProjectPath}");
            writer.WriteLine($"Template: {report.Template}");
            writer.WriteLine($"Project: {report.ProjectId?.ToString("D")}");
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
    public static NewProjectReport UsageError(IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(diagnostics);
        return new NewProjectReport(
            ContractVersions.NewProjectReportVersion,
            "newProject",
            Ok: false,
            CliExitCode.Usage,
            ReportEnvelope.Count(reportDiagnostics),
            reportDiagnostics,
            null,
            null,
            null);
    }
}
