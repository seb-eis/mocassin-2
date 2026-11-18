// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Contracts;
using Mocassin.Core.Diagnostics;
using Mocassin.Results.Packages;

namespace Mocassin.Cli.Inspect;

/// <summary>
/// Implements <c>mocassin inspect</c> as a thin read-only wrapper over <see cref="IMslPackageInspector"/>.
/// </summary>
/// <remarks>
/// The command never opens the package writable and never creates <c>-wal</c>/<c>-shm</c> sidecars. A missing,
/// directory, or unreadable path is <c>CLI002</c>/3; an unreadable package structure is <c>CLI009</c>/1 with a
/// hint to run <c>verify</c>.
/// </remarks>
internal sealed class InspectCommand
{
    private readonly IMslPackageInspector inspector;

    /// <summary>
    /// Initializes the production command.
    /// </summary>
    public InspectCommand()
        : this(new MslPackageInspector())
    {
    }

    /// <summary>
    /// Initializes the command with an explicit inspector for tests.
    /// </summary>
    /// <param name="inspector">The read-only inspector.</param>
    internal InspectCommand(IMslPackageInspector inspector)
    {
        ArgumentNullException.ThrowIfNull(inspector);
        this.inspector = inspector;
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
        InspectRequest request,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        var input = PackageInput.CheckReadable(request.PackagePath);
        if (input is not null)
        {
            return Finish(
                request,
                stdout,
                stderr,
                Failure(CliExitCode.Io, [input]));
        }

        try
        {
            var inspection = await inspector.InspectAsync(request.PackagePath, cancellationToken)
                .ConfigureAwait(false);
            var report = new InspectReport(
                ContractVersions.InspectReportVersion,
                "inspect",
                Ok: true,
                CliExitCode.Success,
                new ReportSummary(0, 0, 0),
                [],
                PackageReportMapper.Map(inspection));
            return Finish(request, stdout, stderr, report);
        }
        catch (InvalidDataException exception)
        {
            return Finish(
                request,
                stdout,
                stderr,
                Failure(
                    CliExitCode.Invalid,
                    [CliDiagnosticFactory.NotAPackage(
                        $"The package '{request.PackagePath}' cannot be inspected as a package: {exception.Message}",
                        $"Run 'mocassin verify {request.PackagePath}' for package detail.")]));
        }
        catch (OperationCanceledException)
        {
            return Finish(
                request,
                stdout,
                stderr,
                Failure(CliExitCode.Cancelled, [CliDiagnosticFactory.Cancelled("The command was cancelled.")]));
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or NotSupportedException
                                              or System.Security.SecurityException)
        {
            return Finish(
                request,
                stdout,
                stderr,
                Failure(
                    CliExitCode.Io,
                    [CliDiagnosticFactory.Io(
                        $"The package '{request.PackagePath}' could not be read: {exception.Message}")]));
        }
#pragma warning disable CA1031 // The command boundary converts an unexpected failure into CLI007/exit 10.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            return Finish(
                request,
                stdout,
                stderr,
                Failure(
                    CliExitCode.Internal,
                    [CliDiagnosticFactory.Internal(
                        $"Unexpected internal error: {exception.GetType().Name}: {exception.Message}")]));
        }
    }

    private static int Finish(
        InspectRequest request,
        TextWriter stdout,
        TextWriter stderr,
        InspectReport report)
    {
        if (request.Json)
        {
            InspectReportWriter.WriteJson(stdout, report);
        }
        else
        {
            foreach (var diagnostic in report.Diagnostics)
            {
                stderr.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
            }

            InspectReportWriter.WriteHuman(stdout, report, request.PackagePath);
        }

        return report.ExitCode;
    }

    private static InspectReport Failure(int exitCode, ImmutableArray<Diagnostic> diagnostics)
    {
        var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(diagnostics);
        return new InspectReport(
            ContractVersions.InspectReportVersion,
            "inspect",
            Ok: false,
            exitCode,
            ReportEnvelope.Count(reportDiagnostics),
            reportDiagnostics,
            null);
    }
}
