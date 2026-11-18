// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Contracts;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Build;

namespace Mocassin.Cli.Build;

/// <summary>
/// Creates the <c>build-v1</c> reports for completed, failed, and usage-error outcomes.
/// </summary>
internal static class BuildReportFactory
{
    /// <summary>
    /// Creates the report of one command outcome.
    /// </summary>
    /// <param name="outcome">The command outcome.</param>
    /// <param name="exitCode">The mapped exit code.</param>
    /// <param name="cliDiagnostics">The CLI-owned diagnostics.</param>
    /// <param name="verification">The verification result name.</param>
    /// <returns>The report.</returns>
    public static BuildReport Create(
        BuildOutcome outcome,
        int exitCode,
        ImmutableArray<Diagnostic> cliDiagnostics,
        string verification)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        var result = outcome.Result;
        var diagnostics = CliDiagnosticFactory.ToReportDiagnostics(
            result.Diagnostics
                .Concat(outcome.VerificationDiagnostics)
                .Concat(cliDiagnostics));
        var timings = result.Timings
            .Select(static timing => new BuildReportTiming(
                BuildEventWriter.StageName(timing.Stage),
                timing.Elapsed.TotalMilliseconds))
            .ToList();
        if (outcome.Request.Preview && result.Preview is not null)
        {
            timings.Add(new BuildReportTiming("preview", result.Preview.Elapsed.TotalMilliseconds));
        }

        if (!outcome.Request.Preview && !string.Equals(verification, "skipped", StringComparison.Ordinal))
        {
            timings.Add(new BuildReportTiming("verify", outcome.VerifyElapsed.TotalMilliseconds));
        }

        return new BuildReport(
            ContractVersions.BuildReportVersion,
            "build",
            exitCode == 0,
            exitCode,
            ReportEnvelope.Count(diagnostics),
            diagnostics,
            outcome.Request.Preview ? "preview" : "build",
            result.Identity.ProjectId,
            result.Identity.ModelHash,
            result.Identity.DocumentHash,
            result.Identity.ModelSignature,
            result.Identity.ParameterizationSignature,
            MapSelection(result.Selection, outcome.Request),
            MapJobSelection(result.JobSelection),
            timings,
            MapPackage(result.Package),
            result.Preview is null ? null : BuildPreviewMapper.Map(result.Preview),
            verification);
    }

    /// <summary>
    /// Creates the report of a usage error before any build work.
    /// </summary>
    /// <param name="preview">Whether the preview mode was requested.</param>
    /// <param name="diagnostics">The usage diagnostics.</param>
    /// <returns>The schema-valid usage report.</returns>
    public static BuildReport UsageError(bool preview, IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(diagnostics);
        return new BuildReport(
            ContractVersions.BuildReportVersion,
            "build",
            Ok: false,
            CliExitCode.Usage,
            ReportEnvelope.Count(reportDiagnostics),
            reportDiagnostics,
            preview ? "preview" : "build",
            null,
            null,
            null,
            null,
            null,
            null,
            new BuildReportJobSelection(null, 0, 0),
            [],
            null,
            null,
            "skipped");
    }

    private static BuildReportSelection? MapSelection(BuildSelection? selection, BuildCommandRequest request)
    {
        if (selection is null)
        {
            return null;
        }

        var buildTemplate = selection.BuildTemplate is null
            ? null
            : new BuildReportTemplate(
                selection.BuildTemplate.Id.Value.ToString("D"),
                selection.BuildTemplate.Name);
        var parameterization = selection.Parameterization is null
            ? new BuildReportParameterization(null, null, selection.ImplicitDefaultParameterization)
            : new BuildReportParameterization(
                selection.Parameterization.Id.Value.ToString("D"),
                selection.Parameterization.Name,
                ImplicitDefault: false);
        return new BuildReportSelection(
            buildTemplate,
            new BuildReportTemplate(
                selection.JobTemplate.Id.Value.ToString("D"),
                selection.JobTemplate.Name),
            parameterization,
            request.OutputPath is null ? selection.OutputName : null);
    }

    private static BuildReportJobSelection? MapJobSelection(JobSelectionResult? selection) =>
        selection is null
            ? null
            : new BuildReportJobSelection(
                selection.IsValid ? selection.Expression : null,
                selection.ExpandedCount,
                selection.SelectedCount);

    private static BuildReportPackage? MapPackage(Mocassin.Packages.SimulationPackage.PackageWriteResult? package) =>
        package is null
            ? null
            : new BuildReportPackage(
                package.Path,
                package.Sha256,
                package.SizeBytes,
                package.JobCount,
                package.ModelCount,
                package.LatticeCount,
                package.LatticeReuseFactor,
                Published: true);
}
