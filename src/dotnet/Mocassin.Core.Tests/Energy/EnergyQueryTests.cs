// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Energy;

[Trait("Tier", "PR-fast")]
public sealed class EnergyQueryTests
{
    [Fact]
    public void ProjectExposesEnergyCollectionsAndLookups()
    {
        var project = EnergyTestModels.CreateProject(EnergyTestModels.CreateEnergyOnlyModel());

        Assert.Equal(6.0, project.StableEnvironment.Cutoff.Value);
        Assert.Equal(2, project.StableEnvironment.Filters.Length);
        Assert.Single(project.UnstableEnvironments);
        Assert.Equal(2, project.Clusters.Length);
        Assert.Equal(2, project.SiteEnergies.Length);

        Assert.Same(
            project.UnstableEnvironments[0],
            project.FindEnvironment(EnergyTestModels.UnstableSiteId));
        Assert.NotNull(project.FindCluster(EnergyTestModels.ClusterIdA));
        Assert.NotNull(project.FindSiteEnergies(EnergyTestModels.CeriumSiteId));
        Assert.Null(project.FindEnvironment(CellSiteId.New()));
        Assert.Null(project.FindCluster(ClusterId.New()));
        Assert.Null(project.FindSiteEnergies(CellSiteId.New()));
    }
}
