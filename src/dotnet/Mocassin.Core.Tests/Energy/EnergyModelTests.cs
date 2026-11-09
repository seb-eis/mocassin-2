// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;
using Mocassin.Core.Units;
using Mocassin.Packages.Projects;

namespace Mocassin.Core.Tests.Energy;

[Trait("Tier", "PR-fast")]
public sealed class EnergyModelTests
{
    [Fact]
    public void MissingStableEnvironmentLoadsDefaultAndEmptySectionsAreOmitted()
    {
        var project = EnergyTestModels.CreateProject(new ModelDefinition());

        using var document = JsonDocument.Parse(ProjectCanonicalizer.ToCanonicalJson(project));
        var model = document.RootElement.GetProperty("model");
        var propertyNames = model.EnumerateObject().Select(static property => property.Name).ToArray();

        Assert.DoesNotContain("stableEnvironment", propertyNames);
        Assert.DoesNotContain("unstableEnvironments", propertyNames);
        Assert.DoesNotContain("clusters", propertyNames);
        Assert.DoesNotContain("siteEnergies", propertyNames);

        var loaded = ProjectJson.Deserialize(ProjectJson.Serialize(project));
        Assert.Equal(StableEnvironment.Default.Cutoff.Value, loaded.Model.StableEnvironment.Cutoff.Value);
        Assert.Empty(loaded.Model.StableEnvironment.Filters);
        Assert.Empty(loaded.Model.UnstableEnvironments);
        Assert.Empty(loaded.Model.Clusters);
        Assert.Empty(loaded.Model.SiteEnergies);
    }

    [Fact]
    public void ExplicitDefaultStableEnvironmentIsOmittedOnWrite()
    {
        var model = new ModelDefinition
        {
            StableEnvironment = new StableEnvironment(StableEnvironment.Default.Cutoff, []),
        };

        using var document = JsonDocument.Parse(
            ProjectCanonicalizer.ToCanonicalJson(EnergyTestModels.CreateProject(model)));
        var modelElement = document.RootElement.GetProperty("model");

        Assert.False(modelElement.TryGetProperty("stableEnvironment", out _));
    }

    [Fact]
    public void EnergySectionsRoundTripAndPreserveSemanticOffsetOrder()
    {
        var project = EnergyTestModels.CreateProject(EnergyTestModels.CreateEnergyOnlyModel());

        var loaded = ProjectJson.Deserialize(ProjectCanonicalizer.ToCanonicalUtf8(project));

        Assert.Equal(
            ProjectCanonicalizer.ToCanonicalJson(project),
            ProjectCanonicalizer.ToCanonicalJson(loaded));
        Assert.Equal(6.0, loaded.Model.StableEnvironment.Cutoff.Value);
        Assert.Equal(2, loaded.Model.StableEnvironment.Filters.Length);
        Assert.Single(loaded.Model.UnstableEnvironments);
        Assert.Equal(2, loaded.Model.Clusters.Length);
        Assert.Equal(2, loaded.Model.SiteEnergies.Length);
        Assert.Equal(
            EnergyTestModels.MigrationCluster.OffsetVectors.ToArray(),
            loaded.Model.Clusters.Single(static cluster => cluster.Id == EnergyTestModels.ClusterIdA)
                .OffsetVectors.ToArray());
    }

    [Fact]
    public async Task ProjectStoreSaveAndLoadPreservesEnergySections()
    {
        var directory = Directory.CreateTempSubdirectory("mocassin-energy-");
        try
        {
            var path = Path.Combine(directory.FullName, "energy.mocprj");
            var store = new ProjectStore();
            var project = EnergyTestModels.CreateProject(EnergyTestModels.CreateCombinedModel());

            var saved = await store.SaveAsync(project, path);
            var loaded = await store.LoadAsync(path);

            Assert.True(saved.IsSuccess);
            Assert.True(loaded.IsSuccess);
            Assert.Equal(
                ProjectCanonicalizer.ToCanonicalJson(saved.SavedProject!),
                ProjectCanonicalizer.ToCanonicalJson(loaded.Project!));
            Assert.Equal(2, loaded.Project!.Model.Clusters.Length);
            Assert.Equal(2, loaded.Project.Model.SiteEnergies.Length);
            Assert.Equal(
                EnergyTestModels.UnstableEnvironment.Filters.ToArray(),
                loaded.Project.Model.UnstableEnvironments.Single().Filters.ToArray());
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void CanonicalModelSortsEnergyCollectionsAndFilters()
    {
        var model = EnergyTestModels.CreateEnergyOnlyModel();
        var shuffled = model with
        {
            StableEnvironment = model.StableEnvironment with
            {
                Filters = [.. model.StableEnvironment.Filters.Reverse()],
            },
            UnstableEnvironments = [.. model.UnstableEnvironments.Reverse()],
            Clusters = [.. model.Clusters.Reverse()],
            SiteEnergies =
            [
                .. model.SiteEnergies
                    .Reverse()
                    .Select(static set => set with { Entries = [.. set.Entries.Reverse()] }),
            ],
        };

        Assert.Equal(
            ProjectCanonicalizer.ToCanonicalJson(EnergyTestModels.CreateProject(model)),
            ProjectCanonicalizer.ToCanonicalJson(EnergyTestModels.CreateProject(shuffled)));
    }

    [Fact]
    public void ReorderingClusterOffsetsChangesCanonicalOutput()
    {
        var model = EnergyTestModels.CreateEnergyOnlyModel();
        var reordered = model with
        {
            Clusters =
            [
                model.Clusters[0],
                model.Clusters[1] with { OffsetVectors = [.. model.Clusters[1].OffsetVectors.Reverse()] },
            ],
        };

        Assert.NotEqual(
            ProjectCanonicalizer.ToCanonicalJson(EnergyTestModels.CreateProject(model)),
            ProjectCanonicalizer.ToCanonicalJson(EnergyTestModels.CreateProject(reordered)));
    }

    [Fact]
    public void EnergyEditChangesModelAndDocumentHashes()
    {
        var model = EnergyTestModels.CreateEnergyOnlyModel();
        var changed = model with
        {
            StableEnvironment = model.StableEnvironment with
            {
                Cutoff = new LengthAngstrom(7.0),
            },
        };

        Assert.NotEqual(
            ProjectHasher.Compute(EnergyTestModels.CreateProject(model)),
            ProjectHasher.Compute(EnergyTestModels.CreateProject(changed)));
        Assert.NotEqual(
            ProjectHasher.ComputeDocument(EnergyTestModels.CreateProject(model)),
            ProjectHasher.ComputeDocument(EnergyTestModels.CreateProject(changed)));
    }
}
