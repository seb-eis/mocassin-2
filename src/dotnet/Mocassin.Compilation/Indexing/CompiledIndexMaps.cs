// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.Compilation.Indexing;

/// <summary>
/// Inspection-only index-to-stable-identifier maps.
/// </summary>
/// <param name="DirectionObjectIds">The direction stable IDs in dense index order.</param>
/// <param name="RuleGeometryIds">The rule stable IDs in dense index order.</param>
/// <param name="PairGeometryIds">The pair geometry stable IDs in canonical order.</param>
/// <param name="ClusterEnergyIds">The cluster energy stable IDs in dense table order.</param>
public sealed record CompiledIndexMaps(
    ImmutableArray<string> DirectionObjectIds,
    ImmutableArray<string> RuleGeometryIds,
    ImmutableArray<string> PairGeometryIds,
    ImmutableArray<string> ClusterEnergyIds)
{
    /// <summary>
    /// Gets the empty index map.
    /// </summary>
    public static CompiledIndexMaps Empty { get; } = new([], [], [], []);
}
