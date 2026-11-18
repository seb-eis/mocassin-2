// SPDX-License-Identifier: MIT

using Mocassin.Cli.CommandLine;

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
    public static async Task<int> Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        using var signals = new CliSignalHandler();
        return await new CliApplication()
            .RunAsync(args, Console.Out, Console.Error, signals.Token)
            .ConfigureAwait(false);
    }
}
