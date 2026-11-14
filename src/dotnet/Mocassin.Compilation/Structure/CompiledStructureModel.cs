// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;

namespace Mocassin.Compilation.Structure;

/// <summary>
/// One authored reference site in the compiled structure.
/// </summary>
/// <param name="Index">The dense site index in ascending <see cref="CellSiteId"/> order.</param>
/// <param name="Id">The stable cell-site identifier.</param>
/// <param name="Name">The user-visible site name.</param>
/// <param name="Fractional">The authored fractional position.</param>
/// <param name="Stability">The stable or unstable role.</param>
/// <param name="ParticleSetId">The authored particle set.</param>
/// <param name="EnvironmentIndex">The compiled environment index.</param>
/// <param name="PositionIndices">The canonical P1 positions owned by this site, ascending.</param>
public sealed record CompiledSite(
    int Index,
    CellSiteId Id,
    string Name,
    Fractional3 Fractional,
    SiteStability Stability,
    ParticleSetId ParticleSetId,
    int EnvironmentIndex,
    ImmutableArray<int> PositionIndices);

/// <summary>
/// One compiled interaction environment.
/// </summary>
/// <param name="Index">The dense environment index; index <c>0</c> is the stable environment.</param>
/// <param name="SiteIndex">The owning reference site, or <c>-1</c> for the shared stable environment.</param>
/// <param name="Stability">The environment stability.</param>
/// <param name="AllowedParticleMask">The selectable compiled particle mask.</param>
/// <param name="UpdateParticleIndices">The compiled particle indices updated through this environment.</param>
public sealed record CompiledEnvironment(
    int Index,
    int SiteIndex,
    SiteStability Stability,
    ulong AllowedParticleMask,
    ImmutableArray<int> UpdateParticleIndices);

/// <summary>
/// One periodic partner image linked from a position.
/// </summary>
/// <param name="PairTableIndex">The shared pair table index.</param>
/// <param name="TargetP">The canonical P1 index of the partner position.</param>
/// <param name="Offset">The representative relative cell offset and target P.</param>
/// <param name="DistanceBucketFm">The representative distance bucket in integer femtometres.</param>
public sealed record CompiledPairLink(
    int PairTableIndex,
    int TargetP,
    Vector4I Offset,
    int DistanceBucketFm);

/// <summary>
/// One cluster table link from a position.
/// </summary>
/// <param name="ClusterTableIndex">The shared cluster table index.</param>
/// <param name="SiteSlot">The position's site slot in the occupation code; <c>0</c> is the centre.</param>
public sealed record CompiledClusterLink(int ClusterTableIndex, int SiteSlot);

/// <summary>
/// One canonical P1 position.
/// </summary>
/// <param name="P">The canonical P1 index.</param>
/// <param name="Encoding">The base-cell encoding <c>(0, 0, 0, P)</c>.</param>
/// <param name="Fractional">The exact canonical fractional position.</param>
/// <param name="Cartesian">The Cartesian position in ångström.</param>
/// <param name="SiteIndex">The owning reference site index.</param>
/// <param name="SiteId">The owning reference site identifier.</param>
/// <param name="Stability">The owning site stability.</param>
/// <param name="AllowedParticleMask">The compiled particles that may occupy the position.</param>
/// <param name="EnvironmentIndex">The compiled environment index.</param>
/// <param name="OrbitIndex">The reference-site orbit index.</param>
/// <param name="PairLinks">The canonical periodic pair links.</param>
/// <param name="ClusterLinks">The canonical cluster links.</param>
public sealed record CompiledPosition(
    int P,
    Vector4I Encoding,
    Fractional3 Fractional,
    Cartesian3 Cartesian,
    int SiteIndex,
    CellSiteId SiteId,
    SiteStability Stability,
    ulong AllowedParticleMask,
    int EnvironmentIndex,
    int OrbitIndex,
    ImmutableArray<CompiledPairLink> PairLinks,
    ImmutableArray<CompiledClusterLink> ClusterLinks);

/// <summary>
/// The compiled P1 structure: cell vectors, sites, environments, positions, and the MMC interaction range.
/// </summary>
/// <param name="CellVectors">The cell vectors <c>a</c>, <c>b</c>, and <c>c</c> in ångström.</param>
/// <param name="CellVolumeAngstrom3">The unit-cell volume in cubic ångström.</param>
/// <param name="Sites">The reference sites in dense index order.</param>
/// <param name="Environments">The environments in dense index order.</param>
/// <param name="Positions">The canonical P1 positions in ascending <c>P</c> order.</param>
/// <param name="InteractionRange">The MMC local-update cube <c>[maxCellA, maxCellB, maxCellC, 0]</c>.</param>
public sealed record CompiledStructureModel(
    ImmutableArray<Cartesian3> CellVectors,
    double CellVolumeAngstrom3,
    ImmutableArray<CompiledSite> Sites,
    ImmutableArray<CompiledEnvironment> Environments,
    ImmutableArray<CompiledPosition> Positions,
    ImmutableArray<int> InteractionRange)
{
    /// <summary>
    /// Gets the empty compiled structure.
    /// </summary>
    public static CompiledStructureModel Empty { get; } = new([], 0.0, [], [], [], [0, 0, 0, 0]);

    /// <summary>
    /// Gets the number of canonical P1 positions.
    /// </summary>
    public int PositionCount => Positions.Length;

    /// <summary>
    /// Gets the number of reference sites.
    /// </summary>
    public int SiteCount => Sites.Length;

    /// <summary>
    /// Gets the number of interaction environments.
    /// </summary>
    public int EnvironmentCount => Environments.Length;
}
