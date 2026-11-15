// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Diagnostics;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Simulations;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Project;
using Mocassin.Packages.Jobs;

namespace Mocassin.Packages.Tests.Jobs;

/// <summary>
/// Smoke-bounds the documented Step 10 expansion budget without turning the local gate into a benchmark.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class JobExpansionPerformanceTests
{
    private const string masterSeed =
        "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f";

    [Fact]
    public void TenThousandSmallJobsExpandWithinTheDocumentedBudget()
    {
        var simulationId = SimulationId.New();
        var model = CreateModel(CreateSimulation(simulationId, 1_000));
        var configs = Enumerable
            .Range(0, 10)
            .Select(index => new JobConfig(JobConfigId.New(), $"c{index}", new LatticeRequest(1, 1, 1)))
            .ToImmutableArray();
        var collection = new JobCollection(
            JobCollectionId.New(),
            "alpha",
            simulationId,
            BaseSettings: null,
            JobCountPerConfig: 1_000,
            RngSeed: null,
            Optimizers: null,
            Configs: [.. configs]);
        var template = new JobTemplate(JobTemplateId.New(), "performance")
        {
            Collections = [collection],
        };
        var project = new Project(
            null,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e02"),
                masterSeed,
                "Job expansion performance",
                null,
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch),
            ModelDefinition.Empty,
            [],
            [template],
            [],
            ViewSettings.Default);

        var started = Stopwatch.GetTimestamp();
        var result = JobTemplateExpander.Expand(project, model, template);
        var elapsed = Stopwatch.GetElapsedTime(started);

        Assert.True(result.IsSuccess);
        Assert.Equal(10_000, result.Jobs.Length);
        Assert.Equal(1, result.Jobs[0].JobId);
        Assert.Equal(10_000, result.Jobs[^1].JobId);
        Assert.True(
            elapsed < TimeSpan.FromSeconds(30),
            $"Expanding 10,000 small jobs took {elapsed}, which exceeds the smoke-test bound of 30 s.");
    }

    private static CompiledModel CreateModel(CompiledSimulation simulation) => new(
        CompiledModel.CurrentFormatVersion,
        "test-model",
        "symmetry-1",
        "catalog-1",
        new string('0', 64))
    {
        Simulations = [simulation],
    };

    private static CompiledSimulation CreateSimulation(SimulationId id, int jobCount) => new(
        id,
        "simulation",
        SimulationKind.Kmc,
        [],
        new CompiledRuntimeSettings(
            TemperatureK: 900.0,
            TargetMcsp: 500,
            WriteOutCount: 7,
            TimeLimitSeconds: null,
            MinSuccessRate: null,
            JobCount: jobCount,
            RngSeed: null,
            Flags: ExecutionFlags.Deterministic,
            Instruction: "compiled",
            PreRunMcsp: 50,
            ElectricFieldModulusVm: 0.0,
            ElectricFieldDirection: Fractional3.Zero,
            NormalizationEnergyEv: null,
            MaxAttemptFrequencyHz: null,
            BreakTolerance: 0.01,
            BreakSampleLengthMcsp: 1000,
            BreakSampleIntervalMcsp: 100,
            ResultSampleMcsp: 200),
        new CompiledSolverIntent(SimulationKind.Kmc, false, false, 0),
        CompiledTrackerModel.Empty);
}
