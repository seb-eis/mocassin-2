// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Build;

namespace Mocassin.Cli.Tests.Build;

/// <summary>
/// A scripted pipeline for command-boundary tests that need a specific result without running the real build.
/// </summary>
internal sealed class ScriptedBuildPipeline : IBuildPipeline
{
    private readonly Func<BuildRequest, BuildResult> factory;

    public ScriptedBuildPipeline(Func<BuildRequest, BuildResult> factory) => this.factory = factory;

    public BuildRequest? LastRequest { get; private set; }

    public async IAsyncEnumerable<BuildEvent> BuildAsync(
        BuildRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        yield return new BuildStarted(0, 0);
        yield return new BuildFinished(factory(request));
        await Task.CompletedTask;
    }

    public Task<BuildResult> BuildToCompletionAsync(
        BuildRequest request,
        CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Task.FromResult(factory(request));
    }

    /// <summary>
    /// Creates a failed result with no identity.
    /// </summary>
    /// <param name="failure">The failure kind.</param>
    /// <param name="diagnostics">The diagnostics.</param>
    /// <returns>The result.</returns>
    public static BuildResult Failure(
        BuildFailureKind failure,
        ImmutableArray<Diagnostic> diagnostics = default) =>
        new(
            BuildStatus.Failed,
            failure,
            BuildIdentity.Empty,
            diagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : diagnostics,
            Selection: null,
            JobSelection: null,
            Preview: null,
            Package: null,
            Timings: [],
            Elapsed: TimeSpan.Zero);

    /// <summary>
    /// Creates a cancelled result with no identity.
    /// </summary>
    /// <returns>The result.</returns>
    public static BuildResult Cancelled() =>
        new(
            BuildStatus.Cancelled,
            BuildFailureKind.None,
            BuildIdentity.Empty,
            [],
            Selection: null,
            JobSelection: null,
            Preview: null,
            Package: null,
            Timings: [],
            Elapsed: TimeSpan.Zero);
}
