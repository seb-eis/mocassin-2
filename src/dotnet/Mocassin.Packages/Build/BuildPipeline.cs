// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;
using Mocassin.Packages.Build.Internal;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Validation;
using Mocassin.Packages.SimulationPackage;

namespace Mocassin.Packages.Build;

/// <summary>
/// The production build orchestration over the Steps 07–11 project, compiler, parameterization, job, and package
/// services.
/// </summary>
/// <remarks>
/// The constructor always configures the validation pipeline with
/// <see cref="JobTemplateValidationStage"/> and one shared <see cref="ILatticePreviewService"/> instance, so
/// <see cref="ValidationOptions.RunBuildStage"/> is never a silent no-op and the preview and the build-stage
/// validation share the same generator. The library never chooses an exit code or prints to the console.
/// </remarks>
public sealed class BuildPipeline : IBuildPipeline
{
    private readonly ILatticePreviewService previewService;
    private readonly IModelCompiler compiler;
    private readonly IParameterizationService parameterizationService;
    private readonly ProjectValidationPipeline validationPipeline;
    private readonly Func<PackageBuildRequest, PackageBuildOptions, ISimulationPackageWriter> writerFactory;

    /// <summary>
    /// Initializes the production pipeline.
    /// </summary>
    public BuildPipeline()
    {
        previewService = new LatticePreviewService();
        compiler = new ModelCompiler();
        parameterizationService = new ParameterizationService();
        validationPipeline = new ProjectValidationPipeline(
            additionalStages: [new JobTemplateValidationStage(previewService)]);
        writerFactory = SimulationPackageWriter.Create;
    }

    /// <summary>
    /// Initializes the pipeline with explicit services for real-object tests and Step 26 composition.
    /// </summary>
    /// <param name="previewService">The shared lattice-preview service.</param>
    /// <param name="compiler">The model compiler.</param>
    /// <param name="parameterizationService">The parameterization service.</param>
    /// <param name="validationPipeline">
    /// The validation pipeline. It must be constructed with a <see cref="JobTemplateValidationStage"/>; the
    /// production constructor guarantees this and the default pipeline would make the build stage a no-op.
    /// </param>
    /// <param name="writerFactory">The package-writer factory, or <see langword="null"/> for the production writer.</param>
    /// <exception cref="ArgumentNullException">A required service is <see langword="null"/>.</exception>
    internal BuildPipeline(
        ILatticePreviewService previewService,
        IModelCompiler compiler,
        IParameterizationService parameterizationService,
        ProjectValidationPipeline validationPipeline,
        Func<PackageBuildRequest, PackageBuildOptions, ISimulationPackageWriter>? writerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(previewService);
        ArgumentNullException.ThrowIfNull(compiler);
        ArgumentNullException.ThrowIfNull(parameterizationService);
        ArgumentNullException.ThrowIfNull(validationPipeline);
        this.previewService = previewService;
        this.compiler = compiler;
        this.parameterizationService = parameterizationService;
        this.validationPipeline = validationPipeline;
        this.writerFactory = writerFactory ?? SimulationPackageWriter.Create;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<BuildEvent> BuildAsync(
        BuildRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (reader, producer, producerTask) = Start(request, cancellationToken);
        try
        {
            // The producer converts cancellation into a BuildFinished(BuiltStatus.Cancelled) result; the
            // consumer must still receive it. Disposing the iterator early cancels the producer in the finally.
            await foreach (var buildEvent in reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
            {
                yield return buildEvent;
            }
        }
        finally
        {
            producer.Cancel();
            try
            {
                await producerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // The consumer stopped reading before the producer observed the cancellation.
            }
            finally
            {
                producer.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public async Task<BuildResult> BuildToCompletionAsync(
        BuildRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (reader, producer, producerTask) = Start(request, cancellationToken);
        BuildResult? result = null;
        try
        {
            await foreach (var buildEvent in reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
            {
                if (buildEvent is BuildFinished finished)
                {
                    result = finished.Result;
                }
            }
        }
        finally
        {
            producer.Cancel();
            try
            {
                await producerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // The producer already converted the cancellation into a result.
            }
            finally
            {
                producer.Dispose();
            }
        }

        return result ?? throw new InvalidOperationException(
            "The build event stream ended without a BuildFinished event.");
    }

    private (ChannelReader<BuildEvent> Reader, CancellationTokenSource Producer, Task ProducerTask) Start(
        BuildRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        var channel = Channel.CreateBounded<BuildEvent>(
            new BoundedChannelOptions(request.Options.EventChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true,
            });
        var producer = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var producerTask = Task.Run(
            () => ProduceAsync(request, channel.Writer, producer.Token),
            CancellationToken.None);
        return (channel.Reader, producer, producerTask);
    }

    private async Task ProduceAsync(
        BuildRequest request,
        ChannelWriter<BuildEvent> writer,
        CancellationToken cancellationToken)
    {
        BuildResult result;
        try
        {
            result = await ExecuteAsync(request, writer, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            result = new BuildRun(request).Build(BuildStatus.Cancelled, BuildFailureKind.None);
        }
#pragma warning disable CA1031 // The pipeline boundary converts every unexpected exception into a result.
        catch (Exception)
#pragma warning restore CA1031
        {
            result = new BuildRun(request).Build(BuildStatus.Failed, BuildFailureKind.Internal);
        }

        try
        {
            await writer.WriteAsync(new BuildFinished(result), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            writer.TryWrite(new BuildFinished(result));
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task<BuildResult> ExecuteAsync(
        BuildRequest request,
        ChannelWriter<BuildEvent> writer,
        CancellationToken cancellationToken)
    {
        var run = new BuildRun(request);
        try
        {
            return await ExecuteStagesAsync(run, writer, cancellationToken).ConfigureAwait(false);
        }
        catch (BuildFailureException failure)
        {
            await EmitDiagnosticsAsync(run, failure.Diagnostics, writer, cancellationToken).ConfigureAwait(false);
            return run.Build(BuildStatus.Failed, failure.Kind);
        }
        catch (OperationCanceledException)
        {
            return run.Build(BuildStatus.Cancelled, BuildFailureKind.None);
        }
#pragma warning disable CA1031 // The pipeline boundary converts every unexpected exception into a result.
        catch (Exception)
#pragma warning restore CA1031
        {
            return run.Build(BuildStatus.Failed, BuildFailureKind.Internal);
        }
    }

    private async Task<BuildResult> ExecuteStagesAsync(
        BuildRun run,
        ChannelWriter<BuildEvent> writer,
        CancellationToken cancellationToken)
    {
        var request = run.Request;

        var load = await StageAsync(
                run,
                writer,
                BuildStage.Load,
                () => new ProjectStore(options: new ProjectStoreOptions { ValidateOnLoad = false })
                    .LoadAsync(request.ProjectPath, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);
        await EmitDiagnosticsAsync(run, load.Diagnostics, writer, cancellationToken).ConfigureAwait(false);
        if (load.Project is null)
        {
            throw new BuildFailureException(ClassifyLoadFailure(load.FailureKind), []);
        }

        var project = load.Project;
        run.Load = load;
        run.Project = project;
        run.Identity = run.Identity with { ProjectId = project.Metadata.ProjectId };

        var selection = await StageAsync(
                run,
                writer,
                BuildStage.Select,
                () =>
                {
                    var resolved = BuildSelectionResolver.Resolve(project, request);
                    if (resolved.Selection is null)
                    {
                        return Task.FromResult(new SelectionOutcome(resolved, null, null));
                    }

                    var valid = BuildPathResolver.TryResolve(
                        request.ProjectPath,
                        request.TargetPath,
                        resolved.Selection.BuildTemplate,
                        out var targetPath,
                        out var error);
                    return Task.FromResult(new SelectionOutcome(resolved, targetPath, valid ? null : error));
                },
                cancellationToken)
            .ConfigureAwait(false);
        await EmitDiagnosticsAsync(run, selection.Resolve.Diagnostics, writer, cancellationToken)
            .ConfigureAwait(false);
        if (selection.Resolve.Selection is null || selection.PathError is not null)
        {
            throw new BuildFailureException(BuildFailureKind.TemplateSelection, []);
        }

        run.Selection = selection.Resolve.Selection;
        run.TargetPath = selection.TargetPath;

        var preValidation = await StageAsync(
                run,
                writer,
                BuildStage.Validate,
                () => Task.FromResult(validationPipeline.Validate(
                    project,
                    loadDiagnostics: load.Diagnostics,
                    options: new ValidationOptions
                    {
                        RunBuildStage = false,
                        WarningsAsErrors = request.WarningsAsErrors,
                    },
                    cancellationToken: cancellationToken)),
                cancellationToken)
            .ConfigureAwait(false);
        await EmitDiagnosticsAsync(run, preValidation.Diagnostics, writer, cancellationToken)
            .ConfigureAwait(false);
        run.Identity = run.Identity with
        {
            ModelHash = preValidation.ModelHash,
            DocumentHash = preValidation.DocumentHash,
        };
        if (!preValidation.IsValid)
        {
            throw new BuildFailureException(BuildFailureKind.Validation, []);
        }

        var compilation = await StageAsync(
                run,
                writer,
                BuildStage.Compile,
                () => Task.FromResult(compiler.Compile(
                    project,
                    new CompilationOptions { MaxDegreeOfParallelism = request.MaxDegreeOfParallelism },
                    cancellationToken)),
                cancellationToken)
            .ConfigureAwait(false);
        await EmitDiagnosticsAsync(run, compilation.Diagnostics, writer, cancellationToken)
            .ConfigureAwait(false);
        if (compilation.Status != CompilationStatus.Succeeded || compilation.Model is null)
        {
            throw new BuildFailureException(BuildFailureKind.Compilation, []);
        }

        var model = compilation.Model;
        run.Model = model;
        run.Identity = run.Identity with { ModelSignature = model.ModelSignature };

        var buildValidation = await StageAsync(
                run,
                writer,
                BuildStage.ValidateBuild,
                () => Task.FromResult(validationPipeline.Validate(
                    project,
                    loadDiagnostics: load.Diagnostics,
                    options: new ValidationOptions
                    {
                        RunBuildStage = true,
                        WarningsAsErrors = request.WarningsAsErrors,
                    },
                    compiledModel: model,
                    cancellationToken: cancellationToken)),
                cancellationToken)
            .ConfigureAwait(false);
        await EmitDiagnosticsAsync(run, buildValidation.Diagnostics, writer, cancellationToken)
            .ConfigureAwait(false);
        if (!buildValidation.IsValid)
        {
            throw new BuildFailureException(BuildFailureKind.Validation, []);
        }

        var parameterization = await StageAsync(
                run,
                writer,
                BuildStage.Parameterize,
                () => Task.FromResult(Parameterize(project, model, run.Selection!, request, cancellationToken)),
                cancellationToken)
            .ConfigureAwait(false);
        await EmitDiagnosticsAsync(run, parameterization.Validation, writer, cancellationToken)
            .ConfigureAwait(false);
        await EmitDiagnosticsAsync(run, parameterization.Apply.Diagnostics, writer, cancellationToken)
            .ConfigureAwait(false);
        var parameterizationWarnings =
            parameterization.Validation.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning)
            || parameterization.Apply.Diagnostics.Any(
                static diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
        if (parameterization.Apply.Model is null
            || parameterization.Validation.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            || parameterization.Apply.Diagnostics.Any(
                static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            || (request.WarningsAsErrors && parameterizationWarnings))
        {
            throw new BuildFailureException(BuildFailureKind.Parameterization, []);
        }

        var parameterized = parameterization.Apply.Model;
        run.Parameterized = parameterized;
        run.Identity = run.Identity with { ParameterizationSignature = parameterized.Signature };

        var expansion = await StageAsync(
                run,
                writer,
                BuildStage.ExpandJobs,
                () => Task.FromResult(JobTemplateExpander.Expand(project, model, run.Selection!.JobTemplate)),
                cancellationToken)
            .ConfigureAwait(false);
        await EmitDiagnosticsAsync(run, expansion.Diagnostics, writer, cancellationToken)
            .ConfigureAwait(false);
        if (!expansion.IsSuccess)
        {
            throw new BuildFailureException(BuildFailureKind.JobExpansion, []);
        }

        var jobSelection = JobSelection.Parse(request.JobSelection, expansion.Jobs.Length);
        run.JobSelection = jobSelection;
        if (!jobSelection.IsValid)
        {
            throw new BuildFailureException(BuildFailureKind.JobSelection, []);
        }

        var selectedIds = jobSelection.JobIds.ToHashSet();
        var selected = expansion.Jobs.Where(job => selectedIds.Contains(job.JobId)).ToImmutableArray();
        await EmitAsync(writer, new BuildStarted(expansion.Jobs.Length, selected.Length), cancellationToken)
            .ConfigureAwait(false);

        if (request.Preview)
        {
            var previewResult = BuildPreviewBuilder.Build(
                project,
                model,
                run.Selection!.JobTemplate,
                selected,
                previewService,
                request.Options,
                cancellationToken);
            await EmitDiagnosticsAsync(run, previewResult.Diagnostics, writer, cancellationToken)
                .ConfigureAwait(false);
            if (previewResult.Preview is null)
            {
                throw new BuildFailureException(BuildFailureKind.Preview, []);
            }

            run.Preview = previewResult.Preview;
            return run.Build(BuildStatus.Succeeded, BuildFailureKind.None);
        }

        var targetPath = run.TargetPath
            ?? throw new InvalidOperationException("A full build requires a resolved target path.");
        ISimulationPackageWriter? packageWriter = null;
        try
        {
            await StageAsync(
                    run,
                    writer,
                    BuildStage.GenerateAndWriteJobs,
                    async () =>
                    {
                        packageWriter = writerFactory(
                            new PackageBuildRequest(
                                targetPath,
                                project.Metadata.ProjectId,
                                jobSelection.Expression,
                                run.CreatedUtc),
                            new PackageBuildOptions
                            {
                                Overwrite = request.Overwrite,
                                CompressionLevel = request.Compact ? 9 : 3,
                                Vacuum = request.Compact,
                            });
                        await packageWriter.WriteModelAsync(parameterized, cancellationToken)
                            .ConfigureAwait(false);
                        var estimator = new ProgressEstimator(
                            request.Options.TimeProvider,
                            request.Options.ProgressInterval);
                        var generationStarted = request.Options.TimeProvider.GetTimestamp();
                        var scheduler = await BuildScheduler.RunAsync(
                                project,
                                model,
                                selected,
                                packageWriter,
                                request.MaxDegreeOfParallelism,
                                async (completed, current) =>
                                {
                                    if (estimator.ShouldEmit(completed == selected.Length))
                                    {
                                        await EmitAsync(
                                                writer,
                                                new BuildProgress(
                                                    BuildStage.GenerateAndWriteJobs,
                                                    completed,
                                                    selected.Length,
                                                    current,
                                                    request.Options.TimeProvider.GetElapsedTime(
                                                        generationStarted)),
                                                cancellationToken)
                                            .ConfigureAwait(false);
                                    }
                                },
                                cancellationToken)
                            .ConfigureAwait(false);
                        await EmitDiagnosticsAsync(run, scheduler.Diagnostics, writer, cancellationToken)
                            .ConfigureAwait(false);
                        if (!scheduler.IsSuccess)
                        {
                            throw new BuildFailureException(
                                BuildFailureKind.LatticeGeneration,
                                scheduler.Diagnostics);
                        }

                        return true;
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            var package = await StageAsync(
                    run,
                    writer,
                    BuildStage.Publish,
                    () => packageWriter!.CompleteAsync(cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            run.Package = package;
        }
        catch (BuildFailureException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new BuildFailureException(BuildFailureKind.Io, []);
        }
        finally
        {
            if (packageWriter is not null)
            {
                try
                {
                    await packageWriter.DisposeAsync().ConfigureAwait(false);
                }
#pragma warning disable CA1031 // A disposal failure must not turn a published build into a failure.
                catch (Exception)
#pragma warning restore CA1031
                {
                    // The publication outcome is already known.
                }
            }
        }

        return cancellationToken.IsCancellationRequested
            ? run.Build(BuildStatus.Cancelled, BuildFailureKind.None)
            : run.Build(BuildStatus.Succeeded, BuildFailureKind.None);
    }

    private ParameterizationOutcome Parameterize(
        Project project,
        CompiledModel model,
        BuildSelection selection,
        BuildRequest request,
        CancellationToken cancellationToken)
    {
        var validation = parameterizationService.Validate(
            project,
            model,
            selection.Parameterization,
            options: null,
            cancellationToken: cancellationToken);
        var apply = parameterizationService.Apply(
            project,
            model,
            selection.Parameterization,
            options: null,
            cancellationToken: cancellationToken);
        return new ParameterizationOutcome(validation, apply);
    }

    private static async Task<T> StageAsync<T>(
        BuildRun run,
        ChannelWriter<BuildEvent> writer,
        BuildStage stage,
        Func<Task<T>> body,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EmitAsync(writer, new BuildStageStarted(stage), cancellationToken).ConfigureAwait(false);
        var started = run.Clock.GetTimestamp();
        try
        {
            return await body().ConfigureAwait(false);
        }
        finally
        {
            var elapsed = run.Clock.GetElapsedTime(started);
            run.Timings.Add(new BuildStageTiming(stage, elapsed));
            await EmitAsync(writer, new BuildStageCompleted(stage, elapsed), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async ValueTask EmitAsync(
        ChannelWriter<BuildEvent> writer,
        BuildEvent buildEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            await writer.WriteAsync(buildEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            writer.TryWrite(buildEvent);
        }
    }

    private static async Task EmitDiagnosticsAsync(
        BuildRun run,
        IEnumerable<Diagnostic> diagnostics,
        ChannelWriter<BuildEvent> writer,
        CancellationToken cancellationToken)
    {
        foreach (var diagnostic in run.AddDiagnostics(diagnostics))
        {
            await EmitAsync(writer, new BuildDiagnostic(diagnostic), cancellationToken).ConfigureAwait(false);
        }
    }

    private static BuildFailureKind ClassifyLoadFailure(ProjectLoadFailureKind failureKind) => failureKind switch
    {
        ProjectLoadFailureKind.NotFound or ProjectLoadFailureKind.TooLarge or ProjectLoadFailureKind.Io =>
            BuildFailureKind.Io,
        _ => BuildFailureKind.Load,
    };

    private static void ValidateRequest(BuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProjectPath);
        if (!request.Preview && string.IsNullOrWhiteSpace(request.TargetPath))
        {
            throw new ArgumentException("A full build requires a target path.", nameof(request));
        }

        if (request.MaxDegreeOfParallelism is < 1 or > 256)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.MaxDegreeOfParallelism,
                "The maximum degree of parallelism must be between 1 and 256.");
        }

        ArgumentNullException.ThrowIfNull(request.JobSelection);
        ArgumentNullException.ThrowIfNull(request.Options);
        ArgumentNullException.ThrowIfNull(request.Options.TimeProvider);
        if (request.Options.EventChannelCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.Options.EventChannelCapacity,
                "The event-channel capacity must be at least 1.");
        }

        if (request.Options.ProgressInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.Options.ProgressInterval,
                "The progress interval must not be negative.");
        }
    }

    private sealed record SelectionOutcome(
        BuildSelectionResolveResult Resolve,
        string? TargetPath,
        string? PathError);

    private sealed record ParameterizationOutcome(
        ImmutableArray<Diagnostic> Validation,
        ParameterizationResult Apply);

    private sealed class BuildFailureException : Exception
    {
        public BuildFailureException(BuildFailureKind kind, ImmutableArray<Diagnostic> diagnostics)
            : base(kind.ToString())
        {
            Kind = kind;
            Diagnostics = diagnostics;
        }

        public BuildFailureKind Kind { get; }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
    }

    private sealed class BuildRun
    {
        private readonly HashSet<(string Code, ModelPath? Path, string Message)> seen = [];

        public BuildRun(BuildRequest request)
        {
            Request = request;
            Clock = request.Options.TimeProvider;
            Started = Clock.GetTimestamp();
            CreatedUtc = request.CreatedUtc ?? Clock.GetUtcNow();
        }

        public BuildRequest Request { get; }

        public TimeProvider Clock { get; }

        public long Started { get; }

        public DateTimeOffset CreatedUtc { get; }

        public BuildIdentity Identity { get; set; } = BuildIdentity.Empty;

        public ProjectLoadResult? Load { get; set; }

        public Project? Project { get; set; }

        public CompiledModel? Model { get; set; }

        public ParameterizedModel? Parameterized { get; set; }

        public string? TargetPath { get; set; }

        public BuildSelection? Selection { get; set; }

        public JobSelectionResult? JobSelection { get; set; }

        public BuildPreview? Preview { get; set; }

        public PackageWriteResult? Package { get; set; }

        public List<Diagnostic> Diagnostics { get; } = [];

        public List<BuildStageTiming> Timings { get; } = [];

        public ImmutableArray<Diagnostic> AddDiagnostics(IEnumerable<Diagnostic>? diagnostics)
        {
            if (diagnostics is null)
            {
                return [];
            }

            var added = ImmutableArray.CreateBuilder<Diagnostic>();
            foreach (var diagnostic in diagnostics)
            {
                if (seen.Add((diagnostic.Code, diagnostic.Path, diagnostic.Message)))
                {
                    Diagnostics.Add(diagnostic);
                    added.Add(diagnostic);
                }
            }

            return added.ToImmutable();
        }

        public BuildResult Build(BuildStatus status, BuildFailureKind failure) =>
            new(
                status,
                failure,
                Identity,
                BuildDiagnosticMerger.Merge(Diagnostics),
                Selection,
                JobSelection,
                Preview,
                Package,
                [.. Timings],
                Clock.GetElapsedTime(Started));
    }
}
