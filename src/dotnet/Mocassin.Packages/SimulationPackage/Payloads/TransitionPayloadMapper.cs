// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Simulations;
using Mocassin.Compilation.Transitions;
using Mocassin.Contracts.Packages;
using Mocassin.Contracts.Parameterization;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Simulations;

namespace Mocassin.Packages.SimulationPackage.Payloads;

/// <summary>
/// Maps the compiled transitions, selection tables, and tracker mappings to the model payload.
/// </summary>
internal static class TransitionPayloadMapper
{
    private const byte trackerSentinel = 0xFF;

    /// <summary>
    /// Maps the compiled transition model.
    /// </summary>
    /// <param name="transitions">The compiled transition model.</param>
    /// <param name="values">The applied parameterization values.</param>
    /// <param name="particleCount">The compiled particle count including void.</param>
    /// <returns>The transition payload.</returns>
    /// <exception cref="InvalidOperationException">The compiled model and applied values disagree.</exception>
    public static TransitionPayload Map(
        CompiledTransitionModel transitions,
        ParameterizationValues values,
        int particleCount)
    {
        if (values.TransitionRuleFrequencies.IsDefault ||
            values.TransitionRuleFrequencies.Length != transitions.Rules.Length)
        {
            throw new InvalidOperationException("The applied rule-frequency count does not match the compiled model.");
        }

        var collections = new JumpCollectionPayload[transitions.Collections.Length];
        for (var index = 0; index < collections.Length; index++)
        {
            var collection = transitions.Collections[index];
            collections[index] = new JumpCollectionPayload(
                MapKind(collection.Kind),
                collection.Transition.Id.ToString("D"),
                collection.MobileParticleMask,
                MapIndices(collection.RuleIndices, "collection rule"),
                MapIndices(collection.DirectionIndices, "collection direction"));
        }

        var directions = new JumpDirectionPayload[transitions.Directions.Length];
        for (var index = 0; index < directions.Length; index++)
        {
            var direction = transitions.Directions[index];
            directions[index] = new JumpDirectionPayload(
                direction.ObjectId,
                (uint)direction.PositionId,
                (uint)direction.CollectionIndex,
                MapPath(direction.Path),
                [.. direction.Movement.Select(static movement => ImmutableArray.Create(movement.A, movement.B, movement.C))],
                [direction.PathVector.X, direction.PathVector.Y, direction.PathVector.Z],
                direction.PathLength,
                direction.IsInverse);
        }

        var rules = new JumpRulePayload[transitions.Rules.Length];
        for (var index = 0; index < rules.Length; index++)
        {
            var rule = transitions.Rules[index];
            if (rule.DirectionIndex < 0 || rule.DirectionIndex >= directions.Length)
            {
                throw new InvalidOperationException($"Rule {rule.Index} references a direction out of range.");
            }

            var representativePathLength = directions[rule.DirectionIndex].Path.Length;
            if (rule.TrackerOrder.IsDefault || rule.TrackerOrder.Length != representativePathLength)
            {
                throw new InvalidOperationException(
                    $"Rule {rule.Index} tracker order has length {rule.TrackerOrder.Length}, expected {representativePathLength}.");
            }

            foreach (var tracker in rule.TrackerOrder)
            {
                if (tracker != trackerSentinel && tracker >= transitions.GlobalTrackers.Length)
                {
                    throw new InvalidOperationException(
                        $"Rule {rule.Index} references global tracker {tracker} out of range.");
                }
            }

            rules[index] = new JumpRulePayload(
                (uint)rule.Index,
                (uint)rule.CollectionIndex,
                rule.State0,
                rule.State1,
                rule.State2,
                values.TransitionRuleFrequencies[index],
                [rule.ChargeTransportVector.X, rule.ChargeTransportVector.Y, rule.ChargeTransportVector.Z],
                rule.StaticCorrection is { } correction
                    ? ImmutableArray.Create(correction.X, correction.Y, correction.Z)
                    : null,
                rule.TrackerOrder);
        }

        return new TransitionPayload(
            [.. collections],
            [.. directions],
            [.. rules],
            MapCounts(transitions.JumpCounts, "jump count"),
            MapCounts(transitions.JumpMappings, "jump mapping"),
            MapStaticTrackerMapping(transitions.StaticTrackers, transitions.PositionCount, particleCount),
            MapGlobalTrackerMapping(transitions.GlobalTrackers, transitions.Collections.Length, particleCount));
    }

    private static string MapKind(SimulationKind kind) =>
        kind == SimulationKind.Kmc ? "kmc" : "mmc";

    private static ImmutableArray<uint> MapIndices(ImmutableArray<int> indices, string description)
    {
        var values = new uint[indices.Length];
        for (var index = 0; index < indices.Length; index++)
        {
            if (indices[index] < 0)
            {
                throw new InvalidOperationException($"A {description} index is negative.");
            }

            values[index] = (uint)indices[index];
        }

        return [.. values];
    }

    private static ImmutableArray<uint> MapCounts(ImmutableArray<int> counts, string description)
    {
        var values = new uint[counts.Length];
        for (var index = 0; index < counts.Length; index++)
        {
            if (counts[index] < 0)
            {
                throw new InvalidOperationException($"A {description} value is negative.");
            }

            values[index] = (uint)counts[index];
        }

        return [.. values];
    }

    private static ImmutableArray<ImmutableArray<int>> MapPath(ImmutableArray<Vector4I> path)
    {
        var values = new ImmutableArray<int>[path.Length];
        for (var index = 0; index < path.Length; index++)
        {
            var node = path[index];
            values[index] = ImmutableArray.Create(node.A, node.B, node.C, node.P);
        }

        return [.. values];
    }

    private static ImmutableArray<int> MapStaticTrackerMapping(
        ImmutableArray<CompiledStaticTracker> trackers,
        int positionCount,
        int particleCount)
    {
        var mapping = new int[positionCount * particleCount];
        Array.Fill(mapping, -1);
        foreach (var tracker in trackers)
        {
            if (tracker.PositionIndex < 0 || tracker.PositionIndex >= positionCount ||
                tracker.ParticleIndex < 0 || tracker.ParticleIndex >= particleCount)
            {
                throw new InvalidOperationException("A static tracker is outside the position/particle range.");
            }

            mapping[(tracker.PositionIndex * particleCount) + tracker.ParticleIndex] = tracker.Index;
        }

        return [.. mapping];
    }

    private static ImmutableArray<int> MapGlobalTrackerMapping(
        ImmutableArray<CompiledGlobalTracker> trackers,
        int collectionCount,
        int particleCount)
    {
        var mapping = new int[collectionCount * particleCount];
        Array.Fill(mapping, -1);
        foreach (var tracker in trackers)
        {
            if (tracker.CollectionIndex < 0 || tracker.CollectionIndex >= collectionCount ||
                tracker.ParticleIndex < 0 || tracker.ParticleIndex >= particleCount)
            {
                throw new InvalidOperationException("A global tracker is outside the collection/particle range.");
            }

            mapping[(tracker.CollectionIndex * particleCount) + tracker.ParticleIndex] = tracker.Index;
        }

        return [.. mapping];
    }
}
