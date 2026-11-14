// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Context;

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// One pure compilation stage over the immutable <see cref="CompilationContext"/>.
/// </summary>
internal interface ICompilationStage
{
    /// <summary>
    /// Gets the stage identity. Stages execute in enum order.
    /// </summary>
    public CompilationStage Stage { get; }

    /// <summary>
    /// Executes the stage.
    /// </summary>
    /// <param name="context">The immutable stage input.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated context, diagnostics, and optional signed model.</returns>
    public CompilationStageOutcome Execute(CompilationContext context, CancellationToken cancellationToken);
}
