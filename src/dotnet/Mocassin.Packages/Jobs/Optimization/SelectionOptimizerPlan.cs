// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Model.Jobs;

namespace Mocassin.Packages.Jobs.Optimization;

/// <summary>
/// The compiled effect of one authored selection optimizer.
/// </summary>
/// <param name="Optimizer">The authored optimizer.</param>
/// <param name="Targets">The compiled P1 targets, sorted by position and particle index.</param>
/// <param name="RemovedStartOptions">
/// The number of selectable start options the optimizer removes from the job simulation, or
/// <see langword="null"/> when the compiled transition table cannot be evaluated.
/// </param>
public sealed record SelectionOptimizerEffect(
    SelectionOptimizer Optimizer,
    ImmutableArray<CompiledSelectionOptimizer> Targets,
    int? RemovedStartOptions);

/// <summary>
/// The compiled selection-optimizer plan of one job collection.
/// </summary>
/// <remarks>
/// The plan never mutates the compiled model. <see cref="Targets"/> is sorted by
/// <c>(position_id, particle_id)</c> and is the payload later encoded by Step 11.
/// <see cref="UseDualDofCorrection"/> starts from the compiled simulation intent and is cleared only when the
/// collection has at least one optimizer and every compiled environment is reduced. The removes-all
/// <c>JOB009</c> check requires the compiled jump table; when it is unavailable the plan records
/// <see cref="StartOptionCheckLimitation"/> instead of pretending that the check ran.
/// </remarks>
/// <param name="Targets">The sorted compiled targets across every authored optimizer.</param>
/// <param name="AuthoredOptimizerCount">The number of authored optimizer entries.</param>
/// <param name="UseDualDofCorrection">The resulting dual degree-of-freedom flag for the descriptors.</param>
/// <param name="Effects">The per-optimizer compiled effect.</param>
/// <param name="StartOptionCheckEvaluated">Whether the removes-all start-option check could be evaluated.</param>
/// <param name="StartOptionCheckLimitation">
/// A precise explanation of why the removes-all check was skipped, or <see langword="null"/>.
/// </param>
public sealed record SelectionOptimizerPlan(
    ImmutableArray<CompiledSelectionOptimizer> Targets,
    int AuthoredOptimizerCount,
    bool UseDualDofCorrection,
    ImmutableArray<SelectionOptimizerEffect> Effects,
    bool StartOptionCheckEvaluated,
    string? StartOptionCheckLimitation)
{
    /// <summary>
    /// Creates the plan of a collection without optimizers.
    /// </summary>
    /// <param name="useDualDofCorrection">The compiled simulation intent.</param>
    /// <returns>The plan with no targets and no effects.</returns>
    public static SelectionOptimizerPlan WithoutOptimizers(bool useDualDofCorrection) =>
        new([], 0, useDualDofCorrection, [], true, null);
}
