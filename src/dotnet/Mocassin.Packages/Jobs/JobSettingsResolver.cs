// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Simulations;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Simulations;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// Resolves the effective settings of one configuration from compiled defaults, collection base settings, and
/// configuration overrides.
/// </summary>
/// <remarks>
/// Each settings layer is validated before it is merged. Invalid user data returns <see langword="null"/> plus
/// diagnostics instead of a partial result. The resolver deliberately performs no compiled-bound supercell,
/// doping, or optimizer checks; those belong to the job-template build stage.
/// </remarks>
public static class JobSettingsResolver
{
    /// <summary>
    /// Resolves the effective settings of one job configuration.
    /// </summary>
    /// <param name="collection">The owning collection.</param>
    /// <param name="config">The configuration to resolve.</param>
    /// <param name="simulation">The compiled simulation whose runtime defaults apply.</param>
    /// <param name="diagnostics">The diagnostics produced by layer validation.</param>
    /// <returns>The resolved settings, or <see langword="null"/> when a layer is invalid.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static ResolvedJobSettings? Resolve(
        JobCollection collection,
        JobConfig config,
        CompiledSimulation simulation,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(simulation);

        var bag = new DiagnosticBag();
        var compiledLayer = ToCompiledLayer(simulation.Settings, simulation.Kind);
        bag.AddRange(JobSettingsValidator.Validate(
            compiledLayer,
            simulation.Kind,
            new ModelPath("simulation", simulation.Id.Value, "settings")));

        if (collection.BaseSettings is not null)
        {
            bag.AddRange(JobSettingsValidator.Validate(
                collection.BaseSettings,
                simulation.Kind,
                new ModelPath("jobCollection", collection.Id.Value, "baseSettings")));
        }

        if (config.Overrides is not null)
        {
            bag.AddRange(JobSettingsValidator.Validate(
                config.Overrides,
                simulation.Kind,
                new ModelPath("jobConfig", config.Id.Value, "overrides")));
        }

        if (bag.HasErrors)
        {
            diagnostics = bag.Sorted;
            return null;
        }

        var defaults = simulation.Settings;
        var baseSettings = collection.BaseSettings;
        var overrides = config.Overrides;
        var extensions = ResolveExtensions(overrides, baseSettings);

        diagnostics = bag.Sorted;
        return new ResolvedJobSettings(
            overrides?.TemperatureK ?? baseSettings?.TemperatureK ?? compiledLayer.TemperatureK ?? defaults.TemperatureK,
            overrides?.TargetMcsp ?? baseSettings?.TargetMcsp ?? compiledLayer.TargetMcsp ?? defaults.TargetMcsp,
            overrides?.WriteOutCount ?? baseSettings?.WriteOutCount ?? compiledLayer.WriteOutCount ?? defaults.WriteOutCount,
            overrides?.TimeLimitSeconds ?? baseSettings?.TimeLimitSeconds ?? defaults.TimeLimitSeconds,
            overrides?.MinSuccessRate ?? baseSettings?.MinSuccessRate ?? defaults.MinSuccessRate,
            overrides?.Flags ?? baseSettings?.Flags ?? defaults.Flags,
            overrides?.Instruction ?? baseSettings?.Instruction ?? defaults.Instruction,
            overrides?.PreRunMcsp ?? baseSettings?.PreRunMcsp ?? compiledLayer.PreRunMcsp ?? defaults.PreRunMcsp,
            overrides?.ElectricFieldModulusVm ?? baseSettings?.ElectricFieldModulusVm ??
                compiledLayer.ElectricFieldModulusVm ?? defaults.ElectricFieldModulusVm,
            overrides?.ElectricFieldDirection ?? baseSettings?.ElectricFieldDirection ??
                compiledLayer.ElectricFieldDirection ?? defaults.ElectricFieldDirection,
            overrides?.NormalizationEnergyEv ?? baseSettings?.NormalizationEnergyEv ?? defaults.NormalizationEnergyEv,
            overrides?.MaxAttemptFrequencyHz ?? baseSettings?.MaxAttemptFrequencyHz ?? defaults.MaxAttemptFrequencyHz,
            overrides?.BreakTolerance ?? baseSettings?.BreakTolerance ?? defaults.BreakTolerance,
            overrides?.BreakSampleLengthMcsp ?? baseSettings?.BreakSampleLengthMcsp ?? defaults.BreakSampleLengthMcsp,
            overrides?.BreakSampleIntervalMcsp ?? baseSettings?.BreakSampleIntervalMcsp ?? defaults.BreakSampleIntervalMcsp,
            overrides?.ResultSampleMcsp ?? baseSettings?.ResultSampleMcsp ?? defaults.ResultSampleMcsp,
            extensions);
    }

    private static JobSettings ToCompiledLayer(CompiledRuntimeSettings settings, SimulationKind kind)
    {
        var layer = new JobSettings
        {
            TemperatureK = settings.TemperatureK,
            TargetMcsp = settings.TargetMcsp,
            WriteOutCount = settings.WriteOutCount,
            TimeLimitSeconds = settings.TimeLimitSeconds,
            MinSuccessRate = settings.MinSuccessRate,
            Flags = settings.Flags,
            Instruction = settings.Instruction,
        };

        if (kind == SimulationKind.Kmc)
        {
            return layer with
            {
                PreRunMcsp = settings.PreRunMcsp,
                ElectricFieldModulusVm = settings.ElectricFieldModulusVm,
                ElectricFieldDirection = settings.ElectricFieldDirection,
                NormalizationEnergyEv = settings.NormalizationEnergyEv,
                MaxAttemptFrequencyHz = settings.MaxAttemptFrequencyHz,
            };
        }

        return layer with
        {
            BreakTolerance = settings.BreakTolerance,
            BreakSampleLengthMcsp = settings.BreakSampleLengthMcsp,
            BreakSampleIntervalMcsp = settings.BreakSampleIntervalMcsp,
            ResultSampleMcsp = settings.ResultSampleMcsp,
        };
    }

    private static ImmutableArray<JobExtension> ResolveExtensions(
        JobSettings? overrides,
        JobSettings? baseSettings)
    {
        if (overrides?.Extensions is { } overrideExtensions)
        {
            return overrideExtensions.IsDefault ? [] : overrideExtensions;
        }

        if (baseSettings?.Extensions is { } baseExtensions)
        {
            return baseExtensions.IsDefault ? [] : baseExtensions;
        }

        return [];
    }
}
