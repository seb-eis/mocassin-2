// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Editing;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Tests.TestSupport;
using Mocassin.Core.Units;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.Editing;

[Trait("Tier", "PR-fast")]
public sealed class ProjectEditorEnergyTests
{
    [Fact]
    public void StableAndUnstableEnvironmentEditsAreApplied()
    {
        var editor = new ProjectEditor();
        var project = EnergyTestModels.CreateProject(EnergyTestModels.CreateCombinedModel());
        var secondSiteId = CellSiteId.New();

        var stable = Apply(editor.SetStableEnvironmentCutoff(project, new LengthAngstrom(7.0)));
        Assert.Equal(7.0, stable.Model.StableEnvironment.Cutoff.Value);

        var updated = Apply(editor.UpdateUnstableEnvironment(
            stable,
            stable.Model.UnstableEnvironments[0] with { Cutoff = new LengthAngstrom(5.0) }));
        Assert.Equal(5.0, updated.Model.UnstableEnvironments[0].Cutoff.Value);

        var added = Apply(editor.AddUnstableEnvironment(
            updated with
            {
                Model = updated.Model with
                {
                    Structure = updated.Model.Structure with
                    {
                        Sites =
                        [
                            .. updated.Model.Structure.Sites,
                            new CellSite(
                                secondSiteId,
                                "Second unstable",
                                new Fractional3(0.5, 0.5, 0.5),
                                EnergyTestModels.OxygenSetId,
                                SiteStability.Unstable),
                        ],
                    },
                },
            },
            new UnstableEnvironment(
                secondSiteId,
                new LengthAngstrom(3.0),
                [])));
        Assert.Equal(2, added.Model.UnstableEnvironments.Length);

        var withoutCluster = Apply(editor.RemoveCluster(updated, EnergyTestModels.ClusterIdA));
        var removed = Apply(editor.RemoveUnstableEnvironment(
            withoutCluster,
            EnergyTestModels.UnstableSiteId));
        Assert.Empty(removed.Model.UnstableEnvironments);
    }

    [Fact]
    public void ClusterEditsPreserveOffsetOrder()
    {
        var editor = new ProjectEditor();
        var project = EnergyTestModels.CreateProject(EnergyTestModels.CreateCombinedModel());
        ImmutableArray<Fractional3> offsets =
        [
            new Fractional3(0.1, 0.0, 0.0),
            new Fractional3(0.2, 0.0, 0.0),
        ];

        var updated = Apply(editor.SetClusterOffsets(project, EnergyTestModels.ClusterIdA, offsets));
        var cluster = updated.Model.Clusters.Single(candidate => candidate.Id == EnergyTestModels.ClusterIdA);
        Assert.Equal(offsets.ToArray(), cluster.OffsetVectors.ToArray());

        var removed = Apply(editor.RemoveCluster(updated, EnergyTestModels.ClusterIdB));
        Assert.DoesNotContain(removed.Model.Clusters, candidate => candidate.Id == EnergyTestModels.ClusterIdB);
    }

    [Fact]
    public void SiteEnergySetAndClearAreApplied()
    {
        var editor = new ProjectEditor();
        var project = EnergyTestModels.CreateProject(EnergyTestModels.CreateCombinedModel());
        ImmutableArray<SiteEnergyEntry> entries =
        [
            new SiteEnergyEntry(EnergyTestModels.OxygenId, new EnergyEv(-0.25)),
            new SiteEnergyEntry(EnergyTestModels.VacancyId, new EnergyEv(0.5)),
        ];

        var updated = Apply(editor.SetSiteEnergies(
            project,
            new SiteEnergySet(EnergyTestModels.OxygenSiteId, entries)));
        var set = updated.Model.SiteEnergies.Single(candidate => candidate.SiteId == EnergyTestModels.OxygenSiteId);
        Assert.Equal(entries.ToArray(), set.Entries.ToArray());

        var cleared = Apply(editor.ClearSiteEnergies(updated, EnergyTestModels.OxygenSiteId));
        Assert.DoesNotContain(cleared.Model.SiteEnergies, candidate => candidate.SiteId == EnergyTestModels.OxygenSiteId);
    }

    [Fact]
    public void RemovingSiteCascadesEnergyReferences()
    {
        var editor = new ProjectEditor();
        var baseModel = EnergyTestModels.CreateCombinedModel();
        var model = baseModel with
        {
            StableEnvironment = baseModel.StableEnvironment with
            {
                Filters =
                [
                    new InteractionFilter(
                        new LengthAngstrom(0.0),
                        new LengthAngstrom(1.0),
                        EnergyTestModels.UnstableSiteId,
                        EnergyTestModels.OxygenSiteId),
                ],
            },
            SiteEnergies =
            [
                .. baseModel.SiteEnergies,
                new SiteEnergySet(
                    EnergyTestModels.UnstableSiteId,
                    [new SiteEnergyEntry(EnergyTestModels.OxygenId, new EnergyEv(0.2))]),
            ],
        };
        var project = EnergyTestModels.CreateProject(model);

        var result = Apply(editor.RemoveCellSiteAndEnergyReferences(project, EnergyTestModels.UnstableSiteId));

        Assert.DoesNotContain(result.Model.Structure.Sites, site => site.Id == EnergyTestModels.UnstableSiteId);
        Assert.DoesNotContain(
            result.Model.UnstableEnvironments,
            environment => environment.SiteId == EnergyTestModels.UnstableSiteId);
        Assert.DoesNotContain(
            result.Model.Clusters,
            cluster => cluster.CenterSiteId == EnergyTestModels.UnstableSiteId);
        Assert.DoesNotContain(
            result.Model.SiteEnergies,
            set => set.SiteId == EnergyTestModels.UnstableSiteId);
        Assert.DoesNotContain(
            result.Model.StableEnvironment.Filters,
            filter => filter.SiteAId == EnergyTestModels.UnstableSiteId ||
                filter.SiteBId == EnergyTestModels.UnstableSiteId);
    }

    [Fact]
    public void RebaseUpdatesEnergyReferences()
    {
        var editor = new ProjectEditor();
        var model = EnergyTestModels.CreateCombinedModel() with
        {
            StableEnvironment = new StableEnvironment(new LengthAngstrom(6.0), []),
            UnstableEnvironments = [],
            Clusters = [EnergyTestModels.SecondCluster],
            SiteEnergies =
            [
                new SiteEnergySet(
                    EnergyTestModels.CeriumSiteId,
                    [new SiteEnergyEntry(EnergyTestModels.CeriumId, new EnergyEv(1.0))]),
            ],
        };
        var project = EnergyTestModels.CreateProject(model);

        var result = Apply(editor.RebaseEnergyReferences(
            project,
            EnergyTestModels.CeriumSiteId,
            EnergyTestModels.OxygenSiteId));

        Assert.Equal(
            EnergyTestModels.OxygenSiteId,
            result.Model.Clusters.Single().CenterSiteId);
        Assert.Equal(
            EnergyTestModels.OxygenSiteId,
            result.Model.SiteEnergies.Single().SiteId);
    }

    private static ProjectModel Apply(ProjectEditResult result)
    {
        Assert.True(
            result.IsSuccess,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        return result.Project;
    }
}
