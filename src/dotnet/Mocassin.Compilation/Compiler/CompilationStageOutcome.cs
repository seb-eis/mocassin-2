// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Context;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// The result of one compilation stage.
/// </summary>
/// <param name="Context">The updated immutable context passed to the next stage.</param>
/// <param name="Diagnostics">The stage diagnostics.</param>
/// <param name="Model">The signed model, only valid from the signing stage.</param>
internal sealed record CompilationStageOutcome(
    CompilationContext Context,
    ImmutableArray<Diagnostic> Diagnostics,
    CompiledModel? Model = null)
{
    /// <summary>
    /// Creates a successful outcome without diagnostics.
    /// </summary>
    /// <param name="context">The updated context.</param>
    /// <returns>The completed outcome.</returns>
    public static CompilationStageOutcome Completed(CompilationContext context) =>
        new(context, ImmutableArray<Diagnostic>.Empty);

    /// <summary>
    /// Creates a failed outcome carrying one error diagnostic.
    /// </summary>
    /// <param name="context">The updated context.</param>
    /// <param name="diagnostic">The error diagnostic.</param>
    /// <returns>The failed outcome.</returns>
    public static CompilationStageOutcome Failed(CompilationContext context, Diagnostic diagnostic) =>
        new(context, ImmutableArray.Create(diagnostic));
}
