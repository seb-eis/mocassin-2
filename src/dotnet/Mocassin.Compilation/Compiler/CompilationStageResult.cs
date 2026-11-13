// SPDX-License-Identifier: MIT

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// Describes how one compilation stage completed.
/// </summary>
public enum CompilationStageStatus
{
    /// <summary>
    /// The stage ran and produced no error diagnostic.
    /// </summary>
    Completed = 0,

    /// <summary>
    /// The stage did not run because an earlier stage failed.
    /// </summary>
    Skipped = 1,

    /// <summary>
    /// The stage ran and produced at least one error diagnostic.
    /// </summary>
    Failed = 2,
}

/// <summary>
/// Records the outcome of one compilation stage for the compilation report.
/// </summary>
public sealed record CompilationStageResult
{
    /// <summary>
    /// Initializes a stage result.
    /// </summary>
    /// <param name="stage">The stage that ran or was skipped.</param>
    /// <param name="status">The stage outcome.</param>
    /// <param name="diagnosticCount">The number of this stage's diagnostics retained by the report.</param>
    /// <param name="elapsed">The measured stage duration.</param>
    /// <exception cref="ArgumentOutOfRangeException">An enum value, count, or duration is out of range.</exception>
    public CompilationStageResult(
        CompilationStage stage,
        CompilationStageStatus status,
        int diagnosticCount,
        TimeSpan elapsed)
    {
        if (!Enum.IsDefined(stage))
        {
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown compilation stage.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown compilation stage status.");
        }

        if (diagnosticCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(diagnosticCount),
                diagnosticCount,
                "The diagnostic count must not be negative.");
        }

        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), elapsed, "The stage duration must not be negative.");
        }

        Stage = stage;
        Status = status;
        DiagnosticCount = diagnosticCount;
        Elapsed = elapsed;
    }

    /// <summary>
    /// Gets the stage that ran or was skipped.
    /// </summary>
    public CompilationStage Stage { get; }

    /// <summary>
    /// Gets the stage outcome.
    /// </summary>
    public CompilationStageStatus Status { get; }

    /// <summary>
    /// Gets the number of this stage's diagnostics retained by the report.
    /// </summary>
    public int DiagnosticCount { get; }

    /// <summary>
    /// Gets the measured stage duration.
    /// </summary>
    public TimeSpan Elapsed { get; }
}
