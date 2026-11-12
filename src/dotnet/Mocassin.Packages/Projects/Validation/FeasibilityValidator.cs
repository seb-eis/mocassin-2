// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Energy;
using Mocassin.Symmetry.Structure;
using Mocassin.Symmetry.Transitions;

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// The diagnostics and optional statistics produced by the feasibility stage.
/// </summary>
/// <param name="Diagnostics">The feasibility diagnostics.</param>
/// <param name="Statistics">The statistics, or <see langword="null"/> when not requested or unavailable.</param>
internal readonly record struct FeasibilityOutcome(
    ImmutableArray<Diagnostic> Diagnostics,
    ModelStatistics? Statistics);

/// <summary>
/// Runs buildability, budget, and statistics checks.
/// </summary>
internal sealed class FeasibilityValidator
{
    private readonly ModelStatisticsService statisticsService;

    /// <summary>
    /// Initializes the feasibility validator.
    /// </summary>
    /// <param name="structurePreview">The structure preview service.</param>
    /// <param name="energyPreview">The energy preview service.</param>
    /// <param name="transitionPreview">The transition preview service.</param>
    public FeasibilityValidator(
        IStructurePreviewService structurePreview,
        IEnergyPreviewService energyPreview,
        ITransitionPreviewService transitionPreview)
    {
        statisticsService = new ModelStatisticsService(structurePreview, energyPreview, transitionPreview);
    }

    /// <summary>
    /// Validates feasibility and computes requested statistics.
    /// </summary>
    /// <param name="project">The project snapshot.</param>
    /// <param name="options">The run options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stage outcome.</returns>
    public FeasibilityOutcome Validate(
        Project project,
        ValidationOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        var bag = new DiagnosticBag();
        AddBuildabilityDiagnostics(project, bag);

        cancellationToken.ThrowIfCancellationRequested();
        var statistics = statisticsService.TryCreate(project, options, out var previewDiagnostics, cancellationToken);
        bag.AddRange(previewDiagnostics);
        if (statistics is not null)
        {
            AddBudgetDiagnostics(statistics, options, bag);
        }

        return new FeasibilityOutcome(
            bag.Sorted,
            options.IncludeStatistics ? statistics : null);
    }

    private static void AddBuildabilityDiagnostics(Project project, DiagnosticBag bag)
    {
        var simulations = project.Model.Simulations.IsDefault
            ? ImmutableArray<Simulation>.Empty
            : project.Model.Simulations;
        if (simulations.Length == 0)
        {
            return;
        }

        var kineticById = (project.Model.KineticTransitions.IsDefault
                ? ImmutableArray<KineticTransition>.Empty
                : project.Model.KineticTransitions)
            .ToDictionary(static transition => transition.Id.Value);
        var chainById = (project.Model.Chains.IsDefault
                ? ImmutableArray<TransitionChain>.Empty
                : project.Model.Chains)
            .ToDictionary(static chain => chain.Id.Value);
        var groupById = (project.Model.StateChangeGroups.IsDefault
                ? ImmutableArray<StateChangeGroup>.Empty
                : project.Model.StateChangeGroups)
            .ToDictionary(static group => group.Id.Value);
        var stateChangeById = (project.Model.StateChanges.IsDefault
                ? ImmutableArray<StateChange>.Empty
                : project.Model.StateChanges)
            .ToDictionary(static stateChange => stateChange.Id.Value);

        var qualifying = new List<(Simulation Simulation, bool HasMobileStart)>();
        foreach (var simulation in simulations)
        {
            if (simulation.Kind != SimulationKind.Kmc)
            {
                continue;
            }

            var references = simulation.TransitionIds.IsDefault
                ? ImmutableArray<TransitionRef>.Empty
                : simulation.TransitionIds;
            if (references.Length == 0)
            {
                // SIM003 already reports a simulation without transitions.
                continue;
            }

            var resolved = true;
            var hasMobileStart = false;
            foreach (var reference in references)
            {
                if (!reference.IsKinetic)
                {
                    // SIM002 already reports a wrong transition kind.
                    resolved = false;
                    break;
                }

                var donors = TryGetMobileStartDonors(
                    reference.Id,
                    kineticById,
                    chainById,
                    groupById,
                    stateChangeById);
                if (donors is null)
                {
                    // SIM001/TRN errors already explain why the transition is not usable.
                    resolved = false;
                    break;
                }

                if (donors.Count > 0)
                {
                    hasMobileStart = true;
                }
            }

            if (resolved)
            {
                qualifying.Add((simulation, hasMobileStart));
            }
        }

        if (qualifying.Count == 0)
        {
            return;
        }

        if (qualifying.TrueForAll(static entry => !entry.HasMobileStart))
        {
            bag.Add(new Diagnostic(
                Step07DiagnosticCodes.NoMobileStartParticle,
                DiagnosticSeverity.Warning,
                "No KMC simulation has a particle that can start at the transition's first stable position.",
                new ModelPath("model", Guid.Empty, "simulations"),
                "Make at least one particle mobile in a KMC simulation so the model can start."));
            return;
        }

        foreach (var (simulation, hasMobileStart) in qualifying)
        {
            if (hasMobileStart)
            {
                continue;
            }

            bag.Add(new Diagnostic(
                Step07DiagnosticCodes.KmcSimulationWithoutMobileStart,
                DiagnosticSeverity.Warning,
                $"KMC simulation '{simulation.Name}' has transitions but no mobile start particle.",
                new ModelPath("simulation", simulation.Id.Value, "transitionIds"),
                "Add a mobile particle to the simulation's start pool or remove the simulation."));
        }
    }

    private static HashSet<Guid>? TryGetMobileStartDonors(
        Guid transitionId,
        Dictionary<Guid, KineticTransition> kineticById,
        Dictionary<Guid, TransitionChain> chainById,
        Dictionary<Guid, StateChangeGroup> groupById,
        Dictionary<Guid, StateChange> stateChangeById)
    {
        if (!kineticById.TryGetValue(transitionId, out var transition) ||
            !chainById.TryGetValue(transition.ChainId.Value, out var chain) ||
            chain.GroupIds.IsDefault ||
            chain.GroupIds.Length == 0 ||
            !groupById.TryGetValue(chain.GroupIds[0].Value, out var group) ||
            group.StateChangeIds.IsDefault)
        {
            return null;
        }

        var donors = new HashSet<Guid>();
        foreach (var stateChangeId in group.StateChangeIds)
        {
            if (!stateChangeById.TryGetValue(stateChangeId.Value, out var stateChange))
            {
                return null;
            }

            if (stateChange.DonorParticleId != ParticleId.Void)
            {
                donors.Add(stateChange.DonorParticleId.Value);
            }
        }

        return donors;
    }

    private static void AddBudgetDiagnostics(
        ModelStatistics statistics,
        ValidationOptions options,
        DiagnosticBag bag)
    {
        if (statistics.EstimatedRuleCount > options.MaxEstimatedRuleCount)
        {
            bag.Add(new Diagnostic(
                Step07DiagnosticCodes.EstimatedRuleCountExceeded,
                DiagnosticSeverity.Error,
                $"The estimated compiled rule count {statistics.EstimatedRuleCount} exceeds the advisory budget of {options.MaxEstimatedRuleCount}.",
                new ModelPath("project", Guid.Empty, "estimatedRuleCount"),
                "Reduce the transition or particle-set count, or raise MaxEstimatedRuleCount knowingly."));
        }

        if (statistics.EstimatedMemoryBytes > options.MaxEstimatedMemoryBytes)
        {
            bag.Add(new Diagnostic(
                Step07DiagnosticCodes.EstimatedMemoryExceeded,
                DiagnosticSeverity.Error,
                $"The estimated energy-table memory {statistics.EstimatedMemoryBytes} bytes exceeds the advisory budget of {options.MaxEstimatedMemoryBytes} bytes.",
                new ModelPath("project", Guid.Empty, "estimatedMemoryBytes"),
                "Reduce clusters or table sizes, or raise MaxEstimatedMemoryBytes knowingly."));
        }
    }
}
