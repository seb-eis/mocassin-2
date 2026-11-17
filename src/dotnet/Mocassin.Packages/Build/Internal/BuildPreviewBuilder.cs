// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Project;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Jobs.Optimization;

namespace Mocassin.Packages.Build.Internal;

/// <summary>
/// Builds the typed <see cref="BuildPreview"/> from the selected job descriptors.
/// </summary>
/// <remarks>
/// This is the only preview-mapping implementation. The pipeline calls it after expansion and job filtering with
/// the same <see cref="ILatticePreviewService"/> instance that the build-stage validation uses, so a preview and a
/// build cannot diverge. It never reloads the project, recompiles the model, or opens a package writer.
/// </remarks>
internal static class BuildPreviewBuilder
{
    /// <summary>
    /// Builds the preview of the selected jobs.
    /// </summary>
    /// <param name="project">The loaded project.</param>
    /// <param name="model">The compiled model.</param>
    /// <param name="template">The effective job template.</param>
    /// <param name="jobs">The selected descriptors in full-expansion order.</param>
    /// <param name="previewService">The shared lattice-preview service.</param>
    /// <param name="options">The injected clock.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The preview or its diagnostics.</returns>
    public static BuildPreviewResult Build(
        Project project,
        CompiledModel model,
        JobTemplate template,
        ImmutableArray<JobDescriptor> jobs,
        ILatticePreviewService previewService,
        BuildOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(previewService);
        ArgumentNullException.ThrowIfNull(options);

        var started = options.TimeProvider.GetTimestamp();
        var bag = new DiagnosticBag();
        var authoredCollections = template.Collections is { IsDefault: false } value
            ? value
            : ImmutableArray<JobCollection>.Empty;
        var collections = new List<BuildPreviewCollection>();
        foreach (var collectionGroup in jobs.GroupBy(static job => job.CollectionIndex).OrderBy(static group => group.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var firstJob = collectionGroup.First();
            if (firstJob.SimulationIndex < 0 || firstJob.SimulationIndex >= model.Simulations.Length)
            {
                throw new InvalidOperationException(
                    $"Job {firstJob.JobId} references compiled simulation index {firstJob.SimulationIndex}, " +
                    $"but the compiled model has {model.Simulations.Length} simulations.");
            }

            if (firstJob.CollectionIndex < 0 || firstJob.CollectionIndex >= authoredCollections.Length)
            {
                throw new InvalidOperationException(
                    $"Job {firstJob.JobId} references authored collection index {firstJob.CollectionIndex}, " +
                    $"but the template has {authoredCollections.Length} collections.");
            }

            var collection = authoredCollections[firstJob.CollectionIndex];
            var authoredConfigs = collection.Configs is { IsDefault: false } configValues
                ? configValues
                : ImmutableArray<JobConfig>.Empty;
            var plan = SelectionOptimizerResolver.Resolve(
                model,
                model.Simulations[firstJob.SimulationIndex],
                collection.Id,
                firstJob.Optimizers,
                out _);
            var optimizers = plan.Effects
                .Select(static effect => new BuildPreviewOptimizer(
                    effect.Optimizer.SiteId.Value.ToString("D", CultureInfo.InvariantCulture),
                    effect.Optimizer.ParticleId.Value.ToString("D", CultureInfo.InvariantCulture),
                    effect.Targets.Length,
                    effect.RemovedStartOptions))
                .ToImmutableArray();

            var configs = new List<BuildPreviewConfig>();
            foreach (var configGroup in collectionGroup.GroupBy(static job => job.ConfigIndex).OrderBy(static group => group.Key))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var job = configGroup.First();
                var latticePreview = previewService.TryCreatePreview(
                    project,
                    model,
                    job.Lattice,
                    job.Dopings,
                    out var latticeDiagnostics);
                bag.AddRange(latticeDiagnostics);
                if (latticePreview is null)
                {
                    return new BuildPreviewResult(null, Merge(bag));
                }

                var requestedFractions = job.Dopings
                    .Where(static value => value is not null)
                    .ToDictionary(static value => value.DopingId, static value => value.Fraction);
                if (job.ConfigIndex < 0 || job.ConfigIndex >= authoredConfigs.Length)
                {
                    throw new InvalidOperationException(
                        $"Job {job.JobId} references authored config index {job.ConfigIndex}, " +
                        $"but collection '{collection.Name}' has {authoredConfigs.Length} configurations.");
                }

                var authoredConfig = authoredConfigs[job.ConfigIndex];
                var dopings = latticePreview.Report.Applications
                    .Select(application => new BuildPreviewDoping(
                        application.DopingId.Value.ToString("D", CultureInfo.InvariantCulture),
                        application.Priority,
                        requestedFractions.TryGetValue(application.DopingId, out var requested) ? requested : 0.0,
                        application.AvailablePrimary,
                        application.RequestedPrimary,
                        application.AppliedPrimary,
                        application.AppliedDependent,
                        application.EffectiveFraction))
                    .ToImmutableArray();
                configs.Add(new BuildPreviewConfig(
                    authoredConfig.Id.Value.ToString("D", CultureInfo.InvariantCulture),
                    job.ConfigName,
                    configGroup.Count(),
                    job.Lattice.A,
                    job.Lattice.B,
                    job.Lattice.C,
                    latticePreview.Report.TotalPositions,
                    latticePreview.Report.TotalChargeBefore,
                    latticePreview.Report.TotalChargeAfter,
                    latticePreview.Report.IsChargeNeutral,
                    dopings));
            }

            collections.Add(new BuildPreviewCollection(
                collection.Id.Value.ToString("D", CultureInfo.InvariantCulture),
                collection.Name,
                firstJob.SimulationId.Value.ToString("D", CultureInfo.InvariantCulture),
                firstJob.Kind.ToString().ToLowerInvariant(),
                firstJob.UseDualDofCorrection,
                collectionGroup.Count(),
                configs.Count,
                optimizers,
                [.. configs]));
        }

        var preview = new BuildPreview(
            template.Id.Value.ToString("D", CultureInfo.InvariantCulture),
            template.Name,
            jobs.Length,
            [.. collections],
            options.TimeProvider.GetElapsedTime(started));
        return new BuildPreviewResult(preview, Merge(bag));
    }

    private static ImmutableArray<Diagnostic> Merge(DiagnosticBag bag) => BuildDiagnosticMerger.Merge(bag.Sorted);
}

/// <summary>
/// The outcome of a preview evaluation.
/// </summary>
/// <param name="Preview">The typed preview, or <see langword="null"/> on invalid lattice or doping data.</param>
/// <param name="Diagnostics">The preview diagnostics.</param>
internal sealed record BuildPreviewResult(
    BuildPreview? Preview,
    ImmutableArray<Diagnostic> Diagnostics);
