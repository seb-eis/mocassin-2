// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Serialization;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Build;

namespace Mocassin.Cli.Build;

/// <summary>
/// Writes the ordered build events as stderr JSON Lines under <c>--json</c>.
/// </summary>
/// <remarks>
/// <c>--quiet</c> suppresses <c>build.stage</c>, <c>build.progress</c>, and <c>log</c>; <c>diagnostic</c>,
/// <c>build.started</c>, and <c>build.finished</c> remain. The final <c>build.finished</c> is written by the
/// command after verification so its exit code is the command outcome, not the pipeline status.
/// </remarks>
internal sealed class BuildEventWriter
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly TextWriter writer;
    private readonly bool quiet;
    private readonly BuildEtaEstimator etaEstimator = new();

    /// <summary>
    /// Initializes the writer.
    /// </summary>
    /// <param name="writer">The standard-error writer.</param>
    /// <param name="quiet">Whether stage, progress, and log events are suppressed.</param>
    public BuildEventWriter(TextWriter writer, bool quiet)
    {
        ArgumentNullException.ThrowIfNull(writer);
        this.writer = writer;
        this.quiet = quiet;
    }

    /// <summary>
    /// Writes one pipeline event.
    /// </summary>
    /// <param name="buildEvent">The event.</param>
    public void Write(BuildEvent buildEvent)
    {
        ArgumentNullException.ThrowIfNull(buildEvent);
        switch (buildEvent)
        {
            case BuildStarted started:
                WriteJson(new StartedEvent(1, "build.started", started.SelectedJobCount));
                break;
            case BuildStageStarted stageStarted when !quiet:
                WriteJson(new StageEvent(1, "build.stage", StageName(stageStarted.Stage), "started", null));
                break;
            case BuildStageCompleted stageCompleted when !quiet:
                WriteJson(new StageEvent(
                    1,
                    "build.stage",
                    StageName(stageCompleted.Stage),
                    "completed",
                    stageCompleted.Elapsed.TotalMilliseconds));
                break;
            case BuildProgress progress:
                WriteProgress(progress);
                break;
            case BuildDiagnostic diagnostic:
                WriteDiagnostic(diagnostic.Diagnostic);
                break;
            case BuildLog log when !quiet:
                WriteJson(new LogEvent(1, "log", log.Level, log.Message));
                break;
            case BuildFinished:
                // The command writes the final event after verification.
                break;
        }
    }

    /// <summary>
    /// Writes the final <c>build.finished</c> event exactly once.
    /// </summary>
    /// <param name="exitCode">The final command exit code.</param>
    /// <param name="wallTime">The total wall time.</param>
    /// <param name="published">Whether a package was published.</param>
    public void WriteFinished(int exitCode, TimeSpan wallTime, bool published) =>
        WriteJson(new FinishedEvent(1, "build.finished", exitCode, wallTime.TotalSeconds, published));

    /// <summary>
    /// Writes the human-readable stage name of a pipeline stage.
    /// </summary>
    /// <param name="stage">The stage.</param>
    /// <returns>The lower-case event name.</returns>
    public static string StageName(BuildStage stage) => stage switch
    {
        BuildStage.Load => "load",
        BuildStage.Select => "select",
        BuildStage.Validate => "validate",
        BuildStage.Compile => "compile",
        BuildStage.ValidateBuild => "validateBuild",
        BuildStage.Parameterize => "parameterize",
        BuildStage.ExpandJobs => "expand",
        BuildStage.GenerateAndWriteJobs => "generate",
        BuildStage.Publish => "publish",
        _ => stage.ToString().ToLowerInvariant(),
    };

    private void WriteProgress(BuildProgress progress)
    {
        if (quiet)
        {
            return;
        }

        var eta = progress.Completed >= progress.Total
            ? 0
            : etaEstimator.Observe(progress.Elapsed, progress.Completed, progress.Total);
        WriteJson(new ProgressEvent(
            1,
            "build.progress",
            progress.Completed,
            progress.Total,
            StageName(progress.Stage),
            progress.Current,
            eta));
    }

    private void WriteDiagnostic(Diagnostic diagnostic)
    {
        var report = CliDiagnosticFactory.ToReportDiagnostic(diagnostic);
        WriteJson(new DiagnosticEvent(1, "diagnostic", report.Code, report.Severity, report.Message, report.Path));
    }

    private void WriteJson<T>(T value) => writer.WriteLine(JsonSerializer.Serialize(value, jsonOptions));

    private sealed record StartedEvent(int SchemaVersion, string Event, int Total);

    private sealed record StageEvent(
        int SchemaVersion,
        string Event,
        string Stage,
        string Status,
        double? Ms);

    private sealed record ProgressEvent(
        int SchemaVersion,
        string Event,
        int Completed,
        int Total,
        string? Stage,
        string? Current,
        double? EtaS);

    private sealed record DiagnosticEvent(
        int SchemaVersion,
        string Event,
        string Code,
        string Severity,
        string Message,
        ReportPath? Path);

    private sealed record LogEvent(int SchemaVersion, string Event, string Level, string Message);

    private sealed record FinishedEvent(
        int SchemaVersion,
        string Event,
        int ExitCode,
        double WallTimeS,
        bool Published);
}
