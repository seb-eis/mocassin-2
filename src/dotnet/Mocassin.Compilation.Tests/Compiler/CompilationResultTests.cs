// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Pins the success/failure contract of <see cref="CompilationResult"/>.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class CompilationResultTests
{
    [Fact]
    public void SucceededResultRequiresAModel()
    {
        Assert.Throws<ArgumentException>(() => new CompilationResult(
            CompilationStatus.Succeeded,
            model: null,
            ImmutableArray<Diagnostic>.Empty,
            CompilerTestSupport.CreateReport()));
    }

    [Fact]
    public void FailedResultRejectsAModel()
    {
        Assert.Throws<ArgumentException>(() => new CompilationResult(
            CompilationStatus.Failed,
            CompilerTestSupport.CreateModel(),
            ImmutableArray.Create(CompilerTestSupport.CreateError()),
            CompilerTestSupport.CreateReport()));
    }

    [Fact]
    public void FailedResultRequiresAnErrorDiagnostic()
    {
        Assert.Throws<ArgumentException>(() => new CompilationResult(
            CompilationStatus.Failed,
            model: null,
            ImmutableArray.Create(CompilerTestSupport.CreateWarning()),
            CompilerTestSupport.CreateReport()));
    }

    [Fact]
    public void SucceededResultAllowsWarningDiagnostics()
    {
        var model = CompilerTestSupport.CreateModel();
        var result = new CompilationResult(
            CompilationStatus.Succeeded,
            model,
            ImmutableArray.Create(CompilerTestSupport.CreateWarning()),
            CompilerTestSupport.CreateReport());

        Assert.True(result.IsSuccess);
        Assert.Same(model, result.Model);
        Assert.Single(result.Diagnostics);
    }

    [Fact]
    public void FailedResultIsNeverSuccessful()
    {
        var result = new CompilationResult(
            CompilationStatus.Failed,
            model: null,
            ImmutableArray.Create(CompilerTestSupport.CreateError()),
            CompilerTestSupport.CreateReport());

        Assert.False(result.IsSuccess);
        Assert.Null(result.Model);
        Assert.Single(result.Diagnostics);
    }

    [Fact]
    public void UnsetDiagnosticsNormalizeToAnEmptyArray()
    {
        var result = new CompilationResult(
            CompilationStatus.Succeeded,
            CompilerTestSupport.CreateModel(),
            diagnostics: default,
            CompilerTestSupport.CreateReport());

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void NullReportIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new CompilationResult(
            CompilationStatus.Succeeded,
            CompilerTestSupport.CreateModel(),
            ImmutableArray<Diagnostic>.Empty,
            report: null!));
    }

    [Fact]
    public void UndefinedStatusIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompilationResult(
            (CompilationStatus)99,
            CompilerTestSupport.CreateModel(),
            ImmutableArray<Diagnostic>.Empty,
            CompilerTestSupport.CreateReport()));
    }
}
