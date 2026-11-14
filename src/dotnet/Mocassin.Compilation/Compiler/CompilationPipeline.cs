// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Context;
using Mocassin.Compilation.Energy;
using Mocassin.Compilation.Structure;
using Mocassin.Compilation.Transitions;
using Mocassin.Compilation.Simulations;
using Mocassin.Compilation.Indexing;
using Mocassin.Compilation.Signatures;

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// Builds and validates the ordered content-stage sequence.
/// </summary>
internal static class CompilationPipeline
{
    /// <summary>
    /// The content stages that must appear, in order, before the report stage.
    /// </summary>
    public static readonly ImmutableArray<CompilationStage> ContentStages =
    [
        CompilationStage.Resolve,
        CompilationStage.Structure,
        CompilationStage.Environments,
        CompilationStage.Pairs,
        CompilationStage.Clusters,
        CompilationStage.Rules,
        CompilationStage.Encoding,
        CompilationStage.Indexing,
        CompilationStage.Signing,
    ];

    /// <summary>
    /// Creates the default fail-closed stage sequence.
    /// </summary>
    /// <returns>The default content stages.</returns>
    public static ImmutableArray<ICompilationStage> CreateDefaultStages() =>
    [
        new ResolveCompilationStage(),
        new StructureCompilationStage(),
        new EnvironmentCompilationStage(),
        new PairCompilationStage(),
        new ClusterCompilationStage(),
        new TransitionCompilationStage(),
        new EncodingCompilationStage(),
        new IndexingCompilationStage(),
        new SigningCompilationStage(),
    ];

    /// <summary>
    /// Validates that a stage sequence contains every content stage exactly once, in enum order.
    /// </summary>
    /// <param name="stages">The supplied content stages.</param>
    /// <returns>The validated sequence.</returns>
    /// <exception cref="ArgumentException">The sequence is default, has a null entry, or is out of order.</exception>
    public static ImmutableArray<ICompilationStage> ValidateStages(ImmutableArray<ICompilationStage> stages)
    {
        if (stages.IsDefault || stages.Length != ContentStages.Length)
        {
            throw new ArgumentException(
                $"The compilation pipeline requires exactly {ContentStages.Length} content stages.",
                nameof(stages));
        }

        for (var index = 0; index < stages.Length; index++)
        {
            var stage = stages[index];
            if (stage is null)
            {
                throw new ArgumentException(
                    $"The compilation stage at index {index} is null.",
                    nameof(stages));
            }

            if (stage.Stage != ContentStages[index])
            {
                throw new ArgumentException(
                    $"The compilation stage at index {index} must be '{ContentStages[index]}' but was '{stage.Stage}'.",
                    nameof(stages));
            }
        }

        return stages;
    }
}
