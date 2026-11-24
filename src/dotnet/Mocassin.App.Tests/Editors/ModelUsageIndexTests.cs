// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors;
using Mocassin.App.ViewModels.Editors.Usage;
using Mocassin.Packages.Projects;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// Pins the usage index's attribution rules, including the m-15 unstable-environment enumeration and the resolved
/// KMC path rule.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ModelUsageIndexTests
{
    [Fact]
    public async Task ParticlesCountTheirSetsAndSetsCountTheirSites()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var project = entry.Project;
        var index = ModelUsageIndex.Create(project);

        var oxygenSet = project.Model.ParticleSets.First(set => set.Name == "Oxygen site");
        var oxygenParticle = oxygenSet.ParticleIds[0];
        var particleUsages = index.GetDependents(ModelObjectRef.Particle(oxygenParticle));

        Assert.Contains(
            particleUsages,
            usage => usage.Kind == ModelUsageKind.ParticleSet && usage.DisplayName == oxygenSet.Name);

        var site = project.Structure.Sites.First(candidate => candidate.ParticleSetId == oxygenSet.Id);
        Assert.Contains(
            index.GetDependents(ModelObjectRef.ParticleSet(oxygenSet.Id)),
            usage => usage.Kind == ModelUsageKind.CellSite && usage.DisplayName == site.Name);
    }

    [Fact]
    public async Task AFilterlessUnstableEnvironmentIsADependentThroughItsSiteId()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var environment = entry.Project.Model.UnstableEnvironments.Single();
        Assert.Empty(environment.Filters);

        var index = ModelUsageIndex.Create(entry.Project);
        var dependents = index.GetDependents(ModelObjectRef.CellSite(environment.SiteId));

        Assert.Contains(
            dependents,
            usage => usage.Kind == ModelUsageKind.EnergyReference
                && usage.Description == "Unstable environment at the site");
        Assert.Contains(
            dependents,
            usage => usage.Kind == ModelUsageKind.EnergyReference && usage.Description == "Cluster centre site");
        Assert.All(
            dependents.Where(static usage => usage.Kind == ModelUsageKind.EnergyReference),
            static usage => Assert.True(usage.IsEnergyReference));
    }

    [Fact]
    public async Task MmcSiteIdsAreAttributedToTheirSites()
    {
        var entry = await LoadAsync("samples", "transitions", "ceria-mmc.mocprj");
        var transition = entry.Project.MetropolisTransitions[0];
        var index = ModelUsageIndex.Create(entry.Project);

        Assert.Contains(
            index.GetDependents(ModelObjectRef.CellSite(transition.SiteAId)),
            usage => usage.Kind == ModelUsageKind.Transition && usage.Reference.ObjectId == transition.Id.Value);
        Assert.Contains(
            index.GetDependents(ModelObjectRef.CellSite(transition.SiteBId)),
            usage => usage.Kind == ModelUsageKind.Transition && usage.Reference.ObjectId == transition.Id.Value);
    }

    [Fact]
    public async Task KineticPathsAreResolvedToTheirOwningSites()
    {
        var entry = await LoadAsync("samples", "transitions", "ceria-kmc.mocprj");
        var transition = entry.Project.KineticTransitions[0];
        var index = ModelUsageIndex.Create(entry.Project);

        var attributed = entry.Project.Structure.Sites
            .Where(site => index.GetDependents(ModelObjectRef.CellSite(site.Id)).Any(usage =>
                usage.Kind == ModelUsageKind.Transition
                && usage.Reference.ObjectId == transition.Id.Value
                && usage.Description.Contains("resolved path", StringComparison.Ordinal)))
            .ToArray();

        Assert.NotEmpty(attributed);
    }

    [Fact]
    public async Task OccupationExchangesAndDopingsAreAttributedToTheirSites()
    {
        var entry = await LoadAsync("samples", "lattice", "ceria-reduction.mocprj");
        var exchange = entry.Project.OccupationExchanges[0];
        var index = ModelUsageIndex.Create(entry.Project);

        Assert.Contains(
            index.GetDependents(ModelObjectRef.CellSite(exchange.SiteId)),
            usage => usage.Kind == ModelUsageKind.OccupationExchange
                && usage.Reference.ObjectId == exchange.Id.Value);

        foreach (var doping in entry.Project.Dopings)
        {
            Assert.Contains(
                index.GetDependents(ModelObjectRef.OccupationExchange(doping.PrimaryExchangeId)),
                usage => usage.Kind == ModelUsageKind.Doping && usage.Reference.ObjectId == doping.Id.Value);
        }
    }

    private static async Task<Mocassin.App.ViewModels.Projects.ProjectWorkspaceEntry> LoadAsync(
        params string[] relativePath)
    {
        var path = RepositoryPaths.Combine(relativePath);
        var store = new ProjectStore(new ManualTimeProvider(), new ProjectStoreOptions { ValidateOnLoad = false });
        var result = await store.LoadAsync(path);
        if (result.Project is null)
        {
            throw new InvalidOperationException($"The fixture {path} did not load: {result.FailureKind}.");
        }

        return new Mocassin.App.ViewModels.Projects.ProjectWorkspaceEntry(
            result.Project,
            path,
            new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero),
            isReadOnly: false);
    }
}
