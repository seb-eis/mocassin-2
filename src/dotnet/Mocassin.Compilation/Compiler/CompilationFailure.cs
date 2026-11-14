// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Context;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// Builds fail-closed stage outcomes while preserving the source diagnostics.
/// </summary>
internal static class CompilationFailure
{
    /// <summary>
    /// Creates a failed stage outcome that wraps the source diagnostics.
    /// </summary>
    /// <param name="context">The stage context.</param>
    /// <param name="code">The compilation diagnostic code.</param>
    /// <param name="message">The compilation diagnostic message.</param>
    /// <param name="sourceDiagnostics">The source diagnostics to preserve.</param>
    /// <returns>The failed stage outcome.</returns>
    public static CompilationStageOutcome FromSource(
        CompilationContext context,
        string code,
        string message,
        ImmutableArray<Diagnostic> sourceDiagnostics)
    {
        var diagnostics = sourceDiagnostics.IsDefault ? [] : sourceDiagnostics;
        var related = diagnostics
            .Where(static diagnostic => diagnostic.Path is not null)
            .Select(static diagnostic => diagnostic.Path!)
            .Distinct()
            .ToImmutableArray();
        var bag = new DiagnosticBag(diagnostics);
        bag.Add(new Diagnostic(code, DiagnosticSeverity.Error, message, related: related));
        return new CompilationStageOutcome(context, bag.Sorted);
    }

    /// <summary>
    /// Creates a failed stage outcome with one error diagnostic.
    /// </summary>
    /// <param name="context">The stage context.</param>
    /// <param name="code">The compilation diagnostic code.</param>
    /// <param name="message">The compilation diagnostic message.</param>
    /// <param name="path">The optional source path.</param>
    /// <returns>The failed stage outcome.</returns>
    public static CompilationStageOutcome FromMessage(
        CompilationContext context,
        string code,
        string message,
        ModelPath? path = null) =>
        new(
            context,
            ImmutableArray.Create(new Diagnostic(code, DiagnosticSeverity.Error, message, path)));
}
