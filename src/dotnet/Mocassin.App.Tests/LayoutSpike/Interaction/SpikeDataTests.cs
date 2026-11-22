// SPDX-License-Identifier: MIT

using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;

using Mocassin.App.Tests.LayoutSpike.Data;

namespace Mocassin.App.Tests.LayoutSpike.Interaction;

/// <summary>
/// Guards the spike's representative-data claim: the rows come from the real samples through the real store, and the
/// generated breadth value is only breadth.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class SpikeDataTests
{
    [Fact]
    public async Task TheCeriaSampleProvidesTheSpecifiedParticlesSetsSitesAndStructure()
    {
        var projects = await SpikeFixtures.GetAsync();

        Assert.Equal(3, projects.Ceria.Particles.Length);
        Assert.Equal(["Ce4+", "O2-", "Vacancy"], projects.Ceria.Particles.Select(particle => particle.Name));
        Assert.Contains(projects.Ceria.Particles, particle => particle.IsVacancy);
        Assert.Equal(3, projects.Ceria.Model.ParticleSets.Length);
        Assert.Equal(3, projects.Ceria.Structure.Sites.Length);
        Assert.Equal(225, projects.Ceria.Structure.SpaceGroup.Number);
        Assert.Single(projects.Ceria.Structure.DummyPositions);
        Assert.Empty(projects.Ceria.KineticTransitions);
        Assert.Empty(projects.Ceria.MetropolisTransitions);
        Assert.Empty(projects.Ceria.StateChanges);
    }

    [Fact]
    public async Task TheTransitionSamplesProvideTheKmcMmcAndPatternBreadthRows()
    {
        var projects = await SpikeFixtures.GetAsync();

        var kmc = Assert.Single(projects.CeriaKmc.KineticTransitions);
        Assert.Equal(TransitionPattern.Migration3, projects.CeriaKmc.Chains[0].Pattern);
        Assert.Equal(3, kmc.ReferencePath.Length);
        var mmc = Assert.Single(projects.CeriaMmc.MetropolisTransitions);
        Assert.NotNull(mmc);
        Assert.Equal(2, projects.Interstitialcy5.KineticTransitions.Length);
        Assert.Equal(2, projects.Vehicle5.KineticTransitions.Length);
        Assert.Single(projects.PolaronHop.MetropolisTransitions);
    }

    [Fact]
    public async Task TheGeneratedBreadthValueHasMoreThanOneThousandSitesAndPaddedParticles()
    {
        var projects = await SpikeFixtures.GetAsync();
        var toy = projects.Toy;

        Assert.Equal(SpikeToyProject.SiteCount, toy.Structure.Sites.Length);
        Assert.True(toy.Structure.Sites.Length > 1000);
        Assert.Equal(3 + SpikeToyProject.PaddedParticleCount, toy.Particles.Length);
        Assert.Equal(
            toy.Structure.Sites.Length,
            toy.Structure.Sites.Select(site => site.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.All(
            toy.Structure.Sites,
            site => Assert.True(
                site.Fractional.A is >= 0 and < 1
                && site.Fractional.B is >= 0 and < 1
                && site.Fractional.C is >= 0 and < 1));
        Assert.Contains(toy.Structure.Sites, site => site.Stability == SiteStability.Unstable);
    }

    [Fact]
    public async Task TheParticleTablePinsTheImplicitVoidRowAndTheTransitionTableMergesSamples()
    {
        var projects = await SpikeFixtures.GetAsync();
        var particles = SpikeRowBuilder.ParticleTable(projects.Ceria, readOnly: false);
        var first = particles.Rows[0];

        Assert.True(first.IsPinned);
        Assert.True(first.IsReadOnly);
        Assert.Equal("Void", first.Title);
        Assert.Equal(4, particles.Rows.Length);
        Assert.Contains(particles.Rows, row => row.Tags.Contains("vacancy"));

        var transitions = SpikeRowBuilder.TransitionTable(projects.CeriaKmc, "ceria-kmc", readOnly: false);
        Assert.Single(transitions.Rows);
        var breadth = SpikeRowBuilder.TransitionTable(projects.PolaronHop, "polaron-hop", readOnly: true);
        Assert.True(Assert.Single(breadth.Rows).IsReadOnly);
        Assert.Contains(transitions.Columns, column => column.Header == "Sample");
    }

    [Fact]
    public async Task TheToyProjectReusesTheRealParticleSetsSoSiteRowsResolveTheirSetNames()
    {
        var projects = await SpikeFixtures.GetAsync();
        var table = SpikeRowBuilder.SiteTable(projects.Toy, readOnly: true);

        Assert.Equal(SpikeToyProject.SiteCount, table.Rows.Length);
        Assert.All(table.Rows, row => Assert.DoesNotContain("?", row.Cells));
        Assert.Equal(
            SpikeToyProject.SiteCount,
            table.Rows.Select(row => row.Cells[0]).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(table.Rows, row => row.Tags.Contains("unstable"));
        Assert.All(table.Rows, row => Assert.True(row.IsReadOnly));
    }

    [Fact]
    public async Task TheWorkspaceBindsCeriaAndTheKmcDocumentAndMarksBreadthRowsReadOnly()
    {
        var projects = await SpikeFixtures.GetAsync();
        var workspace = SpikeWorkspaceViewModel.Create(projects);

        Assert.Equal(SpikeDomain.Particles, workspace.Domains[0].Domain);
        Assert.NotNull(workspace.Domains[0].Editable?.Coordinator);
        Assert.NotNull(workspace.Domains[5].Editable?.Coordinator);
        Assert.Equal("ceria-kmc", workspace.Domains[5].Editable?.Label);
        Assert.Contains(workspace.Domains[5].SourceRows(), row => row.IsReadOnly);
        Assert.Contains(workspace.Domains[5].SourceRows(), row => !row.IsReadOnly);
        Assert.All(
            workspace.Domains[0].SourceRows().Where(row => row.Title == "Void"),
            row => Assert.True(row.IsReadOnly));
        Assert.True(workspace.Domains[2].HasGeneratedBreadth);
        Assert.False(workspace.Domains[4].HasGeneratedBreadth);
    }
}
