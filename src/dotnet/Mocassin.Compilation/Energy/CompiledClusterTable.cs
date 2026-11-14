// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;

namespace Mocassin.Compilation.Energy;

/// <summary>
/// One equivalent occupation tuple in a compiled cluster expansion map.
/// </summary>
/// <param name="OccupationCode">The encoded occupation tuple.</param>
/// <param name="PermutationIndex">The induced-permutation index that produced it.</param>
public sealed record CompiledClusterExpansion(ulong OccupationCode, int PermutationIndex);

/// <summary>
/// One canonical occupation in a compiled cluster table.
/// </summary>
/// <param name="CanonicalCode">The canonical occupation code.</param>
/// <param name="CanonicalTuple">The canonical particle indices in site order.</param>
/// <param name="Expansions">Every equivalent occupation code and its induced permutation.</param>
public sealed record CompiledClusterOccupation(
    ulong CanonicalCode,
    ImmutableArray<int> CanonicalTuple,
    ImmutableArray<CompiledClusterExpansion> Expansions);

/// <summary>
/// One shared cluster energy table.
/// </summary>
/// <param name="Index">The dense cluster table index.</param>
/// <param name="ClusterEnergyId">The stable cluster energy identity shared by symmetry-equivalent centres.</param>
/// <param name="ClusterId">The authored cluster identifier.</param>
/// <param name="CenterSiteIndex">The compiled centre site index.</param>
/// <param name="CenterP">The canonical centre P1 index.</param>
/// <param name="SitePIndices">The cluster site P1 indices; slot <c>0</c> is the centre.</param>
/// <param name="GeometryEncodings">The full cluster geometry encodings; slot <c>0</c> is the centre.</param>
/// <param name="Occupations">The canonical occupations sorted by code.</param>
/// <param name="Energies">The occupation energies in eV; zero until Step 09 applies values.</param>
/// <param name="ParticleTableMapping">The allowed centre particle indices in ascending row order.</param>
public sealed record CompiledClusterTable(
    int Index,
    string ClusterEnergyId,
    ClusterId ClusterId,
    int CenterSiteIndex,
    int CenterP,
    ImmutableArray<int> SitePIndices,
    ImmutableArray<Vector4I> GeometryEncodings,
    ImmutableArray<CompiledClusterOccupation> Occupations,
    ImmutableArray<double> Energies,
    ImmutableArray<int> ParticleTableMapping)
{
    /// <summary>
    /// Gets the number of canonical occupations.
    /// </summary>
    public int OccupationCount => Occupations.Length;
}
