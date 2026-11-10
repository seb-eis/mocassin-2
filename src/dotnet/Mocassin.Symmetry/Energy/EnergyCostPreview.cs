// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.Symmetry.Energy;

/// <summary>
/// Advisory cost estimate for compiling the energy model.
/// </summary>
/// <param name="StableP1PositionCount">The stable reference-site P1 position count.</param>
/// <param name="PairImageCount">The sampled pair-image count after filters.</param>
/// <param name="UniquePairInteractionCount">The symmetry-reduced directed pair geometry count.</param>
/// <param name="ClusterCosts">The per-cluster estimates in model order.</param>
/// <param name="EstimatedMemoryBytes">The estimated total energy-table memory in bytes.</param>
public sealed record EnergyCostPreview(
    long StableP1PositionCount,
    long PairImageCount,
    long UniquePairInteractionCount,
    ImmutableArray<ClusterCostPreview> ClusterCosts,
    long EstimatedMemoryBytes);
