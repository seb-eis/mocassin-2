// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using MessagePack;

namespace Mocassin.Contracts.Packages;

/// <summary>
/// The <c>.msl</c> job payload.
/// </summary>
/// <remarks>
/// The payload carries the fully resolved settings of one expanded job. The <c>jobs</c> row stores only the
/// payload-derived columns plus authored/build metadata (names, indices, seed, and <c>created_utc</c>).
/// <see cref="Flags"/> is the composed <c>SolverJobFlags</c> word.
/// </remarks>
/// <param name="FormatVersion">The payload-encoding version.</param>
/// <param name="SimulationIndex">The index into the model payload's <c>simulations</c> array.</param>
/// <param name="Kind">The job kind (<c>kmc</c> or <c>mmc</c>).</param>
/// <param name="TemperatureK">The temperature in kelvin.</param>
/// <param name="TargetMcsp">The target successful steps per mobile particle.</param>
/// <param name="WriteOutCount">The intermediate data-out count; zero means the final state only.</param>
/// <param name="PreRunMcsp">The KMC pre-run MCSP; zero disables the pre-run.</param>
/// <param name="TimeLimitS">The wall-clock limit in seconds, or <see langword="null"/>.</param>
/// <param name="MinSuccessRate">The lower abort rate in MCS/s, or <see langword="null"/>.</param>
/// <param name="ElectricFieldVm">The electric-field modulus in V/m.</param>
/// <param name="ElectricFieldDirection">The normalized Cartesian field direction.</param>
/// <param name="NormalizationEnergyEv">The normalization energy in eV, or <see langword="null"/>.</param>
/// <param name="MaxAttemptFrequencyHz">The job-level maximum attempt frequency in hertz, or <see langword="null"/>.</param>
/// <param name="BreakTolerance">The MMC break tolerance, or <see langword="null"/>.</param>
/// <param name="BreakSampleLengthMcsp">The MMC break sample length, or <see langword="null"/>.</param>
/// <param name="BreakSampleIntervalMcsp">The MMC break sample interval, or <see langword="null"/>.</param>
/// <param name="ResultSampleMcsp">The MMC result sample length, or <see langword="null"/>.</param>
/// <param name="Flags">The composed <c>SolverJobFlags</c> word.</param>
/// <param name="SelectionOptimizers">
/// The KMC start options removed from the selection pool, sorted by position and particle index.
/// </param>
/// <param name="Extensions">The effective extension entries, sorted by <see cref="ExtensionPayload.Id"/>.</param>
/// <param name="Instruction">The optional advanced instruction, or <see langword="null"/>.</param>
/// <param name="DopingInfo">The canonical priority-ordered doping summary, or an empty string.</param>
[MessagePackObject]
public sealed record JobPayload(
    [property: Key("format_version")] uint FormatVersion,
    [property: Key("simulation_index")] uint SimulationIndex,
    [property: Key("kind")] string Kind,
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
    [property: Key("flags")] uint Flags,
    [property: Key("selection_optimizers")] ImmutableArray<SelectionOptimizerPayload> SelectionOptimizers,
    [property: Key("extensions")] ImmutableArray<ExtensionPayload> Extensions,
    [property: Key("instruction")] string? Instruction,
    [property: Key("doping_info")] string DopingInfo);

/// <summary>
/// One compiled KMC selection optimizer entry.
/// </summary>
/// <param name="PositionId">The canonical P1 position index.</param>
/// <param name="ParticleId">The compiled particle index removed from the start pool.</param>
[MessagePackObject]
public sealed record SelectionOptimizerPayload(
    [property: Key("position_id")] uint PositionId,
    [property: Key("particle_id")] byte ParticleId);

/// <summary>
/// One opaque solver-extension configuration.
/// </summary>
/// <remarks>
/// <see cref="ConfigJson"/> is the UTF-8 canonical JSON text of the authored config, stored as a MessagePack
/// string rather than a map so authored number tokens (including integers above 2^63) are preserved verbatim.
/// </remarks>
/// <param name="Id">The stable extension identifier.</param>
/// <param name="ConfigJson">The canonical JSON text of the authored configuration.</param>
[MessagePackObject]
public sealed record ExtensionPayload(
    [property: Key("id")] string Id,
    [property: Key("config_json")] string ConfigJson);
