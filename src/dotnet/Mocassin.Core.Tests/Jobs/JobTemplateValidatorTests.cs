// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.Jobs;

[Trait("Tier", "PR-fast")]
public sealed class JobTemplateValidatorTests
{
    [Fact]
    public void ValidTemplateHasNoJobDiagnostics()
    {
        var project = CreateProject(CreateValidTemplate());

        Assert.DoesNotContain(
            ProjectValidator.Validate(project),
            static diagnostic => diagnostic.Code.StartsWith("JOB", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingCollectionsIsJob001()
    {
        var template = new JobTemplate(TemplateId(900_000), "Empty") { Collections = [] };

        Assert.Contains(
            ProjectValidator.Validate(CreateProject(template)),
            diagnostic => diagnostic.Code == JobDiagnosticCodes.NoCollections);
    }

    [Fact]
    public void UnknownSimulationIsJob002()
    {
        var collection = CreateCollection() with
        {
            SimulationId = SimulationId.Create(Guid.Parse("11111111-1111-1111-1111-111111111111")),
        };

        Assert.Contains(
            ProjectValidator.Validate(CreateProject(CreateTemplate(collection))),
            diagnostic => diagnostic.Code == JobDiagnosticCodes.UnknownSimulation);
    }

    [Fact]
    public void EmptyConfigsIsJob003()
    {
        var collection = CreateCollection() with { Configs = [] };

        Assert.Contains(
            ProjectValidator.Validate(CreateProject(CreateTemplate(collection))),
            diagnostic => diagnostic.Code == JobDiagnosticCodes.NoConfigs);
    }

    [Fact]
    public void InvalidCellSizeIsJob004()
    {
        var config = CreateConfig(0) with { Lattice = new LatticeRequest(0, 12, 257) };

        Assert.Contains(
            ProjectValidator.Validate(CreateProject(CreateTemplate(CreateCollection(config)))),
            diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidCellSize);
    }

    [Fact]
    public void InvalidDopingFractionIsJob005()
    {
        var config = CreateConfig(0) with
        {
            Dopings =
            [
                new DopingValue(LatticeTestModels.CeriaReductionDoping.Id, double.NaN),
                new DopingValue(LatticeTestModels.IsovalentDoping.Id, 1.5),
            ],
        };

        var diagnostics = ProjectValidator.Validate(CreateProject(CreateTemplate(CreateCollection(config))));

        Assert.Equal(
            2,
            diagnostics.Count(static diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidDopingFraction));
    }

    [Fact]
    public void UnknownDopingIsJob006()
    {
        var config = CreateConfig(0) with
        {
            Dopings = [new DopingValue(DopingId.Create(Guid.NewGuid()), 0.01)],
        };

        Assert.Contains(
            ProjectValidator.Validate(CreateProject(CreateTemplate(CreateCollection(config)))),
            diagnostic => diagnostic.Code == JobDiagnosticCodes.UnknownDoping);
    }

    [Fact]
    public void DuplicateAndEmptyNamesAreJob007()
    {
        var first = CreateCollection(CreateConfig(0)) with { Name = "same" };
        var second = CreateCollection(CreateConfig(1)) with { Id = CollectionId(900_102), Name = "same" };
        var configs = ImmutableArray.Create(
            CreateConfig(2) with { Name = "duplicate" },
            CreateConfig(3) with { Name = "duplicate" },
            CreateConfig(4) with { Name = string.Empty });
        var third = CreateCollectionFrom(configs) with { Id = CollectionId(900_103), Name = string.Empty };

        var diagnostics = ProjectValidator.Validate(
            CreateProject(CreateTemplate(first, second, third)));

        Assert.Equal(
            4,
            diagnostics.Count(static diagnostic => diagnostic.Code == JobDiagnosticCodes.DuplicateName));
    }

    [Fact]
    public void JobCountOutsideRangeIsJob008()
    {
        var zero = CreateCollection() with { JobCountPerConfig = 0 };
        var high = CreateCollection() with { Id = CollectionId(900_104), JobCountPerConfig = 1001 };

        var diagnostics = ProjectValidator.Validate(CreateProject(CreateTemplate(zero, high)));

        Assert.Equal(
            2,
            diagnostics.Count(static diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidJobCount));
    }

    [Fact]
    public void InvalidOptimizerReferencesAreJob009()
    {
        var optimizers = ImmutableArray.Create(
            new SelectionOptimizer(CellSiteId.Create(Guid.NewGuid()), LatticeTestModels.Ce4Plus.Id),
            new SelectionOptimizer(LatticeTestModels.UnstableSite.Id, LatticeTestModels.Ce4Plus.Id),
            new SelectionOptimizer(LatticeTestModels.CeSite.Id, ParticleId.Create(Guid.NewGuid())),
            new SelectionOptimizer(LatticeTestModels.CeSite.Id, LatticeTestModels.O2Minus.Id));
        var collection = CreateCollection() with { Optimizers = optimizers };

        var diagnostics = ProjectValidator.Validate(CreateProject(CreateTemplate(collection)));

        Assert.Equal(
            4,
            diagnostics.Count(static diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidOptimizer));
    }

    [Fact]
    public void InvalidSettingsLayerIsJob015()
    {
        var collection = CreateCollection() with
        {
            BaseSettings = new JobSettings { TemperatureK = 0.0, PreRunMcsp = -1 },
        };

        var diagnostics = ProjectValidator.Validate(CreateProject(CreateTemplate(collection)));

        Assert.Equal(
            2,
            diagnostics.Count(static diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidSettings));
    }

    [Fact]
    public void DuplicateDopingAndOptimizerAreJob017()
    {
        var doping = new DopingValue(LatticeTestModels.CeriaReductionDoping.Id, 0.01);
        var config = CreateConfig(0) with { Dopings = [doping, doping] };
        var optimizer = new SelectionOptimizer(LatticeTestModels.CeSite.Id, LatticeTestModels.Ce4Plus.Id);
        var collection = CreateCollection(config) with { Optimizers = [optimizer, optimizer] };

        var diagnostics = ProjectValidator.Validate(CreateProject(CreateTemplate(collection)));

        Assert.Equal(
            2,
            diagnostics.Count(static diagnostic => diagnostic.Code == JobDiagnosticCodes.DuplicateReference));
    }

    [Fact]
    public void InvalidExtensionIsJob018()
    {
        using var document = JsonDocument.Parse("[1,2,3]");
        var settings = new JobSettings
        {
            Extensions = [new JobExtension("bad", document.RootElement.Clone())],
        };
        var collection = CreateCollection() with { BaseSettings = settings };

        Assert.Contains(
            ProjectValidator.Validate(CreateProject(CreateTemplate(collection))),
            diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidExtension);
    }

    [Fact]
    public void TemplateAndCollectionCapsAreJob020()
    {
        var templates = ImmutableArray.CreateBuilder<JobTemplate>();
        for (var index = 0; index < 101; index++)
        {
            templates.Add(CreateTemplate(CreateCollection(CreateConfig(index))) with { Id = TemplateId(901_000 + index) });
        }

        var project = LatticeTestModels.CreateProject(CreateModel()) with
        {
            JobTemplates = templates.ToImmutable(),
        };

        Assert.Contains(
            ProjectValidator.Validate(project),
            diagnostic => diagnostic.Code == JobDiagnosticCodes.LimitExceeded);

        var collections = ImmutableArray.CreateBuilder<JobCollection>();
        for (var index = 0; index < 101; index++)
        {
            collections.Add(CreateCollection(CreateConfig(index)) with { Id = CollectionId(902_000 + index) });
        }

        Assert.Contains(
            ProjectValidator.Validate(CreateProject(CreateTemplateFrom(collections.ToImmutable()))),
            diagnostic => diagnostic.Code == JobDiagnosticCodes.LimitExceeded);
    }

    [Fact]
    public void ConfigDopingAndOptimizerCapsAreJob020()
    {
        var configs = ImmutableArray.CreateBuilder<JobConfig>();
        for (var index = 0; index < 1001; index++)
        {
            configs.Add(CreateConfig(index));
        }

        Assert.Contains(
            ProjectValidator.Validate(CreateProject(CreateTemplate(CreateCollectionFrom(configs.ToImmutable())))),
            diagnostic => diagnostic.Code == JobDiagnosticCodes.LimitExceeded);

        var doping = new DopingValue(LatticeTestModels.CeriaReductionDoping.Id, 0.01);
        var manyDopings = ImmutableArray.CreateBuilder<DopingValue>();
        var manyOptimizers = ImmutableArray.CreateBuilder<SelectionOptimizer>();
        for (var index = 0; index < 1001; index++)
        {
            manyDopings.Add(doping);
            manyOptimizers.Add(new SelectionOptimizer(LatticeTestModels.CeSite.Id, LatticeTestModels.Ce4Plus.Id));
        }

        var config = CreateConfig(0) with { Dopings = manyDopings.ToImmutable() };
        var collection = CreateCollection(config) with { Optimizers = manyOptimizers.ToImmutable() };

        Assert.Contains(
            ProjectValidator.Validate(CreateProject(CreateTemplate(collection))),
            diagnostic => diagnostic.Code == JobDiagnosticCodes.LimitExceeded);
    }

    [Fact]
    public void TotalJobsAboveWarningThresholdIsJob013()
    {
        var configs = ImmutableArray.CreateBuilder<JobConfig>();
        for (var index = 0; index < 11; index++)
        {
            configs.Add(CreateConfig(index));
        }

        var collection = CreateCollectionFrom(configs.ToImmutable()) with { JobCountPerConfig = 1000 };
        var diagnostics = ProjectValidator.Validate(CreateProject(CreateTemplate(collection)));

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == JobDiagnosticCodes.JobCountWarning);
        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Code == JobDiagnosticCodes.JobLimitExceeded);
    }

    [Fact]
    public void TotalJobsAboveHardLimitIsJob019()
    {
        var configs = ImmutableArray.CreateBuilder<JobConfig>();
        for (var index = 0; index < 100; index++)
        {
            configs.Add(CreateConfig(index));
        }

        var collection = CreateCollectionFrom(configs.ToImmutable()) with { JobCountPerConfig = 1000 };
        var diagnostics = ProjectValidator.Validate(CreateProject(CreateTemplate(collection)));

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == JobDiagnosticCodes.JobLimitExceeded);
    }

    private static ProjectModel CreateProject(JobTemplate template) =>
        LatticeTestModels.CreateProject(CreateModel()) with { JobTemplates = [template] };

    private static ModelDefinition CreateModel() =>
        LatticeTestModels.CreateLatticeModel() with
        {
            Simulations = [SimulationTestModels.CreateKinetic(transitionIds: [])],
        };

    private static JobTemplate CreateValidTemplate() =>
        CreateTemplate(CreateCollection(CreateConfig(0)));

    private static JobTemplate CreateTemplate(params JobCollection[] collections) =>
        new(TemplateId(900_000), "KMC series") { Collections = [.. collections] };

    private static JobCollection CreateCollection(params JobConfig[] configs) =>
        new(
            CollectionId(900_100),
            "T773",
            SimulationTestModels.KineticId,
            new JobSettings { TemperatureK = 773.0 },
            4,
            null,
            [new SelectionOptimizer(LatticeTestModels.CeSite.Id, LatticeTestModels.Ce4Plus.Id)],
            [.. configs]);

    private static JobConfig CreateConfig(int index) =>
        new(
            ConfigId(900_200 + index),
            $"x{index:000}",
            new LatticeRequest(10, 10, 10),
            new JobSettings { TargetMcsp = 200 },
            [new DopingValue(LatticeTestModels.CeriaReductionDoping.Id, 0.01)]);

    private static JobTemplateId TemplateId(int value) => JobTemplateId.Create(GuidOf(value));

    private static JobCollectionId CollectionId(int value) => JobCollectionId.Create(GuidOf(value));

    private static JobConfigId ConfigId(int value) => JobConfigId.Create(GuidOf(value));

    private static Guid GuidOf(int value) => Guid.Parse($"00000000-0000-0000-0000-{value:D12}");

    private static JobCollection CreateCollectionFrom(ImmutableArray<JobConfig> configs) =>
        CreateCollection([.. configs]);

    private static JobTemplate CreateTemplateFrom(ImmutableArray<JobCollection> collections) =>
        CreateTemplate([.. collections]);
}
