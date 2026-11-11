// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.Simulations;

[Trait("Tier", "PR-fast")]
public sealed class SimulationTests
{
    [Fact]
    public void KineticDefaultsMatchTheDocumentedContract()
    {
        var simulation = SimulationTestModels.CreateKinetic();

        Assert.Equal(SimulationKind.Kmc, simulation.Kind);
        Assert.Equal(1000.0, simulation.TemperatureK);
        Assert.Equal(1000, simulation.TargetMcsp);
        Assert.Equal(100, simulation.WriteOutCount);
        Assert.Equal(31_536_000, simulation.TimeLimitSeconds);
        Assert.Null(simulation.MinSuccessRate);
        Assert.Equal(12, simulation.JobCount);
        Assert.Null(simulation.RngSeed);
        Assert.Equal(ExecutionFlags.None, simulation.Flags);
        Assert.Null(simulation.Instruction);
        Assert.Single(simulation.TransitionIds);
        Assert.Equal(100, simulation.PreRunMcsp);
        Assert.Equal(1.0e7, simulation.ElectricFieldModulusVm);
        Assert.Equal(new Fractional3(1.0, 0.0, 0.0), simulation.ElectricFieldDirection);
        Assert.Null(simulation.NormalizationEnergyEv);
        Assert.Null(simulation.MaxAttemptFrequencyHz);
    }

    [Fact]
    public void MetropolisDefaultsMatchTheDocumentedContract()
    {
        var simulation = SimulationTestModels.CreateMetropolis();

        Assert.Equal(SimulationKind.Mmc, simulation.Kind);
        Assert.Equal(1000.0, simulation.TemperatureK);
        Assert.Equal(1000, simulation.TargetMcsp);
        Assert.Equal(100, simulation.WriteOutCount);
        Assert.Equal(31_536_000, simulation.TimeLimitSeconds);
        Assert.Null(simulation.MinSuccessRate);
        Assert.Equal(12, simulation.JobCount);
        Assert.Null(simulation.RngSeed);
        Assert.Equal(ExecutionFlags.None, simulation.Flags);
        Assert.Null(simulation.Instruction);
        Assert.Single(simulation.TransitionIds);
        Assert.Equal(0.0, simulation.BreakTolerance);
        Assert.Equal(1000, simulation.BreakSampleLengthMcsp);
        Assert.Equal(100, simulation.BreakSampleIntervalMcsp);
        Assert.Equal(200, simulation.ResultSampleMcsp);
    }

    [Fact]
    public void KindIsDerivedFromTheConcreteRecord()
    {
        Assert.Equal(SimulationKind.Kmc, SimulationTestModels.CreateKinetic().Kind);
        Assert.Equal(SimulationKind.Mmc, SimulationTestModels.CreateMetropolis().Kind);
        Assert.IsType<KineticSimulation>(SimulationTestModels.CreateKinetic());
        Assert.IsType<MetropolisSimulation>(SimulationTestModels.CreateMetropolis());
    }

    [Fact]
    public void SimulationsRoundTripThroughProjectJson()
    {
        var project = SimulationTestModels.CreateProject(
            SimulationTestModels.CreateKinetic() with
            {
                TemperatureK = 773.0,
                Flags = ExecutionFlags.NoJumpLogging | ExecutionFlags.Deterministic,
                RngSeed = "seed",
                MinSuccessRate = 25.0,
                Instruction = "instruction",
                PreRunMcsp = 250,
                ElectricFieldModulusVm = 2.0e7,
                ElectricFieldDirection = new Fractional3(0.0, 1.0, 0.0),
                NormalizationEnergyEv = 1.5,
                MaxAttemptFrequencyHz = 1.0e13,
            },
            SimulationTestModels.CreateMetropolis() with
            {
                TemperatureK = 1200.0,
                BreakTolerance = 0.05,
                BreakSampleLengthMcsp = 2000,
                BreakSampleIntervalMcsp = 200,
                ResultSampleMcsp = 400,
            });

        var loaded = ProjectJson.Deserialize(ProjectJson.Serialize(project));

        Assert.Equal(
            ProjectCanonicalizer.ToCanonicalJson(project),
            ProjectCanonicalizer.ToCanonicalJson(loaded));
        Assert.Equal(2, loaded.Model.Simulations.Length);
        var kinetic = Assert.IsType<KineticSimulation>(
            loaded.Model.Simulations.Single(static simulation => simulation.Kind == SimulationKind.Kmc));
        Assert.Equal(773.0, kinetic.TemperatureK);
        Assert.Equal(ExecutionFlags.NoJumpLogging | ExecutionFlags.Deterministic, kinetic.Flags);
        Assert.Equal("seed", kinetic.RngSeed);
        Assert.Equal(25.0, kinetic.MinSuccessRate);
        Assert.Equal("instruction", kinetic.Instruction);
        Assert.Equal(250, kinetic.PreRunMcsp);
        Assert.Equal(2.0e7, kinetic.ElectricFieldModulusVm);
        Assert.Equal(new Fractional3(0.0, 1.0, 0.0), kinetic.ElectricFieldDirection);
        Assert.Equal(1.5, kinetic.NormalizationEnergyEv);
        Assert.Equal(1.0e13, kinetic.MaxAttemptFrequencyHz);
        var metropolis = Assert.IsType<MetropolisSimulation>(
            loaded.Model.Simulations.Single(static simulation => simulation.Kind == SimulationKind.Mmc));
        Assert.Equal(1200.0, metropolis.TemperatureK);
        Assert.Equal(0.05, metropolis.BreakTolerance);
        Assert.Equal(2000, metropolis.BreakSampleLengthMcsp);
        Assert.Equal(200, metropolis.BreakSampleIntervalMcsp);
        Assert.Equal(400, metropolis.ResultSampleMcsp);
    }

    [Fact]
    public void PersistedDtoUsesKindSettingsAndOnlyTheMatchingTransitionArray()
    {
        var project = SimulationTestModels.CreateProject(
            SimulationTestModels.CreateKinetic(),
            SimulationTestModels.CreateMetropolis());

        using var document = JsonDocument.Parse(ProjectCanonicalizer.ToCanonicalJson(project));
        var simulations = document.RootElement.GetProperty("model").GetProperty("simulations");
        var kinetic = simulations[0];
        var metropolis = simulations[1];

        Assert.Equal("kmc", kinetic.GetProperty("kind").GetString());
        Assert.True(kinetic.TryGetProperty("kineticTransitionIds", out _));
        Assert.False(kinetic.TryGetProperty("metropolisTransitionIds", out _));
        Assert.True(kinetic.TryGetProperty("settings", out _));
        Assert.Equal("mmc", metropolis.GetProperty("kind").GetString());
        Assert.True(metropolis.TryGetProperty("metropolisTransitionIds", out _));
        Assert.False(metropolis.TryGetProperty("kineticTransitionIds", out _));
        Assert.True(metropolis.TryGetProperty("settings", out _));
    }

    [Fact]
    public void TransitionOrderIsPreserved()
    {
        var model = SimulationTestModels.CreateModel();
        var second = TransitionTestModels.KineticTransition with
        {
            Id = KineticTransitionId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6b20")),
            Name = "Second kinetic transition",
        };
        model = model with { KineticTransitions = [.. model.KineticTransitions, second] };
        var order = ImmutableArray.Create(
            TransitionRef.From(second.Id),
            TransitionRef.From(TransitionTestModels.KineticTransition.Id));
        model = model with
        {
            Simulations = [SimulationTestModels.CreateKinetic(order)],
        };
        var project = TransitionTestModels.CreateProject(model);

        var loaded = ProjectJson.Deserialize(ProjectJson.Serialize(project));

        Assert.Equal(order.ToArray(), loaded.Model.Simulations[0].TransitionIds.ToArray());
    }

    [Fact]
    public void ExecutionFlagsSerializeAsSortedCamelCaseNames()
    {
        var simulation = SimulationTestModels.CreateKinetic() with
        {
            Flags = ExecutionFlags.Deterministic | ExecutionFlags.NoSaving | ExecutionFlags.UseFastExp,
        };

        using var document = JsonDocument.Parse(
            ProjectCanonicalizer.ToCanonicalJson(SimulationTestModels.CreateProject(simulation)));
        var flags = document.RootElement
            .GetProperty("model")
            .GetProperty("simulations")[0]
            .GetProperty("settings")
            .GetProperty("executionFlags")
            .EnumerateArray()
            .Select(static item => item.GetString()!)
            .ToArray();

        Assert.Equal(["noSaving", "useFastExp", "deterministic"], flags);
    }

    [Fact]
    public void NoneExecutionFlagsSerializeAsEmptyArray()
    {
        using var document = JsonDocument.Parse(
            ProjectCanonicalizer.ToCanonicalJson(
                SimulationTestModels.CreateProject(SimulationTestModels.CreateKinetic())));
        var flags = document.RootElement
            .GetProperty("model")
            .GetProperty("simulations")[0]
            .GetProperty("settings")
            .GetProperty("executionFlags");

        Assert.Equal(JsonValueKind.Array, flags.ValueKind);
        Assert.Equal(0, flags.GetArrayLength());
    }

    [Fact]
    public void DuplicateExecutionFlagNamesAreRejected()
    {
        var json = ProjectCanonicalizer.ToCanonicalJson(
            SimulationTestModels.CreateProject(SimulationTestModels.CreateKinetic()));
        var modified = json.Replace(
            "\"executionFlags\":[]",
            "\"executionFlags\":[\"noSaving\",\"noSaving\"]",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(modified));
    }

    [Fact]
    public void DerivedExecutionFlagNamesAreRejected()
    {
        var json = ProjectCanonicalizer.ToCanonicalJson(
            SimulationTestModels.CreateProject(SimulationTestModels.CreateKinetic()));
        var modified = json.Replace(
            "\"executionFlags\":[]",
            "\"executionFlags\":[\"kmcSimulation\"]",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(modified));
    }

    [Fact]
    public void CanonicalValuesNormalizeToNull()
    {
        var canonical = SimulationTestModels.CreateKinetic() with
        {
            MinSuccessRate = 0.0,
            RngSeed = string.Empty,
            NormalizationEnergyEv = 0.0,
        };

        using var document = JsonDocument.Parse(
            ProjectCanonicalizer.ToCanonicalJson(
                SimulationTestModels.CreateProject(canonical)));
        var settings = document.RootElement
            .GetProperty("model")
            .GetProperty("simulations")[0]
            .GetProperty("settings");

        Assert.Equal(JsonValueKind.Null, settings.GetProperty("minSuccessRate").ValueKind);
        Assert.Equal(JsonValueKind.Null, settings.GetProperty("rngSeed").ValueKind);
        Assert.Equal(JsonValueKind.Null, settings.GetProperty("normalizationEnergyEv").ValueKind);
        Assert.Equal(
            ProjectHasher.Compute(SimulationTestModels.CreateProject(canonical)),
            ProjectHasher.Compute(SimulationTestModels.CreateProject(
                SimulationTestModels.CreateKinetic() with
                {
                    MinSuccessRate = null,
                    RngSeed = null,
                    NormalizationEnergyEv = null,
                })));
    }

    [Fact]
    public void PersistedKindShapeMismatchIsReported()
    {
        var simulation = SimulationTestModels.CreateKinetic() with
        {
            HasPersistedKindMismatch = true,
        };

        var diagnostics = SimulationModelValidator.Validate(
            SimulationTestModels.CreateModel(simulation));

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == SimulationDiagnosticCodes.KindShapeMismatch);
    }

    [Fact]
    public void PersistedKindShapeMismatchIsDetectedFromJson()
    {
        var json = ProjectCanonicalizer.ToCanonicalJson(
            SimulationTestModels.CreateProject(SimulationTestModels.CreateKinetic()));
        var modified = json.Replace(
            "\"settings\":{",
            "\"settings\":{\"breakTolerance\":0.1,",
            StringComparison.Ordinal);

        var loaded = ProjectJson.Deserialize(modified);

        Assert.Contains(
            SimulationModelValidator.Validate(loaded.Model),
            static diagnostic => diagnostic.Code == SimulationDiagnosticCodes.KindShapeMismatch);
    }

    [Fact]
    public void EmptySimulationArrayIsOmitted()
    {
        var json = ProjectCanonicalizer.ToCanonicalJson(
            SimulationTestModels.CreateProject());

        using var document = JsonDocument.Parse(json);
        Assert.False(document.RootElement.GetProperty("model").TryGetProperty("simulations", out _));
    }

    [Fact]
    public void SimulationEditsChangeModelAndDocumentHashes()
    {
        var project = SimulationTestModels.CreateProject(SimulationTestModels.CreateKinetic());
        var edited = project with
        {
            Model = project.Model with
            {
                Simulations = [project.Model.Simulations[0] with { TemperatureK = 800.0 }],
            },
        };

        Assert.NotEqual(ProjectHasher.Compute(project), ProjectHasher.Compute(edited));
        Assert.NotEqual(ProjectHasher.ComputeDocument(project), ProjectHasher.ComputeDocument(edited));
    }

    [Fact]
    public void ProjectExposesSimulationQueriesThroughIProjectQuery()
    {
        var project = SimulationTestModels.CreateProject(SimulationTestModels.CreateKinetic());

        Assert.Contains(typeof(ISimulationQuery), typeof(IProjectQuery).GetInterfaces());
        Assert.Contains(typeof(ISimulationQuery), typeof(ProjectModel).GetInterfaces());
        Assert.Single(project.Simulations);
        Assert.Equal(
            SimulationTestModels.KineticId,
            project.Find(SimulationTestModels.KineticId)?.Id);
        Assert.Null(project.Find(SimulationId.New()));
    }

    [Fact]
    public void UnknownSimulationPropertiesAreRejected()
    {
        var json = ProjectCanonicalizer.ToCanonicalJson(
            SimulationTestModels.CreateProject(SimulationTestModels.CreateKinetic()));
        var modified = json.Replace(
            "\"settings\":{",
            "\"settings\":{\"unknown\":1,",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(modified));
    }

    [Fact]
    public void UnknownTopLevelSimulationPropertiesAreRejected()
    {
        var json = ProjectCanonicalizer.ToCanonicalJson(
            SimulationTestModels.CreateProject(SimulationTestModels.CreateKinetic()));
        var modified = json.Replace(
            "\"kind\":\"kmc\"",
            "\"kind\":\"kmc\",\"unknown\":1",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(modified));
    }

    [Fact]
    public void SimulationArrayIsSortedByIdInCanonicalJson()
    {
        var model = SimulationTestModels.CreateModel(
            SimulationTestModels.CreateKinetic(),
            SimulationTestModels.CreateMetropolis());
        var reversed = model with { Simulations = [.. model.Simulations.Reverse()] };

        using var document = JsonDocument.Parse(
            ProjectCanonicalizer.ToCanonicalJson(TransitionTestModels.CreateProject(reversed)));
        var ids = document.RootElement
            .GetProperty("model")
            .GetProperty("simulations")
            .EnumerateArray()
            .Select(static item => item.GetProperty("id").GetString())
            .ToArray();

        Assert.Equal(ids.Order(StringComparer.Ordinal), ids);
    }
}
