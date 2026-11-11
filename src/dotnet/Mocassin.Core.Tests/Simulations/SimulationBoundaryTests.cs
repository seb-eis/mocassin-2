// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Simulations;

[Trait("Tier", "PR-fast")]
public sealed class SimulationBoundaryTests
{
    [Theory]
    [InlineData(0.1)]
    [InlineData(10_000.0)]
    public void TemperatureAtHardBoundaryIsAccepted(double temperature)
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateKinetic() with { TemperatureK = temperature },
            SimulationDiagnosticCodes.InvalidTemperature);
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(1_000_000L)]
    public void TargetMcspAtHardBoundaryIsAccepted(long targetMcsp)
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateKinetic() with { TargetMcsp = targetMcsp },
            SimulationDiagnosticCodes.InvalidTargetMcsp);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public void JobCountAtHardBoundaryIsAccepted(int jobCount)
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateKinetic() with { JobCount = jobCount },
            SimulationDiagnosticCodes.InvalidJobCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void WriteOutCountAtHardBoundaryIsAccepted(int writeOutCount)
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateKinetic() with { WriteOutCount = writeOutCount },
            SimulationDiagnosticCodes.InvalidWriteOutCount);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1_000_000L)]
    public void PreRunAtHardBoundaryIsAccepted(long preRunMcsp)
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateKinetic() with { PreRunMcsp = preRunMcsp },
            SimulationDiagnosticCodes.InvalidPreRunMcsp);
    }

    [Fact]
    public void NullTimeLimitIsAccepted()
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateKinetic() with { TimeLimitSeconds = null },
            SimulationDiagnosticCodes.InvalidTimeLimit);
    }

    [Fact]
    public void OneSecondTimeLimitIsAccepted()
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateKinetic() with { TimeLimitSeconds = 1 },
            SimulationDiagnosticCodes.InvalidTimeLimit);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(int.MaxValue)]
    public void MinSuccessRateAtHardBoundaryIsAccepted(double minSuccessRate)
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateKinetic() with { MinSuccessRate = minSuccessRate },
            SimulationDiagnosticCodes.InvalidMinSuccessRate);
    }

    [Fact]
    public void HundredCharacterRngSeedIsAccepted()
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateKinetic() with { RngSeed = new string('x', 100) },
            SimulationDiagnosticCodes.RngSeedTooLong);
    }

    [Fact]
    public void ZeroFieldWithZeroDirectionIsAccepted()
    {
        var simulation = SimulationTestModels.CreateKinetic() with
        {
            ElectricFieldModulusVm = 0.0,
            ElectricFieldDirection = new Fractional3(0.0, 0.0, 0.0),
        };

        AssertDoesNotHave(simulation, SimulationDiagnosticCodes.InvalidFieldDirection);
        AssertDoesNotHave(simulation, SimulationDiagnosticCodes.InvalidFieldModulus);
    }

    [Fact]
    public void ZeroNormalizationEnergyIsAccepted()
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateKinetic() with { NormalizationEnergyEv = 0.0 },
            SimulationDiagnosticCodes.InvalidNormalizationEnergy);
    }

    [Fact]
    public void TenElectronVoltNormalizationIsAccepted()
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateKinetic() with { NormalizationEnergyEv = 10.0 },
            SimulationDiagnosticCodes.NormalizationEnergyAboveRecommended);
    }

    [Fact]
    public void PositiveMaxAttemptFrequencyIsAccepted()
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateKinetic() with { MaxAttemptFrequencyHz = 1.0 },
            SimulationDiagnosticCodes.InvalidMaxAttemptFrequency);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void BreakToleranceAtHardBoundaryIsAccepted(double breakTolerance)
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateMetropolis() with { BreakTolerance = breakTolerance },
            SimulationDiagnosticCodes.InvalidBreakTolerance);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(100_000L)]
    public void BreakSampleLengthAtHardBoundaryIsAccepted(long breakSampleLength)
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateMetropolis() with { BreakSampleLengthMcsp = breakSampleLength },
            SimulationDiagnosticCodes.InvalidBreakSampleLength);
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(1000L)]
    public void BreakSampleIntervalAtHardBoundaryIsAccepted(long breakSampleInterval)
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateMetropolis() with { BreakSampleIntervalMcsp = breakSampleInterval },
            SimulationDiagnosticCodes.InvalidBreakSampleInterval);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1_000_000L)]
    public void ResultSampleAtHardBoundaryIsAccepted(long resultSample)
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateMetropolis() with { ResultSampleMcsp = resultSample },
            SimulationDiagnosticCodes.InvalidResultSample);
    }

    [Fact]
    public void ZeroWriteOutDoesNotWarn()
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateKinetic() with { WriteOutCount = 0 },
            SimulationDiagnosticCodes.WriteOutOutsideRecommendedBand);
    }

    [Fact]
    public void ZeroMinSuccessRateDoesNotWarn()
    {
        AssertDoesNotHave(
            SimulationTestModels.CreateKinetic() with { MinSuccessRate = 0.0 },
            SimulationDiagnosticCodes.MinSuccessRateBelowRecommended);
    }

    private static void AssertDoesNotHave(Simulation simulation, string code)
    {
        var diagnostics = SimulationModelValidator.Validate(
            SimulationTestModels.CreateModel(simulation));
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Code == code);
    }
}
