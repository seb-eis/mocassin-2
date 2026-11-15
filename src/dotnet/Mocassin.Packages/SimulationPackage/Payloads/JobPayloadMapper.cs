// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Contracts.Packages;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Simulations;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Jobs.Optimization;

namespace Mocassin.Packages.SimulationPackage.Payloads;

/// <summary>
/// Maps a real expanded job descriptor and its resolved settings to the persisted job payload.
/// </summary>
internal static class JobPayloadMapper
{
    /// <summary>
    /// Maps one job.
    /// </summary>
    /// <param name="descriptor">The expanded job descriptor.</param>
    /// <param name="cellVectors">The compiled cell vectors used for the field-direction conversion.</param>
    /// <param name="dopingInfo">The generated lattice's canonical doping summary.</param>
    /// <returns>The job payload.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="descriptor"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A resolved setting is outside the payload range.</exception>
    public static JobPayload Map(
        JobDescriptor descriptor,
        ImmutableArray<Cartesian3> cellVectors,
        string dopingInfo)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(dopingInfo);
        var settings = descriptor.Settings;
        if (descriptor.SimulationIndex < 0)
        {
            throw new InvalidOperationException("The simulation index must be non-negative.");
        }

        var flags = SolverJobFlagsMapper.Compose(
            settings.Flags,
            descriptor.Kind,
            settings.PreRunMcsp,
            descriptor.UseDualDofCorrection);
        return new JobPayload(
            PackageFormatConstants.JobPayloadFormatVersion,
            (uint)descriptor.SimulationIndex,
            MapKind(descriptor.Kind),
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
            (uint)flags,
            MapOptimizers(descriptor.CompiledOptimizers),
            MapExtensions(settings.Extensions),
            settings.Instruction,
            dopingInfo);
    }

    private static string MapKind(SimulationKind kind) =>
        kind == SimulationKind.Kmc ? "kmc" : "mmc";

    private static ImmutableArray<SelectionOptimizerPayload> MapOptimizers(
        ImmutableArray<CompiledSelectionOptimizer> optimizers)
    {
        var mapped = new SelectionOptimizerPayload[optimizers.Length];
        for (var index = 0; index < optimizers.Length; index++)
        {
            var optimizer = optimizers[index];
            if (optimizer.PositionId < 0 || optimizer.ParticleId is < 0 or > byte.MaxValue)
            {
                throw new InvalidOperationException("A compiled selection optimizer is outside the payload range.");
            }

            mapped[index] = new SelectionOptimizerPayload(
                (uint)optimizer.PositionId,
                (byte)optimizer.ParticleId);
        }

        return [.. mapped];
    }

    private static ImmutableArray<ExtensionPayload> MapExtensions(
        ImmutableArray<JobExtension> extensions)
    {
        var ordered = extensions
            .OrderBy(static extension => extension.Id, StringComparer.Ordinal)
            .ToArray();
        var mapped = new ExtensionPayload[ordered.Length];
        for (var index = 0; index < ordered.Length; index++)
        {
            var extension = ordered[index];
            ArgumentException.ThrowIfNullOrWhiteSpace(extension.Id);
            mapped[index] = new ExtensionPayload(extension.Id, extension.ToCanonicalJson());
        }

        return [.. mapped];
    }

    private static ulong NonNegative(long value, string name) =>
        value >= 0 ? (ulong)value : throw new InvalidOperationException($"{name} must be non-negative.");

    private static uint CheckedUInt(int value, string name) =>
        value >= 0 ? (uint)value : throw new InvalidOperationException($"{name} must be non-negative.");
}
