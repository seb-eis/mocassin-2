// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.Diagnostics;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Project;
using Mocassin.Packages.Build;
using Mocassin.Packages.Projects;
using Mocassin.Results.Packages;
using PipelineRequest = Mocassin.Packages.Build.BuildRequest;
using PipelineResult = Mocassin.Packages.Build.BuildResult;

namespace Mocassin.Cli.Build;

/// <summary>
/// Implements <c>mocassin build</c> and <c>mocassin build --preview</c> over the shared build pipeline.
/// </summary>
/// <remarks>
/// The command owns parsing results, the human and JSON-Lines formatting, the caller-composed post-publication
/// verification, and the single exit/failure mapping. It never reloads or recompiles inside the preview path:
/// the typed <see cref="Mocassin.Packages.Build.BuildPreview"/> is projected field-for-field.
/// </remarks>
internal sealed class BuildCommand
{
    private readonly IBuildPipeline pipeline;
    private readonly IPackageVerifier verifier;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Initializes the production command.
    /// </summary>
    public BuildCommand()
        : this(new BuildPipeline(), new PackageVerifier(), TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes the command with explicit services for tests.
    /// </summary>
    /// <param name="pipeline">The build pipeline.</param>
    /// <param name="verifier">The post-publication verifier.</param>
    /// <param name="timeProvider">The clock.</param>
    internal BuildCommand(IBuildPipeline pipeline, IPackageVerifier verifier, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(verifier);
        ArgumentNullException.ThrowIfNull(timeProvider);
        this.pipeline = pipeline;
        this.verifier = verifier;
        this.timeProvider = timeProvider;
    }

    /// <summary>
    /// Runs the command.
    /// </summary>
    /// <param name="request">The typed request.</param>
    /// <param name="stdout">The standard-output writer.</param>
    /// <param name="stderr">The standard-error writer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The process exit code.</returns>
    public async Task<int> RunAsync(
        BuildCommandRequest request,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        if (request.Preview && (request.OutputPath is not null || request.Force || request.Compact))
        {
            return WriteUsageError(
                request,
                stdout,
                stderr,
                [CliDiagnosticFactory.Usage(
                    "build --preview does not accept -o, --force, or --compact.")]);
        }

        var started = timeProvider.GetTimestamp();
        var events = request.Json ? new BuildEventWriter(stderr, request.Quiet) : null;

        if (request.Jobs is not null)
        {
            var selectionError = JobSelection.ValidateSyntax(request.Jobs);
            if (selectionError is not null)
            {
                return WriteUsageError(
                    request,
                    stdout,
                    stderr,
                    [CliDiagnosticFactory.JobSelection(selectionError)]);
            }
        }

        if (!request.Preview && request.OutputPath is not null)
        {
            var targetDiagnostic = ValidateTarget(request.OutputPath, request.Force);
            if (targetDiagnostic is not null)
            {
                var invalidTarget = SyntheticFailure(
                    project: null,
                    BuildFailureKind.Io,
                    [targetDiagnostic]);
                return Finish(
                    request,
                    stdout,
                    stderr,
                    new BuildOutcome(
                        invalidTarget,
                        request,
                        null,
                        TryFullPath(request.OutputPath),
                        OutputExists: targetDiagnostic.Code == "CLI003",
                        false,
                        TimeSpan.Zero,
                        []),
                    "skipped",
                    diagnosticsRendered: false,
                    events,
                    started);
            }
        }

        ProjectLoadResult load;
        try
        {
            load = await new ProjectStore(options: new ProjectStoreOptions { ValidateOnLoad = false })
                .LoadAsync(request.ProjectPath, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Finish(
                request,
                stdout,
                stderr,
                new BuildOutcome(
                    SyntheticCancelled(),
                    request,
                    null,
                    null,
                    false,
                    false,
                    TimeSpan.Zero,
                    []),
                "skipped",
                diagnosticsRendered: false,
                events,
                started);
        }

        if (load.Project is null)
        {
            var failure = ClassifyLoadFailure(load.FailureKind);
            var synthetic = SyntheticFailure(project: null, failure, load.Diagnostics);
            return Finish(
                request,
                stdout,
                stderr,
                new BuildOutcome(synthetic, request, null, null, false, false, TimeSpan.Zero, []),
                "skipped",
                diagnosticsRendered: false,
                events,
                started);
        }

        var project = load.Project;
        var resolution = BuildSelectionResolver.Resolve(project, SelectionRequest(request));
        if (resolution.Selection is null)
        {
            var synthetic = SyntheticFailure(project, BuildFailureKind.TemplateSelection, resolution.Diagnostics);
            return Finish(
                request,
                stdout,
                stderr,
                new BuildOutcome(synthetic, request, project, null, false, false, TimeSpan.Zero, []),
                "skipped",
                diagnosticsRendered: false,
                events,
                started);
        }

        var selection = resolution.Selection;
        string? targetPath = null;
        if (!request.Preview)
        {
            if (!BuildPathResolver.TryResolve(
                    request.ProjectPath,
                    request.OutputPath,
                    selection.BuildTemplate,
                    out targetPath,
                    out _))
            {
                if (request.OutputPath is not null)
                {
                    return WriteUsageError(
                        request,
                        stdout,
                        stderr,
                        [CliDiagnosticFactory.Usage($"The output path '{request.OutputPath}' is not valid.")]);
                }

                var synthetic = SyntheticFailure(project, BuildFailureKind.TemplateSelection, []);
                return Finish(
                    request,
                    stdout,
                    stderr,
                    new BuildOutcome(synthetic, request, project, null, false, false, TimeSpan.Zero, []),
                    "skipped",
                    diagnosticsRendered: false,
                    events,
                    started);
            }

            if (targetPath is not null)
            {
                var targetDiagnostic = ValidateTarget(targetPath, request.Force);
                if (targetDiagnostic is not null)
                {
                    var synthetic = SyntheticFailure(project, BuildFailureKind.Io, [targetDiagnostic]);
                    return Finish(
                        request,
                        stdout,
                        stderr,
                        new BuildOutcome(
                            synthetic,
                            request,
                            project,
                            targetPath,
                            OutputExists: targetDiagnostic.Code == "CLI003",
                            false,
                            TimeSpan.Zero,
                            []),
                        "skipped",
                        diagnosticsRendered: false,
                        events,
                        started);
                }
            }
        }

        var pipelineRequest = CreatePipelineRequest(request, selection, targetPath);
        var renderer = request.Json ? null : new BuildProgressRenderer(stderr, request.Preview, request.Quiet);
        PipelineResult? result = null;
        await foreach (var buildEvent in pipeline.BuildAsync(pipelineRequest, cancellationToken))
        {
            if (result is not null)
            {
                continue;
            }

            switch (buildEvent)
            {
                case BuildStageCompleted completed:
                    renderer?.StageCompleted(completed.Stage, completed.Elapsed);
                    break;
                case BuildProgress progress:
                    renderer?.Progress(progress);
                    break;
                case BuildDiagnostic diagnostic:
                    renderer?.Diagnostic(diagnostic.Diagnostic);
                    break;
                case BuildFinished finished:
                    result = finished.Result;
                    break;
            }

            events?.Write(buildEvent);
        }

        if (result is null)
        {
            var synthetic = SyntheticFailure(project, BuildFailureKind.Internal, []);
            return Finish(
                request,
                stdout,
                stderr,
                new BuildOutcome(synthetic, request, project, targetPath, false, false, TimeSpan.Zero, []),
                "skipped",
                diagnosticsRendered: renderer is not null,
                events,
                started);
        }

        if (request.Preview && result.Preview is not null)
        {
            renderer?.PreviewCompleted(result.Preview.Elapsed);
        }

        var verification = "skipped";
        var verificationDiagnostics = ImmutableArray<Diagnostic>.Empty;
        var verifyElapsed = TimeSpan.Zero;
        if (!request.Preview && result.Status == BuildStatus.Succeeded && result.Package is not null)
        {
            try
            {
                var verifyStarted = timeProvider.GetTimestamp();
                var verificationReport = await verifier.VerifyAsync(
                        result.Package.Path,
                        new PackageVerificationOptions(),
                        cancellationToken)
                    .ConfigureAwait(false);
                verifyElapsed = timeProvider.GetElapsedTime(verifyStarted);
                verificationDiagnostics = verificationReport.Diagnostics;
                verification = verificationReport.IsValid ? "passed" : "failed";
                renderer?.Verified(verificationReport, verifyElapsed);
                if (events is not null)
                {
                    foreach (var diagnostic in verificationDiagnostics)
                    {
                        events.Write(new BuildDiagnostic(diagnostic));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                result = result with { Status = BuildStatus.Cancelled };
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                result = result with { Status = BuildStatus.Failed, Failure = BuildFailureKind.Io };
            }
#pragma warning disable CA1031 // Verification is a command boundary; an unexpected verifier failure is internal.
            catch (Exception)
#pragma warning restore CA1031
            {
                result = result with { Status = BuildStatus.Failed, Failure = BuildFailureKind.Internal };
            }
        }

        return Finish(
            request,
            stdout,
            stderr,
            new BuildOutcome(
                result,
                request,
                project,
                targetPath,
                false,
                string.Equals(verification, "failed", StringComparison.Ordinal),
                verifyElapsed,
                verificationDiagnostics),
            verification,
            diagnosticsRendered: renderer is not null,
            events,
            started);
    }

    private int Finish(
        BuildCommandRequest request,
        TextWriter stdout,
        TextWriter stderr,
        BuildOutcome outcome,
        string verification,
        bool diagnosticsRendered,
        BuildEventWriter? events,
        long started)
    {
        var (exitCode, cliDiagnostics) = BuildExitMapper.Map(outcome);
        var report = BuildReportFactory.Create(outcome, exitCode, cliDiagnostics, verification);
        if (request.Json)
        {
            BuildReportWriter.WriteJson(stdout, report);
        }
        else
        {
            if (!diagnosticsRendered)
            {
                foreach (var diagnostic in report.Diagnostics)
                {
                    stderr.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
                }
            }
            else
            {
                foreach (var diagnostic in cliDiagnostics)
                {
                    WriteDiagnostic(stderr, diagnostic);
                }
            }

            BuildReportWriter.WriteHuman(stdout, report);
        }

        events?.WriteFinished(exitCode, timeProvider.GetElapsedTime(started), outcome.Result.Package is not null);
        return exitCode;
    }

    private static int WriteUsageError(
        BuildCommandRequest request,
        TextWriter stdout,
        TextWriter stderr,
        IReadOnlyList<Diagnostic> diagnostics)
    {
        if (request.Json)
        {
            BuildReportWriter.WriteJson(stdout, BuildReportFactory.UsageError(request.Preview, diagnostics));
        }
        else
        {
            foreach (var diagnostic in diagnostics)
            {
                WriteDiagnostic(stderr, diagnostic);
            }
        }

        return CliExitCode.Usage;
    }

    private static void WriteDiagnostic(TextWriter writer, Diagnostic diagnostic) =>
        writer.WriteLine(string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{diagnostic.Code}: {diagnostic.Message}"));

    private static PipelineRequest SelectionRequest(BuildCommandRequest request) => new()
    {
        ProjectPath = request.ProjectPath,
        Preview = request.Preview,
        BuildTemplateId = request.BuildTemplateId is { } buildTemplate
            ? BuildTemplateId.Create(buildTemplate)
            : null,
        JobTemplateId = request.JobTemplateId is { } jobTemplate ? JobTemplateId.Create(jobTemplate) : null,
        ParameterizationId = request.ParameterizationId is { } parameterization
            ? ParameterizationId.Create(parameterization)
            : null,
    };

    private PipelineRequest CreatePipelineRequest(
        BuildCommandRequest request,
        BuildSelection selection,
        string? targetPath) => new()
        {
            ProjectPath = request.ProjectPath,
            TargetPath = targetPath,
            BuildTemplateId = selection.BuildTemplate?.Id,
            JobTemplateId = selection.JobTemplate.Id,
            ParameterizationId = selection.Parameterization?.Id,
            JobSelection = request.Jobs ?? "all",
            Preview = request.Preview,
            Overwrite = request.Force,
            Compact = request.Compact,
            MaxDegreeOfParallelism = request.Parallel == 0 ? Environment.ProcessorCount : request.Parallel,
            WarningsAsErrors = request.WarningsAsErrors,
            Options = new BuildOptions { TimeProvider = timeProvider },
        };

    private static PipelineResult SyntheticFailure(
        Project? project,
        BuildFailureKind failure,
        ImmutableArray<Diagnostic> diagnostics)
    {
        var identity = project is null
            ? BuildIdentity.Empty
            : new BuildIdentity(project.Metadata.ProjectId, null, null, null, null);
        return new PipelineResult(
            BuildStatus.Failed,
            failure,
            identity,
            diagnostics,
            Selection: null,
            JobSelection: null,
            Preview: null,
            Package: null,
            Timings: [],
            Elapsed: TimeSpan.Zero);
    }

    private static PipelineResult SyntheticCancelled() =>
        new(
            BuildStatus.Cancelled,
            BuildFailureKind.None,
            BuildIdentity.Empty,
            [],
            Selection: null,
            JobSelection: null,
            Preview: null,
            Package: null,
            Timings: [],
            Elapsed: TimeSpan.Zero);

    private static BuildFailureKind ClassifyLoadFailure(ProjectLoadFailureKind failureKind) => failureKind switch
    {
        ProjectLoadFailureKind.NotFound or ProjectLoadFailureKind.TooLarge or ProjectLoadFailureKind.Io =>
            BuildFailureKind.Io,
        _ => BuildFailureKind.Load,
    };

    private static Diagnostic? ValidateTarget(string path, bool force)
    {
        var fullPath = TryFullPath(path);
        if (fullPath is null)
        {
            return CliDiagnosticFactory.Io($"The output path '{path}' is not valid.");
        }

        if (Directory.Exists(fullPath))
        {
            return CliDiagnosticFactory.Io($"The output path '{fullPath}' is an existing directory.");
        }

        string? parent;
        try
        {
            parent = Path.GetDirectoryName(fullPath);
        }
        catch (Exception exception) when (exception is ArgumentException
                                              or NotSupportedException
                                              or PathTooLongException)
        {
            return CliDiagnosticFactory.Io($"The output path '{path}' is not valid: {exception.Message}");
        }

        if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
        {
            return CliDiagnosticFactory.Io(
                $"The output directory '{parent}' does not exist; create it first.");
        }

        return !force && File.Exists(fullPath)
            ? CliDiagnosticFactory.OutputExists(
                $"The package '{fullPath}' already exists. Pass --force to replace it.")
            : null;
    }

    private static string? TryFullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException
                                              or NotSupportedException
                                              or PathTooLongException)
        {
            return null;
        }
    }
}
