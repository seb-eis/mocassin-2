// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Cli.Build;
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
    public static Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(params string[] arguments) =>
        RunAsync(new CliApplication(), arguments);

    /// <summary>
    /// Runs one command line through an application with an explicit build-command seam.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <param name="arguments">The raw process arguments.</param>
    /// <returns>The exit code and the captured standard output and error.</returns>
    public static async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        CliApplication application,
        params string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(application);
        using var stdout = new StringWriter(CultureInfo.InvariantCulture);
        using var stderr = new StringWriter(CultureInfo.InvariantCulture);
        var exitCode = await application.RunAsync(arguments, stdout, stderr);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    /// <summary>
    /// Runs one command line through an application whose build command uses injected services.
    /// </summary>
    /// <param name="buildCommand">The build command.</param>
    /// <param name="arguments">The raw process arguments.</param>
    /// <returns>The exit code and the captured standard output and error.</returns>
    public static Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        BuildCommand buildCommand,
        params string[] arguments) =>
        RunAsync(new CliApplication(new CliCommandGraph(), buildCommand), arguments);
}
