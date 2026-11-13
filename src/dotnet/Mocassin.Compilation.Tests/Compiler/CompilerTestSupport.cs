// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Model;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Builds real projects and injectable stage sequences for the compiler orchestration tests.
/// </summary>
internal static class CompilerTestSupport
{
    /// <summary>
    /// Gets the fixed master seed used by test projects.
    /// </summary>
    public const string MasterSeed = "0000000000000000000000000000000000000000000000000000000000000000";

    /// <summary>
    /// Creates a minimal, structurally valid project value.
    /// </summary>
    /// <returns>The project snapshot.</returns>
    public static Project CreateProject() => new(
        schema: null,
        schemaVersion: ProjectSchema.CurrentVersion,
        metadata: new ProjectMetadata(
            Guid.Parse("9e4b9a41-0f6b-4b2c-84ef-0c6d4c9f6a01", CultureInfo.InvariantCulture),
            MasterSeed,
            "Compiler core test",
            Description: null,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch),
        model: ModelDefinition.Empty,
        parameterizations: [],
        jobTemplates: [],
        buildTemplates: [],
        viewSettings: ViewSettings.Default);

    /// <summary>
    /// Creates a compiled-model header for successful-stage tests.
    /// </summary>
    /// <param name="modelSignature">The signature to record.</param>
    /// <returns>The compiled model.</returns>
    public static CompiledModel CreateModel(string modelSignature = "test-model-signature") => new(
        CompiledModel.CurrentFormatVersion,
        modelSignature,
        "symmetry-1",
        "spglib-test/catalog-1",
        "0123456789abcdef");

    /// <summary>
    /// Creates a report for direct result-contract tests.
    /// </summary>
    /// <returns>The report.</returns>
    public static CompilationReport CreateReport() => new(
        "test-compiler/1",
        modelSignature: null,
        "symmetry-1",
        spaceGroupDataVersion: null,
        spaceGroupDataSha256: null,
        [],
        CompilationCacheStatistics.Empty,
        "Test report.");

    /// <summary>
    /// Creates an error diagnostic for injected stage failures.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic CreateError(string code = "CMP006", string message = "Injected failure.") =>
        new(code, DiagnosticSeverity.Error, message);

    /// <summary>
    /// Creates a warning diagnostic for injected stage warnings.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic CreateWarning(string code = "CMP010", string message = "Injected warning.") =>
        new(code, DiagnosticSeverity.Warning, message);
}

/// <summary>
/// Builds complete content-stage sequences with injected behavior.
/// </summary>
internal static class TestStages
{
    /// <summary>
    /// Creates a pipeline in which every stage completes and the signing stage returns the supplied model.
    /// </summary>
    /// <param name="model">The model returned by the signing stage; <see langword="null"/> returns none.</param>
    /// <returns>The content stages.</returns>
    public static ImmutableArray<ICompilationStage> Succeeding(CompiledModel? model = null) =>
        Create((stage, context, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return stage == CompilationStage.Signing && model is not null
                ? new CompilationStageOutcome(context, ImmutableArray<Diagnostic>.Empty, model)
                : CompilationStageOutcome.Completed(context);
        });

    /// <summary>
    /// Creates a pipeline that fails at one stage with the supplied error.
    /// </summary>
    /// <param name="failedStage">The stage that reports the error.</param>
    /// <param name="diagnostic">The error diagnostic.</param>
    /// <returns>The content stages.</returns>
    public static ImmutableArray<ICompilationStage> FailingAt(CompilationStage failedStage, Diagnostic diagnostic) =>
        Create((stage, context, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return stage == failedStage
                ? CompilationStageOutcome.Failed(context, diagnostic)
                : CompilationStageOutcome.Completed(context);
        });

    /// <summary>
    /// Creates a pipeline with one delegate per stage.
    /// </summary>
    /// <param name="execute">The stage behavior.</param>
    /// <returns>The content stages.</returns>
    public static ImmutableArray<ICompilationStage> Create(
        Func<CompilationStage, CompilationContext, CancellationToken, CompilationStageOutcome> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return
        [
            .. CompilationPipeline.ContentStages.Select(
                stage => (ICompilationStage)new StubCompilationStage(stage, execute)),
        ];
    }

    private sealed class StubCompilationStage : ICompilationStage
    {
        private readonly Func<CompilationStage, CompilationContext, CancellationToken, CompilationStageOutcome> execute;

        public StubCompilationStage(
            CompilationStage stage,
            Func<CompilationStage, CompilationContext, CancellationToken, CompilationStageOutcome> execute)
        {
            Stage = stage;
            this.execute = execute;
        }

        public CompilationStage Stage { get; }

        public CompilationStageOutcome Execute(CompilationContext context, CancellationToken cancellationToken) =>
            execute(Stage, context, cancellationToken);
    }
}
