// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Project;

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// The stateless, synchronous default <see cref="IModelCompiler"/> implementation.
/// </summary>
/// <remarks>
/// The compiler performs no I/O and holds no mutable state, so one instance may be reused across concurrent
/// <see cref="Compile"/> calls. The project is treated as an immutable, already validated snapshot and is
/// never mutated.
/// </remarks>
public sealed class ModelCompiler : IModelCompiler
{
    private readonly ImmutableArray<ICompilationStage> stages;

    /// <summary>
    /// Initializes the default compiler.
    /// </summary>
    public ModelCompiler()
        : this(CompilationPipeline.CreateDefaultStages())
    {
    }

    /// <summary>
    /// Initializes a compiler with an explicit content-stage sequence.
    /// </summary>
    /// <param name="stages">The content stages in pipeline order.</param>
    /// <exception cref="ArgumentException">The sequence is incomplete, contains a null, or is out of order.</exception>
    internal ModelCompiler(ImmutableArray<ICompilationStage> stages)
    {
        this.stages = CompilationPipeline.ValidateStages(stages);
    }

    /// <inheritdoc />
    public CompilationResult Compile(
        Project project,
        CompilationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        var effectiveOptions = CompilationOptions.Validate(options);
        cancellationToken.ThrowIfCancellationRequested();
        return CompilationOrchestrator.Run(stages, project, effectiveOptions, cancellationToken);
    }
}
