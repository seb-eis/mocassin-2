// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Simulations;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Project;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Jobs.Optimization;

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// Runs the compiled-bound job-template checks in the <see cref="ValidationStage.Build"/> stage.
/// </summary>
/// <remarks>
/// The Core <c>JobTemplateValidator</c> owns the structural rules that do not need a compiled model.
/// This stage delegates to that validator first and then adds the checks that need compiled positions,
/// simulations, transitions, and particle charges: the total-position and job-count limits, the optimizer
/// removes-all rule, and the doping/truncation/compensation/neutrality outcomes. Doping outcomes reuse the
/// shared <see cref="LatticePreviewService"/> generator so the validation and the build cannot diverge.
/// </remarks>
public sealed class JobTemplateValidationStage : IValidationStage
{
    private const int jobWarningThreshold = 10_000;
    private const int jobHardLimit = 99_999;

    private readonly ILatticePreviewService previewService;

    /// <summary>
    /// Initializes the stage with the production preview service.
    /// </summary>
    public JobTemplateValidationStage()
        : this(new LatticePreviewService())
    {
    }

    /// <summary>
    /// Initializes the stage with an explicit lattice-preview service.
    /// </summary>
    /// <param name="previewService">The service that evaluates the doping outcomes on the compiled model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="previewService"/> is <see langword="null"/>.</exception>
    public JobTemplateValidationStage(ILatticePreviewService previewService)
    {
        ArgumentNullException.ThrowIfNull(previewService);
        this.previewService = previewService;
    }

    /// <inheritdoc />
    public ValidationStage Stage => ValidationStage.Build;

    /// <inheritdoc />
    public ImmutableArray<Diagnostic> Validate(ValidationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.CompiledModel is null)
        {
            return [];
        }

        var project = context.Project;
        var model = context.CompiledModel;
        var bag = new DiagnosticBag();
        bag.AddRange(ProjectValidator.Validate(project));

        var templates = project.JobTemplates.IsDefault
            ? ImmutableArray<JobTemplate>.Empty
            : project.JobTemplates;
        var simulations = IndexSimulations(model);
        foreach (var template in templates)
        {
            if (template is null)
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            ValidateTemplate(project, model, template, simulations, bag, cancellationToken);
        }

        return bag.Sorted;
    }

    private void ValidateTemplate(
        Project project,
        CompiledModel model,
        JobTemplate template,
        Dictionary<SimulationId, CompiledSimulation> simulations,
        DiagnosticBag bag,
        CancellationToken cancellationToken)
    {
        var collections = template.Collections is { IsDefault: false } value
            ? value
            : ImmutableArray<JobCollection>.Empty;
        if (collections.Length == 0)
        {
            return;
        }

        var totalJobs = 0L;
        foreach (var collection in collections)
        {
            if (collection is null)
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            simulations.TryGetValue(collection.SimulationId, out var simulation);
            var configs = collection.Configs is { IsDefault: false } configValues
                ? configValues
                : ImmutableArray<JobConfig>.Empty;
            var jobCount = collection.JobCountPerConfig ?? simulation?.Settings.JobCount ?? 0;
            if (jobCount > 0 && configs.Length > 0)
            {
                totalJobs += (long)jobCount * configs.Length;
            }
        }

        var templatePath = new ModelPath("jobTemplate", template.Id.Value, "collections");
        if (totalJobs > jobHardLimit)
        {
            bag.Add(Error(
                JobDiagnosticCodes.JobLimitExceeded,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The template expands to {totalJobs} jobs; the hard limit is {jobHardLimit}."),
                templatePath));
            return;
        }

        if (totalJobs > jobWarningThreshold)
        {
            bag.Add(Warning(
                JobDiagnosticCodes.JobCountWarning,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The template expands to {totalJobs} jobs; the warning threshold is {jobWarningThreshold}."),
                templatePath));
        }

        foreach (var collection in collections)
        {
            if (collection is null)
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (simulations.TryGetValue(collection.SimulationId, out var simulation))
            {
                ValidateOptimizers(model, simulation, collection, bag);
            }

            var configs = collection.Configs is { IsDefault: false } configValues
                ? configValues
                : ImmutableArray<JobConfig>.Empty;
            foreach (var config in configs)
            {
                if (config is null)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                ValidateLattice(project, model, config, bag);
            }
        }
    }

    private static void ValidateOptimizers(
        CompiledModel model,
        CompiledSimulation simulation,
        JobCollection collection,
        DiagnosticBag bag)
    {
        var optimizers = collection.Optimizers is { IsDefault: false } value
            ? value
            : ImmutableArray<SelectionOptimizer>.Empty;
        if (optimizers.Length == 0)
        {
            return;
        }

        _ = SelectionOptimizerResolver.Resolve(
            model,
            simulation,
            collection.Id,
            optimizers,
            out var diagnostics);
        bag.AddRange(diagnostics);
    }

    private void ValidateLattice(
        Project project,
        CompiledModel model,
        JobConfig config,
        DiagnosticBag bag)
    {
        var request = config.Lattice;
        if (request is null)
        {
            return;
        }

        var positionCount = model.Structure.PositionCount;
        if (positionCount <= 0)
        {
            throw new InvalidOperationException("The compiled model has no canonical P1 positions.");
        }

        if (request.A is < SupercellGenerator.MinimumCellCount or > SupercellGenerator.MaximumCellCount ||
            request.B is < SupercellGenerator.MinimumCellCount or > SupercellGenerator.MaximumCellCount ||
            request.C is < SupercellGenerator.MinimumCellCount or > SupercellGenerator.MaximumCellCount)
        {
            return;
        }

        var totalPositions = (long)request.A * request.B * request.C * positionCount;
        var path = new ModelPath("jobConfig", config.Id.Value, "lattice");
        if (totalPositions > SupercellGenerator.MaximumPositions)
        {
            bag.Add(Error(
                JobDiagnosticCodes.InvalidCellSize,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The supercell request ({request.A}, {request.B}, {request.C}) with {positionCount} positions " +
                    $"per cell expands to {totalPositions} positions; the hard limit is " +
                    $"{SupercellGenerator.MaximumPositions}."),
                path));
            return;
        }

        if (totalPositions > SupercellGenerator.PositionWarningThreshold)
        {
            bag.Add(Warning(
                JobDiagnosticCodes.LargeSupercell,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The supercell has {totalPositions} positions; the warning threshold is " +
                    $"{SupercellGenerator.PositionWarningThreshold}."),
                path));
        }

        var dopings = config.Dopings is { IsDefault: false } value
            ? value
            : ImmutableArray<DopingValue>.Empty;
        if (dopings.Length == 0)
        {
            return;
        }

        _ = previewService.TryCreatePreview(project, model, request, dopings, out var diagnostics);
        foreach (var diagnostic in diagnostics)
        {
            // The stage owns the total-position diagnostics; the preview repeats them on its own path.
            if (diagnostic.Code is JobDiagnosticCodes.InvalidCellSize or JobDiagnosticCodes.LargeSupercell)
            {
                continue;
            }

            bag.Add(diagnostic);
        }
    }

    private static Dictionary<SimulationId, CompiledSimulation> IndexSimulations(CompiledModel model)
    {
        var simulations = model.Simulations.IsDefault
            ? ImmutableArray<CompiledSimulation>.Empty
            : model.Simulations;
        var lookup = new Dictionary<SimulationId, CompiledSimulation>(simulations.Length);
        foreach (var simulation in simulations)
        {
            if (simulation is not null)
            {
                lookup.TryAdd(simulation.Id, simulation);
            }
        }

        return lookup;
    }

    private static Diagnostic Error(string code, string message, ModelPath path) =>
        new(code, DiagnosticSeverity.Error, message, path, JobDiagnosticCodes.HelpFor(code));

    private static Diagnostic Warning(string code, string message, ModelPath path) =>
        new(code, DiagnosticSeverity.Warning, message, path, JobDiagnosticCodes.HelpFor(code));
}
