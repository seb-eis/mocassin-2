// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.Diagnostics;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;
using Mocassin.Packages.Build;

namespace Mocassin.Cli.Build;

/// <summary>
/// The command outcome used by the single exit/failure mapping.
/// </summary>
/// <param name="Result">The authoritative pipeline result.</param>
/// <param name="Request">The typed command request.</param>
/// <param name="Project">The preflight project, or <see langword="null"/> when the load failed.</param>
/// <param name="TargetPath">The resolved target path, or <see langword="null"/> for a preview.</param>
/// <param name="OutputExists">Whether the target already existed without <c>--force</c>.</param>
/// <param name="VerificationFailed">Whether post-publication verification failed.</param>
/// <param name="VerifyElapsed">The verification wall time.</param>
/// <param name="VerificationDiagnostics">The verification diagnostics.</param>
internal sealed record BuildOutcome(
    BuildResult Result,
    BuildCommandRequest Request,
    Project? Project,
    string? TargetPath,
    bool OutputExists,
    bool VerificationFailed,
    TimeSpan VerifyElapsed,
    ImmutableArray<Diagnostic> VerificationDiagnostics);

/// <summary>
/// Maps a <see cref="BuildOutcome"/> to the documented exit code and composes the CLI-owned diagnostics.
/// </summary>
/// <remarks>
/// This is the only place that turns a build result into an exit code. The library never supplies one. Domain
/// diagnostics keep their codes; the CLI adds <c>CLI002</c>–<c>CLI007</c> only for failures that have no domain
/// diagnostic, per §11.1.
/// </remarks>
internal static class BuildExitMapper
{
    /// <summary>
    /// Maps an outcome.
    /// </summary>
    /// <param name="outcome">The command outcome.</param>
    /// <returns>The process exit code and the CLI-owned diagnostics.</returns>
    public static (int ExitCode, ImmutableArray<Diagnostic> CliDiagnostics) Map(BuildOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        var result = outcome.Result;
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        if (outcome.VerificationFailed)
        {
            if (!outcome.VerificationDiagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                diagnostics.Add(CliDiagnosticFactory.NotAPackage(
                    "The published package failed its post-publication verification."));
            }

            return (CliExitCode.Invalid, diagnostics.ToImmutable());
        }

        if (result.Status == BuildStatus.Cancelled)
        {
            diagnostics.Add(CliDiagnosticFactory.Cancelled("The build was cancelled."));
            return (CliExitCode.Cancelled, diagnostics.ToImmutable());
        }

        if (result.Status == BuildStatus.Succeeded)
        {
            return (CliExitCode.Success, diagnostics.ToImmutable());
        }

        var exitCode = result.Failure switch
        {
            BuildFailureKind.None => CliExitCode.Internal,
            BuildFailureKind.Io => CliExitCode.Io,
            BuildFailureKind.Load => CliExitCode.Invalid,
            BuildFailureKind.TemplateSelection =>
                result.Diagnostics.Any(static diagnostic => diagnostic.Code == "PROJ010")
                    ? CliExitCode.Invalid
                    : CliExitCode.Usage,
            BuildFailureKind.JobSelection => CliExitCode.Usage,
            BuildFailureKind.Validation
                or BuildFailureKind.JobExpansion
                or BuildFailureKind.LatticeGeneration
                or BuildFailureKind.Preview => CliExitCode.Invalid,
            BuildFailureKind.Compilation or BuildFailureKind.Parameterization => CliExitCode.Compilation,
            BuildFailureKind.PackageWrite =>
                result.Diagnostics.Any(static diagnostic =>
                    diagnostic.Code.StartsWith("PKG", StringComparison.Ordinal)
                    && diagnostic.Severity == DiagnosticSeverity.Error)
                    ? CliExitCode.Invalid
                    : CliExitCode.Internal,
            _ => CliExitCode.Internal,
        };

        switch (result.Failure)
        {
            case BuildFailureKind.None:
                diagnostics.Add(CliDiagnosticFactory.Internal(
                    "The build reported failure without a failure kind."));
                break;
            case BuildFailureKind.Io:
                if (outcome.OutputExists)
                {
                    diagnostics.Add(CliDiagnosticFactory.OutputExists(
                        $"The package '{outcome.TargetPath}' already exists. Pass --force to replace it."));
                }
                else if (!result.Diagnostics.Any(
                             static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
                {
                    diagnostics.Add(CliDiagnosticFactory.Io("The build failed with an I/O error."));
                }

                break;
            case BuildFailureKind.TemplateSelection:
                if (!result.Diagnostics.Any(static diagnostic => diagnostic.Code == "PROJ010"))
                {
                    diagnostics.Add(CliDiagnosticFactory.Selection(SelectionMessage(outcome)));
                }

                break;
            case BuildFailureKind.JobSelection:
                diagnostics.Add(CliDiagnosticFactory.JobSelection(
                    result.JobSelection?.Error ?? "The job selection is invalid."));
                break;
            case BuildFailureKind.PackageWrite:
                if (exitCode == CliExitCode.Internal)
                {
                    diagnostics.Add(CliDiagnosticFactory.Internal(
                        "The package writer failed without a package diagnostic."));
                }

                break;
            case BuildFailureKind.Internal:
                diagnostics.Add(CliDiagnosticFactory.Internal("An unexpected internal error occurred."));
                break;
        }

        return (exitCode, diagnostics.ToImmutable());
    }

    private static string SelectionMessage(BuildOutcome outcome)
    {
        var project = outcome.Project;
        var request = outcome.Request;
        if (project is null)
        {
            return "The build recipe selection is missing or ambiguous.";
        }

        var buildTemplates = project.BuildTemplates.IsDefault
            ? []
            : project.BuildTemplates.Where(static template => template is not null).ToArray();
        var jobTemplates = project.JobTemplates.IsDefault
            ? []
            : project.JobTemplates.Where(static template => template is not null).ToArray();
        var parameterizations = project.Parameterizations.IsDefault
            ? []
            : project.Parameterizations.Where(static template => template is not null).ToArray();

        if (request.BuildTemplateId is { } buildTemplateId
            && jobTemplates.Any(template => template.Id.Value == buildTemplateId))
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"The id '{buildTemplateId:D}' is a job template; use --job-template.");
        }

        if (request.BuildTemplateId is null && buildTemplates.Length > 1)
        {
            return "The project contains several build templates; select one with --template: "
                + Describe(buildTemplates.Select(static template => (template.Id.Value, template.Name)));
        }

        if (request.JobTemplateId is null && buildTemplates.Length == 0 && jobTemplates.Length > 1)
        {
            return "The project contains several job templates; select one with --job-template: "
                + Describe(jobTemplates.Select(static template => (template.Id.Value, template.Name)));
        }

        if (request.ParameterizationId is null && buildTemplates.Length == 0 && parameterizations.Length > 1)
        {
            return "The project contains several parameterization templates; select one with --parameterization: "
                + Describe(parameterizations.Select(static template => (template.Id.Value, template.Name)));
        }

        if (jobTemplates.Length == 0)
        {
            return "The project does not contain a job template; add one before building.";
        }

        return "The build recipe selection is missing or ambiguous.";
    }

    private static string Describe(IEnumerable<(Guid Id, string Name)> templates) =>
        string.Join(
            ", ",
            templates.Select(static template => string.Create(
                CultureInfo.InvariantCulture,
                $"'{template.Name}' ({template.Id:D})")));
}
