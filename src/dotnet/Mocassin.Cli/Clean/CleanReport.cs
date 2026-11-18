// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Contracts;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Cli.Clean;

/// <summary>
/// The <c>clean-v1</c> JSON report emitted by <c>mocassin clean</c>.
/// </summary>
/// <param name="SchemaVersion">The report schema version.</param>
/// <param name="Kind">The report kind.</param>
/// <param name="Ok">Whether the exit code is zero.</param>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="Summary">The diagnostic counts.</param>
/// <param name="Diagnostics">The diagnostics.</param>
/// <param name="DryRun">Whether the requested mode was a dry run.</param>
/// <param name="PackagePath">The package path, or <see langword="null"/>.</param>
/// <param name="Directory">The listed directory, or <see langword="null"/>.</param>
/// <param name="JobSelection">The package's recorded job selection, or <see langword="null"/>.</param>
/// <param name="JobCount">The package's job count.</param>
/// <param name="Folders">The listed job folders.</param>
/// <param name="TotalBytes">The listed folders' total size.</param>
/// <param name="OlderThanDays">The applied age filter, or <see langword="null"/> for no filter.</param>
/// <param name="RemovedCount">The removed count; always zero in Step 12.</param>
internal sealed record CleanReport(
    int SchemaVersion,
    string Kind,
    bool Ok,
    int ExitCode,
    ReportSummary Summary,
    IReadOnlyList<ReportDiagnostic> Diagnostics,
    bool DryRun,
    string? PackagePath,
    string? Directory,
    string? JobSelection,
    int JobCount,
    IReadOnlyList<CleanReportFolder> Folders,
    long TotalBytes,
    int? OlderThanDays,
    int RemovedCount);

/// <summary>
/// One listed job folder in the clean report.
/// </summary>
/// <param name="Path">The folder path.</param>
/// <param name="JobId">The job identifier parsed from the folder name.</param>
/// <param name="LastWriteUtc">The folder's last-write timestamp.</param>
/// <param name="SizeBytes">The recursive folder size in bytes.</param>
internal sealed record CleanReportFolder(
    string? Path,
    int JobId,
    string? LastWriteUtc,
    long SizeBytes);

/// <summary>
/// Writes the <c>clean-v1</c> report in its JSON and human forms.
/// </summary>
internal static class CleanReportWriter
{
    /// <summary>
    /// Writes the report as exactly one camelCase JSON object.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteJson(TextWriter writer, CleanReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        JsonReportWriter.Write(writer, report);
    }

    /// <summary>
    /// Writes the human folder listing.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteHuman(TextWriter writer, CleanReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        writer.WriteLine($"Package: {report.PackagePath ?? "-"}");
        writer.WriteLine($"Directory: {report.Directory ?? "-"}");
        writer.WriteLine(
            $"Mode: {(report.DryRun ? "dry run" : "reserved")}, selection {report.JobSelection ?? "-"}, " +
            $"{report.JobCount} job(s)");
        foreach (var folder in report.Folders)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{System.IO.Path.GetFileName(folder.Path ?? string.Empty),-12} {folder.Path}  " +
                $"{folder.LastWriteUtc}  {folder.SizeBytes} bytes"));
        }

        writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Total: {report.TotalBytes} bytes"));
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
    public static CleanReport UsageError(IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(diagnostics);
        return new CleanReport(
            ContractVersions.CleanReportVersion,
            "clean",
            Ok: false,
            CliExitCode.Usage,
            ReportEnvelope.Count(reportDiagnostics),
            reportDiagnostics,
            DryRun: false,
            PackagePath: null,
            Directory: null,
            JobSelection: null,
            JobCount: 0,
            Folders: [],
            TotalBytes: 0,
            OlderThanDays: null,
            RemovedCount: 0);
    }
}
