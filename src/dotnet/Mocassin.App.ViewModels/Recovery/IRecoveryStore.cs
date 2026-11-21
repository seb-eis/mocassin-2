// SPDX-License-Identifier: MIT

using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Recovery;

/// <summary>
/// Lists, writes, deletes, and sweeps the application-local recovery snapshots. It never prompts.
/// </summary>
internal interface IRecoveryStore
{
    /// <summary>
    /// Lists the valid recovery candidates in the recovery directory.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The candidates, newest first.</returns>
    public Task<IReadOnlyList<RecoveryCandidate>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a snapshot and its manifest.
    /// </summary>
    /// <param name="project">The immutable project snapshot.</param>
    /// <param name="projectPath">The main project path recorded in the manifest.</param>
    /// <param name="source">The snapshot source.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The write result.</returns>
    public Task<RecoveryWriteResult> WriteAsync(
        ProjectModel project,
        string projectPath,
        RecoverySource source,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes one candidate's snapshot and manifest.
    /// </summary>
    /// <param name="candidate">The candidate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when a file was deleted.</returns>
    public Task<bool> DeleteAsync(RecoveryCandidate candidate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes recovery files older than the retention.
    /// </summary>
    /// <param name="now">The current UTC time.</param>
    /// <param name="retention">The retention duration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of deleted candidates.</returns>
    public Task<int> SweepAsync(
        DateTimeOffset now,
        TimeSpan retention,
        CancellationToken cancellationToken = default);
}
