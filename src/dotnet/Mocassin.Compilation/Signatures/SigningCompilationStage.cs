// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Context;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Compilation.Signatures;

/// <summary>
/// Builds the final compiled model and computes its canonical signature.
/// </summary>
internal sealed class SigningCompilationStage : ICompilationStage
{
    /// <inheritdoc />
    public CompilationStage Stage => CompilationStage.Signing;

    /// <inheritdoc />
    public CompilationStageOutcome Execute(CompilationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Particles is null || context.Structure is null || context.Energy is null
            || context.Transitions is null || context.IndexMaps is null)
        {
            throw new InvalidOperationException("The signing stage requires every compiled content stage.");
        }

        try
        {
            var unsigned = new CompiledModel(
                CompiledModel.CurrentFormatVersion,
                "unsigned",
                context.Versions.SymmetryContractVersion,
                context.Versions.SpaceGroupDataVersion,
                context.Versions.SpaceGroupDataSha256)
            {
                Particles = context.Particles,
                Structure = context.Structure,
                Energy = context.Energy,
                Transitions = context.Transitions,
                Simulations = context.CompiledSimulations.IsDefault ? [] : context.CompiledSimulations,
                IndexMaps = context.IndexMaps,
            };
            string signature;
            if (context.Cache is not null)
            {
                var projectionHash = System.Convert.ToHexStringLower(
                    System.Security.Cryptography.SHA256.HashData(ModelSignatureProjection.ComputeProjection(unsigned)));
                var key = StageCacheKeys.Signing(context, projectionHash);
                signature = context.Cache.TryGet<string>(key, out var cachedSignature) && cachedSignature is not null
                    ? cachedSignature
                    : context.Cache.GetOrAdd(key, () => ModelSignatureProjection.Compute(unsigned));
            }
            else
            {
                signature = ModelSignatureProjection.Compute(unsigned);
            }

            var model = unsigned with { ModelSignature = signature };
            return new CompilationStageOutcome(context, [], model);
        }
        catch (InvalidOperationException exception)
        {
            return new CompilationStageOutcome(
                context,
                [new Diagnostic(
                    CompilationErrorCodes.Signature,
                    DiagnosticSeverity.Error,
                    $"The compiled model cannot be signed: {exception.Message}")]);
        }
    }
}
