// SPDX-License-Identifier: MIT

using Mocassin.Cli.Validation;

namespace Mocassin.Cli;

/// <summary>
/// The command-line entry point.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the command-line application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The process exit code.</returns>
    public static Task<int> Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Length == 0 || !string.Equals(args[0], "validate", StringComparison.Ordinal))
        {
            var json = args.Contains("--json", StringComparer.Ordinal);
            Console.Error.WriteLine("usage: mocassin <command> [options]");
            Console.Error.WriteLine("commands: validate");
            if (json)
            {
                var (report, exitCode) = ValidationReportWriter.UsageError();
                ValidationReportWriter.WriteJson(Console.Out, report);
                return Task.FromResult(exitCode);
            }

            return Task.FromResult(2);
        }

        return ValidateCommand.RunAsync(args[1..], Console.Out, Console.Error);
    }
}
