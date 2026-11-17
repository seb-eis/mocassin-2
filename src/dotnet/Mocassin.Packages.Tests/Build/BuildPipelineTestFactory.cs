// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Compiler;
using Mocassin.Packages.Build;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.Projects.Validation;
using Mocassin.Packages.SimulationPackage;

namespace Mocassin.Packages.Tests.Build;

/// <summary>
/// Creates a pipeline with real services and optional failure-injection seams at the writer and preview
/// boundaries.
/// </summary>
internal static class BuildPipelineTestFactory
{
    public static BuildPipeline Create(
        ILatticePreviewService? previewService = null,
        Func<PackageBuildRequest, PackageBuildOptions, ISimulationPackageWriter>? writerFactory = null)
    {
        var preview = previewService ?? new LatticePreviewService();
        var validation = new ProjectValidationPipeline(
            additionalStages: [new JobTemplateValidationStage(preview)]);
        return new BuildPipeline(
            preview,
            new ModelCompiler(),
            new ParameterizationService(),
            validation,
            writerFactory);
    }
}

/// <summary>
/// A clock that never advances, for deterministic throttling and timing tests.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset now;

    public FixedTimeProvider(DateTimeOffset now) => this.now = now;

    public override DateTimeOffset GetUtcNow() => now;

    public override long GetTimestamp() => 0;
}

/// <summary>
/// Wraps a real writer and runs an action after the first job write.
/// </summary>
internal sealed class HookedWriter : ISimulationPackageWriter
{
    private readonly ISimulationPackageWriter inner;
    private readonly Action afterFirstJobWrite;
    private bool jobWritten;

    public HookedWriter(ISimulationPackageWriter inner, Action afterFirstJobWrite)
    {
        this.inner = inner;
        this.afterFirstJobWrite = afterFirstJobWrite;
    }

    public Task WriteModelAsync(ParameterizedModel model, CancellationToken cancellationToken = default) =>
        inner.WriteModelAsync(model, cancellationToken);

    public async Task WriteJobAsync(JobPackage job, CancellationToken cancellationToken = default)
    {
        await inner.WriteJobAsync(job, cancellationToken);
        if (!jobWritten)
        {
            jobWritten = true;
            afterFirstJobWrite();
        }
    }

    public Task<PackageWriteResult> CompleteAsync(CancellationToken cancellationToken = default) =>
        inner.CompleteAsync(cancellationToken);

    public ValueTask DisposeAsync() => inner.DisposeAsync();
}

/// <summary>
/// A preview service that can be made to fail after the build-stage validation has completed.
/// </summary>
internal sealed class FailingPreviewService : ILatticePreviewService
{
    private readonly ILatticePreviewService inner;

    public FailingPreviewService(ILatticePreviewService inner) => this.inner = inner;

    public volatile bool FailNext;

    public LatticePreview? TryCreatePreview(
        Mocassin.Core.Project.Project project,
        CompiledModel model,
        Mocassin.Core.Model.Jobs.LatticeRequest request,
        System.Collections.Immutable.ImmutableArray<Mocassin.Core.Model.Jobs.DopingValue> dopings,
        out System.Collections.Immutable.ImmutableArray<Mocassin.Core.Diagnostics.Diagnostic> diagnostics)
    {
        if (FailNext)
        {
            diagnostics = [];
            return null;
        }

        return inner.TryCreatePreview(project, model, request, dopings, out diagnostics);
    }

    public JobCountPreview? TryCreateJobCountPreview(
        Mocassin.Core.Project.Project project,
        Mocassin.Core.Model.Jobs.JobTemplate jobTemplate,
        Mocassin.Core.Ids.SimulationId simulationId,
        out System.Collections.Immutable.ImmutableArray<Mocassin.Core.Diagnostics.Diagnostic> diagnostics) =>
        inner.TryCreateJobCountPreview(project, jobTemplate, simulationId, out diagnostics);
}
