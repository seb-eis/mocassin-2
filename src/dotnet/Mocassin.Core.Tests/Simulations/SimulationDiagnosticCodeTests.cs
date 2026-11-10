// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Simulations;

[Trait("Tier", "PR-fast")]
public sealed class SimulationDiagnosticCodeTests
{
    private static readonly string[] codes =
    [
        SimulationDiagnosticCodes.UnknownTransition,
        SimulationDiagnosticCodes.WrongTransitionKind,
        SimulationDiagnosticCodes.NoTransitions,
        SimulationDiagnosticCodes.InvalidTemperature,
        SimulationDiagnosticCodes.InvalidTargetMcsp,
        SimulationDiagnosticCodes.InvalidJobCount,
        SimulationDiagnosticCodes.InvalidFieldDirection,
        SimulationDiagnosticCodes.InvalidPreRunMcsp,
        SimulationDiagnosticCodes.InvalidName,
        SimulationDiagnosticCodes.InvalidWriteOutCount,
        SimulationDiagnosticCodes.InvalidTimeLimit,
        SimulationDiagnosticCodes.InvalidMinSuccessRate,
        SimulationDiagnosticCodes.InvalidFieldModulus,
        SimulationDiagnosticCodes.InvalidNormalizationEnergy,
        SimulationDiagnosticCodes.InvalidMaxAttemptFrequency,
        SimulationDiagnosticCodes.InvalidBreakTolerance,
        SimulationDiagnosticCodes.InvalidBreakSampleLength,
        SimulationDiagnosticCodes.InvalidBreakSampleInterval,
        SimulationDiagnosticCodes.InvalidResultSample,
        SimulationDiagnosticCodes.RngSeedTooLong,
        SimulationDiagnosticCodes.TransitionLimitExceeded,
        SimulationDiagnosticCodes.DuplicateTransitionReference,
        SimulationDiagnosticCodes.KindShapeMismatch,
        SimulationDiagnosticCodes.TemperatureOutsideRecommendedBand,
        SimulationDiagnosticCodes.TargetMcspAboveRecommended,
        SimulationDiagnosticCodes.JobCountOutsideRecommendedBand,
        SimulationDiagnosticCodes.PreRunAboveRecommended,
        SimulationDiagnosticCodes.WriteOutOutsideRecommendedBand,
        SimulationDiagnosticCodes.FieldModulusOutsideRecommendedBand,
        SimulationDiagnosticCodes.BreakToleranceAboveRecommended,
        SimulationDiagnosticCodes.BreakSampleLengthAboveRecommended,
        SimulationDiagnosticCodes.BreakSampleIntervalAboveRecommended,
        SimulationDiagnosticCodes.ResultSampleAboveRecommended,
        SimulationDiagnosticCodes.NormalizationEnergyAboveRecommended,
        SimulationDiagnosticCodes.InvalidExecutionFlags,
        SimulationDiagnosticCodes.TransitionCountOutsideRecommendedBand,
        SimulationDiagnosticCodes.MinSuccessRateBelowRecommended,
    ];

    [Fact]
    public void AllSimulationCodesAreUniqueAndHaveHelpText()
    {
        Assert.Equal(37, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(37, codes.Length);

        for (var index = 0; index < codes.Length; index++)
        {
            Assert.Equal($"SIM{index + 1:000}", codes[index]);
            Assert.False(string.IsNullOrWhiteSpace(SimulationDiagnosticCodes.HelpFor(codes[index])));
        }
    }

    [Fact]
    public void UnknownCodeHasNoHelpText()
    {
        Assert.Null(SimulationDiagnosticCodes.HelpFor("SIM999"));
        Assert.Null(SimulationDiagnosticCodes.HelpFor("LAT001"));
    }

    [Fact]
    public void ValidSimulationsDoNotEmitAnySimulationCode()
    {
        var model = SimulationTestModels.CreateModel(
            SimulationTestModels.CreateKinetic(),
            SimulationTestModels.CreateMetropolis());

        Assert.DoesNotContain(
            SimulationModelValidator.Validate(model),
            diagnostic => diagnostic.Code.StartsWith("SIM", StringComparison.Ordinal));
    }
}
