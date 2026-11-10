// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.Symmetry.Energy;

/// <summary>
/// Advisory cost estimate for one authored cluster.
/// </summary>
/// <param name="ClusterId">The cluster identifier.</param>
/// <param name="RawPermutationCount">The unsymmetrized occupation-tuple count.</param>
/// <param name="UniquePermutationCount">The symmetry-reduced occupation-tuple count.</param>
/// <param name="EstimatedBytes">The estimated energy-table size in bytes.</param>
public sealed record ClusterCostPreview(
    ClusterId ClusterId,
    long RawPermutationCount,
    long UniquePermutationCount,
    long EstimatedBytes);
