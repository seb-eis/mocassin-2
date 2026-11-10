// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Transitions;

namespace Mocassin.Core.Model.Simulations;

/// <summary>
/// Validates the definition-level simulation rules that need only the model graph.
/// </summary>
/// <remarks>
/// Job expansion, effective job counts, package writing, and solver abort behaviour belong to later steps. The
/// validator never throws for user data.
/// </remarks>
internal static class SimulationModelValidator
{
    private const int maxTransitions = 100;
    private const int recommendedTransitionMaximum = 10;

    /// <summary>
    /// Validates simulation kinds, references, defaults, hard limits, and recommended bands.
    /// </summary>
    /// <param name="model">The model to validate.</param>
    /// <returns>The diagnostics in stable order.</returns>
    public static ImmutableArray<Diagnostic> Validate(ModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var bag = new DiagnosticBag();
        var simulations = model.Simulations.IsDefault
            ? ImmutableArray<Simulation>.Empty
            : model.Simulations;
        var kineticTransitionIds = (model.KineticTransitions.IsDefault
                ? ImmutableArray<KineticTransition>.Empty
                : model.KineticTransitions)
            .Select(static transition => transition.Id.Value)
            .ToHashSet();
        var metropolisTransitionIds = (model.MetropolisTransitions.IsDefault
                ? ImmutableArray<MetropolisTransition>.Empty
                : model.MetropolisTransitions)
            .Select(static transition => transition.Id.Value)
            .ToHashSet();

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var simulation in simulations)
        {
            ValidateSimulation(
                simulation,
                kineticTransitionIds,
                metropolisTransitionIds,
                names,
                bag);
        }

        return bag.Sorted;
    }

    private static void ValidateSimulation(
        Simulation simulation,
        HashSet<Guid> kineticTransitionIds,
        HashSet<Guid> metropolisTransitionIds,
        HashSet<string> names,
        DiagnosticBag bag)
    {
        ValidateName(simulation.Name, simulation.Id.Value, names, bag);

        if (simulation.HasPersistedKindMismatch)
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.KindShapeMismatch,
                "The persisted simulation kind-specific shape disagrees with the concrete record.",
                simulation.Id.Value,
                "kind"));
        }

        var transitionIds = simulation.TransitionIds.IsDefault
            ? ImmutableArray<TransitionRef>.Empty
            : simulation.TransitionIds;
        ValidateTransitions(
            simulation.Kind,
            transitionIds,
            kineticTransitionIds,
            metropolisTransitionIds,
            simulation.Id.Value,
            bag);
        ValidateCommonSettings(simulation, bag);
        if (simulation is KineticSimulation kinetic)
        {
            ValidateKineticSettings(kinetic, bag);
        }
        else if (simulation is MetropolisSimulation metropolis)
        {
            ValidateMetropolisSettings(metropolis, bag);
        }
    }

    private static void ValidateName(
        string name,
        Guid simulationId,
        HashSet<string> names,
        DiagnosticBag bag)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidName,
                "Simulation name must not be empty or whitespace-only.",
                simulationId,
                "name"));
            return;
        }

        if (!names.Add(name))
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidName,
                $"Simulation name '{name}' is used more than once.",
                simulationId,
                "name"));
        }
    }

    private static void ValidateTransitions(
        SimulationKind kind,
        ImmutableArray<TransitionRef> transitionIds,
        HashSet<Guid> kineticTransitionIds,
        HashSet<Guid> metropolisTransitionIds,
        Guid simulationId,
        DiagnosticBag bag)
    {
        if (transitionIds.Length == 0)
        {
            bag.Add(Warning(
                SimulationDiagnosticCodes.NoTransitions,
                "Simulation has no transitions; it cannot be built until Step 07/10 feasibility passes.",
                simulationId,
                "transitionIds"));
            return;
        }

        if (transitionIds.Length > maxTransitions)
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.TransitionLimitExceeded,
                $"Simulation references {transitionIds.Length} transitions; the limit is {maxTransitions}.",
                simulationId,
                "transitionIds"));
        }
        else if (transitionIds.Length > recommendedTransitionMaximum)
        {
            bag.Add(Warning(
                SimulationDiagnosticCodes.TransitionCountOutsideRecommendedBand,
                $"Simulation references {transitionIds.Length} transitions; the recommended maximum is " +
                $"{recommendedTransitionMaximum}.",
                simulationId,
                "transitionIds"));
        }

        var expectedKind = kind == SimulationKind.Kmc ? TransitionKind.Kinetic : TransitionKind.Metropolis;
        var expectedIds = kind == SimulationKind.Kmc ? kineticTransitionIds : metropolisTransitionIds;
        var seen = new HashSet<(TransitionKind Kind, Guid Id)>();
        foreach (var transition in transitionIds)
        {
            if (transition.Kind != expectedKind)
            {
                bag.Add(Error(
                    SimulationDiagnosticCodes.WrongTransitionKind,
                    $"Simulation of kind '{kind}' references a transition of kind '{transition.Kind}'.",
                    simulationId,
                    "transitionIds"));
                continue;
            }

            if (!seen.Add((transition.Kind, transition.Id)))
            {
                bag.Add(Error(
                    SimulationDiagnosticCodes.DuplicateTransitionReference,
                    $"Simulation references transition '{transition.Id:D}' more than once.",
                    simulationId,
                    "transitionIds"));
                continue;
            }

            if (!expectedIds.Contains(transition.Id))
            {
                bag.Add(Error(
                    SimulationDiagnosticCodes.UnknownTransition,
                    $"Simulation references unknown transition '{transition.Id:D}'.",
                    simulationId,
                    "transitionIds"));
            }
        }
    }

    private static void ValidateCommonSettings(Simulation simulation, DiagnosticBag bag)
    {
        if (!double.IsFinite(simulation.TemperatureK) ||
            simulation.TemperatureK < 0.1 ||
            simulation.TemperatureK > 10_000.0)
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidTemperature,
                "Simulation temperature must be finite and within 0.1..10000 K.",
                simulation.Id.Value,
                "temperatureK"));
        }
        else if (simulation.TemperatureK is < 100.0 or > 5000.0)
        {
            bag.Add(Warning(
                SimulationDiagnosticCodes.TemperatureOutsideRecommendedBand,
                "Simulation temperature is outside the recommended 100..5000 K band.",
                simulation.Id.Value,
                "temperatureK"));
        }

        if (simulation.TargetMcsp is < 1 or > 1_000_000)
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidTargetMcsp,
                "Target MCSP must be within 1..1,000,000.",
                simulation.Id.Value,
                "targetMcsp"));
        }
        else if (simulation.TargetMcsp > 1000)
        {
            bag.Add(Warning(
                SimulationDiagnosticCodes.TargetMcspAboveRecommended,
                "Target MCSP is above the recommended value of 1000.",
                simulation.Id.Value,
                "targetMcsp"));
        }

        if (simulation.JobCount is < 1 or > 1000)
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidJobCount,
                "Job count must be within 1..1000.",
                simulation.Id.Value,
                "jobCount"));
        }
        else if (simulation.JobCount is < 5 or > 100)
        {
            bag.Add(Warning(
                SimulationDiagnosticCodes.JobCountOutsideRecommendedBand,
                "Job count is outside the recommended 5..100 band.",
                simulation.Id.Value,
                "jobCount"));
        }

        if (simulation.WriteOutCount is < 0 or > 1000)
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidWriteOutCount,
                "Write-out count must be within 0..1000.",
                simulation.Id.Value,
                "writeOutCount"));
        }
        else if (simulation.WriteOutCount != 0 &&
                 simulation.WriteOutCount is < 5 or > 100)
        {
            bag.Add(Warning(
                SimulationDiagnosticCodes.WriteOutOutsideRecommendedBand,
                "Write-out count is outside the recommended 5..100 band; 0 remains allowed.",
                simulation.Id.Value,
                "writeOutCount"));
        }

        if (simulation.TimeLimitSeconds is { } timeLimit && timeLimit <= 0)
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidTimeLimit,
                "Time limit must be positive when present.",
                simulation.Id.Value,
                "timeLimitSeconds"));
        }

        if (simulation.MinSuccessRate is { } minSuccessRate)
        {
            if (!double.IsFinite(minSuccessRate) ||
                minSuccessRate < 0.0 ||
                minSuccessRate > int.MaxValue)
            {
                bag.Add(Error(
                    SimulationDiagnosticCodes.InvalidMinSuccessRate,
                    "Minimum success rate must be finite and within 0..int.MaxValue.",
                    simulation.Id.Value,
                    "minSuccessRate"));
            }
            else if (minSuccessRate is > 0.0 and < 10.0)
            {
                bag.Add(Warning(
                    SimulationDiagnosticCodes.MinSuccessRateBelowRecommended,
                    "Minimum success rate is positive but below the recommended 10 MCS/s.",
                    simulation.Id.Value,
                    "minSuccessRate"));
            }
        }

        if (simulation.RngSeed is { Length: > 100 })
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.RngSeedTooLong,
                "RNG seed must not be longer than 100 characters.",
                simulation.Id.Value,
                "rngSeed"));
        }

        if ((simulation.Flags & ~ExecutionFlagsJsonConverter.AuthoredMask) != 0)
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidExecutionFlags,
                "Execution flags contain a duplicate, derived, or compiler-owned flag.",
                simulation.Id.Value,
                "executionFlags"));
        }
    }

    private static void ValidateKineticSettings(KineticSimulation simulation, DiagnosticBag bag)
    {
        if (!simulation.ElectricFieldDirection.IsFinite ||
            (simulation.ElectricFieldDirection.LengthSquared == 0.0 &&
             simulation.ElectricFieldModulusVm != 0.0))
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidFieldDirection,
                "Electric-field direction must have three finite components and must be non-zero for a " +
                "non-zero field.",
                simulation.Id.Value,
                "electricFieldDirection"));
        }

        if (simulation.PreRunMcsp is < 0 or > 1_000_000)
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidPreRunMcsp,
                "Pre-run MCSP must be within 0..1,000,000.",
                simulation.Id.Value,
                "preRunMcsp"));
        }
        else if (simulation.PreRunMcsp > 1000)
        {
            bag.Add(Warning(
                SimulationDiagnosticCodes.PreRunAboveRecommended,
                "Pre-run MCSP is above the recommended value of 1000.",
                simulation.Id.Value,
                "preRunMcsp"));
        }

        if (!double.IsFinite(simulation.ElectricFieldModulusVm) ||
            simulation.ElectricFieldModulusVm < 0.0)
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidFieldModulus,
                "Electric-field modulus must be finite and non-negative.",
                simulation.Id.Value,
                "electricFieldModulusVm"));
        }
        else if (simulation.ElectricFieldModulusVm is < 1.0e4 or > 1.0e9)
        {
            bag.Add(Warning(
                SimulationDiagnosticCodes.FieldModulusOutsideRecommendedBand,
                "Electric-field modulus is outside the recommended 1e4..1e9 V/m band.",
                simulation.Id.Value,
                "electricFieldModulusVm"));
        }

        if (simulation.NormalizationEnergyEv is { } normalizationEnergy)
        {
            if (!double.IsFinite(normalizationEnergy) || normalizationEnergy < 0.0)
            {
                bag.Add(Error(
                    SimulationDiagnosticCodes.InvalidNormalizationEnergy,
                    "Normalization energy must be finite and non-negative when present.",
                    simulation.Id.Value,
                    "normalizationEnergyEv"));
            }
            else if (normalizationEnergy > 10.0)
            {
                bag.Add(Warning(
                    SimulationDiagnosticCodes.NormalizationEnergyAboveRecommended,
                    "Normalization energy is above the recommended 10 eV.",
                    simulation.Id.Value,
                    "normalizationEnergyEv"));
            }
        }

        if (simulation.MaxAttemptFrequencyHz is { } maxAttemptFrequency)
        {
            if (!double.IsFinite(maxAttemptFrequency) || maxAttemptFrequency <= 0.0)
            {
                bag.Add(Error(
                    SimulationDiagnosticCodes.InvalidMaxAttemptFrequency,
                    "Maximum attempt frequency must be finite and positive when present.",
                    simulation.Id.Value,
                    "maxAttemptFrequencyHz"));
            }
        }
    }

    private static void ValidateMetropolisSettings(MetropolisSimulation simulation, DiagnosticBag bag)
    {
        if (!double.IsFinite(simulation.BreakTolerance) ||
            simulation.BreakTolerance is < 0.0 or > 1.0)
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidBreakTolerance,
                "Break tolerance must be finite and within 0..1.",
                simulation.Id.Value,
                "breakTolerance"));
        }
        else if (simulation.BreakTolerance > 0.1)
        {
            bag.Add(Warning(
                SimulationDiagnosticCodes.BreakToleranceAboveRecommended,
                "Break tolerance is above the recommended 0.1.",
                simulation.Id.Value,
                "breakTolerance"));
        }

        if (simulation.BreakSampleLengthMcsp is < 0 or > 100_000)
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidBreakSampleLength,
                "Break sample length must be within 0..100,000 MCSP.",
                simulation.Id.Value,
                "breakSampleLengthMcsp"));
        }
        else if (simulation.BreakSampleLengthMcsp > 10_000)
        {
            bag.Add(Warning(
                SimulationDiagnosticCodes.BreakSampleLengthAboveRecommended,
                "Break sample length is above the recommended 10,000 MCSP.",
                simulation.Id.Value,
                "breakSampleLengthMcsp"));
        }

        if (simulation.BreakSampleIntervalMcsp is < 1 or > 1000)
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidBreakSampleInterval,
                "Break sample interval must be within 1..1000 MCSP.",
                simulation.Id.Value,
                "breakSampleIntervalMcsp"));
        }
        else if (simulation.BreakSampleIntervalMcsp > 100)
        {
            bag.Add(Warning(
                SimulationDiagnosticCodes.BreakSampleIntervalAboveRecommended,
                "Break sample interval is above the recommended 100 MCSP.",
                simulation.Id.Value,
                "breakSampleIntervalMcsp"));
        }

        if (simulation.ResultSampleMcsp is < 0 or > 1_000_000)
        {
            bag.Add(Error(
                SimulationDiagnosticCodes.InvalidResultSample,
                "Result sample must be within 0..1,000,000 MCSP.",
                simulation.Id.Value,
                "resultSampleMcsp"));
        }
        else if (simulation.ResultSampleMcsp > 10_000)
        {
            bag.Add(Warning(
                SimulationDiagnosticCodes.ResultSampleAboveRecommended,
                "Result sample is above the recommended 10,000 MCSP.",
                simulation.Id.Value,
                "resultSampleMcsp"));
        }
    }

    private static Diagnostic Error(string code, string message, Guid simulationId, string property) =>
        new(
            code,
            DiagnosticSeverity.Error,
            message,
            new ModelPath("simulation", simulationId, property),
            SimulationDiagnosticCodes.HelpFor(code));

    private static Diagnostic Warning(string code, string message, Guid simulationId, string property) =>
        new(
            code,
            DiagnosticSeverity.Warning,
            message,
            new ModelPath("simulation", simulationId, property),
            SimulationDiagnosticCodes.HelpFor(code));
}
