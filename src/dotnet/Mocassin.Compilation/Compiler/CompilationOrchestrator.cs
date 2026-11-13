// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Mocassin.Compilation.Caching;
using Mocassin.Compilation.Context;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Contracts;

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// Executes the ordered compilation stages and assembles the result and report.
/// </summary>
internal static class CompilationOrchestrator
{
    private const string compilerVersion = "mocassin-compilation/1";

    /// <summary>
    /// Runs every stage in order, stopping at the first stage with an error diagnostic.
    /// </summary>
    /// <param name="stages">The validated content stages.</param>
    /// <param name="project">The validated project.</param>
    /// <param name="options">The effective options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The complete compilation result.</returns>
    /// <exception cref="OperationCanceledException">Compilation was cancelled.</exception>
    /// <exception cref="InvalidOperationException">A stage violated the compilation contract.</exception>
    public static CompilationResult Run(
        ImmutableArray<ICompilationStage> stages,
        Project project,
        CompilationOptions options,
        CancellationToken cancellationToken)
    {
        var context = new CompilationContext(project, options);
        var executions = new List<StageExecution>(stages.Length + 1);
        CompiledModel? model = null;
        CompilationStage? failedStage = null;

        foreach (var stage in stages)
        {
            if (failedStage is not null)
            {
                executions.Add(StageExecution.Skipped(stage.Stage));
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var started = Stopwatch.GetTimestamp();
            var outcome = stage.Execute(context, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"The compilation stage '{stage.Stage}' returned no outcome.");
            var elapsed = Stopwatch.GetElapsedTime(started);
            var diagnostics = outcome.Diagnostics.IsDefault
                ? ImmutableArray<Diagnostic>.Empty
                : outcome.Diagnostics;

            if (outcome.Context is null)
            {
                throw new InvalidOperationException(
                    $"The compilation stage '{stage.Stage}' returned no context.");
            }

            if (outcome.Model is not null)
            {
                if (stage.Stage != CompilationStage.Signing)
                {
                    throw new InvalidOperationException(
                        $"Only the signing stage may produce a compiled model; '{stage.Stage}' returned one.");
                }

                if (model is not null)
                {
                    throw new InvalidOperationException("The signing stage produced more than one compiled model.");
                }

                model = outcome.Model;
            }

            context = outcome.Context;
            var hasError = diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            executions.Add(new StageExecution(
                stage.Stage,
                diagnostics,
                hasError ? CompilationStageStatus.Failed : CompilationStageStatus.Completed,
                elapsed));
            if (hasError)
            {
                failedStage = stage.Stage;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (failedStage is not null)
        {
            model = null;
        }
        else if (model is null)
        {
            throw new InvalidOperationException(
                "The compilation pipeline completed without producing a signed compiled model.");
        }

        var reportStarted = Stopwatch.GetTimestamp();
        var finalized = FinalizeStages(executions);
        var status = failedStage is null ? CompilationStatus.Succeeded : CompilationStatus.Failed;
        var stageResults = finalized.Stages.Add(new CompilationStageResult(
            CompilationStage.Report,
            CompilationStageStatus.Completed,
            0,
            Stopwatch.GetElapsedTime(reportStarted)));
        var report = new CompilationReport(
            compilerVersion,
            model?.ModelSignature,
            SymmetryContract.Version,
            model?.SpaceGroupDataVersion,
            model?.SpaceGroupDataSha256,
            stageResults,
            CompilationCacheStatistics.Empty,
            BuildSummary(status, failedStage, finalized.Diagnostics));
        return new CompilationResult(status, model, finalized.Diagnostics, report);
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
            .Select((stage, index) => new CompilationStageResult(
                stage.Stage,
                stage.Status,
                counts[index],
                stage.Elapsed))
            .ToImmutableArray();
        return new FinalizedStages(diagnostics, results);
    }

    private static string BuildSummary(
        CompilationStatus status,
        CompilationStage? failedStage,
        ImmutableArray<Diagnostic> diagnostics)
    {
        var errors = diagnostics.Count(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var warnings = diagnostics.Count(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
        var counts = string.Create(
            CultureInfo.InvariantCulture,
            $"{FormatCount(errors, "error")}, {FormatCount(warnings, "warning")}");
        return status == CompilationStatus.Succeeded
            ? $"Compilation succeeded ({counts})."
            : $"Compilation failed at stage '{failedStage}' ({counts}).";
    }

    private static string FormatCount(int count, string singular) => count == 1
        ? string.Create(CultureInfo.InvariantCulture, $"1 {singular}")
        : string.Create(CultureInfo.InvariantCulture, $"{count} {singular}s");

    private sealed record StageExecution(
        CompilationStage Stage,
        ImmutableArray<Diagnostic> Diagnostics,
        CompilationStageStatus Status,
        TimeSpan Elapsed)
    {
        public static StageExecution Skipped(CompilationStage stage) =>
            new(stage, ImmutableArray<Diagnostic>.Empty, CompilationStageStatus.Skipped, TimeSpan.Zero);
    }

    private readonly record struct FinalizedStages(
        ImmutableArray<Diagnostic> Diagnostics,
        ImmutableArray<CompilationStageResult> Stages);
}
