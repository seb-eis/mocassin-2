// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Cli.CommandLine;

namespace Mocassin.Cli.Tests.CommandLine;

/// <summary>
/// Runs the CLI in-process with captured output.
/// </summary>
internal static class CliTestRunner
{
    /// <summary>
    /// Runs one command line through <see cref="Mocassin.Cli.CommandLine.CliApplication"/>.
    /// </summary>
    /// <param name="arguments">The raw process arguments.</param>
    /// <returns>The exit code and the captured standard output and error.</returns>
    public static async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(params string[] arguments)
    {
        using var stdout = new StringWriter(CultureInfo.InvariantCulture);
        using var stderr = new StringWriter(CultureInfo.InvariantCulture);
        var exitCode = await new CliApplication().RunAsync(arguments, stdout, stderr);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }
}
