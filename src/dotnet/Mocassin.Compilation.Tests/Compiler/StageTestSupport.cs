// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

using Mocassin.Compilation.Energy;
using Mocassin.Compilation.Structure;
using Mocassin.Compilation.Transitions;
using Mocassin.Compilation.Simulations;
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
        params ICompilationStage[] stages) =>
        Run(project, CompilationOptions.Default, stages);

    /// <summary>
    /// Runs the supplied stages in order with explicit options.
    /// </summary>
    /// <param name="project">The project to compile.</param>
    /// <param name="options">The compilation options.</param>
    /// <param name="stages">The stage sequence.</param>
    /// <returns>The final context and the accumulated diagnostics.</returns>
    public static (CompilationContext Context, ImmutableArray<Diagnostic> Diagnostics) Run(
        Project project,
        CompilationOptions options,
        params ICompilationStage[] stages)
    {
        var context = new CompilationContext(project, options);
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

    /// <summary>
    /// Creates a project whose first simulation references the supplied transitions.
    /// </summary>
    /// <param name="project">The source project.</param>
    /// <param name="transitions">The transition references.</param>
    /// <returns>The updated project.</returns>
    public static Project WithSimulationTransitions(Project project, params Core.Model.Transitions.TransitionRef[] transitions)
    {
        var model = project.Model;
        Core.Model.Simulations.Simulation created = new Core.Model.Simulations.KineticSimulation
        {
            Id = new Core.Ids.SimulationId(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f99")),
            Name = "Compiler test",
            TransitionIds = [.. transitions],
        };
        return project with { Model = model with { Simulations = [created] } };
    }

    /// <summary>
    /// Creates a project whose first simulation references the first transition of the requested kind.
    /// </summary>
    /// <param name="project">The source project.</param>
    /// <param name="kinetic">Whether to reference a kinetic transition.</param>
    /// <returns>The updated project.</returns>
    public static Project WithFirstSimulationTransition(Project project, bool kinetic)
    {
        var model = project.Model;
        var transition = kinetic
            ? Core.Model.Transitions.TransitionRef.From(model.KineticTransitions[0].Id)
            : Core.Model.Transitions.TransitionRef.From(model.MetropolisTransitions[0].Id);
        if (model.Simulations.IsDefaultOrEmpty)
        {
            Core.Model.Simulations.Simulation created = kinetic
                ? new Core.Model.Simulations.KineticSimulation
                {
                    Id = new Core.Ids.SimulationId(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f99")),
                    Name = "Compiler test",
                    TransitionIds = [transition],
                }
                : new Core.Model.Simulations.MetropolisSimulation
                {
                    Id = new Core.Ids.SimulationId(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f99")),
                    Name = "Compiler test",
                    TransitionIds = [transition],
                };
            return project with { Model = model with { Simulations = [created] } };
        }

        var simulation = model.Simulations[0];
        Core.Model.Simulations.Simulation updated = simulation is Core.Model.Simulations.KineticSimulation kineticSimulation
            ? kineticSimulation with { TransitionIds = [transition] }
            : ((Core.Model.Simulations.MetropolisSimulation)simulation) with { TransitionIds = [transition] };
        return project with { Model = model with { Simulations = [updated] } };
    }

    /// <summary>
    /// Creates the cluster stage prefix.
    /// </summary>
    /// <returns>The ordered stages.</returns>
    public static ICompilationStage[] ClusterStages() =>
    [
        .. StructureAndPairStages(),
        new ClusterCompilationStage(),
    ];

    /// <summary>
    /// Creates the transition-rule stage prefix.
    /// </summary>
    /// <returns>The ordered stages.</returns>
    public static ICompilationStage[] RuleStages() =>
    [
        .. ClusterStages(),
        new TransitionCompilationStage(),
    ];

    /// <summary>
    /// Creates the simulation-encoding stage prefix.
    /// </summary>
    /// <returns>The ordered stages.</returns>
    public static ICompilationStage[] EncodingStages() =>
    [
        .. RuleStages(),
        new EncodingCompilationStage(),
    ];
}
