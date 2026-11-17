// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Cli.Diagnostics;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Build;
using Mocassin.Results.Packages;

namespace Mocassin.Cli.Build;

/// <summary>
/// Renders the human stage, progress, diagnostic, and verification lines on standard error.
/// </summary>
/// <remarks>
/// Human progress goes to stderr and the final summary to stdout, so stdout stays scriptable. <c>--json</c>
/// replaces this renderer with <see cref="BuildEventWriter"/>; <c>--quiet</c> suppresses stage and progress
/// lines but never diagnostics.
/// </remarks>
internal sealed class BuildProgressRenderer
{
    private readonly TextWriter writer;
    private readonly bool preview;
    private readonly bool quiet;
    private readonly BuildEtaEstimator etaEstimator = new();

    /// <summary>
    /// Initializes the renderer.
    /// </summary>
    /// <param name="writer">The standard-error writer.</param>
    /// <param name="preview">Whether the run is a no-write preview.</param>
    /// <param name="quiet">Whether stage and progress lines are suppressed.</param>
    public BuildProgressRenderer(TextWriter writer, bool preview, bool quiet)
    {
        ArgumentNullException.ThrowIfNull(writer);
        this.writer = writer;
        this.preview = preview;
        this.quiet = quiet;
    }

    /// <summary>Gets the number of rendered steps for the mode.</summary>
    public int TotalSteps => preview ? 8 : 10;

    /// <summary>
    /// Renders a completed stage.
    /// </summary>
    /// <param name="stage">The stage.</param>
    /// <param name="elapsed">The stage wall time.</param>
    public void StageCompleted(BuildStage stage, TimeSpan elapsed)
    {
        if (quiet)
        {
            return;
        }

        writer.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"[{Step(stage)}/{TotalSteps}] {Label(stage)}… {FormatElapsed(elapsed)}"));
    }

    /// <summary>
    /// Renders one throttled progress sample.
    /// </summary>
    /// <param name="progress">The progress event.</param>
    public void Progress(BuildProgress progress)
    {
        if (quiet)
        {
            return;
        }

        var eta = progress.Completed >= progress.Total
            ? 0
            : etaEstimator.Observe(progress.Elapsed, progress.Completed, progress.Total);
        var percent = progress.Total == 0 ? 0 : (progress.Completed * 10) / progress.Total;
        var bar = new string('#', percent) + new string('-', 10 - percent);
        var etaText = eta is null
            ? string.Empty
            : string.Create(
                CultureInfo.InvariantCulture,
                $" (eta {eta.Value.ToString("0.0", CultureInfo.InvariantCulture)} s)");
        writer.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"[{Step(progress.Stage)}/{TotalSteps}] {Label(progress.Stage)}… [{bar}] " +
            $"{progress.Completed}/{progress.Total}{etaText}"));
    }

    /// <summary>
    /// Renders one diagnostic; <c>--quiet</c> does not suppress diagnostics.
    /// </summary>
    /// <param name="diagnostic">The diagnostic.</param>
    public void Diagnostic(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        var hint = string.IsNullOrWhiteSpace(diagnostic.Hint) ? string.Empty : $" Hint: {diagnostic.Hint}";
        writer.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{CliDiagnosticFactory.SeverityName(diagnostic.Severity).ToUpperInvariant()}  {diagnostic.Code}  " +
            $"{diagnostic.Message}{hint}"));
    }

    /// <summary>
    /// Renders the caller-composed preview step.
    /// </summary>
    /// <param name="elapsed">The preview evaluation time.</param>
    public void PreviewCompleted(TimeSpan elapsed)
    {
        if (quiet)
        {
            return;
        }

        writer.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"[8/{TotalSteps}] Previewing configurations… {FormatElapsed(elapsed)}"));
    }

    /// <summary>
    /// Renders the caller-composed verification step and its diagnostics.
    /// </summary>
    /// <param name="report">The verification report.</param>
    /// <param name="elapsed">The verification wall time.</param>
    public void Verified(PackageVerificationReport report, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(report);
        foreach (var diagnostic in report.Diagnostics)
        {
            Diagnostic(diagnostic);
        }

        if (quiet)
        {
            return;
        }

        var status = report.IsValid ? "passed" : "failed";
        writer.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"[10/{TotalSteps}] Verifying package… {status}, {report.Checks.Length} check(s) " +
            $"{FormatElapsed(elapsed)}"));
    }

    private static int Step(BuildStage stage) => stage switch
    {
        BuildStage.Load => 1,
        BuildStage.Select => 2,
        BuildStage.Validate => 3,
        BuildStage.Compile => 4,
        BuildStage.ValidateBuild => 5,
        BuildStage.Parameterize => 6,
        BuildStage.ExpandJobs => 7,
        BuildStage.GenerateAndWriteJobs => 8,
        BuildStage.Publish => 9,
        _ => 0,
    };

    private static string Label(BuildStage stage) => stage switch
    {
        BuildStage.Load => "Loading project",
        BuildStage.Select => "Selecting build recipe",
        BuildStage.Validate => "Validating project",
        BuildStage.Compile => "Compiling model",
        BuildStage.ValidateBuild => "Validating job templates",
        BuildStage.Parameterize => "Parameterizing",
        BuildStage.ExpandJobs => "Expanding jobs",
        BuildStage.GenerateAndWriteJobs => "Generating and writing jobs",
        BuildStage.Publish => "Publishing",
        _ => stage.ToString(),
    };

    private static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalMilliseconds >= 1000
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"done ({elapsed.TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture)} s)")
            : string.Create(
                CultureInfo.InvariantCulture,
                $"done ({elapsed.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)} ms)");
}
