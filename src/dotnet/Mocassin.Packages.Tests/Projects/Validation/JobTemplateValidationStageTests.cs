// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects.Validation;
using Mocassin.Packages.Tests.Parameterization;

namespace Mocassin.Packages.Tests.Projects.Validation;

/// <summary>
/// Verifies the compiled-bound Step 10 build stage against the real ceria sample and targeted project variants.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class JobTemplateValidationStageTests
{
    private const string ceriaSweep = "samples/jobs/ceria-temperature-sweep.mocprj";

    private static readonly CellSiteId ceriumSite =
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c04"));

    private static readonly CellSiteId oxygenSite1 =
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f15"));

    private static readonly CellSiteId oxygenSite2 =
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f17"));

    private static readonly ParticleId oxygenParticle =
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f10"));

    private static readonly ParticleId vacancyParticle =
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f11"));

    private static readonly ParticleId cerium4PlusParticle =
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c01"));

    private static readonly ParticleId cerium3PlusParticle =
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c02"));

    [Fact]
    public void RealCompiledModelProducesNoJobErrorsForTheSample()
    {
        var (project, model) = ParameterizationTestSupport.LoadAndCompile(ceriaSweep);

        var diagnostics = RunStage(project, model);

        Assert.DoesNotContain(diagnostics, static diagnostic =>
            diagnostic.Code.StartsWith("JOB", StringComparison.Ordinal) &&
            diagnostic.Severity == DiagnosticSeverity.Error);

        var result = new ProjectValidationPipeline(additionalStages: [new JobTemplateValidationStage()])
            .Validate(
                project,
                options: new ValidationOptions { RunBuildStage = true },
                compiledModel: model);
        Assert.True(result.IsValid, string.Join(" | ", result.Diagnostics.Select(static item => item.ToString())));
        var build = Assert.Single(result.Stages.Where(static item => item.Stage == ValidationStage.Build));
        Assert.Equal(ValidationStageStatus.Completed, build.Status);
    }

    [Fact]
    public void StageReturnsNoDiagnosticsWithoutACompiledModel()
    {
        var (project, _) = ParameterizationTestSupport.LoadAndCompile(ceriaSweep);
        var context = new ValidationContext(
            project,
            ValidationOptions.Default,
            DiagnosticCatalog.Default,
            []);

        var diagnostics = new JobTemplateValidationStage().Validate(context, CancellationToken.None);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void TotalPositionOverflowIsJob004WithoutGeneratingAConfig()
    {
        var (project, model) = ParameterizationTestSupport.LoadAndCompile(ceriaSweep);
        var template = CreateTemplate(
            simulationIdFromSample,
            jobCount: 1,
            Config("huge", new LatticeRequest(256, 256, 256)));

        var diagnostics = RunStage(project with { JobTemplates = [template] }, model);

        Assert.Contains(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidCellSize &&
            diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.LargeSupercell);
    }

    [Fact]
    public void LargeSupercellIsJob014()
    {
        var (project, model) = ParameterizationTestSupport.LoadAndCompile(ceriaSweep);
        var template = CreateTemplate(
            simulationIdFromSample,
            jobCount: 1,
            Config("large", new LatticeRequest(32, 32, 32)));

        var diagnostics = RunStage(project with { JobTemplates = [template] }, model);

        Assert.Contains(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.LargeSupercell &&
            diagnostic.Severity == DiagnosticSeverity.Warning);
        Assert.DoesNotContain(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidCellSize);
    }

    [Fact]
    public void ImpossibleCompensationAfterConsumingTheDependentSourceIsJob012()
    {
        var (project, model) = ParameterizationTestSupport.LoadAndCompile(ceriaSweep);
        var primary = project.Find(dopingIdFromSample)!;
        var consumeOxygen = new Doping(
            DopingId.New(),
            "Consume oxygen",
            1,
            occupationExchangeIdFromSample,
            null,
            ChargeCompensation: false);
        var template = CreateTemplate(
            simulationIdFromSample,
            jobCount: 1,
            new JobConfig(
                JobConfigId.New(),
                "impossible",
                new LatticeRequest(10, 10, 10),
                Overrides: null,
                Dopings:
                [
                    new DopingValue(consumeOxygen.Id, 1.0),
                    new DopingValue(primary.Id, 0.01),
                ]));
        var changed = project with
        {
            Model = project.Model with
            {
                Dopings =
                [
                    consumeOxygen,
                    primary with { Priority = 2 },
                ],
            },
            JobTemplates = [template],
        };

        var diagnostics = RunStage(changed, model);

        Assert.Contains(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidCompensation &&
            diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void OptimizersThatRemoveEveryStartOptionAreJob009()
    {
        var (project, model) = ParameterizationTestSupport.LoadAndCompile(ceriaSweep);
        ImmutableArray<SelectionOptimizer> optimizers =
        [
            new SelectionOptimizer(ceriumSite, cerium4PlusParticle),
            new SelectionOptimizer(ceriumSite, cerium3PlusParticle),
            new SelectionOptimizer(oxygenSite1, oxygenParticle),
            new SelectionOptimizer(oxygenSite1, vacancyParticle),
            new SelectionOptimizer(oxygenSite2, oxygenParticle),
            new SelectionOptimizer(oxygenSite2, vacancyParticle),
        ];
        var template = CreateTemplate(
            simulationIdFromSample,
            jobCount: 1,
            [Config("tiny", new LatticeRequest(1, 1, 1))],
            optimizers);

        var diagnostics = RunStage(project with { JobTemplates = [template] }, model);

        Assert.Contains(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidOptimizer &&
            diagnostic.Severity == DiagnosticSeverity.Error &&
            diagnostic.Message.Contains("every selectable start option", StringComparison.Ordinal));
    }

    [Fact]
    public void TotalJobsAboveTheWarningThresholdIsJob013()
    {
        var (project, model) = ParameterizationTestSupport.LoadAndCompile(ceriaSweep);
        var configs = Enumerable
            .Range(0, 11)
            .Select(index => Config($"c{index}", new LatticeRequest(1, 1, 1)))
            .ToArray();
        var template = CreateTemplate(simulationIdFromSample, jobCount: 1_000, configs);

        var diagnostics = RunStage(project with { JobTemplates = [template] }, model);

        Assert.Contains(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.JobCountWarning &&
            diagnostic.Severity == DiagnosticSeverity.Warning);
        Assert.DoesNotContain(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.JobLimitExceeded);
    }

    [Fact]
    public void TotalJobsAboveTheHardLimitIsJob019()
    {
        var (project, model) = ParameterizationTestSupport.LoadAndCompile(ceriaSweep);
        var configs = Enumerable
            .Range(0, 100)
            .Select(index => Config($"c{index}", new LatticeRequest(1, 1, 1)))
            .ToArray();
        var template = CreateTemplate(simulationIdFromSample, jobCount: 1_000, configs);

        var diagnostics = RunStage(project with { JobTemplates = [template] }, model);

        Assert.Contains(diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.JobLimitExceeded &&
            diagnostic.Severity == DiagnosticSeverity.Error);
    }

    private static ImmutableArray<Diagnostic> RunStage(Project project, CompiledModel model)
    {
        var context = new ValidationContext(
            project,
            ValidationOptions.Default,
            DiagnosticCatalog.Default,
            [],
            model);
        return new JobTemplateValidationStage().Validate(context, CancellationToken.None);
    }

    private static JobTemplate CreateTemplate(
        SimulationId simulationId,
        int? jobCount,
        params JobConfig[] configs) =>
        CreateTemplate(simulationId, jobCount, configs, null);

    private static JobTemplate CreateTemplate(
        SimulationId simulationId,
        int? jobCount,
        JobConfig[] configs,
        ImmutableArray<SelectionOptimizer>? optimizers) => new(JobTemplateId.New(), "stage")
        {
            Collections =
            [
                new JobCollection(
                    JobCollectionId.New(),
                    "alpha",
                    simulationId,
                    BaseSettings: null,
                    JobCountPerConfig: jobCount,
                    RngSeed: null,
                    Optimizers: optimizers,
                    Configs: [.. configs]),
            ],
        };

    private static JobConfig Config(string name, LatticeRequest lattice) => new(
        JobConfigId.New(),
        name,
        lattice);

    private static readonly SimulationId simulationIdFromSample =
        SimulationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f70"));

    private static readonly DopingId dopingIdFromSample =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c07"));

    private static readonly OccupationExchangeId occupationExchangeIdFromSample =
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c06"));
}
