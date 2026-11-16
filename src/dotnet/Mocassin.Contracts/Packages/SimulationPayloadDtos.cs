// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using MessagePack;

namespace Mocassin.Contracts.Packages;

/// <summary>
/// One compiled simulation encoding.
/// </summary>
/// <remarks>
/// <see cref="DefaultSettings"/> carries the compiled runtime defaults. <see cref="TrackerModels"/> are
/// definitions only; Step 18 owns the mutable tracker values and the mobile-tracker assignment.
/// </remarks>
/// <param name="Id">The stable project simulation identifier.</param>
/// <param name="Kind">The simulation kind (<c>kmc</c> or <c>mmc</c>).</param>
/// <param name="JumpCollectionIds">The active compiled collection indices.</param>
/// <param name="DefaultSettings">The compiled runtime-default settings.</param>
/// <param name="TrackerModels">The tracker index lists used by this simulation; empty for MMC.</param>
[MessagePackObject]
public sealed record SimulationPayload(
    [property: Key("id")] string Id,
    [property: Key("kind")] string Kind,
    [property: Key("jump_collection_ids")] ImmutableArray<uint> JumpCollectionIds,
    [property: Key("default_settings")] SimulationSettingsPayload DefaultSettings,
    [property: Key("tracker_models")] TrackerModelPayload TrackerModels);

/// <summary>
/// The compiled runtime-default settings of one simulation.
/// </summary>
/// <remarks>
/// <see cref="Flags"/> contains only the authored bits (<c>noSaving</c>, <c>noJumpLogging</c>,
/// <c>useFastExp</c>, <c>deterministic</c>). Derived intent (<c>useDualDofCorrection</c>, <c>usePrerun</c>, and
/// the kind bit) is composed into the job <c>flags</c> word, not the model payload.
/// <see cref="ElectricFieldDirection"/> is the normalized Cartesian unit vector; a zero modulus stores the
/// normalized authored direction when non-zero and <c>(1, 0, 0)</c> when the authored vector is also zero.
/// </remarks>
/// <param name="TemperatureK">The temperature in kelvin.</param>
/// <param name="TargetMcsp">The target successful steps per mobile particle.</param>
/// <param name="WriteOutCount">The intermediate data-out count; zero means the final state only.</param>
/// <param name="PreRunMcsp">The KMC pre-run MCSP; zero disables the pre-run.</param>
/// <param name="TimeLimitS">The wall-clock limit in seconds, or <see langword="null"/>.</param>
/// <param name="MinSuccessRate">The lower abort rate in MCS/s, or <see langword="null"/>.</param>
/// <param name="ElectricFieldVm">The electric-field modulus in V/m.</param>
/// <param name="ElectricFieldDirection">The normalized Cartesian field direction.</param>
/// <param name="NormalizationEnergyEv">The normalization energy in eV, or <see langword="null"/>.</param>
/// <param name="MaxAttemptFrequencyHz">The maximum attempt frequency in hertz, or <see langword="null"/>.</param>
/// <param name="BreakTolerance">The MMC break tolerance, or <see langword="null"/>.</param>
/// <param name="BreakSampleLengthMcsp">The MMC break sample length, or <see langword="null"/>.</param>
/// <param name="BreakSampleIntervalMcsp">The MMC break sample interval, or <see langword="null"/>.</param>
/// <param name="ResultSampleMcsp">The MMC result sample length, or <see langword="null"/>.</param>
/// <param name="Flags">The authored execution flags.</param>
[MessagePackObject]
public sealed record SimulationSettingsPayload(
    [property: Key("temperature_k")] double TemperatureK,
    [property: Key("target_mcsp")] ulong TargetMcsp,
    [property: Key("write_out_count")] uint WriteOutCount,
    [property: Key("pre_run_mcsp")] ulong PreRunMcsp,
    [property: Key("time_limit_s")] ulong? TimeLimitS,
    [property: Key("min_success_rate")] double? MinSuccessRate,
    [property: Key("electric_field_vm")] double ElectricFieldVm,
    [property: Key("electric_field_direction")] ImmutableArray<double> ElectricFieldDirection,
    [property: Key("normalization_energy_ev")] double? NormalizationEnergyEv,
    [property: Key("max_attempt_frequency_hz")] double? MaxAttemptFrequencyHz,
    [property: Key("break_tolerance")] double? BreakTolerance,
    [property: Key("break_sample_length_mcsp")] ulong? BreakSampleLengthMcsp,
    [property: Key("break_sample_interval_mcsp")] ulong? BreakSampleIntervalMcsp,
    [property: Key("result_sample_mcsp")] ulong? ResultSampleMcsp,
    [property: Key("flags")] uint Flags);

/// <summary>
/// The global and static tracker index lists of one compiled simulation.
/// </summary>
/// <remarks>
/// The tracker definitions themselves are derivable from <c>structure.global_tracker_count</c> and the
/// transition tracker-mapping arrays; this record carries only the per-simulation index lists. Both arrays are
/// empty for MMC.
/// </remarks>
/// <param name="GlobalTrackerIndices">The global tracker indices referenced by the simulation.</param>
/// <param name="StaticTrackerIndices">The static tracker indices referenced by the simulation.</param>
[MessagePackObject]
public sealed record TrackerModelPayload(
    [property: Key("global_tracker_indices")] ImmutableArray<uint> GlobalTrackerIndices,
    [property: Key("static_tracker_indices")] ImmutableArray<uint> StaticTrackerIndices);
