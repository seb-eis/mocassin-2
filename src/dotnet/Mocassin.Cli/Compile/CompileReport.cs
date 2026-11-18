// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Contracts;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Cli.Compile;

/// <summary>
/// The <c>compile-v1</c> JSON report emitted by <c>mocassin compile</c>.
/// </summary>
/// <param name="SchemaVersion">The report schema version.</param>
/// <param name="Kind">The report kind.</param>
/// <param name="Ok">Whether the exit code is zero.</param>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="Summary">The diagnostic counts.</param>
/// <param name="Diagnostics">The diagnostics.</param>
/// <param name="ProjectId">The project identity, or <see langword="null"/>.</param>
/// <param name="ModelHash">The canonical model hash, or <see langword="null"/>.</param>
/// <param name="DocumentHash">The canonical document hash, or <see langword="null"/>.</param>
/// <param name="ModelSignature">The compiled-model signature, or <see langword="null"/>.</param>
/// <param name="CompilerVersion">The compiler identity, or <see langword="null"/> before compilation.</param>
/// <param name="OutputPath">The written inspection path, or <see langword="null"/>.</param>
/// <param name="OutputSha256">The written inspection SHA-256, or <see langword="null"/>.</param>
/// <param name="Counts">The compiled-content counts, or <see langword="null"/> before compilation.</param>
/// <param name="Cache">The stage-cache statistics, or <see langword="null"/> before compilation.</param>
/// <param name="Stages">The ordered compilation stages.</param>
internal sealed record CompileReport(
    int SchemaVersion,
    string Kind,
    bool Ok,
    int ExitCode,
    ReportSummary Summary,
    IReadOnlyList<ReportDiagnostic> Diagnostics,
    Guid? ProjectId,
    string? ModelHash,
    string? DocumentHash,
    string? ModelSignature,
    string? CompilerVersion,
    string? OutputPath,
    string? OutputSha256,
    CompileReportCounts? Counts,
    CompileReportCache? Cache,
    IReadOnlyList<CompileReportStage> Stages);

/// <summary>
/// The compiled-content counts in the compile report.
/// </summary>
/// <param name="ParticleCount">The compiled particle count.</param>
/// <param name="PositionCount">The canonical P1 position count.</param>
/// <param name="SiteCount">The reference-site count.</param>
/// <param name="EnvironmentCount">The interaction-environment count.</param>
/// <param name="PairTableCount">The pair-table count.</param>
/// <param name="ClusterTableCount">The cluster-table count.</param>
/// <param name="DirectionCount">The compiled direction count.</param>
/// <param name="RuleCount">The compiled rule count.</param>
/// <param name="SimulationCount">The compiled simulation count.</param>
internal sealed record CompileReportCounts(
    int ParticleCount,
    int PositionCount,
    int SiteCount,
    int EnvironmentCount,
    int PairTableCount,
    int ClusterTableCount,
    int DirectionCount,
    int RuleCount,
    int SimulationCount);

/// <summary>
/// The stage-cache statistics in the compile report.
/// </summary>
/// <param name="Hits">The cache hits.</param>
/// <param name="Misses">The cache misses.</param>
/// <param name="Capacity">The configured capacity.</param>
internal sealed record CompileReportCache(int Hits, int Misses, int Capacity);

/// <summary>
/// One compilation stage timing in the compile report.
/// </summary>
/// <param name="Name">The lower-case stage name.</param>
/// <param name="Ms">The elapsed milliseconds.</param>
internal sealed record CompileReportStage(string Name, double Ms);

/// <summary>
/// Writes the <c>compile-v1</c> report in its JSON and human forms.
/// </summary>
internal static class CompileReportWriter
{
    /// <summary>
    /// Writes the report as exactly one camelCase JSON object.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteJson(TextWriter writer, CompileReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        JsonReportWriter.Write(writer, report);
    }

    /// <summary>
    /// Writes the human summary.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteHuman(TextWriter writer, CompileReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        writer.WriteLine($"Project: {Format(report.ProjectId?.ToString("D"))}");
        writer.WriteLine($"Model hash: {Format(report.ModelHash)}");
        writer.WriteLine($"Model signature: {Format(report.ModelSignature)}");
        writer.WriteLine($"Compiler: {Format(report.CompilerVersion)}");
        if (report.Counts is not null)
        {
            var counts = report.Counts;
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"Counts: particles {counts.ParticleCount}, positions {counts.PositionCount}, " +
                $"sites {counts.SiteCount}, environments {counts.EnvironmentCount}, " +
                $"pair tables {counts.PairTableCount}, cluster tables {counts.ClusterTableCount}, " +
                $"directions {counts.DirectionCount}, rules {counts.RuleCount}, " +
                $"simulations {counts.SimulationCount}"));
        }

        if (report.Cache is not null)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"Cache: {report.Cache.Hits} hit(s), {report.Cache.Misses} miss(es), " +
                $"capacity {report.Cache.Capacity}"));
        }

        if (report.Stages.Count > 0)
        {
            writer.WriteLine(
                "Stages: " + string.Join(
                    ", ",
                    report.Stages.Select(static stage => string.Create(
                        CultureInfo.InvariantCulture,
                        $"{stage.Name} {stage.Ms.ToString("0.###", CultureInfo.InvariantCulture)} ms"))));
        }

        if (report.OutputPath is not null)
        {
            writer.WriteLine($"Output: {report.OutputPath} (sha256 {report.OutputSha256})");
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
    public static CompileReport UsageError(IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(diagnostics);
        return new CompileReport(
            ContractVersions.CompileReportVersion,
            "compile",
            Ok: false,
            CliExitCode.Usage,
            ReportEnvelope.Count(reportDiagnostics),
            reportDiagnostics,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            []);
    }

    private static string Format(string? value) => string.IsNullOrEmpty(value) ? "-" : value;
}
