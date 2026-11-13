// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Context;
using Mocassin.Core.Geometry;

namespace Mocassin.Compilation.Indexing;

/// <summary>
/// Finalizes quantization, dense-index maps, and the stable-ID inspection projection.
/// </summary>
internal sealed class IndexingCompilationStage : ICompilationStage
{
    /// <inheritdoc />
    public CompilationStage Stage => CompilationStage.Indexing;

    /// <inheritdoc />
    public CompilationStageOutcome Execute(CompilationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Structure is null || context.Energy is null || context.Transitions is null
            || context.Simulations.IsDefault)
        {
            throw new InvalidOperationException("The indexing stage requires all compiled content stages.");
        }

        var rules = context.Transitions.Rules
            .Select(rule => rule with
            {
                ChargeTransportVector = Quantize(rule.ChargeTransportVector),
            })
            .ToImmutableArray();
        var directions = context.Transitions.Directions
            .Select(direction => direction with { PathVector = Quantize(direction.PathVector) })
            .ToImmutableArray();
        var transitions = context.Transitions with { Rules = rules, Directions = directions };
        var maps = new CompiledIndexMaps(
            [.. directions.Select(static direction => direction.ObjectId)],
            [.. rules.Select(static rule => rule.RuleGeometryId)],
            [.. context.Energy.PairGeometries.Select(static geometry => geometry.GeometryId)],
            [.. context.Energy.ClusterTables.Select(static table => table.ClusterEnergyId)]);
        return new CompilationStageOutcome(
            context with { Transitions = transitions, IndexMaps = maps },
            []);
    }

    private static Cartesian3 Quantize(Cartesian3 value) => new(
        Quantize(value.X),
        Quantize(value.Y),
        Quantize(value.Z));

    private static double Quantize(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException("The compiled model contains a non-finite transport value.");
        }

        var rounded = Math.Round(value, 12, MidpointRounding.AwayFromZero);
        return rounded == 0.0 ? 0.0 : rounded;
    }
}
