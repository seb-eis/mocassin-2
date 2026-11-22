// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.Tests.LayoutSpike.Data;

/// <summary>
/// Builds the spike tables from a real project value. Nothing here invents model shapes: every row is read from
/// the loaded sample and only formatted for display.
/// </summary>
internal static class SpikeRowBuilder
{
    /// <summary>
    /// Builds the particle, set, and site tables.
    /// </summary>
    /// <param name="project">The loaded project.</param>
    /// <returns>The model tables.</returns>
    public static ImmutableArray<SpikeTable> BuildModelTables(ProjectModel project) =>
    [
        ParticleTable(project, readOnly: false),
        SetTable(project),
        SiteTable(project, readOnly: false),
    ];

    /// <summary>
    /// Builds the state-change, chain, and transition tables.
    /// </summary>
    /// <param name="project">The loaded project.</param>
    /// <param name="sample">The sample label.</param>
    /// <param name="readOnly">
    /// Whether the rows are display-only because another project is the editable document.
    /// </param>
    /// <returns>The transition tables.</returns>
    public static ImmutableArray<SpikeTable> BuildTransitionTables(
        ProjectModel project,
        string sample,
        bool readOnly) =>
    [
        StateChangeTable(project, sample, readOnly),
        ChainTable(project, sample, readOnly),
        TransitionTable(project, sample, readOnly),
    ];

    /// <summary>
    /// Builds the particle table, including the pinned implicit void row.
    /// </summary>
    /// <param name="project">The loaded project.</param>
    /// <param name="readOnly">Whether the authored rows are display-only.</param>
    /// <returns>The particle table.</returns>
    public static SpikeTable ParticleTable(ProjectModel project, bool readOnly)
    {
        var setUsage = project.Model.ParticleSets
            .SelectMany(set => set.ParticleIds)
            .GroupBy(id => id.Value)
            .ToDictionary(group => group.Key, group => group.Count());
        var changeUsage = project.StateChanges
            .SelectMany(change => new[] { change.DonorParticleId.Value, change.AcceptorParticleId.Value })
            .GroupBy(id => id)
            .ToDictionary(group => group.Key, group => group.Count());

        var rows = ImmutableArray.CreateBuilder<SpikeRow>();
        rows.Add(
            new SpikeRow(
                SpikeDomain.Particles,
                Particle.Void.Id.Value,
                "Void",
                ["▣ Void", "∅", "0", "implicit", "—"],
                ["void"],
                IsReadOnly: true,
                IsPinned: true,
                SourceLabel: null,
                Explanation: "Void is implicit and pinned: it is never stored, edited, or deleted."));

        foreach (var particle in project.Particles)
        {
            var sets = setUsage.GetValueOrDefault(particle.Id.Value);
            var changes = changeUsage.GetValueOrDefault(particle.Id.Value);
            var tags = ImmutableArray.CreateBuilder<string>();
            if (particle.Charge.Value != 0.0)
            {
                tags.Add("charged");
            }

            if (particle.IsVacancy)
            {
                tags.Add("vacancy");
            }

            if (sets == 0 && changes == 0)
            {
                tags.Add("unused");
            }

            rows.Add(
                new SpikeRow(
                    SpikeDomain.Particles,
                    particle.Id.Value,
                    particle.Name,
                    [
                        particle.Name,
                        particle.Symbol,
                        Format(particle.Charge.Value),
                        particle.IsVacancy ? "yes" : "no",
                        $"{sets} sets / {changes} chg",
                    ],
                    tags.ToImmutable(),
                    readOnly,
                    IsPinned: false,
                    SourceLabel: null,
                    Explanation: null));
        }

        return new SpikeTable(SpikeDomain.Particles, SpikeColumns.For(SpikeDomain.Particles), rows.ToImmutable());
    }

    /// <summary>
    /// Builds the particle-set table.
    /// </summary>
    /// <param name="project">The loaded project.</param>
    /// <returns>The set table.</returns>
    public static SpikeTable SetTable(ProjectModel project)
    {
        var siteUsage = project.Structure.Sites
            .GroupBy(site => site.ParticleSetId.Value)
            .ToDictionary(group => group.Key, group => group.Count());
        var rows = ImmutableArray.CreateBuilder<SpikeRow>();
        foreach (var set in project.Model.ParticleSets)
        {
            var names = set.ParticleIds
                .Select(id => SpikeModelLookup.FindParticle(project, id.Value)?.Name ?? "?")
                .ToImmutableArray();
            var tags = names.Length == 1 ? ImmutableArray.Create("single") : ImmutableArray.Create("multi");
            rows.Add(
                new SpikeRow(
                    SpikeDomain.ParticleSets,
                    set.Id.Value,
                    set.Name,
                    [set.Name, string.Join(", ", names), $"{siteUsage.GetValueOrDefault(set.Id.Value)} sites"],
                    tags,
                    IsReadOnly: false,
                    IsPinned: false,
                    SourceLabel: null,
                    Explanation: null));
        }

        return new SpikeTable(SpikeDomain.ParticleSets, SpikeColumns.For(SpikeDomain.ParticleSets), rows.ToImmutable());
    }

    /// <summary>
    /// Builds the cell-site table.
    /// </summary>
    /// <param name="project">The loaded project.</param>
    /// <param name="readOnly">Whether the rows are display-only.</param>
    /// <returns>The site table.</returns>
    public static SpikeTable SiteTable(ProjectModel project, bool readOnly)
    {
        var rows = ImmutableArray.CreateBuilder<SpikeRow>();
        foreach (var site in project.Structure.Sites)
        {
            rows.Add(SiteRow(project, site, readOnly));
        }

        return new SpikeTable(SpikeDomain.Sites, SpikeColumns.For(SpikeDomain.Sites), rows.ToImmutable());
    }

    /// <summary>
    /// Builds one site row.
    /// </summary>
    /// <param name="project">The owning project.</param>
    /// <param name="site">The site.</param>
    /// <param name="readOnly">Whether the row is display-only.</param>
    /// <returns>The row.</returns>
    public static SpikeRow SiteRow(ProjectModel project, CellSite site, bool readOnly) =>
        new(
            SpikeDomain.Sites,
            site.Id.Value,
            site.Name,
            [
                site.Name,
                Format(site.Fractional.A),
                Format(site.Fractional.B),
                Format(site.Fractional.C),
                site.Stability.ToString().ToLowerInvariant(),
                SpikeModelLookup.FindSet(project, site.ParticleSetId.Value)?.Name ?? "?",
            ],
            site.Stability == SiteStability.Stable ? ["stable"] : ["unstable"],
            readOnly,
            IsPinned: false,
            SourceLabel: null,
            Explanation: null);

    /// <summary>
    /// Builds the state-change table.
    /// </summary>
    /// <param name="project">The loaded project.</param>
    /// <param name="sample">The sample label.</param>
    /// <param name="readOnly">Whether the rows are display-only.</param>
    /// <returns>The state-change table.</returns>
    public static SpikeTable StateChangeTable(ProjectModel project, string sample, bool readOnly)
    {
        var rows = ImmutableArray.CreateBuilder<SpikeRow>();
        foreach (var change in project.StateChanges)
        {
            var donor = ParticleName(project, change.DonorParticleId);
            var acceptor = ParticleName(project, change.AcceptorParticleId);
            var tags = change.DonorParticleId == Particle.Void.Id || change.AcceptorParticleId == Particle.Void.Id
                ? ImmutableArray.Create("void")
                : ImmutableArray<string>.Empty;
            rows.Add(
                new SpikeRow(
                    SpikeDomain.StateChanges,
                    change.Id.Value,
                    change.Name,
                    [change.Name, donor, acceptor, sample],
                    tags,
                    readOnly,
                    IsPinned: false,
                    SourceLabel: sample,
                    Explanation: null));
        }

        return new SpikeTable(
            SpikeDomain.StateChanges,
            SpikeColumns.For(SpikeDomain.StateChanges),
            rows.ToImmutable());
    }

    /// <summary>
    /// Builds the chain table.
    /// </summary>
    /// <param name="project">The loaded project.</param>
    /// <param name="sample">The sample label.</param>
    /// <param name="readOnly">Whether the rows are display-only.</param>
    /// <returns>The chain table.</returns>
    public static SpikeTable ChainTable(ProjectModel project, string sample, bool readOnly)
    {
        var rows = ImmutableArray.CreateBuilder<SpikeRow>();
        foreach (var chain in project.Chains)
        {
            rows.Add(
                new SpikeRow(
                    SpikeDomain.Chains,
                    chain.Id.Value,
                    chain.Name,
                    [
                        chain.Name,
                        chain.Pattern.ToString(),
                        chain.GroupIds.Length.ToString(CultureInfo.InvariantCulture),
                        chain.GeneratesReverse ? "yes" : "no",
                        sample,
                    ],
                    chain.GeneratesReverse ? ["auto-reverse"] : ImmutableArray<string>.Empty,
                    readOnly,
                    IsPinned: false,
                    SourceLabel: sample,
                    Explanation: null));
        }

        return new SpikeTable(SpikeDomain.Chains, SpikeColumns.For(SpikeDomain.Chains), rows.ToImmutable());
    }

    /// <summary>
    /// Builds the combined kinetic and Metropolis transition table.
    /// </summary>
    /// <param name="project">The loaded project.</param>
    /// <param name="sample">The sample label.</param>
    /// <param name="readOnly">Whether the rows are display-only.</param>
    /// <returns>The transition table.</returns>
    public static SpikeTable TransitionTable(ProjectModel project, string sample, bool readOnly)
    {
        var rows = ImmutableArray.CreateBuilder<SpikeRow>();
        foreach (var transition in project.KineticTransitions)
        {
            var chain = SpikeModelLookup.FindChain(project, transition.ChainId.Value)?.Name ?? "?";
            rows.Add(
                new SpikeRow(
                    SpikeDomain.Transitions,
                    transition.Id.Value,
                    transition.Name,
                    [
                        transition.Name,
                        "KMC",
                        chain,
                        $"{transition.ReferencePath.Length} path points",
                        sample,
                    ],
                    ["kmc"],
                    readOnly,
                    IsPinned: false,
                    SourceLabel: sample,
                    Explanation: null));
        }

        foreach (var transition in project.MetropolisTransitions)
        {
            var chain = SpikeModelLookup.FindChain(project, transition.ChainId.Value)?.Name ?? "?";
            var siteA = SpikeModelLookup.FindSite(project, transition.SiteAId.Value)?.Name ?? "?";
            var siteB = SpikeModelLookup.FindSite(project, transition.SiteBId.Value)?.Name ?? "?";
            rows.Add(
                new SpikeRow(
                    SpikeDomain.Transitions,
                    transition.Id.Value,
                    transition.Name,
                    [transition.Name, "MMC", chain, $"{siteA} → {siteB}", sample],
                    ["mmc"],
                    readOnly,
                    IsPinned: false,
                    SourceLabel: sample,
                    Explanation: null));
        }

        return new SpikeTable(
            SpikeDomain.Transitions,
            SpikeColumns.For(SpikeDomain.Transitions),
            rows.ToImmutable());
    }

    /// <summary>
    /// Gets a particle name, including the implicit void particle.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="id">The particle identifier.</param>
    /// <returns>The display name.</returns>
    public static string ParticleName(ProjectModel project, ParticleId id) =>
        SpikeModelLookup.FindParticle(project, id.Value)?.Name ?? "?";

    /// <summary>
    /// Formats a double with the spike's invariant three-decimal convention.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The formatted text.</returns>
    public static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
