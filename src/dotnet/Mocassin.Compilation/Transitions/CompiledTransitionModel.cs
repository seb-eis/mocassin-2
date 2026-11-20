// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Transitions;
using Mocassin.Compilation.Simulations;

namespace Mocassin.Compilation.Transitions;

/// <summary>
/// One compiled jump collection shared by every simulation that uses the transition.
/// </summary>
/// <param name="Index">The dense collection index.</param>
/// <param name="Transition">The compiled transition reference.</param>
/// <param name="Kind">The simulation kind that owns the collection.</param>
/// <param name="MobileParticleMask">The compiled mobile-particle mask.</param>
/// <param name="DirectionIndices">The collection direction indices.</param>
/// <param name="RuleIndices">The collection rule indices.</param>
public sealed record CompiledJumpCollection(
    int Index,
    TransitionRef Transition,
    SimulationKind Kind,
    ulong MobileParticleMask,
    ImmutableArray<int> DirectionIndices,
    ImmutableArray<int> RuleIndices);

/// <summary>
/// One compiled jump direction orbit representative.
/// </summary>
/// <param name="Index">The dense direction index.</param>
/// <param name="ObjectId">The stable direction identity.</param>
/// <param name="CollectionIndex">The owning collection index.</param>
/// <param name="PositionId">The representative start P1 index.</param>
/// <param name="Path">The relative node encodings; node <c>0</c> is <c>(0, 0, 0, PositionId)</c>.</param>
/// <param name="Movement">The representative per-node fractional movement.</param>
/// <param name="PathVector">The representative Cartesian path vector in ångström.</param>
/// <param name="PathLength">The representative Cartesian path length in ångström.</param>
/// <param name="IsInverse">Whether the direction is an automatically generated inverse.</param>
/// <param name="EquivalentStartPositions">Every symmetry-equivalent start P1 index.</param>
public sealed record CompiledJumpDirection(
    int Index,
    string ObjectId,
    int CollectionIndex,
    int PositionId,
    ImmutableArray<Vector4I> Path,
    ImmutableArray<Fractional3> Movement,
    Cartesian3 PathVector,
    double PathLength,
    bool IsInverse,
    ImmutableArray<int> EquivalentStartPositions);

/// <summary>
/// One compiled jump rule.
/// </summary>
/// <param name="Index">The dense persisted rule index.</param>
/// <param name="RuleGeometryId">The stable rule identity.</param>
/// <param name="CollectionIndex">The owning collection index.</param>
/// <param name="DirectionIndex">The representative direction index.</param>
/// <param name="DirectionIndices">Every merged direction index with the same outcome.</param>
/// <param name="State0">The state-0 code, six bits per node.</param>
/// <param name="State1">The state-1 code, six bits per node.</param>
/// <param name="State2">The state-2 code, six bits per node.</param>
/// <param name="Movement">The per-node fractional movement.</param>
/// <param name="ChargeTransportVector">
/// The node-wise endpoint-moment difference in e·Å:
/// <c>Σ_node (q(state2[node]) − q(state0[node])) · x(node)</c>.
/// </param>
/// <param name="StaticCorrection">The optional static S2 correction; <see langword="null"/> in v1.</param>
/// <param name="TrackerOrder">The per-node global tracker index, or <c>0xFF</c> for a non-mobile node.</param>
/// <param name="IsInverse">Whether the rule was generated as an automatic inverse.</param>
/// <param name="FrequencyHz">The default attempt frequency in hertz.</param>
public sealed record CompiledJumpRule(
    int Index,
    string RuleGeometryId,
    int CollectionIndex,
    int DirectionIndex,
    ImmutableArray<int> DirectionIndices,
    ulong State0,
    ulong State1,
    ulong State2,
    ImmutableArray<Fractional3> Movement,
    Cartesian3 ChargeTransportVector,
    Cartesian3? StaticCorrection,
    ImmutableArray<byte> TrackerOrder,
    bool IsInverse,
    double FrequencyHz)
{
    /// <summary>
    /// The default attempt frequency used until Step 09 applies parameterized values.
    /// </summary>
    public const double DefaultFrequencyHz = 1.0e13;
}

/// <summary>
/// The compiled transition model: collections, directions, rules, and jump selection tables.
/// </summary>
/// <param name="Collections">The collections in transition order.</param>
/// <param name="Directions">The directions in collection and relative-path order.</param>
/// <param name="Rules">The rules in collection and state-triplet order.</param>
/// <param name="JumpCounts">The selectable direction count per <c>(position, particle)</c>.</param>
/// <param name="JumpOffsets">The prefix offsets into <paramref name="JumpMappings"/>.</param>
/// <param name="JumpMappings">The selectable direction indices in relative-slot order.</param>
/// <param name="ParticleCount">The compiled particle count.</param>
/// <param name="PositionCount">The canonical P1 position count.</param>
public sealed record CompiledTransitionModel(
    ImmutableArray<CompiledJumpCollection> Collections,
    ImmutableArray<CompiledJumpDirection> Directions,
    ImmutableArray<CompiledJumpRule> Rules,
    ImmutableArray<int> JumpCounts,
    ImmutableArray<int> JumpOffsets,
    ImmutableArray<int> JumpMappings,
    int ParticleCount,
    int PositionCount)
{
    /// <summary>
    /// Gets the empty compiled transition model.
    /// </summary>
    public static CompiledTransitionModel Empty { get; } = new([], [], [], [], [], [], 1, 0);

    /// <summary>
    /// Gets the KMC global tracker definitions.
    /// </summary>
    public ImmutableArray<CompiledGlobalTracker> GlobalTrackers { get; init; } = [];

    /// <summary>
    /// Gets the KMC static tracker definitions.
    /// </summary>
    public ImmutableArray<CompiledStaticTracker> StaticTrackers { get; init; } = [];

    /// <summary>Gets the number of combinations rejected by mass conservation.</summary>
    public int RejectedByMass { get; init; }

    /// <summary>Gets the number of combinations rejected by charge conservation.</summary>
    public int RejectedByCharge { get; init; }

    /// <summary>Gets the number of combinations rejected by invalid state roles.</summary>
    public int RejectedByInvalidState { get; init; }

    /// <summary>
    /// Gets the number of compiled collections.
    /// </summary>
    public int CollectionCount => Collections.Length;

    /// <summary>
    /// Gets the number of compiled directions.
    /// </summary>
    public int DirectionCount => Directions.Length;

    /// <summary>
    /// Gets the number of compiled rules.
    /// </summary>
    public int RuleCount => Rules.Length;

    /// <summary>
    /// Gets the number of automatically generated inverse rules.
    /// </summary>
    public int InverseRuleCount => Rules.Count(static rule => rule.IsInverse);
}
