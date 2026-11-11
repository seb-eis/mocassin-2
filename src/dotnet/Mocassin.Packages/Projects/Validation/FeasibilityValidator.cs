// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;

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
/// The rule set and the statistics service are added by the cross-domain-validation and model-statistics
/// features. The pipeline keeps the stage so its order and stage reporting are stable from the first feature.
/// </remarks>
internal sealed class FeasibilityValidator
{
    /// <summary>
    /// Validates feasibility and computes requested statistics.
    /// </summary>
    /// <param name="project">The project snapshot.</param>
    /// <param name="options">The run options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stage outcome.</returns>
    public static FeasibilityOutcome Validate(
        Project project,
        ValidationOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();
        return new FeasibilityOutcome(ImmutableArray<Diagnostic>.Empty, Statistics: null);
    }
}
