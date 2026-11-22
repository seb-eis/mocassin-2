// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.App.Tests.LayoutSpike.Data;

/// <summary>
/// The spike's single column policy per domain. Every option uses these widths for both its header and its rows.
/// </summary>
internal static class SpikeColumns
{
    /// <summary>
    /// Gets the column policy for a domain.
    /// </summary>
    /// <param name="domain">The domain.</param>
    /// <returns>The shared columns.</returns>
    public static ImmutableArray<SpikeColumn> For(SpikeDomain domain) => domain switch
    {
        SpikeDomain.Particles =>
        [
            SpikeColumn.Star("Name", 1.4),
            SpikeColumn.Fixed("Symbol", 80),
            SpikeColumn.Fixed("Charge", 78),
            SpikeColumn.Fixed("Vacancy", 88),
            SpikeColumn.Fixed("Used", 124),
        ],
        SpikeDomain.ParticleSets =>
        [
            SpikeColumn.Star("Name", 1.4),
            SpikeColumn.Star("Members", 1.3),
            SpikeColumn.Fixed("Used", 90),
        ],
        SpikeDomain.Sites =>
        [
            SpikeColumn.Star("Name", 1.3),
            SpikeColumn.Fixed("A", 52),
            SpikeColumn.Fixed("B", 52),
            SpikeColumn.Fixed("C", 52),
            SpikeColumn.Fixed("Stability", 96),
            SpikeColumn.Star("Particle set", 1.2),
        ],
        SpikeDomain.StateChanges =>
        [
            SpikeColumn.Star("Name", 1.2),
            SpikeColumn.Star("Donor", 1.0),
            SpikeColumn.Star("Acceptor", 1.0),
            SpikeColumn.Fixed("Sample", 116),
        ],
        SpikeDomain.Chains =>
        [
            SpikeColumn.Star("Name", 1.2),
            SpikeColumn.Star("Pattern", 1.0),
            SpikeColumn.Fixed("Groups", 58),
            SpikeColumn.Fixed("Reverse", 62),
            SpikeColumn.Fixed("Sample", 116),
        ],
        SpikeDomain.Transitions =>
        [
            SpikeColumn.Star("Name", 1.2),
            SpikeColumn.Fixed("Kind", 52),
            SpikeColumn.Star("Chain", 1.0),
            SpikeColumn.Star("Details", 1.6),
            SpikeColumn.Fixed("Sample", 116),
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unknown spike domain."),
    };

    /// <summary>
    /// Gets the filter choices for a domain.
    /// </summary>
    /// <param name="domain">The domain.</param>
    /// <returns>The filter choices, "All rows" first.</returns>
    public static ImmutableArray<SpikeFilterOption> Filters(SpikeDomain domain) => domain switch
    {
        SpikeDomain.Particles =>
        [
            new SpikeFilterOption("All particles", null),
            new SpikeFilterOption("Charged", "charged"),
            new SpikeFilterOption("Vacancy", "vacancy"),
            new SpikeFilterOption("Unused", "unused"),
        ],
        SpikeDomain.ParticleSets =>
        [
            new SpikeFilterOption("All sets", null),
            new SpikeFilterOption("Single member", "single"),
            new SpikeFilterOption("Multiple members", "multi"),
        ],
        SpikeDomain.Sites =>
        [
            new SpikeFilterOption("All sites", null),
            new SpikeFilterOption("Stable", "stable"),
            new SpikeFilterOption("Unstable", "unstable"),
        ],
        SpikeDomain.StateChanges =>
        [
            new SpikeFilterOption("All state changes", null),
            new SpikeFilterOption("Involves void", "void"),
        ],
        SpikeDomain.Chains =>
        [
            new SpikeFilterOption("All chains", null),
            new SpikeFilterOption("Auto reverse", "auto-reverse"),
        ],
        SpikeDomain.Transitions =>
        [
            new SpikeFilterOption("All transitions", null),
            new SpikeFilterOption("KMC only", "kmc"),
            new SpikeFilterOption("MMC only", "mmc"),
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unknown spike domain."),
    };
}
