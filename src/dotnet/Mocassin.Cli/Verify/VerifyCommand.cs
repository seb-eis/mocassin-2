// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Cli.Inspect;
using Mocassin.Contracts;
using Mocassin.Core.Diagnostics;
using Mocassin.Results.Packages;

namespace Mocassin.Cli.Verify;

/// <summary>
/// Implements <c>mocassin verify</c> as a thin read-only wrapper over <see cref="IPackageVerifier"/>.
/// </summary>
/// <remarks>
/// The <c>PKG</c> diagnostics and ordered checks are preserved in the report. A missing, directory, or
/// unreadable path is <c>CLI002</c>/3; an invalid package is exit 1. The command never mutates the package or
/// writes a results store.
/// </remarks>
internal sealed class VerifyCommand
{
    private readonly IPackageVerifier verifier;

    /// <summary>
    /// Initializes the production command.
    /// </summary>
    public VerifyCommand()
        : this(new PackageVerifier())
    {
    }

    /// <summary>
    /// Initializes the command with an explicit verifier for tests.
    /// </summary>
    /// <param name="verifier">The read-only verifier.</param>
    internal VerifyCommand(IPackageVerifier verifier)
    {
        ArgumentNullException.ThrowIfNull(verifier);
        this.verifier = verifier;
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
        VerifyRequest request,
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
            return Finish(request, stdout, stderr, Failure(CliExitCode.Io, [input]));
        }

        try
        {
            var verification = await verifier.VerifyAsync(
                    request.PackagePath,
                    new PackageVerificationOptions(),
                    cancellationToken)
                .ConfigureAwait(false);
            var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(verification.Diagnostics);
            var exitCode = verification.IsValid ? CliExitCode.Success : CliExitCode.Invalid;
            var report = new VerifyReport(
                ContractVersions.VerifyReportVersion,
                "verify",
                exitCode == 0,
                exitCode,
                ReportEnvelope.Count(reportDiagnostics),
                reportDiagnostics,
                verification.IsValid,
                verification.Checks
                    .Select(static check => new VerifyReportCheck(check.Code, check.Passed, check.Message))
                    .ToArray(),
                PackageReportMapper.Map(verification.Inspection));
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
                        $"The package '{request.PackagePath}' cannot be verified as a package: {exception.Message}")]));
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
        VerifyRequest request,
        TextWriter stdout,
        TextWriter stderr,
        VerifyReport report)
    {
        if (request.Json)
        {
            VerifyReportWriter.WriteJson(stdout, report);
        }
        else
        {
            foreach (var diagnostic in report.Diagnostics)
            {
                stderr.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
            }

            VerifyReportWriter.WriteHuman(stdout, report, request.PackagePath);
        }

        return report.ExitCode;
    }

    private static VerifyReport Failure(int exitCode, ImmutableArray<Diagnostic> diagnostics)
    {
        var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(diagnostics);
        return new VerifyReport(
            ContractVersions.VerifyReportVersion,
            "verify",
            Ok: false,
            exitCode,
            ReportEnvelope.Count(reportDiagnostics),
            reportDiagnostics,
            Valid: false,
            Checks: [],
            Package: null);
    }
}
