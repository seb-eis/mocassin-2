// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Covers the compiler boundary, cancellation, the fail-closed default pipeline, and stage sequencing.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ModelCompilerTests
{
    [Fact]
    public void NullProjectIsAProgrammerError()
    {
        var compiler = new ModelCompiler();

        Assert.Throws<ArgumentNullException>(() => compiler.Compile(null!));
    }

    [Fact]
    public void CancelledTokenThrowsAndReturnsNoModel()
    {
        var compiler = new ModelCompiler();
        var project = CompilerTestSupport.CreateProject();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => compiler.Compile(project, cancellationToken: cancellation.Token));
    }

    [Fact]
    public void DefaultPipelineFailsClosedAtTheFirstPlaceholderStage()
    {
        var compiler = new ModelCompiler();
        var project = CompilerTestSupport.CreateProject();

        var result = compiler.Compile(project);

        Assert.Equal(CompilationStatus.Failed, result.Status);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Model);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("CMP001", diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Resolve", diagnostic.Message, StringComparison.Ordinal);

        AssertAllStagesReported(result.Report, expectedFailedStage: CompilationStage.Resolve, expectedDiagnosticCount: 1);
        Assert.Null(result.Report.ModelSignature);
        Assert.Equal("symmetry-1", result.Report.SymmetryContractVersion);
        Assert.Null(result.Report.SpaceGroupDataVersion);
        Assert.Null(result.Report.SpaceGroupDataSha256);
        Assert.Contains("'Resolve'", result.Report.Summary, StringComparison.Ordinal);
        Assert.Equal(0, result.Report.Cache.Hits);
        Assert.Equal(0, result.Report.Cache.Misses);
        Assert.Equal(0, result.Report.Cache.Capacity);
    }

    [Fact]
    public void FailedCompilationDoesNotMutateTheProject()
    {
        var compiler = new ModelCompiler();
        var project = CompilerTestSupport.CreateProject();
        var before = ProjectHasher.ComputeDocument(project);

        _ = compiler.Compile(project);

        Assert.Equal(before, ProjectHasher.ComputeDocument(project));
    }

    [Fact]
    public void CompilerInstanceIsReusableConcurrently()
    {
        var compiler = new ModelCompiler();
        var project = CompilerTestSupport.CreateProject();
        var failures = 0;

        Parallel.For(0, 8, _ =>
        {
            var result = compiler.Compile(project);
            if (result.Status != CompilationStatus.Failed || result.Model is not null)
            {
                Interlocked.Increment(ref failures);
            }
        });

        Assert.Equal(0, failures);
    }

    [Fact]
    public void SuccessfulPipelineReturnsTheSignedModelAndReport()
    {
        var model = CompilerTestSupport.CreateModel();
        var compiler = new ModelCompiler(TestStages.Succeeding(model));
        var project = CompilerTestSupport.CreateProject();

        var result = compiler.Compile(project);

        Assert.Equal(CompilationStatus.Succeeded, result.Status);
        Assert.True(result.IsSuccess);
        Assert.Same(model, result.Model);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(CompilationStageStatus.Completed, result.Report.Stages[^1].Status);
        Assert.Equal(CompilationStage.Report, result.Report.Stages[^1].Stage);
        Assert.All(result.Report.Stages, stage => Assert.Equal(CompilationStageStatus.Completed, stage.Status));
        Assert.Equal(model.ModelSignature, result.Report.ModelSignature);
        Assert.Equal(model.SpaceGroupDataVersion, result.Report.SpaceGroupDataVersion);
        Assert.Equal(model.SpaceGroupDataSha256, result.Report.SpaceGroupDataSha256);
        Assert.Contains("Compilation succeeded", result.Report.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void WarningOnlyStagesStillSucceed()
    {
        var model = CompilerTestSupport.CreateModel();
        var warning = CompilerTestSupport.CreateWarning();
        var compiler = new ModelCompiler(TestStages.Create((stage, context, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stage == CompilationStage.Structure)
            {
                return new CompilationStageOutcome(context, ImmutableArray.Create(warning));
            }

            return stage == CompilationStage.Signing
                ? new CompilationStageOutcome(context, ImmutableArray<Diagnostic>.Empty, model)
                : CompilationStageOutcome.Completed(context);
        }));
        var project = CompilerTestSupport.CreateProject();

        var result = compiler.Compile(project);

        Assert.True(result.IsSuccess);
        Assert.Same(model, result.Model);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(1, result.Report.Stages[(int)CompilationStage.Structure].DiagnosticCount);
        Assert.Contains("0 errors, 1 warning", result.Report.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void FailureStopsThePipelineAndSkipsLaterContentStages()
    {
        var diagnostic = CompilerTestSupport.CreateError();
        var compiler = new ModelCompiler(TestStages.FailingAt(CompilationStage.Clusters, diagnostic));
        var project = CompilerTestSupport.CreateProject();

        var result = compiler.Compile(project);

        Assert.Equal(CompilationStatus.Failed, result.Status);
        AssertAllStagesReported(result.Report, expectedFailedStage: CompilationStage.Clusters, expectedDiagnosticCount: 1);
    }

    [Fact]
    public void CancellationBetweenStagesThrowsAndProducesNoResult()
    {
        var model = CompilerTestSupport.CreateModel();
        using var cancellation = new CancellationTokenSource();
        var compiler = new ModelCompiler(TestStages.Create((stage, context, cancellationToken) =>
        {
            if (stage == CompilationStage.Resolve)
            {
                cancellation.Cancel();
                return CompilationStageOutcome.Completed(context);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return stage == CompilationStage.Signing
                ? new CompilationStageOutcome(context, ImmutableArray<Diagnostic>.Empty, model)
                : CompilationStageOutcome.Completed(context);
        }));
        var project = CompilerTestSupport.CreateProject();

        Assert.Throws<OperationCanceledException>(
            () => compiler.Compile(project, cancellationToken: cancellation.Token));
        Assert.True(cancellation.IsCancellationRequested);
    }

    [Fact]
    public void AModelBeforeSigningIsAContractViolation()
    {
        var compiler = new ModelCompiler(TestStages.Create((stage, context, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return stage == CompilationStage.Structure
                ? new CompilationStageOutcome(
                    context,
                    ImmutableArray<Diagnostic>.Empty,
                    CompilerTestSupport.CreateModel())
                : CompilationStageOutcome.Completed(context);
        }));
        var project = CompilerTestSupport.CreateProject();

        Assert.Throws<InvalidOperationException>(() => compiler.Compile(project));
    }

    [Fact]
    public void ASuccessfulPipelineWithoutASignedModelIsAContractViolation()
    {
        var compiler = new ModelCompiler(TestStages.Succeeding(model: null));
        var project = CompilerTestSupport.CreateProject();

        Assert.Throws<InvalidOperationException>(() => compiler.Compile(project));
    }

    [Fact]
    public void IncompletePipelineIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new ModelCompiler(ImmutableArray<ICompilationStage>.Empty));
    }

    [Fact]
    public void OutOfOrderPipelineIsRejected()
    {
        var stages = TestStages.Succeeding(CompilerTestSupport.CreateModel());
        var reordered = stages.SetItem(0, stages[1]).SetItem(1, stages[0]);

        Assert.Throws<ArgumentException>(() => new ModelCompiler(reordered));
    }

    [Fact]
    public void DuplicateDiagnosticsAcrossStagesAreReportedOnce()
    {
        var diagnostic = CompilerTestSupport.CreateError();
        var compiler = new ModelCompiler(TestStages.Create((stage, context, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stage is CompilationStage.Structure or CompilationStage.Environments)
            {
                return new CompilationStageOutcome(context, ImmutableArray.Create(diagnostic));
            }

            return CompilationStageOutcome.Completed(context);
        }));
        var project = CompilerTestSupport.CreateProject();

        var result = compiler.Compile(project);

        Assert.Equal(CompilationStatus.Failed, result.Status);
        Assert.Single(result.Diagnostics);
        Assert.Equal(1, result.Report.Stages[(int)CompilationStage.Structure].DiagnosticCount);
        Assert.Equal(0, result.Report.Stages[(int)CompilationStage.Environments].DiagnosticCount);
    }

    private static void AssertAllStagesReported(
        CompilationReport report,
        CompilationStage expectedFailedStage,
        int expectedDiagnosticCount)
    {
        Assert.Equal(10, report.Stages.Length);
        for (var index = 0; index < 9; index++)
        {
            var stage = report.Stages[index];
            Assert.Equal((CompilationStage)index, stage.Stage);
            var expected = index < (int)expectedFailedStage
                ? CompilationStageStatus.Completed
                : index == (int)expectedFailedStage
                    ? CompilationStageStatus.Failed
                    : CompilationStageStatus.Skipped;
            Assert.Equal(expected, stage.Status);
            Assert.Equal(
                index == (int)expectedFailedStage ? expectedDiagnosticCount : 0,
                stage.DiagnosticCount);
            Assert.True(stage.Elapsed >= TimeSpan.Zero);
        }

        var reportStage = report.Stages[9];
        Assert.Equal(CompilationStage.Report, reportStage.Stage);
        Assert.Equal(CompilationStageStatus.Completed, reportStage.Status);
        Assert.Equal(0, reportStage.DiagnosticCount);
        Assert.Contains(
            string.Create(CultureInfo.InvariantCulture, $"stage '{expectedFailedStage}'"),
            report.Summary,
            StringComparison.Ordinal);
    }
}
