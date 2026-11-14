// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Simulations;

namespace Mocassin.Core.Model.Jobs;

/// <summary>
/// A partial, nullable job-setting override. <see langword="null"/> means inherit; explicit zero values override.
/// </summary>
/// <remarks>
/// KMC-only and MMC-only fields are validated against the selected simulation kind in the job-template
/// validator. The record is deliberately a flat settings object so project JSON and the job payload can share the
/// same field names.
/// </remarks>
public sealed record JobSettings
{
    /// <summary>Gets the temperature in kelvin.</summary>
    public double? TemperatureK { get; init; }

    /// <summary>Gets the target successful steps per mobile particle.</summary>
    public long? TargetMcsp { get; init; }

    /// <summary>Gets the intermediate data-out count.</summary>
    public int? WriteOutCount { get; init; }

    /// <summary>Gets the wall-clock limit in seconds.</summary>
    public long? TimeLimitSeconds { get; init; }

    /// <summary>Gets the lower abort rate in MCS/s.</summary>
    public double? MinSuccessRate { get; init; }

    /// <summary>Gets the authored execution flags.</summary>
    public ExecutionFlags? Flags { get; init; }

    /// <summary>Gets the optional advanced instruction.</summary>
    public string? Instruction { get; init; }

    /// <summary>Gets the KMC pre-run MCSP.</summary>
    public long? PreRunMcsp { get; init; }

    /// <summary>Gets the KMC electric-field modulus in V/m.</summary>
    public double? ElectricFieldModulusVm { get; init; }

    /// <summary>Gets the KMC fractional electric-field direction.</summary>
    public Fractional3? ElectricFieldDirection { get; init; }

    /// <summary>Gets the KMC normalization energy in eV.</summary>
    public double? NormalizationEnergyEv { get; init; }

    /// <summary>Gets the KMC maximum attempt frequency in hertz.</summary>
    public double? MaxAttemptFrequencyHz { get; init; }

    /// <summary>Gets the MMC break tolerance.</summary>
    public double? BreakTolerance { get; init; }

    /// <summary>Gets the MMC break sample length in MCSP.</summary>
    public long? BreakSampleLengthMcsp { get; init; }

    /// <summary>Gets the MMC break sample interval in MCSP.</summary>
    public long? BreakSampleIntervalMcsp { get; init; }

    /// <summary>Gets the MMC result sample length in MCSP.</summary>
    public long? ResultSampleMcsp { get; init; }

    /// <summary>
    /// Gets the extension configuration entries. <see langword="null"/> inherits; a non-null array replaces the
    /// inherited list as a whole.
    /// </summary>
    public ImmutableArray<JobExtension>? Extensions { get; init; }
}
