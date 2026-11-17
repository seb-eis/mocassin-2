// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Build;

/// <summary>
/// Sequences project loading, selection, validation, compilation, parameterization, expansion, preview or
/// lattice generation, package writing, and publication.
/// </summary>
/// <remarks>
/// The pipeline is UI-independent: the CLI and Step 26's view models call the same implementation and receive the
/// same typed result and event stream. The library never chooses an exit code, prints to the console, or reads
/// <see cref="Environment.ExitCode"/>.
/// </remarks>
public interface IBuildPipeline
{
    /// <summary>
    /// Runs one build and streams its ordered events.
    /// </summary>
    /// <param name="request">The complete build request.</param>
    /// <param name="cancellationToken">Cancels the build at stage boundaries and between jobs.</param>
    /// <returns>The ordered event stream, ending with exactly one <see cref="BuildFinished"/>.</returns>
    public IAsyncEnumerable<BuildEvent> BuildAsync(
        BuildRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs one build and returns the final result.
    /// </summary>
    /// <param name="request">The complete build request.</param>
    /// <param name="cancellationToken">Cancels the build at stage boundaries and between jobs.</param>
    /// <returns>The authoritative build result.</returns>
    public Task<BuildResult> BuildToCompletionAsync(
        BuildRequest request,
        CancellationToken cancellationToken = default);
}
