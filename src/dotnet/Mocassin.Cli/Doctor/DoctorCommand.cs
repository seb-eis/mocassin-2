// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Cli.Doctor.Checks;
using Mocassin.Contracts;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Cli.Doctor;

/// <summary>
/// Implements <c>mocassin doctor</c> with the seven checks and the <see cref="IDoctorCheck"/> seam.
/// </summary>
/// <remarks>
/// A check that fails makes the environment unhealthy (exit 1); warnings do not. The solver is optional until
/// Step 19, so a missing solver is a warning. The runtime, SQLite, permission, disk, catalog, and template
/// checks run for real in the healthy CI path; tests replace the check list to exercise outcomes without
/// spoofing the operating system.
/// </remarks>
internal sealed class DoctorCommand
{
    private readonly IReadOnlyList<IDoctorCheck> checks;
    private readonly ISolverProbe solverProbe;

    /// <summary>
    /// Initializes the production command.
    /// </summary>
    public DoctorCommand()
        : this(null, null)
    {
    }

    /// <summary>
    /// Initializes the command with an explicit check list and solver probe for tests.
    /// </summary>
    /// <param name="checks">The checks, or <see langword="null"/> for the seven production checks.</param>
    /// <param name="solverProbe">The solver probe, or <see langword="null"/> for the process probe.</param>
    internal DoctorCommand(IEnumerable<IDoctorCheck>? checks, ISolverProbe? solverProbe = null)
    {
        this.solverProbe = solverProbe ?? new ProcessSolverProbe();
        this.checks = checks?.ToArray() ?? CreateDefaultChecks(this.solverProbe);
    }

    /// <summary>
    /// Runs the command.
    /// </summary>
    /// <param name="request">The typed request.</param>
    /// <param name="stdout">The standard-output writer.</param>
    /// <param name="stderr">The standard-error writer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The process exit code.</returns>
    public async Task<int> RunAsync(
        DoctorRequest request,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        var results = new List<DoctorCheckResult>(checks.Count);
        try
        {
            foreach (var check in checks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    results.Add(await check.RunAsync(cancellationToken).ConfigureAwait(false));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
#pragma warning disable CA1031 // A throwing check is reported as a failing check, not a command crash.
                catch (Exception exception)
#pragma warning restore CA1031
                {
                    results.Add(new DoctorCheckResult(
                        check.Name,
                        DoctorCheckStatus.Fail,
                        $"The check threw {exception.GetType().Name}: {exception.Message}",
                        exception.GetType().Name));
                }
            }

            var environment = await BuildEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            var healthy = results.All(static result => result.Status != DoctorCheckStatus.Fail);
            var report = new DoctorReport(
                ContractVersions.DoctorReportVersion,
                "doctor",
                Ok: healthy,
                healthy ? CliExitCode.Success : CliExitCode.Invalid,
                new ReportSummary(0, 0, 0),
                [],
                healthy,
                results.Select(static result => new DoctorReportCheck(
                        result.Name,
                        StatusName(result.Status),
                        result.Message,
                        result.Detail))
                    .ToArray(),
                environment);
            return Finish(request, stdout, stderr, report);
        }
        catch (OperationCanceledException)
        {
            var diagnostics = CliDiagnosticFactory.ToReportDiagnostics(
                [CliDiagnosticFactory.Cancelled("The command was cancelled.")]);
            return Finish(
                request,
                stdout,
                stderr,
                new DoctorReport(
                    ContractVersions.DoctorReportVersion,
                    "doctor",
                    Ok: false,
                    CliExitCode.Cancelled,
                    ReportEnvelope.Count(diagnostics),
                    diagnostics,
                    Healthy: false,
                    Checks: [],
                    Environment: null));
        }
    }

    private static int Finish(
        DoctorRequest request,
        TextWriter stdout,
        TextWriter stderr,
        DoctorReport report)
    {
        if (request.Json)
        {
            DoctorReportWriter.WriteJson(stdout, report);
        }
        else
        {
            foreach (var diagnostic in report.Diagnostics)
            {
                stderr.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
            }

            DoctorReportWriter.WriteHuman(stdout, report);
        }

        return report.ExitCode;
    }

    private async Task<DoctorReportEnvironment> BuildEnvironmentAsync(CancellationToken cancellationToken)
    {
        var framework = RuntimeInformation.FrameworkDescription;
        var os = RuntimeInformation.OSDescription;
        var architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        string? sqliteVersion = null;
        string? catalogVersion = null;
        string? catalogSha256 = null;
        string? solverPath = null;
        string? solverVersion = null;
        try
        {
            sqliteVersion = SqliteCheck.ReadVersion();
        }
#pragma warning disable CA1031 // The environment field remains null; the check reports the failure.
        catch (Exception)
#pragma warning restore CA1031
        {
        }

        try
        {
            var catalog = SpaceGroupCatalog.Default;
            catalogVersion = catalog.DataVersion;
            catalogSha256 = catalog.DataSha256;
        }
#pragma warning disable CA1031 // The environment field remains null; the check reports the failure.
        catch (Exception)
#pragma warning restore CA1031
        {
        }

        try
        {
            var probe = await solverProbe.ProbeAsync(cancellationToken).ConfigureAwait(false);
            solverPath = probe.Path;
            solverVersion = probe.Version;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // The environment field remains null; the check reports the failure.
        catch (Exception)
#pragma warning restore CA1031
        {
        }

        return new DoctorReportEnvironment(
            framework,
            os,
            architecture,
            sqliteVersion,
            catalogVersion,
            catalogSha256,
            solverPath,
            solverVersion);
    }

    private static IReadOnlyList<IDoctorCheck> CreateDefaultChecks(ISolverProbe solverProbe) =>
    [
        new RuntimeCheck(),
        new SqliteCheck(),
        new WritePermissionCheck(),
        new DiskSpaceCheck(),
        new SymmetryCatalogCheck(),
        new ProjectTemplateCheck(),
        new SolverProbeCheck(solverProbe),
    ];

    private static string StatusName(DoctorCheckStatus status) => status switch
    {
        DoctorCheckStatus.Ok => "ok",
        DoctorCheckStatus.Warn => "warn",
        _ => "fail",
    };
}
