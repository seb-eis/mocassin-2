// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Simulations;

namespace Mocassin.Core.Tests.Jobs;

[Trait("Tier", "PR-fast")]
public sealed class JobSettingsValidatorTests
{
    private static readonly ModelPath path = new(
        "jobCollection",
        Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7a01"),
        "baseSettings");

    [Fact]
    public void ValidKmcAndMmcLayersEmitNoDiagnostics()
    {
        var kmc = new JobSettings
        {
            TemperatureK = 773.0,
            TargetMcsp = 200,
            WriteOutCount = 10,
            TimeLimitSeconds = 3600,
            MinSuccessRate = 12.5,
            Flags = ExecutionFlags.NoSaving | ExecutionFlags.Deterministic,
            Instruction = "custom instruction",
            PreRunMcsp = 100,
            ElectricFieldModulusVm = 1.0e7,
            ElectricFieldDirection = new Fractional3(1.0, 0.0, 0.0),
            NormalizationEnergyEv = 0.5,
            MaxAttemptFrequencyHz = 1.0e13,
        };
        var mmc = new JobSettings
        {
            TemperatureK = 773.0,
            TargetMcsp = 200,
            WriteOutCount = 10,
            TimeLimitSeconds = null,
            MinSuccessRate = null,
            Flags = ExecutionFlags.None,
            BreakTolerance = 0.01,
            BreakSampleLengthMcsp = 1000,
            BreakSampleIntervalMcsp = 100,
            ResultSampleMcsp = 200,
        };

        Assert.Empty(JobSettingsValidator.Validate(kmc, SimulationKind.Kmc, path));
        Assert.Empty(JobSettingsValidator.Validate(mmc, SimulationKind.Mmc, path));
        Assert.Empty(JobSettingsValidator.Validate(null, SimulationKind.Kmc, path));
    }

    [Fact]
    public void EveryHardRangeViolationIsReportedAsJob015()
    {
        var settings = new JobSettings
        {
            TemperatureK = double.NaN,
            TargetMcsp = 0,
            WriteOutCount = -1,
            TimeLimitSeconds = 0,
            MinSuccessRate = double.PositiveInfinity,
            Flags = (ExecutionFlags)128,
            PreRunMcsp = -1,
            ElectricFieldModulusVm = double.NaN,
            ElectricFieldDirection = new Fractional3(double.NaN, 0.0, 0.0),
            NormalizationEnergyEv = -1.0,
            MaxAttemptFrequencyHz = 0.0,
            BreakTolerance = 1.5,
            BreakSampleLengthMcsp = -1,
            BreakSampleIntervalMcsp = 0,
            ResultSampleMcsp = -1,
        };

        var diagnostics = JobSettingsValidator.Validate(settings, kind: null, path);

        Assert.All(
            invalidProperties,
            property => Assert.Contains(
                diagnostics,
                diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidSettings &&
                              diagnostic.Path?.Property == $"baseSettings.{property}"));
    }

    private static readonly string[] invalidProperties =
    {
                "temperatureK",
                "targetMcsp",
                "writeOutCount",
                "timeLimitSeconds",
                "minSuccessRate",
                "flags",
                "preRunMcsp",
                "electricFieldModulusVm",
                "electricFieldDirection",
                "normalizationEnergyEv",
                "maxAttemptFrequencyHz",
                "breakTolerance",
                "breakSampleLengthMcsp",
                "breakSampleIntervalMcsp",
                "resultSampleMcsp",
    };

    [Fact]
    public void KmcOnlyFieldsOnMmcAreJob015()
    {
        var settings = new JobSettings
        {
            PreRunMcsp = 0,
            ElectricFieldModulusVm = 0.0,
            ElectricFieldDirection = new Fractional3(1.0, 0.0, 0.0),
            NormalizationEnergyEv = null,
            MaxAttemptFrequencyHz = 1.0e13,
        };

        var diagnostics = JobSettingsValidator.Validate(settings, SimulationKind.Mmc, path);

        Assert.Equal(4, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal(JobDiagnosticCodes.InvalidSettings, diagnostic.Code));
        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Path?.Property?.EndsWith("normalizationEnergyEv", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void MmcOnlyFieldsOnKmcAreJob015()
    {
        var settings = new JobSettings
        {
            BreakTolerance = 0.01,
            BreakSampleLengthMcsp = 1000,
            BreakSampleIntervalMcsp = 100,
            ResultSampleMcsp = 200,
        };

        var diagnostics = JobSettingsValidator.Validate(settings, SimulationKind.Kmc, path);

        Assert.Equal(4, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal(JobDiagnosticCodes.InvalidSettings, diagnostic.Code));
    }

    [Fact]
    public void ExtensionCountAndIdRulesAreJob018()
    {
        var tooMany = ImmutableArray.CreateBuilder<JobExtension>();
        for (var index = 0; index < 17; index++)
        {
            tooMany.Add(Extension($"id-{index}", "{}"));
        }

        var duplicateIds = ImmutableArray.Create(
            Extension("same", "{}"),
            Extension("same", "{}"));
        var invalidIds = ImmutableArray.Create(
            Extension(string.Empty, "{}"),
            Extension(new string('x', 65), "{}"));

        Assert.Contains(
            JobSettingsValidator.Validate(Settings(tooMany.ToImmutable()), SimulationKind.Kmc, path),
            diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidExtension);
        Assert.Contains(
            JobSettingsValidator.Validate(Settings(duplicateIds), SimulationKind.Kmc, path),
            diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidExtension &&
                          diagnostic.Message.Contains("more than once", StringComparison.Ordinal));
        Assert.Contains(
            JobSettingsValidator.Validate(Settings(invalidIds), SimulationKind.Kmc, path),
            diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidExtension &&
                          diagnostic.Message.Contains("must not be empty", StringComparison.Ordinal));
        Assert.Contains(
            JobSettingsValidator.Validate(Settings(invalidIds), SimulationKind.Kmc, path),
            diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidExtension &&
                          diagnostic.Message.Contains("64 characters", StringComparison.Ordinal));
    }

    [Fact]
    public void ExtensionConfigMustBeAnObject()
    {
        var extensions = ImmutableArray.Create(Extension("array", "[1,2,3]"));

        var diagnostics = JobSettingsValidator.Validate(Settings(extensions), SimulationKind.Kmc, path);

        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidExtension &&
                          diagnostic.Message.Contains("JSON object", StringComparison.Ordinal));
    }

    [Fact]
    public void ExtensionConfigAndTotalSizeBoundsAreJob018()
    {
        var oversized = ImmutableArray.Create(Extension("large", $"{{\"data\":\"{new string('x', 70_000)}\"}}"));
        var totalOversized = ImmutableArray.Create(
            Extension("a", $"{{\"data\":\"{new string('x', 60_000)}\"}}"),
            Extension("b", $"{{\"data\":\"{new string('x', 60_000)}\"}}"),
            Extension("c", $"{{\"data\":\"{new string('x', 60_000)}\"}}"),
            Extension("d", $"{{\"data\":\"{new string('x', 60_000)}\"}}"),
            Extension("e", $"{{\"data\":\"{new string('x', 60_000)}\"}}"));

        var perConfig = JobSettingsValidator.Validate(Settings(oversized), SimulationKind.Kmc, path);
        var total = JobSettingsValidator.Validate(Settings(totalOversized), SimulationKind.Kmc, path);

        Assert.Contains(
            perConfig,
            diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidExtension &&
                          diagnostic.Message.Contains("must not exceed 65536", StringComparison.Ordinal));
        Assert.Contains(
            total,
            diagnostic => diagnostic.Code == JobDiagnosticCodes.InvalidExtension &&
                          diagnostic.Message.Contains("in total", StringComparison.Ordinal));
    }

    private static JobSettings Settings(ImmutableArray<JobExtension> extensions) =>
        new() { TemperatureK = 773.0, Extensions = extensions };

    private static JobExtension Extension(string id, string json)
    {
        using var document = JsonDocument.Parse(json);
        return new JobExtension(id, document.RootElement.Clone());
    }
}
