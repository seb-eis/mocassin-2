// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Energy;

/// <summary>
/// Defines one multi-body interaction geometry relative to a centre reference site.
/// </summary>
/// <param name="Id">The stable cluster identifier.</param>
/// <param name="Name">The user-visible cluster name.</param>
/// <param name="CenterSiteId">The stable or unstable reference site at the cluster centre.</param>
/// <param name="OffsetVectors">
/// The absolute fractional offsets of the surrounding positions relative to the centre. The order is semantic
/// and is never re-sorted. The array holds the surrounding positions only; the centre is implicit.
/// </param>
/// <remarks>
/// A valid cluster has between <see cref="EnergyLimits.ClusterOffsetsMin"/> and
/// <see cref="EnergyLimits.ClusterOffsetsMax"/> surrounding offsets. The cluster contributes to the energy of
/// its centre only.
/// </remarks>
public sealed record Cluster(
    ClusterId Id,
    string Name,
    CellSiteId CenterSiteId,
    ImmutableArray<Fractional3> OffsetVectors);
