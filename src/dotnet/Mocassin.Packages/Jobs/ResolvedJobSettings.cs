// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Simulations;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// The fully resolved, non-nullable settings of one expanded job.
/// </summary>
/// <remarks>
/// Resolution follows the compiled simulation defaults, the collection base settings, and the configuration
/// overrides, in that order. The duplicate job changes only its seed, never these values.
/// </remarks>
/// <param name="TemperatureK">The temperature in kelvin.</param>
/// <param name="TargetMcsp">The target successful steps per mobile particle.</param>
/// <param name="WriteOutCount">The intermediate data-out count.</param>
/// <param name="TimeLimitSeconds">The wall-clock limit, or <see langword="null"/>.</param>
/// <param name="MinSuccessRate">The lower abort rate, or <see langword="null"/>.</param>
/// <param name="Flags">The authored execution flags.</param>
/// <param name="Instruction">The optional advanced instruction.</param>
/// <param name="PreRunMcsp">The KMC pre-run MCSP; 0 disables it.</param>
/// <param name="ElectricFieldModulusVm">The KMC field modulus in V/m.</param>
/// <param name="ElectricFieldDirection">The KMC fractional field direction.</param>
/// <param name="NormalizationEnergyEv">The KMC normalization energy, or <see langword="null"/>.</param>
/// <param name="MaxAttemptFrequencyHz">The KMC maximum attempt frequency, or <see langword="null"/>.</param>
/// <param name="BreakTolerance">The MMC break tolerance.</param>
/// <param name="BreakSampleLengthMcsp">The MMC break sample length.</param>
/// <param name="BreakSampleIntervalMcsp">The MMC break sample interval.</param>
/// <param name="ResultSampleMcsp">The MMC result sample length.</param>
/// <param name="Extensions">The effective extension entries; an empty array means none.</param>
public sealed record ResolvedJobSettings(
    double TemperatureK,
    long TargetMcsp,
    int WriteOutCount,
    long? TimeLimitSeconds,
    double? MinSuccessRate,
    ExecutionFlags Flags,
    string? Instruction,
    long PreRunMcsp,
    double ElectricFieldModulusVm,
    Fractional3 ElectricFieldDirection,
    double? NormalizationEnergyEv,
    double? MaxAttemptFrequencyHz,
    double BreakTolerance,
    long BreakSampleLengthMcsp,
    long BreakSampleIntervalMcsp,
    long ResultSampleMcsp,
    ImmutableArray<JobExtension> Extensions);
