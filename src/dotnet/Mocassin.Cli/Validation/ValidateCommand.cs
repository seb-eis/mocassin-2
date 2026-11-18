// SPDX-License-Identifier: MIT

using Mocassin.Cli.CommandLine;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.Cli.Validation;

/// <summary>
/// Implements the <c>mocassin validate</c> command.
/// </summary>
/// <remarks>
/// The command graph owns parsing, arity, and usage errors. This handler runs the Step 07 validation pipeline
/// and keeps the <c>validation-v1</c> report and the 0/1/2/3 exit-code contract unchanged.
/// </remarks>
internal static class ValidateCommand
{
    /// <summary>
    /// Runs the command.
    /// </summary>
    /// <param name="request">The typed request.</param>
    /// <param name="stdout">The standard-output writer.</param>
    /// <param name="stderr">The standard-error writer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(
        ValidateRequest request,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        var options = new ValidationOptions
        {
            IncludeStatistics = request.Statistics,
            WarningsAsErrors = request.WarningsAsErrors,
        };

        ValidationResult result;
        try
        {
            result = await new ProjectValidationPipeline()
                .ValidateFileAsync(
                    request.ProjectPath,
                    options: options,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or NotSupportedException
                                              or System.Security.SecurityException)
        {
            var (ioReport, ioExitCode) = ValidationReportWriter.IoError(
                $"Could not read project '{request.ProjectPath}': {exception.Message}");
            if (request.Json)
            {
                ValidationReportWriter.WriteJson(stdout, ioReport);
            }
            else
            {
                stderr.WriteLine(ioReport.Diagnostics[0].Message);
            }

            return ioExitCode;
        }

        var (report, exitCode) = ValidationReportWriter.FromResult(result);
        if (request.Json)
        {
            ValidationReportWriter.WriteJson(stdout, report);
        }
        else
        {
            ValidationReportWriter.WriteHuman(stdout, result, exitCode);
        }

        return exitCode;
    }
}
