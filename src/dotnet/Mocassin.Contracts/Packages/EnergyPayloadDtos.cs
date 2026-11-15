// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using MessagePack;

namespace Mocassin.Contracts.Packages;

/// <summary>
/// The applied energy values of a model payload.
/// </summary>
/// <remarks>
/// All values are the applied Step 09 values in compiled table/occupation order, never the compiler's zero
/// defaults.
/// </remarks>
/// <param name="PairTables">The applied pair-energy tables in ascending compiled table order.</param>
/// <param name="ClusterTables">The applied cluster-energy tables in ascending compiled table order.</param>
/// <param name="DefectBackground">
/// The dense <c>[position][particle]</c> site-energy background in eV, row-major, with void at column zero.
/// </param>
[MessagePackObject]
public sealed record EnergyPayload(
    [property: Key("pair_tables")] ImmutableArray<PairTablePayload> PairTables,
    [property: Key("cluster_tables")] ImmutableArray<ClusterTablePayload> ClusterTables,
    [property: Key("defect_background")] ImmutableArray<double> DefectBackground);

/// <summary>
/// One applied pair-energy table.
/// </summary>
/// <param name="ObjectId">The shared stable pair-energy identity.</param>
/// <param name="Energies">
/// The <c>particle_count × particle_count</c> row-major table indexed by
/// <c>[centerParticle][partnerParticle]</c>.
/// </param>
[MessagePackObject]
public sealed record PairTablePayload(
    [property: Key("object_id")] string ObjectId,
    [property: Key("energies")] ImmutableArray<double> Energies);

/// <summary>
/// One applied cluster-energy table.
/// </summary>
/// <param name="ObjectId">The stable cluster-energy identity.</param>
/// <param name="OccupationCodes">The canonical occupation codes, sorted ascending.</param>
/// <param name="Energies">The applied energy per occupation code.</param>
/// <param name="ParticleTableMapping">
/// One row per allowed centre particle in ascending compiled particle index.
/// </param>
[MessagePackObject]
public sealed record ClusterTablePayload(
    [property: Key("object_id")] string ObjectId,
    [property: Key("occupation_codes")] ImmutableArray<ulong> OccupationCodes,
    [property: Key("energies")] ImmutableArray<double> Energies,
    [property: Key("particle_table_mapping")] ImmutableArray<byte> ParticleTableMapping);
