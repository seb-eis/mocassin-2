// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Project;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Jobs.Optimization;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.Cli.Build;

/// <summary>
/// Implements the no-write <c>mocassin build --preview</c> command.
/// </summary>
/// <remarks>
/// The command loads, validates, compiles, expands, and previews the selected job template. It never opens or
/// writes a <c>.msl</c> package and never calls the Step 11 writer. Step 12 owns the full build command and the
/// package-writing options.
/// </remarks>
internal static class PreviewCommand
{
    private const int previewSchemaVersion = 1;

    /// <summary>
    /// Parses and runs the command.
    /// </summary>
    /// <param name="arguments">The arguments after the command name, including <c>--preview</c>.</param>
    /// <param name="stdout">The standard-output writer.</param>
    /// <param name="stderr">The standard-error writer.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(
        IReadOnlyList<string> arguments,
        TextWriter stdout,
        TextWriter stderr)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        var json = false;
        var preview = false;
        var path = default(string);
        Guid? templateId = null;
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            switch (argument)
            {
                case "--json":
                    json = true;
                    break;
                case "--preview":
                    preview = true;
                    break;
                case "--template":
                    if (index + 1 >= arguments.Count ||
                        arguments[index + 1].StartsWith('-') ||
                        !Guid.TryParse(arguments[index + 1], out var parsedTemplate))
                    {
                        return WriteUsageError(json, stdout, stderr, "--template requires a job-template GUID.");
                    }

                    templateId = parsedTemplate;
                    index++;
                    break;
                default:
                    if (argument.StartsWith('-'))
                    {
                        return WriteUsageError(json, stdout, stderr, $"Unknown option '{argument}'.");
                    }

                    if (path is not null)
                    {
                        return WriteUsageError(json, stdout, stderr, "Exactly one project path is required.");
                    }

                    path = argument;
                    break;
            }
        }

        if (!preview)
        {
            return WriteUsageError(json, stdout, stderr, "The preview command requires --preview.");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return WriteUsageError(json, stdout, stderr, "A project path is required.");
        }

        ProjectLoadResult load;
        try
        {
            load = await new ProjectStore(options: new ProjectStoreOptions { ValidateOnLoad = false })
                .LoadAsync(path)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or NotSupportedException
                                              or System.Security.SecurityException)
        {
            var (ioReport, ioExitCode) = IoError(path, exception.Message);
            Write(json, stdout, stderr, ioReport);
            return ioExitCode;
        }

        if (load.Project is null)
        {
            var exitCode = LoadExitCode(load.FailureKind);
            var report = Failure(exitCode, load.Diagnostics);
            Write(json, stdout, stderr, report);
            return exitCode;
        }

        var project = load.Project;
        var compilation = new ModelCompiler().Compile(project);
        if (!compilation.IsSuccess)
        {
            var report = Failure(1, compilation.Diagnostics);
            Write(json, stdout, stderr, report);
            return 1;
        }

        var validation = new ProjectValidationPipeline(
                additionalStages: [new JobTemplateValidationStage()])
            .Validate(
                project,
                load.Diagnostics,
                new ValidationOptions { RunBuildStage = true },
                compilation.Model);
        if (validation.Diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            var report = Failure(1, validation.Diagnostics);
            Write(json, stdout, stderr, report);
            return 1;
        }

        var templates = project.JobTemplates.IsDefault
            ? ImmutableArray<JobTemplate>.Empty
            : project.JobTemplates.Where(static template => template is not null).ToImmutableArray();
        if (templateId is null)
        {
            if (templates.Length == 0)
            {
                var report = Failure(
                    1,
                    [new Diagnostic(
                        ProjectDiagnosticCodes.UnknownReference,
                        DiagnosticSeverity.Error,
                        "The project does not contain a job template to preview.",
                        new ModelPath("project", project.Metadata.ProjectId, "jobTemplates"))]);
                Write(json, stdout, stderr, report);
                return 1;
            }

            if (templates.Length > 1)
            {
                return WriteUsageError(
                    json,
                    stdout,
                    stderr,
                    "The project contains several job templates; select one with --template <jobTemplateId>.");
            }
        }

        var requestedTemplateId = templateId;
        var template = requestedTemplateId is null
            ? templates[0]
            : templates.FirstOrDefault(item => item.Id.Value == requestedTemplateId.Value);
        if (template is null)
        {
            var report = Failure(
                1,
                [new Diagnostic(
                    ProjectDiagnosticCodes.UnknownReference,
                    DiagnosticSeverity.Error,
                    $"The project does not contain the job template '{requestedTemplateId.GetValueOrDefault():D}'.",
                    new ModelPath("project", project.Metadata.ProjectId, "jobTemplates"))]);
            Write(json, stdout, stderr, report);
            return 1;
        }

        var expansion = JobTemplateExpander.Expand(project, compilation.Model!, template);
        var diagnostics = new DiagnosticBag(validation.Diagnostics);
        diagnostics.AddRange(expansion.Diagnostics);
        if (!expansion.IsSuccess)
        {
            var report = Failure(1, diagnostics.Sorted);
            Write(json, stdout, stderr, report);
            return 1;
        }

        var previewData = TryCreatePreviewData(
            project,
            compilation.Model!,
            template,
            expansion.Jobs,
            diagnostics,
            out var previewDiagnostics);
        diagnostics.AddRange(previewDiagnostics);
        if (previewData is null)
        {
            var report = Failure(1, diagnostics.Sorted);
            Write(json, stdout, stderr, report);
            return 1;
        }

        var sorted = Deduplicate(diagnostics.Sorted);
        var successReport = new BuildPreviewReport(
            previewSchemaVersion,
            "buildPreview",
            project.Metadata.ProjectId,
            validation.ModelHash,
            validation.DocumentHash,
            Valid: true,
            ExitCode: 0,
            Summary(sorted, previewData),
            sorted.Select(PreviewReportWriter.ToReportDiagnostic).ToArray(),
            previewData);
        Write(json, stdout, stderr, successReport);
        return 0;
    }

    private static BuildPreviewData? TryCreatePreviewData(
        Project project,
        CompiledModel model,
        JobTemplate template,
        ImmutableArray<JobDescriptor> jobs,
        DiagnosticBag diagnostics,
        out ImmutableArray<Diagnostic> previewDiagnostics)
    {
        var bag = new DiagnosticBag();
        var previewService = new LatticePreviewService();
        var authoredCollections = template.Collections is { IsDefault: false } value
            ? value
            : ImmutableArray<JobCollection>.Empty;
        var collections = new List<BuildPreviewCollection>();
        foreach (var collectionGroup in jobs.GroupBy(static job => job.CollectionIndex).OrderBy(static group => group.Key))
        {
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
                .ToArray();

            var configs = new List<BuildPreviewConfig>();
            foreach (var configGroup in collectionGroup.GroupBy(static job => job.ConfigIndex).OrderBy(static group => group.Key))
            {
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
                    previewDiagnostics = Deduplicate(bag.Sorted);
                    return null;
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
                    .ToArray();
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
                configs));
        }

        diagnostics.AddRange(bag.Sorted);
        previewDiagnostics = Deduplicate(bag.Sorted);
        return new BuildPreviewData(
            template.Id.Value.ToString("D", CultureInfo.InvariantCulture),
            template.Name,
            jobs.Length,
            collections);
    }

    private static ImmutableArray<Diagnostic> Deduplicate(ImmutableArray<Diagnostic> diagnostics)
    {
        var seen = new HashSet<(string Code, ModelPath? Path, string Message)>();
        var builder = ImmutableArray.CreateBuilder<Diagnostic>(diagnostics.Length);
        foreach (var diagnostic in diagnostics)
        {
            if (seen.Add((diagnostic.Code, diagnostic.Path, diagnostic.Message)))
            {
                builder.Add(diagnostic);
            }
        }

        return builder.ToImmutable();
    }

    private static BuildPreviewSummary Summary(ImmutableArray<Diagnostic> diagnostics, BuildPreviewData? preview) =>
        new(
            diagnostics.Count(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
            diagnostics.Count(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning),
            diagnostics.Count(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Info),
            preview?.TotalJobs ?? 0,
            preview?.Collections.Count ?? 0,
            preview?.Collections.Sum(static collection => collection.Configs.Count) ?? 0);

    private static BuildPreviewReport Failure(int exitCode, ImmutableArray<Diagnostic> diagnostics)
    {
        var items = Deduplicate(diagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : diagnostics);
        return new BuildPreviewReport(
            previewSchemaVersion,
            "buildPreview",
            null,
            null,
            null,
            Valid: false,
            exitCode,
            Summary(items, preview: null),
            items.Select(PreviewReportWriter.ToReportDiagnostic).ToArray(),
            null);
    }

    private static (BuildPreviewReport Report, int ExitCode) IoError(string path, string message) =>
        (Failure(3, [new Diagnostic(
            ProjectDiagnosticCodes.InvalidDocument,
            DiagnosticSeverity.Error,
            $"Could not read project '{path}': {message}")]), 3);

    private static int LoadExitCode(ProjectLoadFailureKind failureKind) => failureKind switch
    {
        ProjectLoadFailureKind.NotFound or ProjectLoadFailureKind.TooLarge or ProjectLoadFailureKind.Io => 3,
        _ => 1,
    };

    private static int WriteUsageError(bool json, TextWriter stdout, TextWriter stderr, string message)
    {
        stderr.WriteLine($"mocassin build --preview: {message}");
        stderr.WriteLine("usage: mocassin build <project.mocprj> --preview [--template <jobTemplateId>] [--json]");
        if (json)
        {
            PreviewReportWriter.WriteJson(stdout, Failure(2, []));
        }

        return 2;
    }

    private static void Write(bool json, TextWriter stdout, TextWriter stderr, BuildPreviewReport report)
    {
        if (json)
        {
            PreviewReportWriter.WriteJson(stdout, report);
            if (!report.Valid && report.Diagnostics.Count > 0)
            {
                foreach (var diagnostic in report.Diagnostics)
                {
                    stderr.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
                }
            }

            return;
        }

        PreviewReportWriter.WriteHuman(stdout, report);
    }
}
