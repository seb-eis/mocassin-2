// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Diagnostics;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Energy;
using Mocassin.Symmetry.Structure;
using Mocassin.Symmetry.Transitions;

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// Runs the ordered load, model, cross-domain, feasibility, and optional build validation stages.
/// </summary>
/// <remarks>
/// The pipeline is stateless and thread-safe. It composes <see cref="ProjectValidationService"/> for the
/// model stage and never re-emits the diagnostics of that stage. Validation never mutates the project.
/// </remarks>
public sealed class ProjectValidationPipeline
{
    private static readonly HashSet<string> buildabilityCodes = new(StringComparer.Ordinal)
    {
        SimulationDiagnosticCodes.NoTransitions,
        EnergyDiagnosticCodes.KmcPathOutsideEnvironment,
        Step07DiagnosticCodes.KmcSimulationWithoutMobileStart,
        Step07DiagnosticCodes.NoMobileStartParticle,
    };

    private readonly ImmutableArray<IValidationStage> buildStages;
    private readonly FeasibilityValidator feasibilityValidator;
    private readonly CrossDomainValidator crossDomainValidator;
    private readonly DiagnosticCatalog catalog = DiagnosticCatalog.Default;

    /// <summary>
    /// Initializes a validation pipeline.
    /// </summary>
    /// <param name="structurePreview">Optional structure preview service for feasibility statistics.</param>
    /// <param name="energyPreview">Optional energy preview service for feasibility statistics.</param>
    /// <param name="transitionPreview">Optional transition preview service for feasibility statistics.</param>
    /// <param name="additionalStages">Optional build-stage extensions owned by later steps.</param>
    /// <exception cref="ArgumentException">A supplied stage does not declare <see cref="ValidationStage.Build"/>.</exception>
    public ProjectValidationPipeline(
        IStructurePreviewService? structurePreview = null,
        IEnergyPreviewService? energyPreview = null,
        ITransitionPreviewService? transitionPreview = null,
        IEnumerable<IValidationStage>? additionalStages = null)
    {
        var structure = structurePreview ?? new StructurePreviewService();
        var energy = energyPreview ?? new EnergyPreviewService();
        var transitions = transitionPreview ?? new TransitionPreviewService();
        crossDomainValidator = new CrossDomainValidator(structure, transitions);
        feasibilityValidator = new FeasibilityValidator(structure, energy, transitions);

        var stages = additionalStages?.ToImmutableArray() ?? ImmutableArray<IValidationStage>.Empty;
        foreach (var stage in stages)
        {
            if (stage is null)
            {
                throw new ArgumentException("A validation stage must not be null.", nameof(additionalStages));
            }

            if (stage.Stage != ValidationStage.Build)
            {
                throw new ArgumentException(
                    $"Only {ValidationStage.Build} extension stages are accepted; stage '{stage.Stage}' is not valid.",
                    nameof(additionalStages));
            }
        }

        buildStages = stages;
    }

    /// <summary>
    /// Validates an already loaded project.
    /// </summary>
    /// <param name="project">The project snapshot.</param>
    /// <param name="loadDiagnostics">Optional caller-owned load diagnostics.</param>
    /// <param name="options">Optional run options.</param>
    /// <param name="compiledModel">
    /// The optional compiled model supplied by build orchestration. Build stages run only when
    /// <see cref="ValidationOptions.RunBuildStage"/> is enabled and this model is present.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The immutable validation result.</returns>
    public ValidationResult Validate(
        Project project,
        ImmutableArray<Diagnostic> loadDiagnostics = default,
        ValidationOptions? options = null,
        CompiledModel? compiledModel = null,
        CancellationToken cancellationToken = default) =>
        ValidateCore(project, compiledModel, loadDiagnostics, options, cancellationToken);

    private ValidationResult ValidateCore(
        Project project,
        CompiledModel? compiledModel,
        ImmutableArray<Diagnostic> loadDiagnostics,
        ValidationOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        var effectiveOptions = GetValidatedOptions(options);
        var load = loadDiagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : loadDiagnostics;

        var stages = new List<StageExecution>(5);
        if (load.Length == 0)
        {
            stages.Add(StageExecution.Skipped(ValidationStage.Load));
        }
        else
        {
            stages.Add(new StageExecution(
                ValidationStage.Load,
                load,
                HasError(load) ? ValidationStageStatus.Failed : ValidationStageStatus.Completed,
                TimeSpan.Zero));
        }

        var identity = ComputeIdentity(project);
        if (HasError(load))
        {
            AppendSkippedBuildStages(stages, effectiveOptions);
            return AssembleResult(identity, loadFailure: null, stages, statistics: null, effectiveOptions);
        }

        return RunLoadedStages(
            project,
            identity,
            loadFailure: null,
            stages,
            effectiveOptions,
            compiledModel,
            cancellationToken);
    }

    /// <summary>
    /// Loads a project through a parse-only <see cref="ProjectStore"/> and validates it.
    /// </summary>
    /// <param name="projectPath">The project file path.</param>
    /// <param name="store">Optional store configured with <see cref="ProjectStoreOptions.ValidateOnLoad"/> disabled.</param>
    /// <param name="options">Optional run options.</param>
    /// <param name="compiledModel">
    /// The optional compiled model supplied by build orchestration. Build stages run only when
    /// <see cref="ValidationOptions.RunBuildStage"/> is enabled and this model is present.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The immutable validation result.</returns>
    /// <exception cref="ArgumentException">The supplied store would validate on load.</exception>
    public Task<ValidationResult> ValidateFileAsync(
        string projectPath,
        ProjectStore? store = null,
        ValidationOptions? options = null,
        CompiledModel? compiledModel = null,
        CancellationToken cancellationToken = default) =>
        ValidateFileCoreAsync(projectPath, store, options, compiledModel, cancellationToken);

    private async Task<ValidationResult> ValidateFileCoreAsync(
        string projectPath,
        ProjectStore? store,
        ValidationOptions? options,
        CompiledModel? compiledModel,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        var effectiveOptions = GetValidatedOptions(options);
        var effectiveStore = store ?? new ProjectStore(options: new ProjectStoreOptions { ValidateOnLoad = false });
        if (effectiveStore.Options.ValidateOnLoad)
        {
            throw new ArgumentException(
                "The validation pipeline requires a store with ValidateOnLoad disabled to avoid validating twice.",
                nameof(store));
        }

        var started = Stopwatch.GetTimestamp();
        var loadResult = await effectiveStore.LoadAsync(projectPath, cancellationToken).ConfigureAwait(false);
        var elapsed = Stopwatch.GetElapsedTime(started);
        var loadDiagnostics = loadResult.Diagnostics;

        var stages = new List<StageExecution>(5)
        {
            new(
                ValidationStage.Load,
                loadDiagnostics,
                loadResult.Project is null || HasError(loadDiagnostics)
                    ? ValidationStageStatus.Failed
                    : ValidationStageStatus.Completed,
                elapsed),
        };

        if (loadResult.Project is null || HasError(loadDiagnostics))
        {
            AppendSkippedBuildStages(stages, effectiveOptions);
            return AssembleResult(
                ProjectIdentity.Empty,
                loadResult.Project is null ? loadResult.FailureKind : NullIfNone(loadResult.FailureKind),
                stages,
                statistics: null,
                effectiveOptions);
        }

        return RunLoadedStages(
            loadResult.Project,
            ComputeIdentity(loadResult.Project),
            loadFailure: null,
            stages,
            effectiveOptions,
            compiledModel,
            cancellationToken);
    }

    private ValidationResult RunLoadedStages(
        Project project,
        ProjectIdentity identity,
        ProjectLoadFailureKind? loadFailure,
        List<StageExecution> stages,
        ValidationOptions options,
        CompiledModel? compiledModel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var modelStarted = Stopwatch.GetTimestamp();
        var modelDiagnostics = ProjectValidationService.Validate(project);
        stages.Add(new StageExecution(
            ValidationStage.Model,
            modelDiagnostics,
            HasError(modelDiagnostics) ? ValidationStageStatus.Failed : ValidationStageStatus.Completed,
            Stopwatch.GetElapsedTime(modelStarted)));

        if (HasError(modelDiagnostics))
        {
            stages.Add(StageExecution.Skipped(ValidationStage.CrossDomain));
            stages.Add(StageExecution.Skipped(ValidationStage.Feasibility));
            AppendSkippedBuildStages(stages, options);
            return AssembleResult(identity, loadFailure, stages, statistics: null, options);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var crossStarted = Stopwatch.GetTimestamp();
        var crossDiagnostics = crossDomainValidator.Validate(project, options, cancellationToken);
        stages.Add(new StageExecution(
            ValidationStage.CrossDomain,
            crossDiagnostics,
            HasError(crossDiagnostics) ? ValidationStageStatus.Failed : ValidationStageStatus.Completed,
            Stopwatch.GetElapsedTime(crossStarted)));

        cancellationToken.ThrowIfCancellationRequested();
        var feasibilityStarted = Stopwatch.GetTimestamp();
        var feasibility = feasibilityValidator.Validate(project, options, cancellationToken);
        var feasibilityDiagnostics = feasibility.Diagnostics.IsDefault
            ? ImmutableArray<Diagnostic>.Empty
            : feasibility.Diagnostics;
        stages.Add(new StageExecution(
            ValidationStage.Feasibility,
            feasibilityDiagnostics,
            HasError(feasibilityDiagnostics) ? ValidationStageStatus.Failed : ValidationStageStatus.Completed,
            Stopwatch.GetElapsedTime(feasibilityStarted)));

        RunBuildStages(project, options, compiledModel, stages, cancellationToken);

        return AssembleResult(identity, loadFailure, stages, feasibility.Statistics, options);
    }

    private void AppendSkippedBuildStages(List<StageExecution> stages, ValidationOptions options)
    {
        if (!options.RunBuildStage)
        {
            return;
        }

        foreach (var _ in buildStages)
        {
            stages.Add(StageExecution.Skipped(ValidationStage.Build));
        }
    }

    private void RunBuildStages(
        Project project,
        ValidationOptions options,
        CompiledModel? compiledModel,
        List<StageExecution> stages,
        CancellationToken cancellationToken)
    {
        if (!options.RunBuildStage || buildStages.Length == 0)
        {
            return;
        }

        var blocked = compiledModel is null || stages.Exists(static stage => HasError(stage.Diagnostics));
        var previous = FinalizeStages(stages).Stages;
        foreach (var stage in buildStages)
        {
            if (blocked)
            {
                stages.Add(StageExecution.Skipped(ValidationStage.Build));
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var started = Stopwatch.GetTimestamp();
            var context = new ValidationContext(project, options, catalog, previous, compiledModel);
            var diagnostics = stage.Validate(context, cancellationToken);
            if (diagnostics.IsDefault)
            {
                diagnostics = ImmutableArray<Diagnostic>.Empty;
            }

            stages.Add(new StageExecution(
                ValidationStage.Build,
                diagnostics,
                HasError(diagnostics) ? ValidationStageStatus.Failed : ValidationStageStatus.Completed,
                Stopwatch.GetElapsedTime(started)));
            if (HasError(diagnostics))
            {
                blocked = true;
            }

            previous = FinalizeStages(stages).Stages;
        }
    }

    private static ValidationResult AssembleResult(
        ProjectIdentity identity,
        ProjectLoadFailureKind? loadFailure,
        List<StageExecution> stages,
        ModelStatistics? statistics,
        ValidationOptions options)
    {
        var finalized = FinalizeStages(stages);
        var diagnostics = finalized.Diagnostics;
        var summary = new DiagnosticSummary(
            diagnostics.Count(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
            diagnostics.Count(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning),
            diagnostics.Count(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Info));

        var isValid = summary.Errors == 0
            && !(options.WarningsAsErrors && summary.Warnings > 0)
            && !(options.RequireBuildable && diagnostics.Any(static diagnostic =>
                diagnostic.Severity != DiagnosticSeverity.Info && buildabilityCodes.Contains(diagnostic.Code)));

        return new ValidationResult(
            identity.ProjectId,
            identity.ModelHash,
            identity.DocumentHash,
            loadFailure,
            isValid,
            summary,
            diagnostics,
            statistics,
            finalized.Stages);
    }

    private static FinalizedStages FinalizeStages(List<StageExecution> stages)
    {
        var seen = new HashSet<(string Code, ModelPath? Path, string Message)>();
        var accepted = new List<Diagnostic>();
        var counts = new int[stages.Count];
        for (var index = 0; index < stages.Count; index++)
        {
            var ordered = new DiagnosticBag(stages[index].Diagnostics).Sorted;
            foreach (var diagnostic in ordered)
            {
                if (seen.Add((diagnostic.Code, diagnostic.Path, diagnostic.Message)))
                {
                    accepted.Add(diagnostic);
                    counts[index]++;
                }
            }
        }

        var diagnostics = new DiagnosticBag(accepted).Sorted;
        var results = stages
            .Select((stage, index) => new ValidationStageResult(stage.Stage, stage.Status, counts[index], stage.Elapsed))
            .ToImmutableArray();
        return new FinalizedStages(diagnostics, results);
    }

    private static ProjectIdentity ComputeIdentity(Project project) =>
        new(
            project.Metadata.ProjectId,
            ProjectHasher.Compute(project),
            ProjectHasher.ComputeDocument(project));

    private static ValidationOptions GetValidatedOptions(ValidationOptions? options)
    {
        var effective = options ?? ValidationOptions.Default;
        if (effective.CellCountA < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "CellCountA must be at least 1.");
        }

        if (effective.CellCountB < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "CellCountB must be at least 1.");
        }

        if (effective.CellCountC < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "CellCountC must be at least 1.");
        }

        return effective;
    }

    private static bool HasError(ImmutableArray<Diagnostic> diagnostics) =>
        diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    private static ProjectLoadFailureKind? NullIfNone(ProjectLoadFailureKind kind) =>
        kind == ProjectLoadFailureKind.None ? null : kind;

    private sealed record StageExecution(
        ValidationStage Stage,
        ImmutableArray<Diagnostic> Diagnostics,
        ValidationStageStatus Status,
        TimeSpan Elapsed)
    {
        public static StageExecution Skipped(ValidationStage stage) =>
            new(stage, ImmutableArray<Diagnostic>.Empty, ValidationStageStatus.Skipped, TimeSpan.Zero);
    }

    private readonly record struct FinalizedStages(
        ImmutableArray<Diagnostic> Diagnostics,
        ImmutableArray<ValidationStageResult> Stages);

    private readonly record struct ProjectIdentity(Guid? ProjectId, string? ModelHash, string? DocumentHash)
    {
        public static ProjectIdentity Empty { get; } = new(null, null, null);
    }
}
