// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Simulations;

[Trait("Tier", "PR-fast")]
public sealed class SimulationModelTests
{
    [Fact]
    public void ValidSimulationsHaveNoSimulationDiagnostics()
    {
        var model = SimulationTestModels.CreateModel(
            SimulationTestModels.CreateKinetic(),
            SimulationTestModels.CreateMetropolis());

        Assert.Empty(SimulationModelValidator.Validate(model));
    }

    [Fact]
    public void Sim001UnknownTransitionIsReported()
    {
        var simulation = SimulationTestModels.CreateKinetic(
            [TransitionRef.From(KineticTransitionId.New())]);

        AssertHas(simulation, SimulationDiagnosticCodes.UnknownTransition);
    }

    [Fact]
    public void Sim002WrongTransitionKindIsReported()
    {
        var simulation = SimulationTestModels.CreateKinetic(
            [TransitionRef.From(TransitionTestModels.MetropolisTransition.Id)]);

        AssertHas(simulation, SimulationDiagnosticCodes.WrongTransitionKind);
    }

    [Fact]
    public void Sim003NoTransitionsIsAWarning()
    {
        var simulation = SimulationTestModels.CreateKinetic([]);

        var diagnostics = Validate(simulation);

        var warning = Assert.Single(
            diagnostics,
            static diagnostic => diagnostic.Code == SimulationDiagnosticCodes.NoTransitions);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Theory]
    [InlineData(0.09)]
    [InlineData(10_001.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Sim004InvalidTemperatureIsReported(double temperature)
    {
        AssertHas(SimulationTestModels.CreateKinetic() with { TemperatureK = temperature },
            SimulationDiagnosticCodes.InvalidTemperature);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1_000_001L)]
    public void Sim005InvalidTargetMcspIsReported(long targetMcsp)
    {
        AssertHas(SimulationTestModels.CreateKinetic() with { TargetMcsp = targetMcsp },
            SimulationDiagnosticCodes.InvalidTargetMcsp);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void Sim006InvalidJobCountIsReported(int jobCount)
    {
        AssertHas(SimulationTestModels.CreateKinetic() with { JobCount = jobCount },
            SimulationDiagnosticCodes.InvalidJobCount);
    }

    [Fact]
    public void Sim007ZeroFieldDirectionWithNonZeroModulusIsReported()
    {
        var simulation = SimulationTestModels.CreateKinetic() with
        {
            ElectricFieldDirection = new Fractional3(0.0, 0.0, 0.0),
            ElectricFieldModulusVm = 1.0e7,
        };

        AssertHas(simulation, SimulationDiagnosticCodes.InvalidFieldDirection);
    }

    [Fact]
    public void Sim007NonFiniteFieldDirectionIsReported()
    {
        var simulation = SimulationTestModels.CreateKinetic() with
        {
            ElectricFieldDirection = new Fractional3(double.NaN, 0.0, 1.0),
        };

        AssertHas(simulation, SimulationDiagnosticCodes.InvalidFieldDirection);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(1_000_001L)]
    public void Sim008InvalidPreRunMcspIsReported(long preRunMcsp)
    {
        AssertHas(SimulationTestModels.CreateKinetic() with { PreRunMcsp = preRunMcsp },
            SimulationDiagnosticCodes.InvalidPreRunMcsp);
    }

    [Fact]
    public void Sim009EmptySimulationNameIsReported()
    {
        AssertHas(SimulationTestModels.CreateKinetic(name: " "), SimulationDiagnosticCodes.InvalidName);
    }

    [Fact]
    public void Sim009DuplicateSimulationNameIsReportedCaseInsensitively()
    {
        var model = SimulationTestModels.CreateModel(
            SimulationTestModels.CreateKinetic(name: "Simulation"),
            SimulationTestModels.CreateMetropolis(name: "SIMULATION"));

        Assert.Contains(
            SimulationModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == SimulationDiagnosticCodes.InvalidName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Sim010InvalidWriteOutCountIsReported(int writeOutCount)
    {
        AssertHas(SimulationTestModels.CreateKinetic() with { WriteOutCount = writeOutCount },
            SimulationDiagnosticCodes.InvalidWriteOutCount);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void Sim011InvalidTimeLimitIsReported(long timeLimit)
    {
        AssertHas(SimulationTestModels.CreateKinetic() with { TimeLimitSeconds = timeLimit },
            SimulationDiagnosticCodes.InvalidTimeLimit);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(int.MaxValue + 1.0)]
    public void Sim012InvalidMinSuccessRateIsReported(double minSuccessRate)
    {
        AssertHas(SimulationTestModels.CreateKinetic() with { MinSuccessRate = minSuccessRate },
            SimulationDiagnosticCodes.InvalidMinSuccessRate);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void Sim013InvalidFieldModulusIsReported(double modulus)
    {
        AssertHas(SimulationTestModels.CreateKinetic() with { ElectricFieldModulusVm = modulus },
            SimulationDiagnosticCodes.InvalidFieldModulus);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void Sim014InvalidNormalizationEnergyIsReported(double normalizationEnergy)
    {
        AssertHas(SimulationTestModels.CreateKinetic() with { NormalizationEnergyEv = normalizationEnergy },
            SimulationDiagnosticCodes.InvalidNormalizationEnergy);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void Sim015InvalidMaxAttemptFrequencyIsReported(double maxAttemptFrequency)
    {
        AssertHas(SimulationTestModels.CreateKinetic() with { MaxAttemptFrequencyHz = maxAttemptFrequency },
            SimulationDiagnosticCodes.InvalidMaxAttemptFrequency);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Sim016InvalidBreakToleranceIsReported(double breakTolerance)
    {
        AssertHas(SimulationTestModels.CreateMetropolis() with { BreakTolerance = breakTolerance },
            SimulationDiagnosticCodes.InvalidBreakTolerance);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(100_001L)]
    public void Sim017InvalidBreakSampleLengthIsReported(long breakSampleLength)
    {
        AssertHas(SimulationTestModels.CreateMetropolis() with { BreakSampleLengthMcsp = breakSampleLength },
            SimulationDiagnosticCodes.InvalidBreakSampleLength);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1001L)]
    public void Sim018InvalidBreakSampleIntervalIsReported(long breakSampleInterval)
    {
        AssertHas(SimulationTestModels.CreateMetropolis() with { BreakSampleIntervalMcsp = breakSampleInterval },
            SimulationDiagnosticCodes.InvalidBreakSampleInterval);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(1_000_001L)]
    public void Sim019InvalidResultSampleIsReported(long resultSample)
    {
        AssertHas(SimulationTestModels.CreateMetropolis() with { ResultSampleMcsp = resultSample },
            SimulationDiagnosticCodes.InvalidResultSample);
    }

    [Fact]
    public void Sim020TooLongRngSeedIsReported()
    {
        AssertHas(SimulationTestModels.CreateKinetic() with { RngSeed = new string('x', 101) },
            SimulationDiagnosticCodes.RngSeedTooLong);
    }

    [Fact]
    public void Sim021TooManyTransitionsIsReported()
    {
        var transitions = Enumerable.Range(0, 101)
            .Select(_ => TransitionRef.From(KineticTransitionId.New()))
            .ToImmutableArray();

        AssertHas(SimulationTestModels.CreateKinetic(transitions),
            SimulationDiagnosticCodes.TransitionLimitExceeded);
    }

    [Fact]
    public void Sim022DuplicateTransitionReferenceIsReported()
    {
        var transition = TransitionRef.From(TransitionTestModels.KineticTransition.Id);

        AssertHas(SimulationTestModels.CreateKinetic([transition, transition]),
            SimulationDiagnosticCodes.DuplicateTransitionReference);
    }

    [Fact]
    public void Sim023PersistedKindMismatchIsReported()
    {
        AssertHas(
            SimulationTestModels.CreateKinetic() with { HasPersistedKindMismatch = true },
            SimulationDiagnosticCodes.KindShapeMismatch);
    }

    [Theory]
    [InlineData(99.0)]
    [InlineData(5001.0)]
    public void Sim024TemperatureOutsideRecommendedBandIsAWarning(double temperature)
    {
        AssertHas(
            SimulationTestModels.CreateKinetic() with { TemperatureK = temperature },
            SimulationDiagnosticCodes.TemperatureOutsideRecommendedBand,
            DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Sim025TargetMcspAboveRecommendedIsAWarning()
    {
        AssertHas(
            SimulationTestModels.CreateKinetic() with { TargetMcsp = 1001 },
            SimulationDiagnosticCodes.TargetMcspAboveRecommended,
            DiagnosticSeverity.Warning);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(101)]
    public void Sim026JobCountOutsideRecommendedBandIsAWarning(int jobCount)
    {
        AssertHas(
            SimulationTestModels.CreateKinetic() with { JobCount = jobCount },
            SimulationDiagnosticCodes.JobCountOutsideRecommendedBand,
            DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Sim027PreRunAboveRecommendedIsAWarning()
    {
        AssertHas(
            SimulationTestModels.CreateKinetic() with { PreRunMcsp = 1001 },
            SimulationDiagnosticCodes.PreRunAboveRecommended,
            DiagnosticSeverity.Warning);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(101)]
    public void Sim028WriteOutOutsideRecommendedBandIsAWarning(int writeOutCount)
    {
        AssertHas(
            SimulationTestModels.CreateKinetic() with { WriteOutCount = writeOutCount },
            SimulationDiagnosticCodes.WriteOutOutsideRecommendedBand,
            DiagnosticSeverity.Warning);
    }

    [Theory]
    [InlineData(1.0e3)]
    [InlineData(1.0e10)]
    public void Sim029FieldModulusOutsideRecommendedBandIsAWarning(double modulus)
    {
        AssertHas(
            SimulationTestModels.CreateKinetic() with { ElectricFieldModulusVm = modulus },
            SimulationDiagnosticCodes.FieldModulusOutsideRecommendedBand,
            DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Sim030BreakToleranceAboveRecommendedIsAWarning()
    {
        AssertHas(
            SimulationTestModels.CreateMetropolis() with { BreakTolerance = 0.2 },
            SimulationDiagnosticCodes.BreakToleranceAboveRecommended,
            DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Sim031BreakSampleLengthAboveRecommendedIsAWarning()
    {
        AssertHas(
            SimulationTestModels.CreateMetropolis() with { BreakSampleLengthMcsp = 10_001 },
            SimulationDiagnosticCodes.BreakSampleLengthAboveRecommended,
            DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Sim032BreakSampleIntervalAboveRecommendedIsAWarning()
    {
        AssertHas(
            SimulationTestModels.CreateMetropolis() with { BreakSampleIntervalMcsp = 101 },
            SimulationDiagnosticCodes.BreakSampleIntervalAboveRecommended,
            DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Sim033ResultSampleAboveRecommendedIsAWarning()
    {
        AssertHas(
            SimulationTestModels.CreateMetropolis() with { ResultSampleMcsp = 10_001 },
            SimulationDiagnosticCodes.ResultSampleAboveRecommended,
            DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Sim034NormalizationEnergyAboveRecommendedIsAWarning()
    {
        AssertHas(
            SimulationTestModels.CreateKinetic() with { NormalizationEnergyEv = 11.0 },
            SimulationDiagnosticCodes.NormalizationEnergyAboveRecommended,
            DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Sim035DerivedExecutionFlagIsReported()
    {
        AssertHas(
            SimulationTestModels.CreateKinetic() with { Flags = (ExecutionFlags)16 },
            SimulationDiagnosticCodes.InvalidExecutionFlags);
    }

    [Fact]
    public void Sim036ManyTransitionReferencesIsAWarning()
    {
        var transitions = Enumerable.Range(0, 11)
            .Select(_ => TransitionRef.From(KineticTransitionId.New()))
            .ToImmutableArray();

        AssertHas(
            SimulationTestModels.CreateKinetic(transitions),
            SimulationDiagnosticCodes.TransitionCountOutsideRecommendedBand,
            DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Sim037LowPositiveMinSuccessRateIsAWarning()
    {
        AssertHas(
            SimulationTestModels.CreateKinetic() with { MinSuccessRate = 5.0 },
            SimulationDiagnosticCodes.MinSuccessRateBelowRecommended,
            DiagnosticSeverity.Warning);
    }

    private static ImmutableArray<Diagnostic> Validate(Simulation simulation) =>
        SimulationModelValidator.Validate(SimulationTestModels.CreateModel(simulation));

    private static void AssertHas(
        Simulation simulation,
        string code,
        DiagnosticSeverity severity = DiagnosticSeverity.Error)
    {
        var diagnostic = Assert.Single(Validate(simulation), candidate => candidate.Code == code);
        Assert.Equal(severity, diagnostic.Severity);
        Assert.NotNull(diagnostic.Path);
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Hint));
    }
}
