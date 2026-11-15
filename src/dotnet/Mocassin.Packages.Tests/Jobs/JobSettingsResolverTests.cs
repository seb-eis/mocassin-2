// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json;
using Mocassin.Compilation.Simulations;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Simulations;
using Mocassin.Packages.Jobs;

namespace Mocassin.Packages.Tests.Jobs;

[Trait("Tier", "PR-fast")]
public sealed class JobSettingsResolverTests
{
    private static readonly SimulationId simulationId =
        SimulationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8a01"));

    [Fact]
    public void ResolvesCompiledDefaultsWhenBothLayersInherit()
    {
        var simulation = CreateSimulation(SimulationKind.Kmc);
        var collection = CreateCollection(CreateConfig());

        var resolved = JobSettingsResolver.Resolve(collection, CreateConfig(), simulation, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.NotNull(resolved);
        Assert.Equal(900.0, resolved.TemperatureK);
        Assert.Equal(500, resolved.TargetMcsp);
        Assert.Equal(7, resolved.WriteOutCount);
        Assert.Null(resolved.TimeLimitSeconds);
        Assert.Null(resolved.MinSuccessRate);
        Assert.Equal(ExecutionFlags.Deterministic, resolved.Flags);
        Assert.Equal("compiled", resolved.Instruction);
        Assert.Equal(50, resolved.PreRunMcsp);
        Assert.Equal(2.0e7, resolved.ElectricFieldModulusVm);
        Assert.Equal(new Fractional3(1.0, 0.0, 0.0), resolved.ElectricFieldDirection);
        Assert.Equal(0.25, resolved.NormalizationEnergyEv);
        Assert.Equal(1.0e13, resolved.MaxAttemptFrequencyHz);
        Assert.Empty(resolved.Extensions);
    }

    [Fact]
    public void CollectionThenConfigOverridesWin()
    {
        using var baseDocument = JsonDocument.Parse("{\"name\":\"base\"}");
        using var configDocument = JsonDocument.Parse("{\"name\":\"config\"}");
        var baseSettings = new JobSettings
        {
            TemperatureK = 800.0,
            WriteOutCount = 5,
            PreRunMcsp = 10,
            Instruction = "base",
            Flags = ExecutionFlags.NoSaving,
            Extensions = [new JobExtension("base", baseDocument.RootElement.Clone())],
        };
        var overrides = new JobSettings
        {
            TemperatureK = 950.0,
            TargetMcsp = 42,
            Instruction = "override",
            Flags = ExecutionFlags.None,
            Extensions = [new JobExtension("config", configDocument.RootElement.Clone())],
        };
        var config = CreateConfig(overrides);
        var collection = CreateCollection(config, baseSettings);

        var resolved = JobSettingsResolver.Resolve(
            collection,
            config,
            CreateSimulation(SimulationKind.Kmc),
            out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.NotNull(resolved);
        Assert.Equal(950.0, resolved.TemperatureK);
        Assert.Equal(42, resolved.TargetMcsp);
        Assert.Equal(5, resolved.WriteOutCount);
        Assert.Equal(10, resolved.PreRunMcsp);
        Assert.Equal("override", resolved.Instruction);
        Assert.Equal(ExecutionFlags.None, resolved.Flags);
        var extension = Assert.Single(resolved.Extensions);
        Assert.Equal("config", extension.Id);
    }

    [Fact]
    public void EmptyConfigExtensionArrayReplacesInheritedList()
    {
        using var baseDocument = JsonDocument.Parse("{\"name\":\"base\"}");
        var baseSettings = new JobSettings
        {
            Extensions = [new JobExtension("base", baseDocument.RootElement.Clone())],
        };
        var config = CreateConfig(new JobSettings { Extensions = [] });

        var resolved = JobSettingsResolver.Resolve(
            CreateCollection(config, baseSettings),
            config,
            CreateSimulation(SimulationKind.Kmc),
            out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.NotNull(resolved);
        Assert.Empty(resolved.Extensions);
    }

    [Fact]
    public void KindInvalidBaseLayerFailsWithJob015()
    {
        var baseSettings = new JobSettings { PreRunMcsp = 100 };
        var config = CreateConfig();

        var resolved = JobSettingsResolver.Resolve(
            CreateCollection(config, baseSettings),
            config,
            CreateSimulation(SimulationKind.Mmc),
            out var diagnostics);

        Assert.Null(resolved);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidSettings);
    }

    [Fact]
    public void KindInvalidConfigLayerFailsWithJob015()
    {
        var config = CreateConfig(new JobSettings { BreakTolerance = 0.1 });

        var resolved = JobSettingsResolver.Resolve(
            CreateCollection(config),
            config,
            CreateSimulation(SimulationKind.Kmc),
            out var diagnostics);

        Assert.Null(resolved);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidSettings);
    }

    [Fact]
    public void InvalidExtensionLayerFailsWithJob018()
    {
        var extensions = ImmutableArray.CreateBuilder<JobExtension>();
        for (var index = 0; index < 17; index++)
        {
            extensions.Add(Extension($"id-{index}", "{}"));
        }

        var config = CreateConfig(new JobSettings { Extensions = extensions.ToImmutable() });

        var resolved = JobSettingsResolver.Resolve(
            CreateCollection(config),
            config,
            CreateSimulation(SimulationKind.Kmc),
            out var diagnostics);

        Assert.Null(resolved);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidExtension);
    }

    [Fact]
    public void InvalidSettingRangeFailsWithoutPartialResult()
    {
        var config = CreateConfig(new JobSettings { TemperatureK = 0.0 });

        var resolved = JobSettingsResolver.Resolve(
            CreateCollection(config),
            config,
            CreateSimulation(SimulationKind.Kmc),
            out var diagnostics);

        Assert.Null(resolved);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(JobDiagnosticCodes.InvalidSettings, diagnostic.Code);
        Assert.Equal("overrides.temperatureK", diagnostic.Path?.Property);
    }

    private static JobCollection CreateCollection(JobConfig config, JobSettings? baseSettings = null) =>
        new(
            JobCollectionId.New(),
            "T900",
            simulationId,
            baseSettings,
            4,
            null,
            null,
            [config]);

    private static JobConfig CreateConfig(JobSettings? overrides = null) =>
        new(
            JobConfigId.New(),
            "x001",
            new LatticeRequest(10, 10, 10),
            overrides);

    private static CompiledSimulation CreateSimulation(SimulationKind kind)
    {
        var settings = new CompiledRuntimeSettings(
            TemperatureK: 900.0,
            TargetMcsp: 500,
            WriteOutCount: 7,
            TimeLimitSeconds: null,
            MinSuccessRate: null,
            JobCount: 4,
            RngSeed: null,
            Flags: ExecutionFlags.Deterministic,
            Instruction: "compiled",
            PreRunMcsp: 50,
            ElectricFieldModulusVm: 2.0e7,
            ElectricFieldDirection: new Fractional3(1.0, 0.0, 0.0),
            NormalizationEnergyEv: 0.25,
            MaxAttemptFrequencyHz: 1.0e13,
            BreakTolerance: 0.01,
            BreakSampleLengthMcsp: 1000,
            BreakSampleIntervalMcsp: 100,
            ResultSampleMcsp: 200);

        return new CompiledSimulation(
            simulationId,
            "Compiled simulation",
            kind,
            [],
            settings,
            new CompiledSolverIntent(kind, UseDualDofCorrection: false, UsePreRun: true, PreRunMcsp: 50),
            CompiledTrackerModel.Empty);
    }

    private static JobExtension Extension(string id, string json)
    {
        using var document = JsonDocument.Parse(json);
        return new JobExtension(id, document.RootElement.Clone());
    }
}
