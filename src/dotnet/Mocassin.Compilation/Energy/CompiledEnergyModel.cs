// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;

namespace Mocassin.Compilation.Energy;

/// <summary>
/// One directed pair geometry.
/// </summary>
/// <param name="GeometryId">The stable directed geometry identifier.</param>
/// <param name="EnergyId">The shared energy-table identity.</param>
/// <param name="PairTableIndex">The shared pair table index.</param>
/// <param name="CenterSiteIndex">The centre reference site index.</param>
/// <param name="CenterSiteId">The centre reference site identifier.</param>
/// <param name="CenterP">The centre canonical P1 index.</param>
/// <param name="PartnerSiteIndex">The partner reference site index.</param>
/// <param name="PartnerSiteId">The partner reference site identifier.</param>
/// <param name="PartnerP">The partner canonical P1 index.</param>
/// <param name="Offset">The representative relative cell offset and partner P.</param>
/// <param name="DistanceBucketFm">The representative distance bucket in femtometres.</param>
/// <param name="DistanceAngstrom">The representative distance in ångström.</param>
/// <param name="IsChiral">Whether the reciprocal direction is a distinct chiral partner.</param>
/// <param name="ChiralPartnerGeometryId">The reciprocal chiral geometry identifier, when present.</param>
public sealed record CompiledPairGeometry(
    string GeometryId,
    string EnergyId,
    int PairTableIndex,
    int CenterSiteIndex,
    CellSiteId CenterSiteId,
    int CenterP,
    int PartnerSiteIndex,
    CellSiteId PartnerSiteId,
    int PartnerP,
    Vector4I Offset,
    int DistanceBucketFm,
    double DistanceAngstrom,
    bool IsChiral,
    string? ChiralPartnerGeometryId);

/// <summary>
/// One shared pair energy table. Values are row-major <c>[centreParticle][partnerParticle]</c> in eV.
/// </summary>
/// <param name="Index">The dense pair table index.</param>
/// <param name="EnergyId">The shared energy identity.</param>
/// <param name="ParticleCount">The number of compiled particles.</param>
/// <param name="Values">The row-major energy values; all zero until Step 09 applies parameter values.</param>
public sealed record CompiledPairTable(
    int Index,
    string EnergyId,
    int ParticleCount,
    ImmutableArray<double> Values)
{
    /// <summary>
    /// Gets the energy value for one centre/partner particle combination.
    /// </summary>
    /// <param name="centerParticle">The compiled centre particle index.</param>
    /// <param name="partnerParticle">The compiled partner particle index.</param>
    /// <returns>The energy in eV.</returns>
    public double GetEnergy(int centerParticle, int partnerParticle) =>
        Values[(centerParticle * ParticleCount) + partnerParticle];
}

/// <summary>
/// The compiled pair-energy model.
/// </summary>
/// <param name="PairGeometries">The directed pair geometries in canonical shell order.</param>
/// <param name="PairTables">The shared pair tables in canonical shell order.</param>
public sealed record CompiledEnergyModel(
    ImmutableArray<CompiledPairGeometry> PairGeometries,
    ImmutableArray<CompiledPairTable> PairTables)
{
    /// <summary>
    /// Gets the empty compiled energy model.
    /// </summary>
    public static CompiledEnergyModel Empty { get; } = new([], []);

    /// <summary>
    /// Gets the number of directed pair geometries.
    /// </summary>
    public int PairGeometryCount => PairGeometries.Length;

    /// <summary>
    /// Gets the number of shared pair tables.
    /// </summary>
    public int PairTableCount => PairTables.Length;
}
