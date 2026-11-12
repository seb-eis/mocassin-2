// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// Advisory statistics about the size and build cost of a model.
/// </summary>
/// <remarks>
/// Counts that can exceed <see cref="int"/> are <see cref="long"/>. The estimates are computed from the
/// preview services and are never persisted or included in a model hash.
/// </remarks>
/// <param name="ParticleCount">The authored particle count, excluding the implicit void particle.</param>
/// <param name="ParticleSetCount">The authored particle-set count.</param>
/// <param name="ReferenceSiteCount">The authored reference-site count.</param>
/// <param name="P1PositionCount">The unit-cell P1 position count.</param>
/// <param name="StablePositionCount">The P1 positions belonging to stable reference sites.</param>
/// <param name="UnstablePositionCount">The P1 positions belonging to unstable reference sites.</param>
/// <param name="KineticTransitionCount">The authored KMC transition count.</param>
/// <param name="MetropolisTransitionCount">The authored MMC transition count.</param>
/// <param name="ClusterCount">The authored cluster count.</param>
/// <param name="EstimatedPairInteractionCount">The distinct pair energy tables from the energy preview.</param>
/// <param name="EstimatedUniqueClusterPermutations">The sum of unique cluster permutations.</param>
/// <param name="EstimatedRuleCount">The advisory rule total before conservation filtering.</param>
/// <param name="EstimatedMemoryBytes">The estimated energy-table memory in bytes.</param>
/// <param name="CellCountA">The requested supercell count along a.</param>
/// <param name="CellCountB">The requested supercell count along b.</param>
/// <param name="CellCountC">The requested supercell count along c.</param>
/// <param name="EstimatedPositions">The P1 position count multiplied by the supercell cell count.</param>
public sealed record ModelStatistics(
    int ParticleCount,
    int ParticleSetCount,
    int ReferenceSiteCount,
    long P1PositionCount,
    long StablePositionCount,
    long UnstablePositionCount,
    int KineticTransitionCount,
    int MetropolisTransitionCount,
    int ClusterCount,
    long EstimatedPairInteractionCount,
    long EstimatedUniqueClusterPermutations,
    long EstimatedRuleCount,
    long EstimatedMemoryBytes,
    int CellCountA,
    int CellCountB,
    int CellCountC,
    long EstimatedPositions);
