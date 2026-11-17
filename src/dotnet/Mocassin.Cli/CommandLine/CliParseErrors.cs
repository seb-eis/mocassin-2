// SPDX-License-Identifier: MIT

using System.CommandLine;
using Mocassin.Cli.Diagnostics;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Cli.CommandLine;

/// <summary>
/// Maps every <see cref="ParseResult.Errors"/> entry to a CLI-owned <c>CLI001</c> diagnostic and human usage.
/// </summary>
internal static class CliParseErrors
{
    private static readonly ParserConfiguration parserConfiguration = new()
    {
        EnablePosixBundling = false,
        ResponseFileTokenReplacer = null,
    };

    /// <summary>
    /// Converts the library parse errors to <c>CLI001</c> diagnostics.
    /// </summary>
    /// <param name="parseResult">The failed parse result.</param>
    /// <returns>One diagnostic per library error.</returns>
    public static IReadOnlyList<Diagnostic> ToDiagnostics(ParseResult parseResult)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        return parseResult.Errors
            .Select(static error => CliDiagnosticFactory.Usage($"Invalid command line: {error.Message}"))
            .ToArray();
    }

    /// <summary>
    /// Writes the CLI-owned <c>CLI001</c> lines and the library help for the resolved command.
    /// </summary>
    /// <param name="parseResult">The failed parse result.</param>
    /// <param name="graph">The command graph.</param>
    /// <param name="writer">The standard-error writer.</param>
    public static void WriteHuman(ParseResult parseResult, CliCommandGraph graph, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(writer);
        foreach (var diagnostic in ToDiagnostics(parseResult))
        {
            writer.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
        }

        WriteHelp(parseResult.CommandResult.Command, graph, writer);
    }

    /// <summary>
    /// Writes the root help text.
    /// </summary>
    /// <param name="graph">The command graph.</param>
    /// <param name="writer">The standard-error writer.</param>
    public static void WriteRootHelp(CliCommandGraph graph, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(writer);
        WriteHelp(graph.Root, graph, writer);
    }

    private static void WriteHelp(Command command, CliCommandGraph graph, TextWriter writer)
    {
        var arguments = ReferenceEquals(command, graph.Root)
            ? new[] { "--help" }
            : new[] { command.Name, "--help" };
        var helpResult = graph.Root.Parse(arguments, parserConfiguration);
        helpResult.Invoke(new InvocationConfiguration { Output = writer, Error = writer });
    }
}
