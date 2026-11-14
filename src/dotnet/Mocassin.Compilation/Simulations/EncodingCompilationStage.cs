// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Context;
using Mocassin.Compilation.Diagnostics;
using Mocassin.Compilation.Transitions;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Simulations;

namespace Mocassin.Compilation.Simulations;

/// <summary>
/// Builds jump count/mapping tables, tracker definitions, and derived solver intent.
/// </summary>
internal sealed class EncodingCompilationStage : ICompilationStage
{
    /// <inheritdoc />
    public CompilationStage Stage => CompilationStage.Encoding;

    /// <inheritdoc />
    public CompilationStageOutcome Execute(CompilationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Structure is null || context.Transitions is null || context.Particles is null)
        {
            throw new InvalidOperationException("The encoding stage requires the compiled structure and rules.");
        }

        if (context.Cache is not null &&
            context.Cache.TryGet<EncodingCacheEntry>(StageCacheKeys.Encoding(context), out var cached) &&
            cached is not null &&
            cached.Transitions.ParticleCount == context.Particles.Count &&
            cached.Transitions.PositionCount == context.Structure.PositionCount)
        {
            return new CompilationStageOutcome(
                context with
                {
                    Transitions = cached.Transitions,
                    CompiledSimulations = cached.Simulations,
                },
                []);
        }

        var transitions = context.Transitions;
        var globalTrackers = BuildGlobalTrackers(transitions);
        if (globalTrackers.Any(static tracker => tracker.Index >= byte.MaxValue))
        {
            return CompilationFailure.FromMessage(
                context,
                CompilationDiagnosticCodes.CompileLimit,
                "The compiled global tracker count exceeds the 8-bit tracker-order limit.");
        }

        var staticTrackers = BuildStaticTrackers(
            context.Structure.PositionCount,
            context.Particles.Count,
            transitions.Collections.Any(static collection => collection.Kind == SimulationKind.Kmc));
        var trackerIndex = globalTrackers
            .ToDictionary(
                static tracker => (tracker.CollectionIndex, tracker.ParticleIndex),
                static tracker => tracker.Index);
        var rules = transitions.Rules
            .Select(rule => rule with
            {
                TrackerOrder = BuildTrackerOrder(
                    rule,
                    transitions,
                    trackerIndex,
                    context.Particles.Count),
            })
            .ToImmutableArray();

        var mapping = BuildMappings(context.Structure.PositionCount, context.Particles.Count, transitions, rules);
        var encoded = transitions with
        {
            Rules = rules,
            JumpCounts = mapping.Counts,
            JumpOffsets = mapping.Offsets,
            JumpMappings = mapping.Directions,
            GlobalTrackers = globalTrackers,
            StaticTrackers = staticTrackers,
        };

        var simulations = BuildSimulations(context, encoded);
        var warnings = new DiagnosticBag();
        var tableBytes = context.Energy is null
            ? 0L
            : context.Energy.PairTables.Aggregate(
                context.Energy.ClusterTables.Aggregate(
                    0L,
                    static (current, table) => SaturatingMath.Add(
                        current,
                        SaturatingMath.Multiply(table.OccupationCount, sizeof(double)))),
                static (current, table) => SaturatingMath.Add(
                    current,
                    SaturatingMath.Multiply(
                        SaturatingMath.Multiply(table.ParticleCount, table.ParticleCount),
                        sizeof(double))));
        if (tableBytes > context.Options.MaxEnergyTableBytes)
        {
            return CompilationFailure.FromMessage(
                context,
                CompilationDiagnosticCodes.CompileLimit,
                $"The estimated energy-table size {tableBytes} bytes exceeds the hard limit " +
                $"{context.Options.MaxEnergyTableBytes}.");
        }

        if (tableBytes > context.Options.AdvisoryEnergyTableBytes)
        {
            warnings.Add(new Diagnostic(
                CompilationDiagnosticCodes.AdvisoryBudget,
                DiagnosticSeverity.Warning,
                $"The estimated energy-table size {tableBytes} bytes exceeds the advisory budget " +
                $"{context.Options.AdvisoryEnergyTableBytes}; the hard limit was not exceeded."));
        }

        context.Cache?.GetOrAdd(
            StageCacheKeys.Encoding(context),
            () => new EncodingCacheEntry(encoded, simulations));
        return new CompilationStageOutcome(
            context with { Transitions = encoded, CompiledSimulations = simulations },
            []);
    }

    private static ImmutableArray<CompiledGlobalTracker> BuildGlobalTrackers(CompiledTransitionModel model)
    {
        var builder = ImmutableArray.CreateBuilder<CompiledGlobalTracker>();
        foreach (var collection in model.Collections
                     .Where(static collection => collection.Kind == SimulationKind.Kmc)
                     .OrderBy(static collection => collection.Index))
        {
            for (var particle = 1; particle < model.ParticleCount; particle++)
            {
                if ((collection.MobileParticleMask & (1UL << particle)) == 0)
                {
                    continue;
                }

                builder.Add(new CompiledGlobalTracker(builder.Count, collection.Index, particle));
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<CompiledStaticTracker> BuildStaticTrackers(
        int positionCount,
        int particleCount,
        bool hasKmc)
    {
        if (!hasKmc)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<CompiledStaticTracker>(
            positionCount * Math.Max(0, particleCount - 1));
        for (var position = 0; position < positionCount; position++)
        {
            for (var particle = 1; particle < particleCount; particle++)
            {
                builder.Add(new CompiledStaticTracker(
                    position * (particleCount - 1) + (particle - 1),
                    position,
                    particle));
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<byte> BuildTrackerOrder(
        CompiledJumpRule rule,
        CompiledTransitionModel model,
        Dictionary<(int CollectionIndex, int ParticleIndex), int> trackerIndex,
        int particleCount)
    {
        var nodes = rule.Movement.Length;
        var order = ImmutableArray.CreateBuilder<byte>(nodes);
        if (model.Collections[rule.CollectionIndex].Kind == SimulationKind.Mmc)
        {
            for (var node = 0; node < nodes; node++)
            {
                order.Add(0xFF);
            }

            return order.MoveToImmutable();
        }

        for (var node = 0; node < nodes; node++)
        {
            var particle = (int)((rule.State0 >> (6 * node)) & 63);
            order.Add(particle == 0 || !trackerIndex.TryGetValue((rule.CollectionIndex, particle), out var index)
                ? (byte)0xFF
                : checked((byte)index));
        }

        return order.MoveToImmutable();
    }

    private static JumpMapping BuildMappings(
        int positionCount,
        int particleCount,
        CompiledTransitionModel model,
        ImmutableArray<CompiledJumpRule> rules)
    {
        var buckets = new List<int>[positionCount * particleCount];
        for (var index = 0; index < buckets.Length; index++)
        {
            buckets[index] = [];
        }

        foreach (var rule in rules)
        {
            foreach (var directionIndex in rule.DirectionIndices)
            {
                var direction = model.Directions[directionIndex];
                var starts = direction.EquivalentStartPositions.IsDefaultOrEmpty
                    ? [direction.PositionId]
                    : direction.EquivalentStartPositions;
                foreach (var start in starts)
                {
                    var particle = (int)(rule.State0 & 63);
                    if (particle == 0)
                    {
                        continue;
                    }

                    var key = (start * particleCount) + particle;
                    if (!buckets[key].Contains(directionIndex))
                    {
                        buckets[key].Add(directionIndex);
                    }

                    if (model.Collections[rule.CollectionIndex].Kind == SimulationKind.Mmc)
                    {
                        var second = (int)((rule.State0 >> 6) & 63);
                        if (second != 0 && second != particle)
                        {
                            var secondKey = (start * particleCount) + second;
                            if (!buckets[secondKey].Contains(directionIndex))
                            {
                                buckets[secondKey].Add(directionIndex);
                            }
                        }
                    }
                }
            }
        }

        var counts = ImmutableArray.CreateBuilder<int>(buckets.Length);
        var offsets = ImmutableArray.CreateBuilder<int>(buckets.Length);
        var directions = ImmutableArray.CreateBuilder<int>();
        foreach (var bucket in buckets)
        {
            counts.Add(bucket.Count);
            offsets.Add(directions.Count);
            bucket.Sort();
            directions.AddRange(bucket);
        }

        return new JumpMapping(counts.MoveToImmutable(), offsets.MoveToImmutable(), directions.ToImmutable());
    }

    private static ImmutableArray<CompiledSimulation> BuildSimulations(
        CompilationContext context,
        CompiledTransitionModel model)
    {
        var builder = ImmutableArray.CreateBuilder<CompiledSimulation>(context.Simulations.Length);
        foreach (var simulation in context.Simulations)
        {
            var collectionIndices = new List<int>();
            foreach (var reference in simulation.TransitionIds.IsDefault ? [] : simulation.TransitionIds)
            {
                var match = model.Collections.FirstOrDefault(collection => collection.Transition == reference);
                if (match is not null && !collectionIndices.Contains(match.Index))
                {
                    collectionIndices.Add(match.Index);
                }
            }

            collectionIndices.Sort();
            var trackerGlobal = new List<int>();
            var trackerStatic = new List<int>();
            if (simulation.Kind == SimulationKind.Kmc)
            {
                trackerGlobal.AddRange(model.GlobalTrackers
                    .Where(tracker => collectionIndices.Contains(tracker.CollectionIndex))
                    .Select(static tracker => tracker.Index));
                trackerStatic.AddRange(model.StaticTrackers.Select(static tracker => tracker.Index));
            }

            var preRun = simulation is KineticSimulation kinetic ? kinetic.PreRunMcsp : 0;
            builder.Add(new CompiledSimulation(
                simulation.Id,
                simulation.Name,
                simulation.Kind,
                [.. collectionIndices],
                Snapshot(simulation),
                new CompiledSolverIntent(
                    simulation.Kind,
                    UseDualDofCorrection: simulation.Kind == SimulationKind.Kmc,
                    UsePreRun: simulation.Kind == SimulationKind.Kmc && preRun > 0,
                    preRun),
                new CompiledTrackerModel([.. trackerGlobal], [.. trackerStatic])));
        }

        return builder.MoveToImmutable();
    }

    private static CompiledRuntimeSettings Snapshot(Simulation simulation)
    {
        if (simulation is KineticSimulation kinetic)
        {
            return new CompiledRuntimeSettings(
                kinetic.TemperatureK,
                kinetic.TargetMcsp,
                kinetic.WriteOutCount,
                kinetic.TimeLimitSeconds,
                kinetic.MinSuccessRate,
                kinetic.JobCount,
                kinetic.RngSeed,
                kinetic.Flags,
                kinetic.Instruction,
                kinetic.PreRunMcsp,
                kinetic.ElectricFieldModulusVm,
                kinetic.ElectricFieldDirection,
                kinetic.NormalizationEnergyEv,
                kinetic.MaxAttemptFrequencyHz,
                0.0,
                0,
                0,
                0);
        }

        var mmc = (MetropolisSimulation)simulation;
        return new CompiledRuntimeSettings(
            mmc.TemperatureK,
            mmc.TargetMcsp,
            mmc.WriteOutCount,
            mmc.TimeLimitSeconds,
            mmc.MinSuccessRate,
            mmc.JobCount,
            mmc.RngSeed,
            mmc.Flags,
            mmc.Instruction,
            0,
            0.0,
            default,
            null,
            null,
            mmc.BreakTolerance,
            mmc.BreakSampleLengthMcsp,
            mmc.BreakSampleIntervalMcsp,
            mmc.ResultSampleMcsp);
    }

    private sealed record EncodingCacheEntry(
        CompiledTransitionModel Transitions,
        ImmutableArray<CompiledSimulation> Simulations);

    private readonly record struct JumpMapping(
        ImmutableArray<int> Counts,
        ImmutableArray<int> Offsets,
        ImmutableArray<int> Directions);
}
