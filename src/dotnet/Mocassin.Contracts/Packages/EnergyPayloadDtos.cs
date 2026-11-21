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
/// The raw-code resolution map of one cluster table.
/// </summary>
/// <remarks>
/// The map is written by the compiler and verifies the writer's canonical reduction. It is a stored
/// serialization of code data the model signature already binds, not a new signed concept.
/// </remarks>
/// <param name="ExpandedCodes">
/// Every raw slot-tuple code the writer enumerated, strictly ascending. The sequence is a superset of the
/// enumerated set: it may also contain orbit codes whose particles are not allowed at the corresponding member
/// slot. Such codes are never produced by a valid lattice.
/// </param>
/// <param name="CanonicalIndices">
/// The index into <see cref="ClusterTablePayload.OccupationCodes"/> and
/// <see cref="ClusterTablePayload.Energies"/> for the orbit of the code at the same position. The two arrays
/// have equal length.
/// </param>
[MessagePackObject]
public sealed record CodeExpansionPayload(
    [property: Key("expanded_codes")] ImmutableArray<ulong> ExpandedCodes,
    [property: Key("canonical_indices")] ImmutableArray<uint> CanonicalIndices);

/// <summary>
/// One applied cluster-energy table.
/// </summary>
/// <param name="ObjectId">The stable cluster-energy identity.</param>
/// <param name="OccupationCodes">The canonical occupation codes, sorted ascending.</param>
/// <param name="CodeExpansions">
/// The optional raw-code resolution map. <see langword="null"/> means every orbit is a singleton, so
/// <paramref name="OccupationCodes"/> is already complete. An absent key in a package written before the
/// member existed has the same meaning but fails closed on a non-canonical raw tuple in a non-singleton table.
/// </param>
/// <param name="Energies">The applied energy per occupation code.</param>
/// <param name="ParticleTableMapping">
/// One row per allowed centre particle in ascending compiled particle index.
/// </param>
[MessagePackObject]
[method: SerializationConstructor]
public sealed record ClusterTablePayload(
    [property: Key("object_id")] string ObjectId,
    [property: Key("occupation_codes")] ImmutableArray<ulong> OccupationCodes,
    [property: Key("code_expansions")] CodeExpansionPayload? CodeExpansions,
    [property: Key("energies")] ImmutableArray<double> Energies,
    [property: Key("particle_table_mapping")] ImmutableArray<byte> ParticleTableMapping)
{
    /// <summary>
    /// Creates a singleton-table payload without a code expansion map.
    /// </summary>
    /// <param name="objectId">The stable cluster-energy identity.</param>
    /// <param name="occupationCodes">The canonical occupation codes, sorted ascending.</param>
    /// <param name="energies">The applied energy per occupation code.</param>
    /// <param name="particleTableMapping">One row per allowed centre particle.</param>
    public ClusterTablePayload(
        string objectId,
        ImmutableArray<ulong> occupationCodes,
        ImmutableArray<double> energies,
        ImmutableArray<byte> particleTableMapping)
        : this(objectId, occupationCodes, null, energies, particleTableMapping)
    {
    }

    /// <summary>
    /// Deconstructs the four members that predate the code expansion map, discarding it.
    /// </summary>
    /// <param name="objectId">The stable cluster-energy identity.</param>
    /// <param name="occupationCodes">The canonical occupation codes.</param>
    /// <param name="energies">The applied energy per occupation code.</param>
    /// <param name="particleTableMapping">One row per allowed centre particle.</param>
    public void Deconstruct(
        out string objectId,
        out ImmutableArray<ulong> occupationCodes,
        out ImmutableArray<double> energies,
        out ImmutableArray<byte> particleTableMapping)
    {
        objectId = ObjectId;
        occupationCodes = OccupationCodes;
        energies = Energies;
        particleTableMapping = ParticleTableMapping;
    }
}
