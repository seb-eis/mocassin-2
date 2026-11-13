// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Context;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// A fail-closed placeholder for a content stage that a later Step 08 feature implements.
/// </summary>
/// <remarks>
/// The placeholder reports <c>CMP001</c> and stops the pipeline instead of returning an incomplete or
/// fabricated compiled model. It is replaced stage by stage as the structure, energy, transition, encoding,
/// indexing, and signing features land.
/// </remarks>
internal sealed class UnavailableCompilationStage : ICompilationStage
{
    private const string preconditionFailedCode = "CMP001";

    /// <summary>
    /// Initializes a placeholder for one content stage.
    /// </summary>
    /// <param name="stage">The content stage that is not available yet.</param>
    /// <exception cref="ArgumentOutOfRangeException">The stage is not a content stage.</exception>
    public UnavailableCompilationStage(CompilationStage stage)
    {
        if (!Enum.IsDefined(stage) || stage == CompilationStage.Report)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stage),
                stage,
                "Only content stages can be unavailable placeholders.");
        }

        Stage = stage;
    }

    /// <inheritdoc />
    public CompilationStage Stage { get; }

    /// <inheritdoc />
    public CompilationStageOutcome Execute(CompilationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostic = new Diagnostic(
            preconditionFailedCode,
            DiagnosticSeverity.Error,
            $"The {Stage} compilation stage is not available in this build; compilation stops instead of returning an incomplete model.",
            hint: "Use a build of Mocassin.Compilation that includes the remaining Step 08 compilation features.");
        return CompilationStageOutcome.Failed(context, diagnostic);
    }
}
