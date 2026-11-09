// SPDX-License-Identifier: MIT

using Mocassin.Core.Units;

namespace Mocassin.Core.Model.Energy;

/// <summary>
/// Defines the sampling, permutation, cluster-count, and energy limits of the energy model.
/// </summary>
public static class EnergyLimits
{
    /// <summary>
    /// The stable-environment position count above which a warning is emitted.
    /// </summary>
    public const int StablePositionsWarning = 500;

    /// <summary>
    /// The stable-environment position count above which an error is emitted.
    /// </summary>
    public const int StablePositionsHard = 5000;

    /// <summary>
    /// The unstable-environment position count above which a warning is emitted.
    /// </summary>
    public const int UnstablePositionsWarning = 100;

    /// <summary>
    /// The unstable-environment position count above which an error is emitted.
    /// </summary>
    public const int UnstablePositionsHard = 1000;

    /// <summary>
    /// The cluster raw-permutation count above which a warning is emitted.
    /// </summary>
    public const int ClusterPermutationsWarning = 500;

    /// <summary>
    /// The cluster raw-permutation count above which an error is emitted.
    /// </summary>
    public const int ClusterPermutationsHard = 5000;

    /// <summary>
    /// The number of clusters per centre site above which a warning is emitted.
    /// </summary>
    public const int ClustersPerPositionWarning = 4;

    /// <summary>
    /// The number of clusters per centre site above which an error is emitted.
    /// </summary>
    public const int ClustersPerPositionHard = 10;

    /// <summary>
    /// The minimum number of surrounding offsets in a cluster.
    /// </summary>
    public const int ClusterOffsetsMin = 2;

    /// <summary>
    /// The maximum number of surrounding offsets in a cluster.
    /// </summary>
    public const int ClusterOffsetsMax = 8;

    /// <summary>
    /// Gets the smallest accepted site energy.
    /// </summary>
    public static EnergyEv MinEnergy { get; } = new(-100.0);

    /// <summary>
    /// Gets the largest accepted site energy.
    /// </summary>
    public static EnergyEv MaxEnergy { get; } = new(100.0);
}
