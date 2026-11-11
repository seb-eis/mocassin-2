// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
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
/// <remarks>
/// The rule set is added by the cross-domain-validation feature. Statistics are computed here because the
/// opt-in estimates and the rule/memory budgets are feasibility work.
/// </remarks>
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

        if (!options.IncludeStatistics)
        {
            return new FeasibilityOutcome(ImmutableArray<Diagnostic>.Empty, Statistics: null);
        }

        var statistics = statisticsService.TryCreate(project, options, out var diagnostics, cancellationToken);
        return new FeasibilityOutcome(diagnostics, statistics);
    }
}
