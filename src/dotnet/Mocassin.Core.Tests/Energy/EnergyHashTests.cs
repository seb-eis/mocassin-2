// SPDX-License-Identifier: MIT

using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;
using Mocassin.Core.Units;

namespace Mocassin.Core.Tests.Energy;

[Trait("Tier", "PR-fast")]
public sealed class EnergyHashTests
{
    [Fact]
    public void ComputeEnergyIgnoresArrayOrderAndUnrelatedModelEdits()
    {
        var energyModel = EnergyTestModels.CreateEnergyOnlyModel();
        var shuffled = energyModel with
        {
            StableEnvironment = energyModel.StableEnvironment with
            {
                Filters = [.. energyModel.StableEnvironment.Filters.Reverse()],
            },
            UnstableEnvironments = [.. energyModel.UnstableEnvironments.Reverse()],
            Clusters = [.. energyModel.Clusters.Reverse()],
            SiteEnergies = [.. energyModel.SiteEnergies.Reverse()],
        };
        var combined = EnergyTestModels.CreateCombinedModel();

        Assert.Equal(
            ProjectHasher.ComputeEnergy(energyModel),
            ProjectHasher.ComputeEnergy(shuffled));
        Assert.Equal(
            ProjectHasher.ComputeEnergy(energyModel),
            ProjectHasher.ComputeEnergy(combined));
        Assert.NotEqual(
            ProjectHasher.Compute(EnergyTestModels.CreateProject(energyModel)),
            ProjectHasher.Compute(EnergyTestModels.CreateProject(combined)));
    }

    [Theory]
    [InlineData("cutoff")]
    [InlineData("filter")]
    [InlineData("unstable")]
    [InlineData("cluster")]
    [InlineData("siteEnergy")]
    public void ComputeEnergyChangesWithEveryEnergySection(string section)
    {
        var model = EnergyTestModels.CreateEnergyOnlyModel();
        var changed = section switch
        {
            "cutoff" => model with
            {
                StableEnvironment = model.StableEnvironment with
                {
                    Cutoff = new LengthAngstrom(7.0),
                },
            },
            "filter" => model with
            {
                StableEnvironment = model.StableEnvironment with
                {
                    Filters = [.. model.StableEnvironment.Filters, NewFilter()],
                },
            },
            "unstable" => model with
            {
                UnstableEnvironments =
                [
                    model.UnstableEnvironments[0] with { Cutoff = new LengthAngstrom(5.0) },
                ],
            },
            "cluster" => model with
            {
                Clusters =
                [
                    model.Clusters[0] with
                    {
                        OffsetVectors = [.. model.Clusters[0].OffsetVectors.Reverse()],
                    },
                    model.Clusters[1],
                ],
            },
            "siteEnergy" => model with
            {
                SiteEnergies =
                [
                    model.SiteEnergies[0] with
                    {
                        Entries =
                        [
                            model.SiteEnergies[0].Entries[0] with { Energy = new EnergyEv(2.0) },
                            model.SiteEnergies[0].Entries[1],
                        ],
                    },
                    model.SiteEnergies[1],
                ],
            },
            _ => throw new ArgumentOutOfRangeException(nameof(section)),
        };

        Assert.NotEqual(ProjectHasher.ComputeEnergy(model), ProjectHasher.ComputeEnergy(changed));
    }

    [Fact]
    public void ComputeEnergyMatchesCanonicalDefaultAndIsStable()
    {
        var empty = new ModelDefinition();
        var explicitDefault = new ModelDefinition
        {
            StableEnvironment = new StableEnvironment(new LengthAngstrom(1.0), []),
            UnstableEnvironments = [],
            Clusters = [],
            SiteEnergies = [],
        };

        Assert.Equal(ProjectHasher.ComputeEnergy(empty), ProjectHasher.ComputeEnergy(explicitDefault));
        Assert.Equal(ProjectHasher.ComputeEnergy(empty), ProjectHasher.ComputeEnergy(new ModelDefinition()));
    }

    private static InteractionFilter NewFilter() => new(
        new LengthAngstrom(1.0),
        new LengthAngstrom(2.0),
        EnergyTestModels.CeriumSiteId,
        EnergyTestModels.OxygenSiteId);
}
