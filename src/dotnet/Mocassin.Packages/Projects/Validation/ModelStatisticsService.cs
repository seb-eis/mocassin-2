// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Energy;
using Mocassin.Symmetry.Structure;
using Mocassin.Symmetry.Transitions;

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// Computes advisory model statistics from the structure, energy, and transition previews.
/// </summary>
/// <remarks>
/// The service is stateless and uses the injected preview caches. Every preview call is atomic because the
/// Step 03–06 preview interfaces take no cancellation token; cancellation is checked between calls and
/// between transitions.
/// </remarks>
public sealed class ModelStatisticsService
{
    private readonly IStructurePreviewService structurePreview;
    private readonly IEnergyPreviewService energyPreview;
    private readonly ITransitionPreviewService transitionPreview;

    /// <summary>
    /// Initializes a statistics service.
    /// </summary>
    /// <param name="structurePreview">The structure preview service.</param>
    /// <param name="energyPreview">The energy preview service.</param>
    /// <param name="transitionPreview">The transition preview service.</param>
    public ModelStatisticsService(
        IStructurePreviewService structurePreview,
        IEnergyPreviewService energyPreview,
        ITransitionPreviewService transitionPreview)
    {
        this.structurePreview = structurePreview ?? throw new ArgumentNullException(nameof(structurePreview));
        this.energyPreview = energyPreview ?? throw new ArgumentNullException(nameof(energyPreview));
        this.transitionPreview = transitionPreview ?? throw new ArgumentNullException(nameof(transitionPreview));
    }

    /// <summary>
    /// Tries to compute the requested statistics.
    /// </summary>
    /// <param name="project">The project snapshot.</param>
    /// <param name="options">The run options, including the requested cell counts.</param>
    /// <param name="diagnostics">The preview diagnostics when computation fails.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The statistics, or <see langword="null"/> when a required preview failed.</returns>
    public ModelStatistics? TryCreate(
        Project project,
        ValidationOptions options,
        out ImmutableArray<Diagnostic> diagnostics,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(options);

        var bag = new DiagnosticBag();

        cancellationToken.ThrowIfCancellationRequested();
        var structure = structurePreview.TryCreatePreview(
            project.Model.Structure,
            options.CellCountA,
            options.CellCountB,
            options.CellCountC,
            out var structureDiagnostics);
        bag.AddRange(structureDiagnostics);
        if (structure is null)
        {
            diagnostics = bag.Sorted;
            return null;
        }

        var p1PositionCount = (long)structure.PositionList.Length;
        var stablePositionCount = 0L;
        var unstablePositionCount = 0L;
        var sites = project.Model.Structure.Sites;
        foreach (var orbit in structure.Orbits)
        {
            if (orbit.ReferenceSiteIndex < 0 || orbit.ReferenceSiteIndex >= sites.Length)
            {
                continue;
            }

            if (sites[orbit.ReferenceSiteIndex].Stability == SiteStability.Stable)
            {
                stablePositionCount += orbit.PositionIndices.Length;
            }
            else
            {
                unstablePositionCount += orbit.PositionIndices.Length;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var energy = energyPreview.TryCreatePreview(
            project.Model,
            options.CellCountA,
            options.CellCountB,
            options.CellCountC,
            out var energyDiagnostics);
        bag.AddRange(energyDiagnostics);
        if (energy is null)
        {
            diagnostics = bag.Sorted;
            return null;
        }

        var estimatedRuleCount = 0L;
        foreach (var transition in project.Model.KineticTransitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var preview = transitionPreview.TryCreatePreview(
                transition,
                project,
                options.CellCountA,
                options.CellCountB,
                options.CellCountC,
                out var transitionDiagnostics);
            bag.AddRange(transitionDiagnostics);
            if (preview is null)
            {
                diagnostics = bag.Sorted;
                return null;
            }

            estimatedRuleCount += preview.ExpectedRuleCount;
        }

        foreach (var transition in project.Model.MetropolisTransitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var preview = transitionPreview.TryCreatePreview(
                transition,
                project,
                options.CellCountA,
                options.CellCountB,
                options.CellCountC,
                out var transitionDiagnostics);
            bag.AddRange(transitionDiagnostics);
            if (preview is null)
            {
                diagnostics = bag.Sorted;
                return null;
            }

            estimatedRuleCount += preview.ExpectedRuleCount;
        }

        var clusterPermutations = energy.ClusterCosts.Sum(static cluster => cluster.UniquePermutationCount);
        var estimatedPositions = p1PositionCount
            * options.CellCountA
            * options.CellCountB
            * options.CellCountC;

        diagnostics = bag.Sorted;
        return new ModelStatistics(
            project.Model.Particles.Length,
            project.Model.ParticleSets.Length,
            sites.Length,
            p1PositionCount,
            stablePositionCount,
            unstablePositionCount,
            project.Model.KineticTransitions.Length,
            project.Model.MetropolisTransitions.Length,
            project.Model.Clusters.Length,
            energy.UniquePairInteractionCount,
            clusterPermutations,
            estimatedRuleCount,
            energy.EstimatedMemoryBytes,
            options.CellCountA,
            options.CellCountB,
            options.CellCountC,
            estimatedPositions);
    }
}
