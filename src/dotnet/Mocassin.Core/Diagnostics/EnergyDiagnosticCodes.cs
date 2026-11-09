// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Stable diagnostic codes emitted by energy-model validation.
/// </summary>
/// <remarks>
/// Core owns the definition-level codes. <c>Mocassin.Symmetry.Energy.EnergyGeometryValidator</c> emits the
/// catalog/cell-dependent subset and references these constants. <c>ENM018</c> is defined here for Step 07,
/// which owns its emission when a KMC transition path leaves the relevant environment cutoff.
/// </remarks>
public static class EnergyDiagnosticCodes
{
    /// <summary>
    /// An environment cutoff is not positive and finite.
    /// </summary>
    public const string InvalidCutoff = "ENM001";

    /// <summary>
    /// An environment cutoff exceeds half the smallest cell dimension.
    /// </summary>
    public const string PeriodicAmbiguity = "ENM002";

    /// <summary>
    /// A filter range is inverted or extends beyond the owning environment cutoff.
    /// </summary>
    public const string InvalidFilterRange = "ENM003";

    /// <summary>
    /// A filter references an unknown or identical site, or an unstable filter is not centred on its site.
    /// </summary>
    public const string InvalidFilterSites = "ENM004";

    /// <summary>
    /// A filter duplicates or redundantly mirrors an earlier filter in the same environment.
    /// </summary>
    public const string DuplicateFilter = "ENM005";

    /// <summary>
    /// An unstable environment references an unknown or stable site, or duplicates another environment.
    /// </summary>
    public const string InvalidUnstableEnvironment = "ENM006";

    /// <summary>
    /// A cluster centre is unknown or an unstable centre has no unstable environment.
    /// </summary>
    public const string InvalidClusterCenter = "ENM007";

    /// <summary>
    /// A cluster has fewer than two or more than eight surrounding offsets.
    /// </summary>
    public const string InvalidClusterSize = "ENM008";

    /// <summary>
    /// Two cluster offsets resolve to the same encoded surrounding position.
    /// </summary>
    public const string DuplicateClusterOffset = "ENM009";

    /// <summary>
    /// A cluster surrounding position is unknown, unstable, filtered, or outside the centre cutoff.
    /// </summary>
    public const string InvalidClusterPosition = "ENM010";

    /// <summary>
    /// A cluster full geometry contains a centre-equivalent or otherwise duplicate position.
    /// </summary>
    public const string DuplicateClusterGeometry = "ENM011";

    /// <summary>
    /// A cluster raw-permutation count exceeds the warning limit.
    /// </summary>
    public const string ClusterPermutationWarning = "ENM012";

    /// <summary>
    /// A cluster raw-permutation count exceeds the hard limit.
    /// </summary>
    public const string ClusterPermutationLimit = "ENM013";

    /// <summary>
    /// A site-energy entry references an unknown site or particle, or repeats a site/particle pair.
    /// </summary>
    public const string InvalidSiteEnergy = "ENM014";

    /// <summary>
    /// A site energy is non-finite or outside the accepted energy range.
    /// </summary>
    public const string SiteEnergyOutOfRange = "ENM015";

    /// <summary>
    /// A site energy names a particle the site cannot occupy.
    /// </summary>
    public const string ForbiddenSiteEnergy = "ENM016";

    /// <summary>
    /// An environment sampling estimate is below one or above the warning limit.
    /// </summary>
    public const string EnvironmentSamplingWarning = "ENM017";

    /// <summary>
    /// A KMC transition path leaves the relevant stable or unstable environment cutoff.
    /// </summary>
    /// <remarks>
    /// Step 05 defines this constant and its help anchor only. Step 07 owns emission and the consolidated
    /// diagnostics documentation.
    /// </remarks>
    public const string KmcPathOutsideEnvironment = "ENM018";

    /// <summary>
    /// An environment sampling estimate exceeds the hard limit.
    /// </summary>
    public const string EnvironmentSamplingLimit = "ENM019";

    /// <summary>
    /// More than four clusters share one centre site.
    /// </summary>
    public const string ClustersPerCenterWarning = "ENM020";

    /// <summary>
    /// More than ten clusters share one centre site.
    /// </summary>
    public const string ClustersPerCenterLimit = "ENM021";

    /// <summary>
    /// Gets the actionable help text for an energy diagnostic code.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <returns>The help text, or <see langword="null"/> when the code is not an energy code.</returns>
    public static string? HelpFor(string code) => code switch
    {
        InvalidCutoff => "Set every stable and unstable environment cutoff to a positive finite value.",
        PeriodicAmbiguity => "Reduce the cutoff below half the smallest cell dimension or accept the ambiguity knowingly.",
        InvalidFilterRange => "Use 0 <= start <= end and keep the end within the environment cutoff.",
        InvalidFilterSites => "Reference existing distinct sites; an unstable filter must start at its environment site.",
        DuplicateFilter => "Remove the duplicate filter; stable filters already apply in both directions.",
        InvalidUnstableEnvironment => "Create at most one unstable environment per unstable site.",
        InvalidClusterCenter => "Use an existing reference site and add an unstable environment for an unstable centre.",
        InvalidClusterSize => "Use 2 to 8 surrounding offsets; the centre is implicit.",
        DuplicateClusterOffset => "Remove offsets that encode to the same periodic position.",
        InvalidClusterPosition => "Use distinct stable surrounding sites inside the centre cutoff and outside its filters.",
        DuplicateClusterGeometry => "Remove centre-equivalent or otherwise duplicate full-geometry positions.",
        ClusterPermutationWarning => "Reduce the cluster size or the occupation sets to lower the permutation count.",
        ClusterPermutationLimit => "Reduce the cluster to at most 5000 raw permutations.",
        InvalidSiteEnergy => "Reference existing sites and particles and keep one entry per site/particle pair.",
        SiteEnergyOutOfRange => "Use a finite site energy in the -100..100 eV range.",
        ForbiddenSiteEnergy => "Remove the entry or add the particle to the site's occupation set.",
        EnvironmentSamplingWarning => "Adjust the cutoff, cell, or filters to sample at least one interaction.",
        KmcPathOutsideEnvironment => "Increase the environment cutoff or shorten the KMC transition path.",
        EnvironmentSamplingLimit => "Reduce the environment cutoff or cell size to stay within the sampling budget.",
        ClustersPerCenterWarning => "Move or remove clusters so that at most four share one centre.",
        ClustersPerCenterLimit => "Reduce the number of clusters per centre to at most ten.",
        _ => null,
    };
}
