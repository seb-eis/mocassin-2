// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Numerics;
using Mocassin.Contracts.Packages;
using Mocassin.Contracts.Parameterization;

namespace Mocassin.Results.Packages.Internal;

/// <summary>
/// Payload-only invariant checks shared by the verifier.
/// </summary>
internal static class PackageVerificationChecks
{
    /// <summary>The maximum path-node count representable by the six-bit state codes.</summary>
    public const int MaxTrackerNodes = 10;

    /// <summary>Rebuilds the applied parameterization values from a model payload.</summary>
    public static ParameterizationValues ValuesFrom(ModelPayload payload) =>
        new(
            [.. payload.Energy.PairTables.Select(static table => table.Energies)],
            [.. payload.Energy.ClusterTables.Select(static table => table.Energies)],
            payload.Energy.DefectBackground,
            [.. payload.Transitions.JumpRules.Select(static rule => rule.FrequencyHz)]);

    /// <summary>Returns whether a string is 64 lowercase hexadecimal characters.</summary>
    public static bool IsHex64(string value) =>
        value.Length == 64 && value.All(static character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');

    /// <summary>Validates the model-payload invariants that are provable from the payload alone.</summary>
    public static void ValidateModel(ModelPayload payload, List<string> errors)
    {
        var particleCount = checked((int)payload.Particle.Count);
        if (particleCount <= 0)
        {
            errors.Add("The model has no particles.");
            return;
        }

        if (payload.Particle.Charges.Length != particleCount ||
            payload.Particle.Symbols.Length != particleCount ||
            payload.Particle.IsVacancy.Length != particleCount)
        {
            errors.Add("The particle arrays do not match the declared particle count.");
        }

        var positionCount = checked((int)payload.Structure.PositionCount);
        if (payload.Structure.Positions.Length != positionCount)
        {
            errors.Add("The position array length does not match position_count.");
        }

        if (payload.Structure.Environments.Length != positionCount)
        {
            errors.Add("The environments array length does not match position_count.");
        }

        if (payload.Structure.SiteCount > positionCount)
        {
            errors.Add("site_count exceeds position_count.");
        }

        for (var index = 0; index < payload.Structure.Positions.Length; index++)
        {
            var position = payload.Structure.Positions[index];
            if (position.Vector4.Length != 4 || position.Vector4[3] != index)
            {
                errors.Add($"Position {index} has an invalid vector4 encoding.");
            }

            if (position.SiteIndex >= payload.Structure.SiteCount)
            {
                errors.Add($"Position {index} references a site out of range.");
            }

            if (!IsStrictlyAscending(position.ParticleIds) ||
                position.ParticleIds.Any(particle => particle >= particleCount))
            {
                errors.Add($"Position {index} has invalid particle_ids.");
            }
        }

        foreach (var (environment, index) in payload.Structure.Environments.Select((value, index) => (value, index)))
        {
            var pairKeys = new HashSet<(uint Target, int A, int B, int C)>();
            foreach (var pair in environment.Pairs)
            {
                if (pair.TargetPosition >= positionCount ||
                    pair.Offset.Length != 3 ||
                    pair.EnergyId >= payload.Energy.PairTables.Length)
                {
                    errors.Add($"Environment {index} has a pair link out of range.");
                    continue;
                }

                if (!pairKeys.Add((
                        pair.TargetPosition,
                        pair.Offset[0],
                        pair.Offset[1],
                        pair.Offset[2])))
                {
                    errors.Add($"Environment {index} has a duplicate (target_position, offset) pair link.");
                }
            }

            foreach (var cluster in environment.Clusters)
            {
                if (cluster.BytePosition >= payload.Structure.SiteCount ||
                    cluster.ClusterId >= payload.Energy.ClusterTables.Length)
                {
                    errors.Add($"Environment {index} has a cluster link out of range.");
                }
            }

            if (!IsStrictlyAscending(environment.UpdateParticleIds) ||
                environment.UpdateParticleIds.Any(particle => particle >= particleCount))
            {
                errors.Add($"Environment {index} has invalid update_particle_ids.");
            }
        }

        if (payload.Structure.InteractionRange.Length != 4)
        {
            errors.Add("interaction_range must contain four values.");
        }

        if (payload.Structure.StaticTrackersPerCell != (uint)(positionCount * (particleCount - 1)))
        {
            errors.Add("static_trackers_per_cell does not match position_count × (particle_count − 1).");
        }

        foreach (var table in payload.Energy.PairTables)
        {
            if (table.Energies.Length != particleCount * particleCount)
            {
                errors.Add("A pair table length does not match particle_count².");
            }

            if (table.Energies.Any(static value => !double.IsFinite(value)))
            {
                errors.Add("A pair table contains NaN or Inf.");
            }
        }

        foreach (var table in payload.Energy.ClusterTables)
        {
            if (table.OccupationCodes.Length != table.Energies.Length)
            {
                errors.Add("A cluster table's occupation and energy lengths disagree.");
            }

            if (table.Energies.Any(static value => !double.IsFinite(value)))
            {
                errors.Add("A cluster table contains NaN or Inf.");
            }

            if (table.ParticleTableMapping.Any(particle => particle >= particleCount))
            {
                errors.Add("A cluster particle-table mapping is out of range.");
            }
        }

        if (payload.Energy.DefectBackground.Length != positionCount * particleCount)
        {
            errors.Add("defect_background length does not match position_count × particle_count.");
        }

        if (payload.Energy.DefectBackground.Any(static value => !double.IsFinite(value)))
        {
            errors.Add("defect_background contains NaN or Inf.");
        }

        var selectionCells = positionCount * particleCount;
        if (payload.Transitions.JumpCount.Length != selectionCells ||
            payload.Transitions.StaticTrackerMapping.Length != selectionCells)
        {
            errors.Add("jump_count or static_tracker_mapping length does not match position_count × particle_count.");
        }

        ulong mappingLength = 0;
        foreach (var count in payload.Transitions.JumpCount)
        {
            mappingLength += count;
        }

        if (mappingLength != (ulong)payload.Transitions.JumpMapping.Length)
        {
            errors.Add("jump_mapping length does not equal the sum of jump_count.");
        }

        if (payload.Transitions.JumpMapping.Any(direction => direction >= payload.Transitions.JumpDirections.Length))
        {
            errors.Add("jump_mapping references a direction out of range.");
        }

        if (payload.Transitions.StaticTrackerMapping.Any(tracker =>
                tracker < -1 || tracker >= payload.Structure.StaticTrackersPerCell))
        {
            errors.Add("static_tracker_mapping has a tracker out of range.");
        }

        if (payload.Transitions.GlobalTrackerMapping.Length !=
            payload.Transitions.JumpCollections.Length * particleCount)
        {
            errors.Add("global_tracker_mapping length does not match collection_count × particle_count.");
        }

        if (payload.Transitions.GlobalTrackerMapping.Any(tracker =>
                tracker < -1 || tracker >= payload.Structure.GlobalTrackerCount))
        {
            errors.Add("global_tracker_mapping has a tracker out of range.");
        }

        foreach (var collection in payload.Transitions.JumpCollections)
        {
            if (collection.RuleIds.Any(rule => rule >= payload.Transitions.JumpRules.Length) ||
                collection.DirectionIds.Any(direction => direction >= payload.Transitions.JumpDirections.Length))
            {
                errors.Add("A jump collection references a rule or direction out of range.");
            }
        }

        foreach (var direction in payload.Transitions.JumpDirections)
        {
            if (direction.Path.Length != direction.Movement.Length ||
                direction.PathVector.Length != 3 ||
                !double.IsFinite(direction.Length))
            {
                errors.Add("A jump direction has inconsistent path or movement data.");
            }
        }

        foreach (var rule in payload.Transitions.JumpRules)
        {
            if (rule.CollectionId >= payload.Transitions.JumpCollections.Length ||
                !double.IsFinite(rule.FrequencyHz) ||
                rule.ChargeTransportVector.Length != 3)
            {
                errors.Add("A jump rule has invalid state, frequency, or charge-transport data.");
            }

            if (rule.StaticCorrection is { Length: not 3 })
            {
                errors.Add("A jump rule static correction is not a three-vector.");
            }

            if (rule.TrackerOrder.IsDefaultOrEmpty || rule.TrackerOrder.Length > MaxTrackerNodes)
            {
                errors.Add("A jump rule tracker_order is empty or exceeds the documented node bound.");
            }
            else if (rule.TrackerOrder.Any(tracker => tracker != 0xFF && tracker >= payload.Structure.GlobalTrackerCount))
            {
                errors.Add("A jump rule tracker_order references a tracker out of range.");
            }
        }

        foreach (var simulation in payload.Simulations)
        {
            if (simulation.JumpCollectionIds.Any(collection =>
                    collection >= payload.Transitions.JumpCollections.Length) ||
                simulation.TrackerModels.GlobalTrackerIndices.Any(tracker =>
                    tracker >= payload.Structure.GlobalTrackerCount) ||
                simulation.TrackerModels.StaticTrackerIndices.Any(tracker =>
                    tracker >= payload.Structure.StaticTrackersPerCell) ||
                simulation.DefaultSettings.ElectricFieldDirection.Length != 3)
            {
                errors.Add("A simulation has an index or field direction out of range.");
            }
        }
    }

    /// <summary>Validates a lattice payload against its referenced model.</summary>
    public static void ValidateLattice(
        LatticePayload payload,
        int particleCount,
        List<string> errors)
    {
        if (payload.LatticeSize.IsDefault || payload.LatticeSize.Length != 3)
        {
            errors.Add("The lattice size does not contain exactly three values.");
            return;
        }

        var product = (ulong)payload.LatticeSize[0] * payload.LatticeSize[1] * payload.LatticeSize[2] * payload.PositionCount;
        if (product > int.MaxValue)
        {
            errors.Add("The linearized lattice length exceeds the signed 32-bit index limit.");
            return;
        }

        if (payload.Lattice.Length != (int)product)
        {
            errors.Add("The lattice length does not equal a × b × c × position_count.");
        }

        if (payload.Lattice.Any(particle => particle >= particleCount))
        {
            errors.Add("A lattice particle index is out of range.");
        }
    }

    /// <summary>Validates the solver flag word and its kind-conditional rules.</summary>
    public static void ValidateFlags(JobPayload payload, List<string> errors)
    {
        var kindBits = payload.Flags & 0xC0u;
        if (BitOperations.PopCount(kindBits) != 1)
        {
            errors.Add("flags does not contain exactly one kind bit.");
        }

        var kmc = (payload.Flags & 0x40u) != 0;
        var mmc = (payload.Flags & 0x80u) != 0;
        if ((payload.Kind == "kmc" && !kmc) || (payload.Kind == "mmc" && !mmc))
        {
            errors.Add("The flags kind bit disagrees with the payload kind.");
        }

        var usePreRun = (payload.Flags & 0x20u) != 0;
        var useDualDof = (payload.Flags & 0x10u) != 0;
        if (kmc && usePreRun != (payload.PreRunMcsp > 0))
        {
            errors.Add("The KMC usePrerun bit disagrees with pre_run_mcsp.");
        }

        if (mmc && (usePreRun || payload.PreRunMcsp != 0 || useDualDof))
        {
            errors.Add("An MMC job has a pre-run or dual-DOF bit set.");
        }
    }

    private static bool IsStrictlyAscending(ImmutableArray<byte> values)
    {
        for (var index = 1; index < values.Length; index++)
        {
            if (values[index] <= values[index - 1])
            {
                return false;
            }
        }

        return true;
    }
}
