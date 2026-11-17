// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Serialization;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Contracts;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.Cli.Validation;

/// <summary>
/// Builds and writes the human and <c>validation-v1</c> report forms.
/// </summary>
internal static class ValidationReportWriter
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// Builds a report for a completed validation run.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <returns>The report and its exit code.</returns>
    public static (ValidationReport Report, int ExitCode) FromResult(ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var exitCode = GetExitCode(result);
        var diagnostics = CliDiagnosticFactory.ToReportDiagnostics(result.Diagnostics);
        var summary = new ReportSummary(
            result.Summary.Errors,
            result.Summary.Warnings,
            result.Summary.Infos);

        var report = new ValidationReport(
            ContractVersions.ValidationReportVersion,
            "validation",
            result.ProjectId,
            result.ModelHash,
            result.DocumentHash,
            result.IsValid,
            exitCode,
            summary,
            diagnostics,
            result.Statistics is null ? null : ToReportStatistics(result.Statistics));
        return (report, exitCode);
    }

    /// <summary>
    /// Builds the report emitted for a usage error, including the CLI-owned <c>CLI001</c> diagnostics.
    /// </summary>
    /// <param name="diagnostics">The usage diagnostics.</param>
    /// <returns>The usage report and exit code <c>2</c>.</returns>
    public static ValidationReport UsageError(IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(diagnostics);
        return new ValidationReport(
            ContractVersions.ValidationReportVersion,
            "validation",
            null,
            null,
            null,
            false,
            2,
            ReportEnvelope.Count(reportDiagnostics),
            reportDiagnostics,
            null);
    }

    /// <summary>
    /// Builds the report emitted for an unclassified I/O failure.
    /// </summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <returns>The report and exit code <c>3</c>.</returns>
    public static (ValidationReport Report, int ExitCode) IoError(string message) =>
        (
            new ValidationReport(
                ContractVersions.ValidationReportVersion,
                "validation",
                null,
                null,
                null,
                false,
                3,
                new ReportSummary(1, 0, 0),
                [new ReportDiagnostic(
                    "PROJ002",
                    "error",
                    message,
                    null,
                    null,
                    [])],
                null),
            3);

    /// <summary>
    /// Writes the JSON report as exactly one object.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteJson(TextWriter writer, ValidationReport report) =>
        writer.WriteLine(JsonSerializer.Serialize(report, jsonOptions));

    /// <summary>
    /// Writes the stable human-readable table and summary.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="result">The validation result.</param>
    /// <param name="exitCode">The exit code.</param>
    public static void WriteHuman(TextWriter writer, ValidationResult result, int exitCode)
    {
        ArgumentNullException.ThrowIfNull(result);
        writer.WriteLine("SEVERITY  CODE    MESSAGE");
        foreach (var diagnostic in result.Diagnostics)
        {
            var path = diagnostic.Path is null ? string.Empty : $" [{diagnostic.Path}]";
            var hint = string.IsNullOrWhiteSpace(diagnostic.Hint) ? string.Empty : $" Hint: {diagnostic.Hint}";
            writer.WriteLine(
                $"{CliDiagnosticFactory.SeverityName(diagnostic.Severity).ToUpperInvariant(),-8}  {diagnostic.Code,-6}  {diagnostic.Message}{path}{hint}");
        }

        writer.WriteLine();
        writer.WriteLine(
            $"Summary: {result.Summary.Errors} error(s), {result.Summary.Warnings} warning(s), {result.Summary.Infos} info(s)");
        if (result.Statistics is not null)
        {
            WriteStatistics(writer, result.Statistics);
        }

        writer.WriteLine($"Result: {(result.IsValid ? "valid" : "invalid")} (exit {exitCode})");
    }

    private static void WriteStatistics(TextWriter writer, ModelStatistics statistics)
    {
        writer.WriteLine();
        writer.WriteLine("Statistic                          Value");
        writer.WriteLine($"Particles                          {statistics.ParticleCount}");
        writer.WriteLine($"Particle sets                      {statistics.ParticleSetCount}");
        writer.WriteLine($"Reference sites                    {statistics.ReferenceSiteCount}");
        writer.WriteLine($"P1 positions                       {statistics.P1PositionCount}");
        writer.WriteLine($"Stable positions                   {statistics.StablePositionCount}");
        writer.WriteLine($"Unstable positions                 {statistics.UnstablePositionCount}");
        writer.WriteLine($"Kinetic transitions                {statistics.KineticTransitionCount}");
        writer.WriteLine($"Metropolis transitions             {statistics.MetropolisTransitionCount}");
        writer.WriteLine($"Clusters                           {statistics.ClusterCount}");
        writer.WriteLine($"Estimated pair interactions        {statistics.EstimatedPairInteractionCount}");
        writer.WriteLine($"Estimated cluster permutations      {statistics.EstimatedUniqueClusterPermutations}");
        writer.WriteLine($"Estimated rules                    {statistics.EstimatedRuleCount}");
        writer.WriteLine($"Estimated memory (bytes)           {statistics.EstimatedMemoryBytes}");
        writer.WriteLine($"Cell counts                        {statistics.CellCountA}x{statistics.CellCountB}x{statistics.CellCountC}");
        writer.WriteLine($"Estimated positions                {statistics.EstimatedPositions}");
    }

    private static int GetExitCode(ValidationResult result) =>
        result.LoadFailure switch
        {
            ProjectLoadFailureKind.NotFound or ProjectLoadFailureKind.TooLarge or ProjectLoadFailureKind.Io => 3,
            ProjectLoadFailureKind.InvalidContent => 1,
            _ => result.IsValid ? 0 : 1,
        };

    private static ValidationReportStatistics ToReportStatistics(ModelStatistics statistics) =>
        new(
            statistics.ParticleCount,
            statistics.ParticleSetCount,
            statistics.ReferenceSiteCount,
            statistics.P1PositionCount,
            statistics.StablePositionCount,
            statistics.UnstablePositionCount,
            statistics.KineticTransitionCount,
            statistics.MetropolisTransitionCount,
            statistics.ClusterCount,
            statistics.EstimatedPairInteractionCount,
            statistics.EstimatedUniqueClusterPermutations,
            statistics.EstimatedRuleCount,
            statistics.EstimatedMemoryBytes,
            statistics.CellCountA,
            statistics.CellCountB,
            statistics.CellCountC,
            statistics.EstimatedPositions);
}
