// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;

namespace Mocassin.Packages.Build;

/// <summary>
/// One ordered stage of the build pipeline.
/// </summary>
public enum BuildStage
{
    /// <summary>Load the project document without model validation.</summary>
    Load = 0,

    /// <summary>Resolve the build template, job template, parameterization, and output path.</summary>
    Select = 1,

    /// <summary>Run validation stages 1–4 before compilation.</summary>
    Validate = 2,

    /// <summary>Compile the model.</summary>
    Compile = 3,

    /// <summary>Run the compiled-bound build validation stages.</summary>
    ValidateBuild = 4,

    /// <summary>Validate and apply the effective parameterization.</summary>
    Parameterize = 5,

    /// <summary>Expand the complete job template and apply the job selection.</summary>
    ExpandJobs = 6,

    /// <summary>Generate the selected lattices and write the package rows.</summary>
    GenerateAndWriteJobs = 7,

    /// <summary>Complete, checkpoint, hash, fsync, and atomically publish the package.</summary>
    Publish = 8,
}

/// <summary>
/// The overall status of one build run.
/// </summary>
public enum BuildStatus
{
    /// <summary>The run completed successfully.</summary>
    Succeeded = 0,

    /// <summary>The run stopped on a documented failure.</summary>
    Failed = 1,

    /// <summary>The user cancelled the run.</summary>
    Cancelled = 2,
}

/// <summary>
/// The documented failure kind of a failed or cancelled build.
/// </summary>
public enum BuildFailureKind
{
    /// <summary>No failure; accompanies <see cref="BuildStatus.Succeeded"/> and <see cref="BuildStatus.Cancelled"/>.</summary>
    None = 0,

    /// <summary>Project, output, disk, or writer I/O.</summary>
    Io,

    /// <summary>A parse, content, or load-time validation failure.</summary>
    Load,

    /// <summary>The build template, job template, or parameterization selection failed.</summary>
    TemplateSelection,

    /// <summary>The <c>--jobs</c> selection is malformed, empty, or out of range.</summary>
    JobSelection,

    /// <summary>The model compiler failed.</summary>
    Compilation,

    /// <summary>A validation stage reported an error.</summary>
    Validation,

    /// <summary>Parameterization validation or application failed, including promoted warnings.</summary>
    Parameterization,

    /// <summary>Job-template expansion failed.</summary>
    JobExpansion,

    /// <summary>Lattice generation failed.</summary>
    LatticeGeneration,

    /// <summary>Preview evaluation returned invalid lattice or doping data.</summary>
    Preview,

    /// <summary>The package writer returned a package error.</summary>
    PackageWrite,

    /// <summary>An unexpected internal error occurred.</summary>
    Internal,
}

/// <summary>
/// One event in the ordered build-event stream.
/// </summary>
public abstract record BuildEvent;

/// <summary>
/// The selected job count is known and job generation begins.
/// </summary>
/// <param name="ExpandedJobCount">The complete template expansion count.</param>
/// <param name="SelectedJobCount">The selected count after the job selection.</param>
public sealed record BuildStarted(int ExpandedJobCount, int SelectedJobCount) : BuildEvent;

/// <summary>
/// A pipeline stage started.
/// </summary>
/// <param name="Stage">The stage.</param>
public sealed record BuildStageStarted(BuildStage Stage) : BuildEvent;

/// <summary>
/// A pipeline stage completed; emitted for a failed stage as well.
/// </summary>
/// <param name="Stage">The stage.</param>
/// <param name="Elapsed">The wall time of the stage.</param>
public sealed record BuildStageCompleted(BuildStage Stage, TimeSpan Elapsed) : BuildEvent;

/// <summary>
/// Throttled job-generation progress.
/// </summary>
/// <param name="Stage">The stage that reports progress.</param>
/// <param name="Completed">The monotonic completed job count.</param>
/// <param name="Total">The selected job count.</param>
/// <param name="Current">The current job label, or <see langword="null"/>.</param>
/// <param name="Elapsed">The time since the stage started.</param>
public sealed record BuildProgress(
    BuildStage Stage,
    int Completed,
    int Total,
    string? Current,
    TimeSpan Elapsed) : BuildEvent;

/// <summary>
/// One diagnostic for live feedback; <see cref="BuildResult.Diagnostics"/> remains the report source.
/// </summary>
/// <param name="Diagnostic">The diagnostic.</param>
public sealed record BuildDiagnostic(Diagnostic Diagnostic) : BuildEvent;

/// <summary>
/// A forwarded human log line.
/// </summary>
/// <param name="Level">The level name, for example <c>info</c>.</param>
/// <param name="Message">The message.</param>
public sealed record BuildLog(string Level, string Message) : BuildEvent;

/// <summary>
/// The final event; emitted exactly once for every run.
/// </summary>
/// <param name="Result">The authoritative build result.</param>
public sealed record BuildFinished(BuildResult Result) : BuildEvent;
