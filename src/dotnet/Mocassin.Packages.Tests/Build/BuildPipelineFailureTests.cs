// SPDX-License-Identifier: MIT

using System.Text.Json.Nodes;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Ids;
using Mocassin.Packages.Build;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.Packages.Tests.Build;

[Trait("Tier", "PR-fast")]
public sealed class BuildPipelineFailureTests
{
    [Fact]
    public async Task MissingProjectIsAnIoFailure()
    {
        using var test = await TestProject.CreateAsync();

        var result = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = System.IO.Path.Combine(test.Directory, "missing.mocprj"),
            Preview = true,
        });

        Assert.Equal(BuildStatus.Failed, result.Status);
        Assert.Equal(BuildFailureKind.Io, result.Failure);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "PROJ002");
    }

    [Fact]
    public async Task MalformedProjectIsALoadFailure()
    {
        using var test = await TestProject.CreateAsync();
        await File.WriteAllTextAsync(test.Path, "{ not json");

        var result = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
        });

        Assert.Equal(BuildStatus.Failed, result.Status);
        Assert.Equal(BuildFailureKind.Load, result.Failure);
    }

    [Fact]
    public async Task ProjectWithoutAJobTemplateIsATemplateSelectionFailure()
    {
        var path = RepositoryPaths.Combine("samples", "ceria", "ceria.mocprj");

        var result = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = path,
            Preview = true,
        });

        Assert.Equal(BuildFailureKind.TemplateSelection, result.Failure);
        Assert.Null(result.Selection);
    }

    [Fact]
    public async Task UnknownBuildTemplateCarriesAProj010Diagnostic()
    {
        using var test = await TestProject.CreateAsync();

        var result = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
            BuildTemplateId = BuildTemplateId.Create(Guid.NewGuid()),
        });

        Assert.Equal(BuildFailureKind.TemplateSelection, result.Failure);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "PROJ010");
    }

    [Fact]
    public async Task MalformedJobSelectionIsAJobSelectionFailureWithTheParserMessage()
    {
        using var test = await TestProject.CreateAsync();

        var result = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
            JobSelection = "0",
        });

        Assert.Equal(BuildFailureKind.JobSelection, result.Failure);
        Assert.NotNull(result.JobSelection);
        Assert.False(result.JobSelection.IsValid);
        Assert.Contains("1-based", result.JobSelection.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExistingTargetWithoutOverwriteIsAnIoFailureAndTheFileIsPreserved()
    {
        using var test = await TestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "existing.msl");
        var original = new byte[] { 9, 8, 7 };
        await File.WriteAllBytesAsync(target, original);

        var result = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            TargetPath = target,
            CreatedUtc = DateTimeOffset.UnixEpoch,
        });

        Assert.Equal(BuildFailureKind.Io, result.Failure);
        Assert.Equal(original, await File.ReadAllBytesAsync(target));
    }

    [Fact]
    public async Task WriterFailureWithoutAnIoExceptionIsAnInternalFailure()
    {
        using var test = await TestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "out.msl");
        var pipeline = BuildPipelineTestFactory.Create(
            writerFactory: (_, _) => throw new InvalidOperationException("writer defect"));

        var result = await pipeline.BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            TargetPath = target,
            CreatedUtc = DateTimeOffset.UnixEpoch,
        });

        Assert.Equal(BuildFailureKind.Internal, result.Failure);
        Assert.False(File.Exists(target));
    }

    [Fact]
    public async Task AStaleParameterizationSignatureIsAParameterizationFailure()
    {
        const string parameterizationId = "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01";
        using var test = await TestProject.CreateAsync(root =>
        {
            AddEmptyParameterization(root, parameterizationId, new string('0', 64));
        });
        var target = System.IO.Path.Combine(test.Directory, "out.msl");

        var result = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            TargetPath = target,
            ParameterizationId = ParameterizationId.Create(Guid.Parse(parameterizationId)),
        });

        Assert.Equal(BuildFailureKind.Parameterization, result.Failure);
        Assert.False(File.Exists(target));
    }

    [Fact]
    public async Task APreviewEvaluationFailureIsAPreviewFailure()
    {
        using var test = await TestProject.CreateAsync();
        var preview = new FailingPreviewService(new Mocassin.Packages.Jobs.LatticePreviewService());
        var pipeline = BuildPipelineTestFactory.Create(preview);
        BuildResult? result = null;

        await foreach (var buildEvent in pipeline.BuildAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
            Options = new BuildOptions { EventChannelCapacity = 1 },
        }))
        {
            if (buildEvent is BuildStageCompleted { Stage: BuildStage.ValidateBuild })
            {
                preview.FailNext = true;
            }

            if (buildEvent is BuildFinished finished)
            {
                result = finished.Result;
            }
        }

        Assert.NotNull(result);
        Assert.Equal(BuildFailureKind.Preview, result.Failure);
        Assert.Null(result.Preview);
    }

    [Fact]
    public async Task TheDefaultValidationPipelineMakesTheBuildStageASilentNoOpTrap()
    {
        var load = await new ProjectStore(options: new ProjectStoreOptions { ValidateOnLoad = false })
            .LoadAsync(TestProject.SamplePath);
        var model = new ModelCompiler().Compile(load.Project!).Model!;
        var options = new ValidationOptions { RunBuildStage = true };

        var defaultResult = new ProjectValidationPipeline().Validate(
            load.Project!,
            loadDiagnostics: load.Diagnostics,
            options: options,
            compiledModel: model);
        var configuredResult = new ProjectValidationPipeline(
                additionalStages: [new JobTemplateValidationStage()])
            .Validate(
                load.Project!,
                loadDiagnostics: load.Diagnostics,
                options: options,
                compiledModel: model);

        Assert.DoesNotContain(defaultResult.Stages, static stage => stage.Stage == ValidationStage.Build);
        Assert.Contains(configuredResult.Stages, static stage => stage.Stage == ValidationStage.Build);
    }

    [Fact]
    public async Task TheRegisteredBuildStageCatchesAnInvalidJobTemplate()
    {
        using var test = await TestProject.CreateAsync(root =>
        {
            root["jobTemplates"]![0]!["collections"]![0]!["configs"]![0]!["lattice"] = new JsonObject
            {
                ["a"] = 256,
                ["b"] = 256,
                ["c"] = 256,
            };
        });

        var result = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
        });

        Assert.Equal(BuildFailureKind.Validation, result.Failure);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "JOB004");
    }

    [Fact]
    public async Task IdentityMatchesTheDirectStep07To09Services()
    {
        using var test = await TestProject.CreateAsync();
        var load = await new ProjectStore(options: new ProjectStoreOptions { ValidateOnLoad = false })
            .LoadAsync(test.Path);
        var model = new ModelCompiler().Compile(load.Project!).Model!;
        var validation = new ProjectValidationPipeline(
                additionalStages: [new JobTemplateValidationStage()])
            .Validate(
                load.Project!,
                loadDiagnostics: load.Diagnostics,
                options: new ValidationOptions { RunBuildStage = true },
                compiledModel: model);
        var parameterized = new ParameterizationService()
            .Apply(load.Project!, model, template: null)
            .Model!;

        var result = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
        });

        Assert.Equal(load.Project!.Metadata.ProjectId, result.Identity.ProjectId);
        Assert.Equal(validation.ModelHash, result.Identity.ModelHash);
        Assert.Equal(validation.DocumentHash, result.Identity.DocumentHash);
        Assert.Equal(model.ModelSignature, result.Identity.ModelSignature);
        Assert.Equal(parameterized.Signature, result.Identity.ParameterizationSignature);
    }

    private static void AddEmptyParameterization(JsonObject root, string id, string modelSignature)
    {
        var array = root["parameterizations"] as JsonArray ?? [];
        root["parameterizations"] = array;
        array.Add(new JsonObject
        {
            ["id"] = id,
            ["name"] = "Empty explicit template",
            ["modelSignature"] = modelSignature,
            ["createdUtc"] = "2026-11-17T12:00:00.0000000Z",
            ["stablePairSets"] = new JsonArray(),
            ["unstablePairSets"] = new JsonArray(),
            ["groupSets"] = new JsonArray(),
            ["siteEnergyOverrides"] = new JsonArray(),
            ["transitionRuleSets"] = new JsonArray(),
        });
    }

    [Fact]
    public async Task InvalidRequestOptionsAreProgrammerErrors()
    {
        using var test = await TestProject.CreateAsync();
        var pipeline = BuildPipelineTestFactory.Create();

        await Assert.ThrowsAsync<ArgumentException>(() => pipeline.BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
        }));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => pipeline.BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
            MaxDegreeOfParallelism = 0,
        }));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => pipeline.BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
            Options = new BuildOptions { EventChannelCapacity = 0 },
        }));
    }
}
