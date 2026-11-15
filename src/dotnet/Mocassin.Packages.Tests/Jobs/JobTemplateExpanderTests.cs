// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Simulations;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Project;
using Mocassin.Packages.Jobs;

namespace Mocassin.Packages.Tests.Jobs;

[Trait("Tier", "PR-fast")]
public sealed class JobTemplateExpanderTests
{
    private const string masterSeed =
        "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f";

    [Fact]
    public void ExpandsCollectionsConfigsAndDuplicatesInAuthoredOrder()
    {
        var kmcId = SimulationId.New();
        var mmcId = SimulationId.New();
        var model = CreateModel(
            CreateSimulation(kmcId, SimulationKind.Kmc, 2),
            CreateSimulation(mmcId, SimulationKind.Mmc, 3));
        var template = CreateTemplate(
            CreateCollection(kmcId, "alpha", 2, Config("c1"), Config("c2")),
            CreateCollection(mmcId, "beta", null, Config("c3")));

        var result = JobTemplateExpander.Expand(CreateProject(template), model, template);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(7, result.Jobs.Length);
        Assert.Equal(Enumerable.Range(1, 7), result.Jobs.Select(static job => job.JobId));

        var expected = new[]
        {
            ("alpha", "c1", 0, 0, 0),
            ("alpha", "c1", 0, 1, 0),
            ("alpha", "c2", 0, 0, 0),
            ("alpha", "c2", 0, 1, 0),
            ("beta", "c3", 1, 0, 1),
            ("beta", "c3", 1, 1, 1),
            ("beta", "c3", 1, 2, 1),
        };

        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index].Item1, result.Jobs[index].CollectionName);
            Assert.Equal(expected[index].Item2, result.Jobs[index].ConfigName);
            Assert.Equal(expected[index].Item3, result.Jobs[index].CollectionIndex);
            Assert.Equal(expected[index].Item4, result.Jobs[index].JobIndex);
            Assert.Equal(expected[index].Item5, result.Jobs[index].SimulationIndex);
        }
    }

    [Fact]
    public void CollectionJobCountOverridesAndInheritsTheSimulationDefault()
    {
        var id = SimulationId.New();
        var model = CreateModel(CreateSimulation(id, SimulationKind.Kmc, 5));
        var template = CreateTemplate(
            CreateCollection(id, "override", 2, Config("c1")),
            CreateCollection(id, "inherit", null, Config("c2")));

        var result = JobTemplateExpander.Expand(CreateProject(template), model, template);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Jobs.Length);
        Assert.Equal(2, result.Jobs.Count(static job => job.CollectionName == "override"));
        Assert.Equal(5, result.Jobs.Count(static job => job.CollectionName == "inherit"));
    }

    [Fact]
    public void SettingsResolutionFailureReturnsDiagnosticsAndNoDescriptors()
    {
        var id = SimulationId.New();
        var model = CreateModel(CreateSimulation(id, SimulationKind.Mmc, 2));
        var template = CreateTemplate(
            CreateCollection(id, "valid", null, Config("c1")),
            CreateCollection(
                id,
                "invalid",
                null,
                Config("c2", overrides: new JobSettings { PreRunMcsp = 10 })));

        var result = JobTemplateExpander.Expand(CreateProject(template), model, template);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Jobs);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidSettings);
    }

    [Fact]
    public void UnknownSimulationIsAJob002DiagnosticAndNeverPartial()
    {
        var id = SimulationId.New();
        var unknown = SimulationId.New();
        var model = CreateModel(CreateSimulation(id, SimulationKind.Kmc, 2));
        var template = CreateTemplate(
            CreateCollection(id, "valid", null, Config("c1")),
            CreateCollection(unknown, "unknown", null, Config("c2")));

        var result = JobTemplateExpander.Expand(CreateProject(template), model, template);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Jobs);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == JobDiagnosticCodes.UnknownSimulation);
    }

    [Fact]
    public void UnknownTemplateIdIsAnUnknownReferenceDiagnostic()
    {
        var id = SimulationId.New();
        var model = CreateModel(CreateSimulation(id, SimulationKind.Kmc, 2));

        var result = JobTemplateExpander.Expand(CreateProject(), model, JobTemplateId.New());

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Jobs);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.UnknownReference);
    }

    [Fact]
    public void EmptyTemplateIsAJob001Diagnostic()
    {
        var id = SimulationId.New();
        var model = CreateModel(CreateSimulation(id, SimulationKind.Kmc, 2));
        var template = new JobTemplate(JobTemplateId.New(), "empty") { Collections = [] };

        var result = JobTemplateExpander.Expand(CreateProject(template), model, template);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Jobs);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == JobDiagnosticCodes.NoCollections);
    }

    [Fact]
    public void DescriptorCarriesCompiledDataSettingsLatticeDopingsAndOptimizers()
    {
        var id = SimulationId.New();
        var model = CreateModel(CreateSimulation(id, SimulationKind.Kmc, 1, useDualDof: true));
        var lattice = new LatticeRequest(2, 3, 4);
        var dopings = ImmutableArray.Create(
            new DopingValue(DopingId.New(), 0.1),
            new DopingValue(DopingId.New(), 0.2));
        var optimizers = ImmutableArray.Create(
            new SelectionOptimizer(CellSiteId.New(), ParticleId.New()),
            new SelectionOptimizer(CellSiteId.New(), ParticleId.New()));
        var config = Config("c1", lattice, dopings, new JobSettings { TemperatureK = 950.0 });
        var collection = CreateCollection(id, "alpha", 1, [config], null, optimizers);
        var template = CreateTemplate(collection);

        var result = JobTemplateExpander.Expand(CreateProject(template), model, template);

        Assert.True(result.IsSuccess);
        var job = Assert.Single(result.Jobs);
        Assert.Equal(id, job.SimulationId);
        Assert.Equal(0, job.SimulationIndex);
        Assert.Equal(SimulationKind.Kmc, job.Kind);
        Assert.True(job.UseDualDofCorrection);
        Assert.Equal(950.0, job.Settings.TemperatureK);
        Assert.Equal(lattice, job.Lattice);
        Assert.Equal(dopings, job.Dopings);
        Assert.Equal(optimizers, job.Optimizers);
        Assert.Equal(32, job.Seed.Length);
        Assert.Equal<byte>(
            JobSeedDeriver.DeriveJobSeed(masterSeed, 0, 0, 0),
            job.Seed);
    }

    [Fact]
    public void CollectionSeedIsUsedForEveryDuplicate()
    {
        var id = SimulationId.New();
        var model = CreateModel(CreateSimulation(id, SimulationKind.Kmc, 2, rngSeed: "simulation-seed"));
        var collection = CreateCollection(id, "alpha", 2, [Config("c1")], "collection-seed");
        var template = CreateTemplate(collection);

        var result = JobTemplateExpander.Expand(CreateProject(template), model, template);

        Assert.True(result.IsSuccess);
        Assert.Equal<byte>(
            JobSeedDeriver.DeriveJobSeed("collection-seed", 0, 0, 0),
            result.Jobs[0].Seed);
        Assert.Equal<byte>(
            JobSeedDeriver.DeriveJobSeed("collection-seed", 0, 0, 1),
            result.Jobs[1].Seed);
        Assert.NotEqual(
            Convert.ToHexStringLower(result.Jobs[0].Seed.AsSpan()),
            Convert.ToHexStringLower(result.Jobs[1].Seed.AsSpan()));
    }

    [Fact]
    public void ExpansionIsDeterministicAndSeedsAreUniquePerJob()
    {
        var id = SimulationId.New();
        var model = CreateModel(CreateSimulation(id, SimulationKind.Kmc, 2));
        var template = CreateTemplate(
            CreateCollection(id, "alpha", 2, Config("c1"), Config("c2")));

        var first = JobTemplateExpander.Expand(CreateProject(template), model, template);
        var second = JobTemplateExpander.Expand(CreateProject(template), model, template);

        Assert.True(first.IsSuccess);
        Assert.Equal(
            first.Jobs.Select(static job => Convert.ToHexStringLower(job.Seed.AsSpan())),
            second.Jobs.Select(static job => Convert.ToHexStringLower(job.Seed.AsSpan())));
        Assert.Equal(4, first.Jobs.Select(static job => Convert.ToHexStringLower(job.Seed.AsSpan())).Distinct().Count());
    }

    [Fact]
    public void EffectiveJobCountOutsideRangeIsAJob008Diagnostic()
    {
        var id = SimulationId.New();
        var model = CreateModel(CreateSimulation(id, SimulationKind.Kmc, 0));
        var template = CreateTemplate(CreateCollection(id, "alpha", null, Config("c1")));

        var result = JobTemplateExpander.Expand(CreateProject(template), model, template);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Jobs);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidJobCount);
    }

    [Fact]
    public void ExpansionAboveTheHardJobLimitIsAJob019Diagnostic()
    {
        var id = SimulationId.New();
        var model = CreateModel(CreateSimulation(id, SimulationKind.Kmc, 1_000));
        var configs = Enumerable.Range(0, 101).Select(index => Config($"c{index}")).ToImmutableArray();
        var template = CreateTemplate(CreateCollection(id, "alpha", 1_000, [.. configs]));

        var result = JobTemplateExpander.Expand(CreateProject(template), model, template);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Jobs);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == JobDiagnosticCodes.JobLimitExceeded);
    }

    private static Project CreateProject(params JobTemplate[] templates) => new(
        null,
        ProjectSchema.CurrentVersion,
        new ProjectMetadata(
            Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e01"),
            masterSeed,
            "Job expansion test",
            null,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch),
        ModelDefinition.Empty,
        [],
        [.. templates],
        [],
        ViewSettings.Default);

    private static CompiledModel CreateModel(params CompiledSimulation[] simulations) => new(
        CompiledModel.CurrentFormatVersion,
        "test-model",
        "symmetry-1",
        "catalog-1",
        new string('0', 64))
    {
        Simulations = [.. simulations],
    };

    private static CompiledSimulation CreateSimulation(
        SimulationId id,
        SimulationKind kind,
        int jobCount,
        string? rngSeed = null,
        bool useDualDof = false)
    {
        var settings = new CompiledRuntimeSettings(
            TemperatureK: 900.0,
            TargetMcsp: 500,
            WriteOutCount: 7,
            TimeLimitSeconds: null,
            MinSuccessRate: null,
            JobCount: jobCount,
            RngSeed: rngSeed,
            Flags: ExecutionFlags.Deterministic,
            Instruction: "compiled",
            PreRunMcsp: kind == SimulationKind.Kmc ? 50 : 0,
            ElectricFieldModulusVm: 0.0,
            ElectricFieldDirection: Fractional3.Zero,
            NormalizationEnergyEv: null,
            MaxAttemptFrequencyHz: null,
            BreakTolerance: 0.01,
            BreakSampleLengthMcsp: 1000,
            BreakSampleIntervalMcsp: 100,
            ResultSampleMcsp: 200);

        return new CompiledSimulation(
            id,
            "simulation",
            kind,
            [],
            settings,
            new CompiledSolverIntent(kind, useDualDof, false, 0),
            CompiledTrackerModel.Empty);
    }

    private static JobConfig Config(
        string name,
        LatticeRequest? lattice = null,
        ImmutableArray<DopingValue>? dopings = null,
        JobSettings? overrides = null) => new(
        JobConfigId.New(),
        name,
        lattice ?? new LatticeRequest(10, 10, 10),
        overrides,
        dopings);

    private static JobCollection CreateCollection(
        SimulationId simulationId,
        string name,
        int? jobCount,
        params JobConfig[] configs) => CreateCollection(simulationId, name, jobCount, configs, null);

    private static JobCollection CreateCollection(
        SimulationId simulationId,
        string name,
        int? jobCount,
        JobConfig[] configs,
        string? rngSeed,
        ImmutableArray<SelectionOptimizer>? optimizers = null) => new(
        JobCollectionId.New(),
        name,
        simulationId,
        BaseSettings: null,
        JobCountPerConfig: jobCount,
        RngSeed: rngSeed,
        Optimizers: optimizers,
        Configs: [.. configs]);

    private static JobTemplate CreateTemplate(params JobCollection[] collections) =>
        new(JobTemplateId.New(), "template") { Collections = [.. collections] };
}
