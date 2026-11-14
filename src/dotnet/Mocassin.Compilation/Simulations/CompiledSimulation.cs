// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Simulations;

namespace Mocassin.Compilation.Simulations;

/// <summary>
/// The derived solver intent for one compiled simulation.
/// </summary>
/// <param name="Kind">The authored simulation kind.</param>
/// <param name="UseDualDofCorrection">Whether the KMC job uses the dual degree-of-freedom correction.</param>
/// <param name="UsePreRun">Whether a pre-run is required.</param>
/// <param name="PreRunMcsp">The authored pre-run MCSP.</param>
public sealed record CompiledSolverIntent(
    SimulationKind Kind,
    bool UseDualDofCorrection,
    bool UsePreRun,
    long PreRunMcsp);

/// <summary>
/// The immutable runtime-default settings snapshot of one compiled simulation.
/// </summary>
/// <param name="TemperatureK">The temperature in kelvin.</param>
/// <param name="TargetMcsp">The target MCSP.</param>
/// <param name="WriteOutCount">The write-out count.</param>
/// <param name="TimeLimitSeconds">The wall-clock limit, or <see langword="null"/>.</param>
/// <param name="MinSuccessRate">The lower abort rate, or <see langword="null"/>.</param>
/// <param name="JobCount">The duplicate job count.</param>
/// <param name="RngSeed">The optional explicit seed.</param>
/// <param name="Flags">The authored execution flags.</param>
/// <param name="Instruction">The optional instruction.</param>
/// <param name="PreRunMcsp">The KMC pre-run MCSP.</param>
/// <param name="ElectricFieldModulusVm">The KMC field modulus in V/m.</param>
/// <param name="ElectricFieldDirection">The KMC fractional field direction.</param>
/// <param name="NormalizationEnergyEv">The KMC normalization energy, or <see langword="null"/>.</param>
/// <param name="MaxAttemptFrequencyHz">The KMC maximum attempt frequency, or <see langword="null"/>.</param>
/// <param name="BreakTolerance">The MMC break tolerance.</param>
/// <param name="BreakSampleLengthMcsp">The MMC break sample length.</param>
/// <param name="BreakSampleIntervalMcsp">The MMC break sample interval.</param>
/// <param name="ResultSampleMcsp">The MMC result sample length.</param>
public sealed record CompiledRuntimeSettings(
    double TemperatureK,
    long TargetMcsp,
    int WriteOutCount,
    long? TimeLimitSeconds,
    double? MinSuccessRate,
    int JobCount,
    string? RngSeed,
    ExecutionFlags Flags,
    string? Instruction,
    long PreRunMcsp,
    double ElectricFieldModulusVm,
    Core.Geometry.Fractional3 ElectricFieldDirection,
    double? NormalizationEnergyEv,
    double? MaxAttemptFrequencyHz,
    double BreakTolerance,
    long BreakSampleLengthMcsp,
    long BreakSampleIntervalMcsp,
    long ResultSampleMcsp);

/// <summary>
/// One global tracker definition keyed by collection and particle.
/// </summary>
/// <param name="Index">The dense global tracker index.</param>
/// <param name="CollectionIndex">The owning collection index.</param>
/// <param name="ParticleIndex">The tracked compiled particle index; never the void particle.</param>
public sealed record CompiledGlobalTracker(int Index, int CollectionIndex, int ParticleIndex);

/// <summary>
/// One static tracker definition keyed by position and particle.
/// </summary>
/// <param name="Index">The dense static tracker index.</param>
/// <param name="PositionIndex">The canonical P1 position index.</param>
/// <param name="ParticleIndex">The tracked compiled particle index; never the void particle.</param>
public sealed record CompiledStaticTracker(int Index, int PositionIndex, int ParticleIndex);

/// <summary>
/// The tracker definitions referenced by one compiled simulation.
/// </summary>
/// <param name="GlobalTrackerIndices">The global tracker indices.</param>
/// <param name="StaticTrackerIndices">The static tracker indices.</param>
public sealed record CompiledTrackerModel(
    ImmutableArray<int> GlobalTrackerIndices,
    ImmutableArray<int> StaticTrackerIndices)
{
    /// <summary>
    /// Gets the empty tracker model used by MMC.
    /// </summary>
    public static CompiledTrackerModel Empty { get; } = new([], []);
}

/// <summary>
/// One compiled simulation.
/// </summary>
/// <param name="Id">The stable simulation identifier.</param>
/// <param name="Name">The user-visible simulation name.</param>
/// <param name="Kind">The simulation kind.</param>
/// <param name="ActiveCollectionIndices">The compiled collection indices used by the simulation.</param>
/// <param name="Settings">The runtime-default settings snapshot.</param>
/// <param name="Intent">The derived solver intent.</param>
/// <param name="Trackers">The tracker definitions; empty for MMC.</param>
public sealed record CompiledSimulation(
    SimulationId Id,
    string Name,
    SimulationKind Kind,
    ImmutableArray<int> ActiveCollectionIndices,
    CompiledRuntimeSettings Settings,
    CompiledSolverIntent Intent,
    CompiledTrackerModel Trackers);
