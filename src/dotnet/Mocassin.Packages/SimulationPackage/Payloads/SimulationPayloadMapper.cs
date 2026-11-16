// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Simulations;
using Mocassin.Contracts.Packages;
using Mocassin.Core.Model.Simulations;

namespace Mocassin.Packages.SimulationPackage.Payloads;

/// <summary>
/// Maps the compiled simulation encodings to the model payload.
/// </summary>
internal static class SimulationPayloadMapper
{
    /// <summary>
    /// Maps the compiled simulations.
    /// </summary>
    /// <param name="model">The compiled model.</param>
    /// <returns>The simulation payloads in compiled order.</returns>
    /// <exception cref="InvalidOperationException">A compiled setting is outside the payload range.</exception>
    public static ImmutableArray<SimulationPayload> Map(CompiledModel model)
    {
        var simulations = new SimulationPayload[model.Simulations.Length];
        for (var index = 0; index < simulations.Length; index++)
        {
            var simulation = model.Simulations[index];
            simulations[index] = new SimulationPayload(
                simulation.Id.Value.ToString("D"),
                MapKind(simulation.Kind),
                MapIndices(simulation.ActiveCollectionIndices, model.Transitions.Collections.Length),
                MapSettings(simulation.Settings, model.Structure.CellVectors),
                new TrackerModelPayload(
                    MapIndices(simulation.Trackers.GlobalTrackerIndices, model.Transitions.GlobalTrackers.Length),
                    MapIndices(
                        simulation.Trackers.StaticTrackerIndices,
                        model.Transitions.StaticTrackers.Length)));
        }

        return [.. simulations];
    }

    private static SimulationSettingsPayload MapSettings(
        CompiledRuntimeSettings settings,
        ImmutableArray<Core.Geometry.Cartesian3> cellVectors) =>
        new(
            settings.TemperatureK,
            NonNegative(settings.TargetMcsp, nameof(settings.TargetMcsp)),
            CheckedUInt(settings.WriteOutCount, nameof(settings.WriteOutCount)),
            NonNegative(settings.PreRunMcsp, nameof(settings.PreRunMcsp)),
            settings.TimeLimitSeconds is { } timeLimit
                ? NonNegative(timeLimit, nameof(settings.TimeLimitSeconds))
                : null,
            settings.MinSuccessRate,
            settings.ElectricFieldModulusVm,
            FieldDirectionConverter.ToCartesianUnit(settings.ElectricFieldDirection, cellVectors),
            settings.NormalizationEnergyEv,
            settings.MaxAttemptFrequencyHz,
            settings.BreakTolerance,
            NonNegative(settings.BreakSampleLengthMcsp, nameof(settings.BreakSampleLengthMcsp)),
            NonNegative(settings.BreakSampleIntervalMcsp, nameof(settings.BreakSampleIntervalMcsp)),
            NonNegative(settings.ResultSampleMcsp, nameof(settings.ResultSampleMcsp)),
            (uint)settings.Flags);

    private static string MapKind(SimulationKind kind) =>
        kind == SimulationKind.Kmc ? "kmc" : "mmc";

    private static ImmutableArray<uint> MapIndices(ImmutableArray<int> indices, int upperBound)
    {
        var values = new uint[indices.Length];
        for (var index = 0; index < indices.Length; index++)
        {
            if (indices[index] < 0 || indices[index] >= upperBound)
            {
                throw new InvalidOperationException($"A simulation tracker or collection index {indices[index]} is out of range.");
            }

            values[index] = (uint)indices[index];
        }

        return [.. values];
    }

    private static ulong NonNegative(long value, string name) =>
        value >= 0 ? (ulong)value : throw new InvalidOperationException($"{name} must be non-negative.");

    private static uint CheckedUInt(int value, string name) =>
        value >= 0 ? (uint)value : throw new InvalidOperationException($"{name} must be non-negative.");
}
