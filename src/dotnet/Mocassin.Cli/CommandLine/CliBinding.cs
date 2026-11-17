// SPDX-License-Identifier: MIT

using System.CommandLine;

namespace Mocassin.Cli.CommandLine;

/// <summary>
/// Converts a successful <see cref="ParseResult"/> into the typed request record each handler takes.
/// </summary>
/// <remarks>
/// Handlers never re-parse <c>string[]</c>. The library has already validated arity, required symbols, option
/// applicability, and typed values when a binder runs.
/// </remarks>
internal static class CliBinding
{
    /// <summary>
    /// Binds the <c>validate</c> command.
    /// </summary>
    /// <param name="parseResult">The successful parse result.</param>
    /// <param name="graph">The command graph that owns the symbols.</param>
    /// <returns>The typed request.</returns>
    public static ValidateRequest BindValidate(ParseResult parseResult, CliCommandGraph graph)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(graph);
        return new ValidateRequest(
            parseResult.GetRequiredValue(graph.ValidateProject),
            parseResult.GetValue(graph.ValidateJson),
            parseResult.GetValue(graph.ValidateWarningsAsErrors),
            parseResult.GetValue(graph.ValidateStats));
    }

    /// <summary>
    /// Binds the <c>build</c> command.
    /// </summary>
    /// <param name="parseResult">The successful parse result.</param>
    /// <param name="graph">The command graph that owns the symbols.</param>
    /// <returns>The typed request.</returns>
    public static BuildRequest BindBuild(ParseResult parseResult, CliCommandGraph graph)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(graph);
        return new BuildRequest(
            parseResult.GetRequiredValue(graph.BuildProject),
            parseResult.GetValue(graph.BuildJson),
            parseResult.GetValue(graph.BuildPreview),
            parseResult.GetValue(graph.BuildOutput),
            OptionalId(parseResult.GetValue(graph.BuildTemplate)),
            OptionalId(parseResult.GetValue(graph.BuildJobTemplate)),
            OptionalId(parseResult.GetValue(graph.BuildParameterization)),
            parseResult.GetValue(graph.BuildJobs),
            parseResult.GetValue(graph.BuildParallel),
            parseResult.GetValue(graph.BuildCompact),
            parseResult.GetValue(graph.BuildForce),
            parseResult.GetValue(graph.BuildWarningsAsErrors),
            parseResult.GetValue(graph.BuildQuiet));
    }

    private static Guid? OptionalId(Guid value) => value == Guid.Empty ? null : value;
}
