// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Contracts;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Cli.Inspect;

/// <summary>
/// The <c>inspect-v1</c> JSON report emitted by <c>mocassin inspect</c>.
/// </summary>
/// <param name="SchemaVersion">The report schema version.</param>
/// <param name="Kind">The report kind.</param>
/// <param name="Ok">Whether the exit code is zero.</param>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="Summary">The diagnostic counts.</param>
/// <param name="Diagnostics">The diagnostics.</param>
/// <param name="Package">The inspection projection, or <see langword="null"/> on failure.</param>
internal sealed record InspectReport(
    int SchemaVersion,
    string Kind,
    bool Ok,
    int ExitCode,
    ReportSummary Summary,
    IReadOnlyList<ReportDiagnostic> Diagnostics,
    PackageReport? Package);

/// <summary>
/// Writes the <c>inspect-v1</c> report in its JSON and human forms.
/// </summary>
internal static class InspectReportWriter
{
    /// <summary>
    /// Writes the report as exactly one camelCase JSON object.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteJson(TextWriter writer, InspectReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        JsonReportWriter.Write(writer, report);
    }

    /// <summary>
    /// Writes the human inspection summary.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    /// <param name="path">The inspected path.</param>
    public static void WriteHuman(TextWriter writer, InspectReport report, string path)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(path);
        writer.WriteLine($"Package: {path}");
        if (report.Package is { } package)
        {
            writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Format version: {package.FormatVersion}"));
            writer.WriteLine($"App version: {package.AppVersion}");
            writer.WriteLine($"Created: {package.CreatedUtc}");
            writer.WriteLine($"Project: {package.ProjectId}");
            writer.WriteLine($"Generator: {package.Generator}");
            writer.WriteLine($"Job selection: {package.JobSelection}");
            writer.WriteLine($"Symmetry contract: {package.SymmetryContractVersion}");
            writer.WriteLine($"Space group data: {package.SpaceGroupDataVersion} (sha256 {package.SpaceGroupDataSha256})");
            writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Models: {package.Models.Count}"));
            foreach (var model in package.Models)
            {
                writer.WriteLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"  {model.Signature} model={model.ModelSignature} payload=v{model.PayloadFormatVersion} " +
                    $"bytes={model.PayloadBytes}"));
            }

            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"Jobs: {package.JobCount}, lattices: {package.LatticeCount}, " +
                $"reuse: {package.LatticeReuseFactor.ToString("0.###", CultureInfo.InvariantCulture)}"));
            writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Collections: {package.Collections.Count}"));
            foreach (var collection in package.Collections)
            {
                writer.WriteLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"  {collection.Kind} / {collection.CollectionName} / {collection.ConfigName}: " +
                    $"{collection.JobCount} job(s)"));
            }

            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"Temperature: {package.MinTemperatureK.ToString("0.###", CultureInfo.InvariantCulture)}-" +
                $"{package.MaxTemperatureK.ToString("0.###", CultureInfo.InvariantCulture)} K"));
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"Electric field: {package.MinElectricFieldVm.ToString("0.###", CultureInfo.InvariantCulture)}-" +
                $"{package.MaxElectricFieldVm.ToString("0.###", CultureInfo.InvariantCulture)} V/m"));
            writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Size: {package.SizeBytes} bytes"));
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
    public static InspectReport UsageError(IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(diagnostics);
        return new InspectReport(
            ContractVersions.InspectReportVersion,
            "inspect",
            Ok: false,
            CliExitCode.Usage,
            ReportEnvelope.Count(reportDiagnostics),
            reportDiagnostics,
            null);
    }
}
