// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.SimulationPackage;

namespace Mocassin.Packages.Build.Internal;

/// <summary>
/// Generates selected job lattices in parallel and writes them through one serialized writer.
/// </summary>
/// <remarks>
/// A bounded reorder window of <c>max(2, 2 × parallel)</c> descriptors keeps memory proportional to the
/// concurrency. Generation runs on the thread pool with at most <c>parallel</c> concurrent slots; only this loop
/// calls <see cref="ISimulationPackageWriter.WriteJobAsync"/>, in ascending <see cref="JobDescriptor.JobId"/>
/// order. The written rows are therefore independent of completion order.
/// </remarks>
internal static class BuildScheduler
{
    /// <summary>
    /// Generates and writes every selected job.
    /// </summary>
    /// <param name="project">The loaded project.</param>
    /// <param name="model">The compiled model.</param>
    /// <param name="jobs">The selected descriptors in full-expansion order.</param>
    /// <param name="writer">The only package writer.</param>
    /// <param name="maxDegreeOfParallelism">The generation concurrency.</param>
    /// <param name="progress">The progress callback after each serialized write.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The scheduler outcome.</returns>
    public static async Task<BuildSchedulerResult> RunAsync(
        Project project,
        CompiledModel model,
        ImmutableArray<JobDescriptor> jobs,
        ISimulationPackageWriter writer,
        int maxDegreeOfParallelism,
        Func<int, string, ValueTask> progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(progress);

        var window = Math.Max(2, 2 * maxDegreeOfParallelism);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var slots = new SemaphoreSlim(maxDegreeOfParallelism);
        var pending = new Dictionary<int, Task<SupercellGenerationResult>>(window);
        var nextToStart = 0;
        var nextToWrite = 0;
        try
        {
            while (nextToWrite < jobs.Length)
            {
                linked.Token.ThrowIfCancellationRequested();
                while (nextToStart < jobs.Length && pending.Count < window)
                {
                    var descriptor = jobs[nextToStart++];
                    pending[descriptor.JobId] = Task.Run(
                        () => GenerateAsync(project, model, descriptor, slots, linked.Token),
                        linked.Token);
                }

                var expected = jobs[nextToWrite];
                SupercellGenerationResult generation;
                try
                {
                    generation = await pending[expected.JobId].ConfigureAwait(false);
                }
                finally
                {
                    pending.Remove(expected.JobId);
                }

                if (!generation.IsSuccess)
                {
                    linked.Cancel();
                    return new BuildSchedulerResult(
                        false,
                        BuildDiagnosticMerger.Merge(generation.Diagnostics),
                        Completed: nextToWrite);
                }

                await writer.WriteJobAsync(
                        new JobPackage(expected, generation.Lattice!),
                        linked.Token)
                    .ConfigureAwait(false);
                nextToWrite++;
                await progress(nextToWrite, $"job-{expected.JobId:D5}").ConfigureAwait(false);
            }

            return new BuildSchedulerResult(true, ImmutableArray<Diagnostic>.Empty, nextToWrite);
        }
        finally
        {
            linked.Cancel();
            await DrainAsync(pending.Values).ConfigureAwait(false);
        }
    }

    private static async Task<SupercellGenerationResult> GenerateAsync(
        Project project,
        CompiledModel model,
        JobDescriptor descriptor,
        SemaphoreSlim slots,
        CancellationToken cancellationToken)
    {
        await slots.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return SupercellGenerator.Generate(project, model, descriptor, cancellationToken);
        }
        finally
        {
            slots.Release();
        }
    }

    private static async Task DrainAsync(IEnumerable<Task<SupercellGenerationResult>> pending)
    {
        try
        {
            await Task.WhenAll(pending).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when the scheduler stops early.
        }
#pragma warning disable CA1031 // A later task's exception is irrelevant once the run has stopped.
        catch (Exception)
#pragma warning restore CA1031
        {
            // A failed generation is reported through the ordered write path.
        }
    }
}

/// <summary>
/// The outcome of the parallel generation and serialized write loop.
/// </summary>
/// <param name="IsSuccess">Whether every selected job was written.</param>
/// <param name="Diagnostics">The lattice-generation diagnostics on failure.</param>
/// <param name="Completed">The number of serialized job writes.</param>
internal sealed record BuildSchedulerResult(
    bool IsSuccess,
    ImmutableArray<Diagnostic> Diagnostics,
    int Completed);
