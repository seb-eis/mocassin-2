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
    public static BuildCommandRequest BindBuild(ParseResult parseResult, CliCommandGraph graph)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(graph);
        return new BuildCommandRequest(
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

    /// <summary>
    /// Binds the <c>new</c> command.
    /// </summary>
    /// <param name="parseResult">The successful parse result.</param>
    /// <param name="graph">The command graph that owns the symbols.</param>
    /// <returns>The typed request.</returns>
    public static NewProjectRequest BindNewProject(ParseResult parseResult, CliCommandGraph graph)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(graph);
        return new NewProjectRequest(
            parseResult.GetRequiredValue(graph.NewName),
            parseResult.GetValue(graph.NewOutput),
            parseResult.GetValue(graph.NewTemplate) ?? "blank",
            parseResult.GetValue(graph.NewJson));
    }

    /// <summary>
    /// Binds the <c>compile</c> command.
    /// </summary>
    /// <param name="parseResult">The successful parse result.</param>
    /// <param name="graph">The command graph that owns the symbols.</param>
    /// <returns>The typed request.</returns>
    public static CompileRequest BindCompile(ParseResult parseResult, CliCommandGraph graph)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(graph);
        return new CompileRequest(
            parseResult.GetRequiredValue(graph.CompileProject),
            parseResult.GetValue(graph.CompileOutput),
            parseResult.GetValue(graph.CompileJson));
    }

    /// <summary>
    /// Binds the <c>inspect</c> command.
    /// </summary>
    /// <param name="parseResult">The successful parse result.</param>
    /// <param name="graph">The command graph that owns the symbols.</param>
    /// <returns>The typed request.</returns>
    public static InspectRequest BindInspect(ParseResult parseResult, CliCommandGraph graph)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(graph);
        return new InspectRequest(
            parseResult.GetRequiredValue(graph.InspectPackage),
            parseResult.GetValue(graph.InspectJson));
    }

    /// <summary>
    /// Binds the <c>verify</c> command.
    /// </summary>
    /// <param name="parseResult">The successful parse result.</param>
    /// <param name="graph">The command graph that owns the symbols.</param>
    /// <returns>The typed request.</returns>
    public static VerifyRequest BindVerify(ParseResult parseResult, CliCommandGraph graph)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(graph);
        return new VerifyRequest(
            parseResult.GetRequiredValue(graph.VerifyPackage),
            parseResult.GetValue(graph.VerifyJson));
    }

    /// <summary>
    /// Binds the <c>doctor</c> command.
    /// </summary>
    /// <param name="parseResult">The successful parse result.</param>
    /// <param name="graph">The command graph that owns the symbols.</param>
    /// <returns>The typed request.</returns>
    public static DoctorRequest BindDoctor(ParseResult parseResult, CliCommandGraph graph)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(graph);
        return new DoctorRequest(parseResult.GetValue(graph.DoctorJson));
    }

    /// <summary>
    /// Binds the <c>clean</c> command.
    /// </summary>
    /// <param name="parseResult">The successful parse result.</param>
    /// <param name="graph">The command graph that owns the symbols.</param>
    /// <returns>The typed request.</returns>
    public static CleanRequest BindClean(ParseResult parseResult, CliCommandGraph graph)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(graph);
        return new CleanRequest(
            parseResult.GetRequiredValue(graph.CleanPackage),
            parseResult.GetValue(graph.CleanDirectory),
            parseResult.GetValue(graph.CleanOlderThan),
            parseResult.GetValue(graph.CleanDryRun),
            parseResult.GetValue(graph.CleanResults),
            parseResult.GetValue(graph.CleanJson));
    }

    private static Guid? OptionalId(Guid value) => value == Guid.Empty ? null : value;
}
