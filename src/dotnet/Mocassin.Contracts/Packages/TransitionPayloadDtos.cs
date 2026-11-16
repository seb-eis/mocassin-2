// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using MessagePack;

namespace Mocassin.Contracts.Packages;

/// <summary>
/// The compiled transition section of a model payload.
/// </summary>
/// <remarks>
/// <see cref="JumpCount"/> is in <c>[position][particle]</c> order and <see cref="JumpMapping"/> is the flat
/// concatenation in <c>[position][particle][relative]</c> order, so the reader derives the offsets from the
/// prefix sums of <see cref="JumpCount"/>. The tracker mappings are definitions only; Step 18 owns the values.
/// </remarks>
/// <param name="JumpCollections">The compiled jump collections in dense index order.</param>
/// <param name="JumpDirections">The compiled direction representatives in dense index order.</param>
/// <param name="JumpRules">The compiled rules in dense rule order.</param>
/// <param name="JumpCount">The selectable direction count per <c>(position, particle)</c>.</param>
/// <param name="JumpMapping">The selectable direction indices in relative-slot order.</param>
/// <param name="StaticTrackerMapping">
/// The dense <c>[position][particle] → tracker offset</c> array with <c>-1</c> for "none".
/// </param>
/// <param name="GlobalTrackerMapping">
/// The dense <c>[collection][particle] → tracker index</c> array with <c>-1</c> for "none".
/// </param>
[MessagePackObject]
public sealed record TransitionPayload(
    [property: Key("jump_collections")] ImmutableArray<JumpCollectionPayload> JumpCollections,
    [property: Key("jump_directions")] ImmutableArray<JumpDirectionPayload> JumpDirections,
    [property: Key("jump_rules")] ImmutableArray<JumpRulePayload> JumpRules,
    [property: Key("jump_count")] ImmutableArray<uint> JumpCount,
    [property: Key("jump_mapping")] ImmutableArray<uint> JumpMapping,
    [property: Key("static_tracker_mapping")] ImmutableArray<int> StaticTrackerMapping,
    [property: Key("global_tracker_mapping")] ImmutableArray<int> GlobalTrackerMapping);

/// <summary>
/// One compiled jump collection.
/// </summary>
/// <remarks>
/// A collection is identified by its dense array index and its authored <see cref="TransitionId"/>; it has no
/// separate stable <c>object_id</c>.
/// </remarks>
/// <param name="Kind">The simulation kind that owns the collection (<c>kmc</c> or <c>mmc</c>).</param>
/// <param name="TransitionId">The authored transition identifier.</param>
/// <param name="MobileParticleMask">The compiled mobile-particle mask.</param>
/// <param name="RuleIds">The collection rule indices.</param>
/// <param name="DirectionIds">The collection direction indices.</param>
[MessagePackObject]
public sealed record JumpCollectionPayload(
    [property: Key("kind")] string Kind,
    [property: Key("transition_id")] string TransitionId,
    [property: Key("mobile_particle_mask")] ulong MobileParticleMask,
    [property: Key("rule_ids")] ImmutableArray<uint> RuleIds,
    [property: Key("direction_ids")] ImmutableArray<uint> DirectionIds);

/// <summary>
/// One compiled jump-direction representative.
/// </summary>
/// <param name="ObjectId">The stable compiled direction identity.</param>
/// <param name="PositionId">The representative start canonical P1 index.</param>
/// <param name="CollectionId">The owning collection index.</param>
/// <param name="Path">
/// The per-node relative encodings <c>[cellA, cellB, cellC, P]</c>; node zero is the start.
/// </param>
/// <param name="Movement">The per-node fractional movement vectors.</param>
/// <param name="PathVector">The Cartesian net path vector in ångström.</param>
/// <param name="Length">The magnitude of <see cref="PathVector"/> in ångström.</param>
/// <param name="IsInverse">Whether the direction is an automatically generated inverse.</param>
[MessagePackObject]
public sealed record JumpDirectionPayload(
    [property: Key("object_id")] string ObjectId,
    [property: Key("position_id")] uint PositionId,
    [property: Key("collection_id")] uint CollectionId,
    [property: Key("path")] ImmutableArray<ImmutableArray<int>> Path,
    [property: Key("movement")] ImmutableArray<ImmutableArray<double>> Movement,
    [property: Key("path_vector")] ImmutableArray<double> PathVector,
    [property: Key("length")] double Length,
    [property: Key("is_inverse")] bool IsInverse);

/// <summary>
/// One compiled jump rule.
/// </summary>
/// <remarks>
/// <see cref="FrequencyHz"/> is the absolute Step 09 applied value and the authoritative model frequency. Step 16
/// derives per-job normalization or pre-check factors from it; no factor is persisted here.
/// </remarks>
/// <param name="RuleId">The dense persisted rule index.</param>
/// <param name="CollectionId">The owning collection index.</param>
/// <param name="State0">The state-0 code, six bits per path node.</param>
/// <param name="State1">The state-1 code; zero and ignored for MMC.</param>
/// <param name="State2">The state-2 code.</param>
/// <param name="FrequencyHz">The absolute applied attempt frequency in hertz.</param>
/// <param name="ChargeTransportVector">
/// The particle-identity charge transport vector <c>Σ q_p · (x_p(S2) − x_p(S0))</c> in e·Å.
/// </param>
/// <param name="StaticCorrection">The optional static S2 correction; <see langword="null"/> in v1.</param>
/// <param name="TrackerOrder">
/// One entry per path node: the global tracker index, or <c>0xFF</c> for a non-mobile node. Variable-length, not
/// a fixed eight-entry array.
/// </param>
[MessagePackObject]
public sealed record JumpRulePayload(
    [property: Key("rule_id")] uint RuleId,
    [property: Key("collection_id")] uint CollectionId,
    [property: Key("state0")] ulong State0,
    [property: Key("state1")] ulong State1,
    [property: Key("state2")] ulong State2,
    [property: Key("frequency_hz")] double FrequencyHz,
    [property: Key("charge_transport_vector")] ImmutableArray<double> ChargeTransportVector,
    [property: Key("static_correction")] ImmutableArray<double>? StaticCorrection,
    [property: Key("tracker_order")] ImmutableArray<byte> TrackerOrder);
