// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Transitions;

namespace Mocassin.Core.Model.Simulations;

/// <summary>
/// Common authored settings of a kinetic or Metropolis simulation.
/// </summary>
/// <remarks>
/// <see cref="Kind"/> is derived from the concrete record type and is not settable. The persisted project JSON
/// is a flat object with <c>kind</c>, the matching transition array, and a <c>settings</c> object; only the
/// matching kind-specific settings are present. Transition arrays preserve authored order and are never
/// re-sorted.
/// </remarks>
[JsonConverter(typeof(SimulationJsonConverter))]
public abstract record Simulation
{
    /// <summary>
    /// Gets the stable simulation identifier.
    /// </summary>
    public required SimulationId Id { get; init; }

    /// <summary>
    /// Gets the user-visible simulation name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the concrete simulation kind.
    /// </summary>
    public abstract SimulationKind Kind { get; }

    /// <summary>
    /// Gets the temperature in kelvin.
    /// </summary>
    public double TemperatureK { get; init; } = 1000.0;

    /// <summary>
    /// Gets the number of successful steps per mobile particle for the main run.
    /// </summary>
    public long TargetMcsp { get; init; } = 1000;

    /// <summary>
    /// Gets the number of intermediate data-out calls; 0 writes only the final state.
    /// </summary>
    public int WriteOutCount { get; init; } = 100;

    /// <summary>
    /// Gets the wall-clock run limit in seconds, or <see langword="null"/> for no limit.
    /// </summary>
    public long? TimeLimitSeconds { get; init; } = 31_536_000;

    /// <summary>
    /// Gets the lower abort rate in MCS/s, or <see langword="null"/> when disabled.
    /// </summary>
    public double? MinSuccessRate { get; init; }

    /// <summary>
    /// Gets the default number of duplicate jobs.
    /// </summary>
    public int JobCount { get; init; } = 12;

    /// <summary>
    /// Gets the optional explicit RNG seed.
    /// </summary>
    public string? RngSeed { get; init; }

    /// <summary>
    /// Gets the authored execution flags.
    /// </summary>
    public ExecutionFlags Flags { get; init; }

    /// <summary>
    /// Gets the optional advanced routine or extension instruction.
    /// </summary>
    public string? Instruction { get; init; }

    /// <summary>
    /// Gets the referenced transitions in authored order.
    /// </summary>
    public ImmutableArray<TransitionRef> TransitionIds { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the persisted kind-specific shape disagreed with <see cref="Kind"/>.
    /// </summary>
    /// <remarks>
    /// This is reader-only validation state. It is never persisted and is reported as <c>SIM023</c>.
    /// </remarks>
    internal bool HasPersistedKindMismatch { get; init; }
}
