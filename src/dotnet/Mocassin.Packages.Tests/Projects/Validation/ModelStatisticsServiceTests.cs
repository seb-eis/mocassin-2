// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Validation;
using Mocassin.Symmetry.Energy;
using Mocassin.Symmetry.Structure;
using Mocassin.Symmetry.Transitions;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Packages.Tests.Projects.Validation;

[Trait("Tier", "PR-fast")]
public sealed class ModelStatisticsServiceTests
{
    private static readonly ValidationOptions options = new()
    {
        IncludeStatistics = true,
        CellCountA = 2,
        CellCountB = 1,
        CellCountC = 3,
    };

    [Fact]
    public void StatisticsMatchThePreviewSources()
    {
        var project = LoadCeria();
        var structurePreview = new StructurePreviewService();
        var energyPreview = new EnergyPreviewService();
        var transitionPreview = new TransitionPreviewService();
        var service = new ModelStatisticsService(structurePreview, energyPreview, transitionPreview);

        var statistics = service.TryCreate(project, options, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.NotNull(statistics);
        var structure = structurePreview.TryCreatePreview(
            project.Model.Structure,
            options.CellCountA,
            options.CellCountB,
            options.CellCountC,
            out _);
        var energy = energyPreview.TryCreatePreview(
            project.Model,
            options.CellCountA,
            options.CellCountB,
            options.CellCountC,
            out _);
        Assert.NotNull(structure);
        Assert.NotNull(energy);

        Assert.Equal(project.Model.Particles.Length, statistics.ParticleCount);
        Assert.Equal(project.Model.ParticleSets.Length, statistics.ParticleSetCount);
        Assert.Equal(project.Model.Structure.Sites.Length, statistics.ReferenceSiteCount);
        Assert.Equal(project.Model.KineticTransitions.Length, statistics.KineticTransitionCount);
        Assert.Equal(project.Model.MetropolisTransitions.Length, statistics.MetropolisTransitionCount);
        Assert.Equal(project.Model.Clusters.Length, statistics.ClusterCount);
        Assert.Equal(structure.PositionList.Length, statistics.P1PositionCount);
        Assert.Equal(statistics.P1PositionCount, statistics.StablePositionCount + statistics.UnstablePositionCount);
        Assert.Equal(energy.UniquePairInteractionCount, statistics.EstimatedPairInteractionCount);
        Assert.Equal(
            energy.ClusterCosts.Sum(static cluster => cluster.UniquePermutationCount),
            statistics.EstimatedUniqueClusterPermutations);
        Assert.Equal(energy.EstimatedMemoryBytes, statistics.EstimatedMemoryBytes);
        Assert.Equal(options.CellCountA, statistics.CellCountA);
        Assert.Equal(options.CellCountB, statistics.CellCountB);
        Assert.Equal(options.CellCountC, statistics.CellCountC);
        Assert.Equal(
            statistics.P1PositionCount * options.CellCountA * options.CellCountB * options.CellCountC,
            statistics.EstimatedPositions);
    }

    [Fact]
    public void EstimatedRuleCountIsThePreviewSum()
    {
        var project = LoadCeria();
        var transitionPreview = new TransitionPreviewService();
        var expected = 0L;
        foreach (var transition in project.Model.KineticTransitions)
        {
            expected += transitionPreview.TryCreatePreview(
                transition,
                project,
                options.CellCountA,
                options.CellCountB,
                options.CellCountC,
                out _)!.ExpectedRuleCount;
        }

        foreach (var transition in project.Model.MetropolisTransitions)
        {
            expected += transitionPreview.TryCreatePreview(
                transition,
                project,
                options.CellCountA,
                options.CellCountB,
                options.CellCountC,
                out _)!.ExpectedRuleCount;
        }

        var service = new ModelStatisticsService(
            new StructurePreviewService(),
            new EnergyPreviewService(),
            transitionPreview);

        var statistics = service.TryCreate(project, options, out _);

        Assert.NotNull(statistics);
        Assert.Equal(expected, statistics.EstimatedRuleCount);
    }

    [Fact]
    public void PipelineReturnsStatisticsOnlyWhenRequested()
    {
        var project = LoadCeria();
        var pipeline = new ProjectValidationPipeline();

        var without = pipeline.Validate(project);
        var with = pipeline.Validate(project, options: new ValidationOptions { IncludeStatistics = true });

        Assert.Null(without.Statistics);
        Assert.NotNull(with.Statistics);
        Assert.Equal(without.Diagnostics, with.Diagnostics);
        Assert.Equal(without.IsValid, with.IsValid);
    }

    [Fact]
    public void StatisticsAreNullAfterAModelError()
    {
        var project = LoadCeria();
        var broken = project with
        {
            BuildTemplates =
            [
                new BuildTemplate(
                    BuildTemplateId.New(),
                    "Broken build",
                    JobTemplateId.New(),
                    ParameterizationId.New(),
                    "broken"),
            ],
        };

        var result = new ProjectValidationPipeline().Validate(
            broken,
            options: new ValidationOptions { IncludeStatistics = true });

        Assert.False(result.IsValid);
        Assert.Null(result.Statistics);
        Assert.Equal(ValidationStageStatus.Failed, result.Stages[1].Status);
    }

    [Fact]
    public void StatisticsKeepCellCountsAndDoNotMutateTheProject()
    {
        var project = LoadCeria();
        var modelHash = ProjectHasher.Compute(project);
        var documentHash = ProjectHasher.ComputeDocument(project);

        var result = new ProjectValidationPipeline().Validate(
            project,
            options: new ValidationOptions { IncludeStatistics = true, CellCountA = 4, CellCountB = 5, CellCountC = 6 });

        Assert.NotNull(result.Statistics);
        Assert.Equal(4, result.Statistics.CellCountA);
        Assert.Equal(5, result.Statistics.CellCountB);
        Assert.Equal(6, result.Statistics.CellCountC);
        Assert.Equal(modelHash, ProjectHasher.Compute(project));
        Assert.Equal(documentHash, ProjectHasher.ComputeDocument(project));
    }

    [Fact]
    public void StatisticsPartitionPositionsByStability()
    {
        var project = LoadCeria();
        var result = new ProjectValidationPipeline().Validate(
            project,
            options: new ValidationOptions { IncludeStatistics = true });

        Assert.NotNull(result.Statistics);
        Assert.True(result.Statistics.UnstablePositionCount > 0);
        Assert.True(result.Statistics.StablePositionCount > 0);
        Assert.Equal(
            result.Statistics.P1PositionCount,
            result.Statistics.StablePositionCount + result.Statistics.UnstablePositionCount);
    }

    private static ProjectModel LoadCeria()
    {
        var project = ProjectJson.Deserialize(
            File.ReadAllBytes(RepositoryPaths.Combine("tests", "contracts", "project-ceria.mocprj")));
        var diagnostics = ProjectValidationService.Validate(project);
        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        return project;
    }
}
