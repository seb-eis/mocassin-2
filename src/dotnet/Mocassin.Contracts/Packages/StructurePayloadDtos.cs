// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using MessagePack;

namespace Mocassin.Contracts.Packages;

/// <summary>
/// The compiled structure section of a model payload.
/// </summary>
/// <remarks>
/// Positions are in ascending canonical <c>P</c> order. Each environment pair entry is one periodic image; see
/// <see cref="PairLinkPayload"/>. The linear lattice index is
/// <c>P + cellC*PositionCount + cellB*PositionCount*Ccount + cellA*PositionCount*Ccount*Bcount</c>, validated in
/// 64-bit and bounded by the signed 32-bit index limit.
/// </remarks>
/// <param name="CellVectorsAng">
/// The Cartesian <c>a</c>, <c>b</c>, and <c>c</c> cell vectors in ångström, so
/// <c>fractional → Cartesian</c> is <c>fa·a + fb·b + fc·c</c>.
/// </param>
/// <param name="Positions">The canonical P1 positions in ascending <c>P</c> order.</param>
/// <param name="PositionCount">The number of canonical P1 positions per unit cell.</param>
/// <param name="SiteCount">The number of user reference sites.</param>
/// <param name="Environments">The interaction environments indexed by compiled environment index.</param>
/// <param name="InteractionRange">The MMC local-update cube <c>[maxCellA, maxCellB, maxCellC, 0]</c>.</param>
/// <param name="StaticTrackersPerCell">
/// <c>position_count × (particle_count − 1)</c>; void has no static tracker.
/// </param>
/// <param name="GlobalTrackerCount">The number of KMC global tracker definitions.</param>
[MessagePackObject]
public sealed record StructurePayload(
    [property: Key("cell_vectors_ang")] ImmutableArray<ImmutableArray<double>> CellVectorsAng,
    [property: Key("positions")] ImmutableArray<PositionPayload> Positions,
    [property: Key("position_count")] uint PositionCount,
    [property: Key("site_count")] uint SiteCount,
    [property: Key("environments")] ImmutableArray<EnvironmentPayload> Environments,
    [property: Key("interaction_range")] ImmutableArray<int> InteractionRange,
    [property: Key("static_trackers_per_cell")] uint StaticTrackersPerCell,
    [property: Key("global_tracker_count")] uint GlobalTrackerCount);

/// <summary>
/// One canonical P1 position of a model payload.
/// </summary>
/// <param name="Vector4">
/// The full encoded position <c>[cellA, cellB, cellC, P]</c>; base positions are <c>[0, 0, 0, P]</c>.
/// </param>
/// <param name="SiteIndex">The owning reference-site index; not the <c>P</c> index.</param>
/// <param name="Stable">Whether the position belongs to a stable reference site.</param>
/// <param name="ParticleIds">
/// The ascending compiled particle indices set in the allowed-particle mask, including void (<c>0</c>) when the
/// position may be empty.
/// </param>
[MessagePackObject]
public sealed record PositionPayload(
    [property: Key("vector4")] ImmutableArray<int> Vector4,
    [property: Key("site_index")] uint SiteIndex,
    [property: Key("stable")] bool Stable,
    [property: Key("particle_ids")] ImmutableArray<byte> ParticleIds);

/// <summary>
/// The interaction environment of one compiled environment index.
/// </summary>
/// <param name="SelectionParticleMask">The selectable compiled particle mask.</param>
/// <param name="Pairs">The periodic pair images linked from positions in this environment.</param>
/// <param name="Clusters">The cluster links used for incremental occupation-code updates.</param>
/// <param name="UpdateParticleIds">The compiled particle indices updated through this environment.</param>
[MessagePackObject]
public sealed record EnvironmentPayload(
    [property: Key("selection_particle_mask")] ulong SelectionParticleMask,
    [property: Key("pairs")] ImmutableArray<PairLinkPayload> Pairs,
    [property: Key("clusters")] ImmutableArray<ClusterLinkPayload> Clusters,
    [property: Key("update_particle_ids")] ImmutableArray<byte> UpdateParticleIds);

/// <summary>
/// One periodic partner image linked from a position.
/// </summary>
/// <remarks>
/// There is one entry per periodic image, not one per symmetry shell. The runtime target is the centre's
/// supercell cell plus <see cref="Offset"/>, wrapped component-wise into the supercell, at
/// <see cref="TargetPosition"/>. <see cref="Offset"/> is required because the same target position occurs once
/// per image and images can use different pair tables.
/// </remarks>
/// <param name="TargetPosition">The partner canonical P1 index.</param>
/// <param name="Offset">The relative cell offset <c>[cellA, cellB, cellC]</c> from the centre position.</param>
/// <param name="EnergyId">The shared pair-table index.</param>
[MessagePackObject]
public sealed record PairLinkPayload(
    [property: Key("target_position")] uint TargetPosition,
    [property: Key("offset")] ImmutableArray<int> Offset,
    [property: Key("energy_id")] uint EnergyId);

/// <summary>
/// One cluster table link from a position.
/// </summary>
/// <param name="ClusterId">The shared cluster-table index.</param>
/// <param name="BytePosition">The position's site slot in the occupation code; <c>0</c> is the centre.</param>
[MessagePackObject]
public sealed record ClusterLinkPayload(
    [property: Key("cluster_id")] uint ClusterId,
    [property: Key("byte_position")] byte BytePosition);
