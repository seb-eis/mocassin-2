// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Validation;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Packages.Tests.Projects.Validation;

[Trait("Tier", "PR-fast")]
public sealed class ProjectValidationPipelineTests
{
    private static readonly DateTimeOffset now = DateTimeOffset.Parse("2026-11-11T12:00:00Z", CultureInfo.InvariantCulture);

    [Fact]
    public void InMemoryLoadIsSkippedWithoutCallerDiagnostics()
    {
        var project = CreateProject();
        var result = new ProjectValidationPipeline().Validate(project);

        Assert.Null(result.LoadFailure);
        Assert.Equal(project.Metadata.ProjectId, result.ProjectId);
        Assert.Equal(ProjectHasher.Compute(project), result.ModelHash);
        Assert.Equal(ProjectHasher.ComputeDocument(project), result.DocumentHash);
        Assert.True(result.IsValid);
        Assert.Null(result.Statistics);

        var stages = result.Stages;
        Assert.Equal(
            [ValidationStage.Load, ValidationStage.Model, ValidationStage.CrossDomain, ValidationStage.Feasibility],
            stages.Select(static stage => stage.Stage));
        Assert.Equal(ValidationStageStatus.Skipped, stages[0].Status);
        Assert.Equal(0, stages[0].DiagnosticCount);
        Assert.Equal(TimeSpan.Zero, stages[0].Elapsed);
        Assert.Equal(ValidationStageStatus.Completed, stages[1].Status);
        Assert.Equal(ValidationStageStatus.Completed, stages[2].Status);
        Assert.Equal(ValidationStageStatus.Completed, stages[3].Status);
    }

    [Fact]
    public void InMemoryLoadCompletesWithCallerDiagnostics()
    {
        var warning = Warning("PROJ999", "Caller-owned warning.");
        var result = new ProjectValidationPipeline().Validate(CreateProject(), [warning]);

        Assert.Equal(ValidationStageStatus.Completed, result.Stages[0].Status);
        Assert.Equal(1, result.Stages[0].DiagnosticCount);
        Assert.Equal(TimeSpan.Zero, result.Stages[0].Elapsed);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "PROJ999");
    }

    [Fact]
    public void InMemoryLoadFailureStopsThePipeline()
    {
        var error = Error("PROJ998", "Caller-owned load error.");
        var result = new ProjectValidationPipeline().Validate(CreateProject(), [error]);

        Assert.Equal(ValidationStageStatus.Failed, result.Stages[0].Status);
        Assert.All(
            result.Stages.Skip(1),
            static stage => Assert.Equal(ValidationStageStatus.Skipped, stage.Status));
        Assert.Single(result.Diagnostics);
        Assert.Equal("PROJ998", result.Diagnostics[0].Code);
        Assert.False(result.IsValid);
        Assert.Null(result.Statistics);
    }

    [Fact]
    public void ModelErrorSkipsCrossDomainAndFeasibility()
    {
        var result = new ProjectValidationPipeline().Validate(CreateProjectWithUnknownReference());

        Assert.Equal(ValidationStageStatus.Failed, result.Stages[1].Status);
        Assert.Equal(ValidationStageStatus.Skipped, result.Stages[2].Status);
        Assert.Equal(ValidationStageStatus.Skipped, result.Stages[3].Status);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void BuildStagesMustDeclareBuild()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new ProjectValidationPipeline(additionalStages: [new RecordingStage(ValidationStage.Model)]));

        Assert.Contains("Build", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildStageRunsOnlyWhenRequestedAndACompiledModelIsPresent()
    {
        var project = LoadCeria();
        var model = new ModelCompiler().Compile(project).Model!;
        var stage = new RecordingStage(ValidationStage.Build);
        var pipeline = new ProjectValidationPipeline(additionalStages: [stage]);

        var without = pipeline.Validate(project);
        Assert.Equal(0, stage.Calls);
        Assert.DoesNotContain(without.Stages, static item => item.Stage == ValidationStage.Build);

        var withoutModel = pipeline.Validate(
            project,
            options: new ValidationOptions { RunBuildStage = true });
        Assert.Equal(0, stage.Calls);
        var skipped = Assert.Single(withoutModel.Stages.Where(static item => item.Stage == ValidationStage.Build));
        Assert.Equal(ValidationStageStatus.Skipped, skipped.Status);

        var with = pipeline.Validate(
            project,
            options: new ValidationOptions { RunBuildStage = true },
            compiledModel: model);
        Assert.Equal(1, stage.Calls);
        var build = Assert.Single(with.Stages.Where(static item => item.Stage == ValidationStage.Build));
        Assert.Equal(ValidationStageStatus.Completed, build.Status);
    }

    [Fact]
    public void BuildStageIsSkippedAfterEarlierError()
    {
        var stage = new RecordingStage(ValidationStage.Build);
        var pipeline = new ProjectValidationPipeline(additionalStages: [stage]);
        var project = LoadCeria() with { BuildTemplates = CreateProjectWithUnknownReference().BuildTemplates };
        var model = new ModelCompiler().Compile(project).Model!;

        var result = pipeline.Validate(
            project,
            options: new ValidationOptions { RunBuildStage = true },
            compiledModel: model);

        Assert.Equal(0, stage.Calls);
        var build = Assert.Single(result.Stages.Where(static item => item.Stage == ValidationStage.Build));
        Assert.Equal(ValidationStageStatus.Skipped, build.Status);
    }

    [Fact]
    public void ValidationDoesNotMutateTheProject()
    {
        var project = CreateProject();
        var modelHash = ProjectHasher.Compute(project);
        var documentHash = ProjectHasher.ComputeDocument(project);

        _ = new ProjectValidationPipeline().Validate(project, options: ValidationOptions.Default with { IncludeStatistics = true });

        Assert.Equal(modelHash, ProjectHasher.Compute(project));
        Assert.Equal(documentHash, ProjectHasher.ComputeDocument(project));
    }

    [Fact]
    public void DuplicateDiagnosticsAreEmittedOnce()
    {
        var warning = Warning("PROJ999", "Duplicate.");
        var result = new ProjectValidationPipeline().Validate(CreateProject(), [warning, warning]);

        Assert.Single(result.Diagnostics);
        Assert.Equal(1, result.Stages[0].DiagnosticCount);
        Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Code == "PROJ999");
    }

    [Fact]
    public void DiagnosticsAreSortedAcrossStages()
    {
        var first = Warning("ZZZ999", "Load warning.");
        var result = new ProjectValidationPipeline().Validate(
            LoadCeria(),
            [first]);

        var codes = result.Diagnostics.Select(static diagnostic => diagnostic.Code).ToArray();
        Assert.Equal(codes.Order(StringComparer.Ordinal), codes);
    }

    [Fact]
    public void WarningsAsErrorsChangesValidityButNotSummary()
    {
        var result = new ProjectValidationPipeline().Validate(
            CreateProject(),
            [Warning("PROJ999", "Warning.")],
            new ValidationOptions { WarningsAsErrors = true });

        Assert.False(result.IsValid);
        Assert.Equal(0, result.Summary.Errors);
        Assert.Equal(1, result.Summary.Warnings);
        Assert.Equal(DiagnosticSeverity.Warning, result.Diagnostics[0].Severity);
    }

    [Fact]
    public void InvalidCellCountsAreProgrammerErrors()
    {
        var pipeline = new ProjectValidationPipeline();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            pipeline.Validate(CreateProject(), options: new ValidationOptions { CellCountA = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            pipeline.Validate(CreateProject(), options: new ValidationOptions { CellCountB = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            pipeline.Validate(CreateProject(), options: new ValidationOptions { CellCountC = 0 }));
    }

    [Fact]
    public async Task ValidateFileAsyncLoadsCeriaWithoutDoubleValidation()
    {
        var validatorCalls = 0;
        var store = new ProjectStore(
            TimeProvider.System,
            new ProjectStoreOptions { ValidateOnLoad = false },
            new AtomicProjectFileWriter(),
            _ =>
            {
                validatorCalls++;
                return ImmutableArray<Diagnostic>.Empty;
            });

        var result = await new ProjectValidationPipeline().ValidateFileAsync(
            RepositoryPaths.Combine("tests", "contracts", "project-ceria.mocprj"),
            store);

        Assert.Equal(0, validatorCalls);
        Assert.Null(result.LoadFailure);
        Assert.NotNull(result.ProjectId);
        Assert.NotNull(result.ModelHash);
        Assert.NotNull(result.DocumentHash);
        Assert.Equal(ValidationStageStatus.Completed, result.Stages[0].Status);
        Assert.Equal(ValidationStageStatus.Completed, result.Stages[1].Status);
        Assert.True(result.IsValid, string.Join(" | ", result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Fact]
    public async Task ValidateFileAsyncRejectsAValidatingStore()
    {
        var store = new ProjectStore(options: new ProjectStoreOptions { ValidateOnLoad = true });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            new ProjectValidationPipeline().ValidateFileAsync(
                RepositoryPaths.Combine("tests", "contracts", "project-ceria.mocprj"),
                store));
    }

    [Fact]
    public async Task MissingFileReturnsNotFoundWithoutIdentity()
    {
        using var directory = new TempDirectory();

        var result = await new ProjectValidationPipeline().ValidateFileAsync(directory.File("missing.mocprj"));

        Assert.Equal(ProjectLoadFailureKind.NotFound, result.LoadFailure);
        Assert.Null(result.ProjectId);
        Assert.Null(result.ModelHash);
        Assert.Null(result.DocumentHash);
        Assert.Equal(ValidationStageStatus.Failed, result.Stages[0].Status);
        Assert.All(
            result.Stages.Skip(1),
            static stage => Assert.Equal(ValidationStageStatus.Skipped, stage.Status));
        Assert.False(result.IsValid);
        Assert.Single(result.Diagnostics);
    }

    [Fact]
    public async Task MalformedJsonReturnsInvalidContent()
    {
        using var directory = new TempDirectory();
        var path = directory.File("malformed.mocprj");
        await File.WriteAllTextAsync(path, "{ not json");

        var result = await new ProjectValidationPipeline().ValidateFileAsync(path);

        Assert.Equal(ProjectLoadFailureKind.InvalidContent, result.LoadFailure);
        Assert.Null(result.ProjectId);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task OversizedFileReturnsTooLarge()
    {
        using var directory = new TempDirectory();
        var path = directory.File("large.mocprj");
        await File.WriteAllTextAsync(path, new string(' ', 128));
        var store = new ProjectStore(
            options: new ProjectStoreOptions { ValidateOnLoad = false, MaxDocumentBytes = 16 });

        var result = await new ProjectValidationPipeline().ValidateFileAsync(path, store);

        Assert.Equal(ProjectLoadFailureKind.TooLarge, result.LoadFailure);
    }

    private static ProjectModel LoadCeria()
    {
        var project = ProjectJson.Deserialize(
            File.ReadAllBytes(RepositoryPaths.Combine("tests", "contracts", "project-ceria.mocprj")));
        var diagnostics = ProjectValidationService.Validate(project);
        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        return project;
    }

    private static ProjectModel CreateProject(string name = "Ceria") =>
        new(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e6f"),
                new string('a', 64),
                name,
                null,
                now,
                now),
            new ModelDefinition(),
            [],
            [],
            [],
            ViewSettings.Default);

    private static ProjectModel CreateProjectWithUnknownReference()
    {
        var build = new BuildTemplate(
            BuildTemplateId.New(),
            "Broken build",
            JobTemplateId.New(),
            ParameterizationId.New(),
            "broken");
        return CreateProject() with { BuildTemplates = [build] };
    }

    private static Diagnostic Warning(string code, string message) =>
        new(code, DiagnosticSeverity.Warning, message);

    private static Diagnostic Error(string code, string message) =>
        new(code, DiagnosticSeverity.Error, message);

    private sealed class RecordingStage(ValidationStage stage) : IValidationStage
    {
        public int Calls { get; private set; }

        public ValidationStage Stage { get; } = stage;

        public ImmutableArray<Diagnostic> Validate(ValidationContext context, CancellationToken cancellationToken)
        {
            Calls++;
            return ImmutableArray<Diagnostic>.Empty;
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "mocassin-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string File(string name) => System.IO.Path.Combine(Path, name);

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort test cleanup.
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort test cleanup.
            }
        }
    }
}
