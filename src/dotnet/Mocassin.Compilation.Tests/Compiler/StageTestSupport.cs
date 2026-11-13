// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

using Mocassin.Compilation.Energy;
using Mocassin.Compilation.Structure;
using Mocassin.Packages.Projects;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Loads real sample projects and executes compilation stages in isolation.
/// </summary>
internal static class StageTestSupport
{
    /// <summary>
    /// Loads a sample project through the parse-only store entry.
    /// </summary>
    /// <param name="relativePath">The sample path relative to the repository root.</param>
    /// <returns>The loaded project snapshot.</returns>
    public static async Task<Project> LoadProjectAsync(string relativePath)
    {
        var store = new ProjectStore(options: new ProjectStoreOptions { ValidateOnLoad = false });
        var result = await store.LoadAsync(RepositoryPaths.Combine(relativePath));
        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        return result.Project!;
    }

    /// <summary>
    /// Runs the supplied stages in order and returns the final context and diagnostics.
    /// </summary>
    /// <param name="project">The project to compile.</param>
    /// <param name="stages">The stage sequence.</param>
    /// <returns>The final context and the accumulated diagnostics.</returns>
    public static (CompilationContext Context, ImmutableArray<Diagnostic> Diagnostics) Run(
        Project project,
        params ICompilationStage[] stages)
    {
        var context = new CompilationContext(project, CompilationOptions.Default);
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (var stage in stages)
        {
            var outcome = stage.Execute(context, CancellationToken.None);
            diagnostics.AddRange(outcome.Diagnostics.IsDefault ? [] : outcome.Diagnostics);
            if (outcome.Diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                break;
            }

            context = outcome.Context;
        }

        return (context, diagnostics.ToImmutable());
    }

    /// <summary>
    /// Creates the structure-and-pair stage prefix.
    /// </summary>
    /// <returns>The ordered stages.</returns>
    public static ICompilationStage[] StructureAndPairStages() =>
    [
        new ResolveCompilationStage(),
        new StructureCompilationStage(),
        new EnvironmentCompilationStage(),
        new PairCompilationStage(),
    ];
}
