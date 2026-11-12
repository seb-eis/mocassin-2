// SPDX-License-Identifier: MIT

using Mocassin.Packages.Projects.Validation;

namespace Mocassin.Cli.Validation;

/// <summary>
/// Implements the <c>mocassin validate</c> command surface.
/// </summary>
internal static class ValidateCommand
{
    /// <summary>
    /// Parses and runs the command.
    /// </summary>
    /// <param name="arguments">The arguments after the command name.</param>
    /// <param name="stdout">The standard-output writer.</param>
    /// <param name="stderr">The standard-error writer.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(
        IReadOnlyList<string> arguments,
        TextWriter stdout,
        TextWriter stderr)
    {
        var json = arguments.Contains("--json", StringComparer.Ordinal);
        var path = default(string);
        var statistics = false;
        var warningsAsErrors = false;
        foreach (var argument in arguments)
        {
            switch (argument)
            {
                case "--json":
                    break;
                case "--stats":
                    statistics = true;
                    break;
                case "--warnings-as-errors":
                    warningsAsErrors = true;
                    break;
                default:
                    if (argument.StartsWith('-'))
                    {
                        return WriteUsageError(json, stdout, stderr, $"Unknown option '{argument}'.");
                    }

                    if (path is not null)
                    {
                        return WriteUsageError(json, stdout, stderr, "Exactly one project path is required.");
                    }

                    path = argument;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return WriteUsageError(json, stdout, stderr, "A project path is required.");
        }

        var options = new ValidationOptions
        {
            IncludeStatistics = statistics,
            WarningsAsErrors = warningsAsErrors,
        };

        ValidationResult result;
        try
        {
            result = await new ProjectValidationPipeline()
                .ValidateFileAsync(path, options: options)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or NotSupportedException
                                              or System.Security.SecurityException)
        {
            var (ioReport, ioExitCode) = ValidationReportWriter.IoError(
                $"Could not read project '{path}': {exception.Message}");
            if (json)
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
        if (json)
        {
            ValidationReportWriter.WriteJson(stdout, report);
        }
        else
        {
            ValidationReportWriter.WriteHuman(stdout, result, exitCode);
        }

        return exitCode;
    }

    private static int WriteUsageError(bool json, TextWriter stdout, TextWriter stderr, string message)
    {
        stderr.WriteLine($"mocassin validate: {message}");
        stderr.WriteLine("usage: mocassin validate <project.mocprj> [--json] [--warnings-as-errors] [--stats]");
        if (json)
        {
            var (report, exitCode) = ValidationReportWriter.UsageError();
            ValidationReportWriter.WriteJson(stdout, report);
            return exitCode;
        }

        return 2;
    }
}
