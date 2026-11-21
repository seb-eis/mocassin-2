// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Recovery;

namespace Mocassin.App.Recovery;

/// <summary>
/// Captures dirty sessions and writes crash snapshots with a bounded asynchronous budget.
/// </summary>
internal sealed class CrashRecoveryWriter : ICrashRecoveryWriter
{
    private static readonly TimeSpan defaultBudget = TimeSpan.FromSeconds(2);
    private readonly IProjectSessionService sessions;
    private readonly IRecoveryStore recovery;
    private readonly TimeProvider timeProvider;
    private readonly TimeSpan budget;

    /// <summary>
    /// Initializes the crash writer.
    /// </summary>
    /// <param name="sessions">The project sessions.</param>
    /// <param name="recovery">The recovery store.</param>
    /// <param name="timeProvider">The clock for the budget delay.</param>
    /// <param name="budget">The optional budget override.</param>
    public CrashRecoveryWriter(
        IProjectSessionService sessions,
        IRecoveryStore recovery,
        TimeProvider timeProvider,
        TimeSpan? budget = null)
    {
        this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        this.recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.budget = budget ?? defaultBudget;
    }

    /// <inheritdoc />
    public IReadOnlyList<CrashRecoverySnapshot> CaptureDirtySessions() =>
        sessions.Sessions
            .Where(session => !session.IsReadOnly && session.IsDirty)
            .Select(session => new CrashRecoverySnapshot(session.Id, session.Path, session.Project))
            .ToArray();

    /// <inheritdoc />
    public async Task<IReadOnlyList<CrashRecoveryResult>> WriteAsync(
        IReadOnlyList<CrashRecoverySnapshot> snapshots,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        if (snapshots.Count == 0)
        {
            return [];
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var writes = snapshots
            .Select(snapshot => WriteOneAsync(snapshot, timeout.Token))
            .ToArray();
        var all = Task.WhenAll(writes);
        var delay = Task.Delay(budget, timeProvider, cancellationToken);
        var completed = await Task.WhenAny(all, delay).ConfigureAwait(false);
        if (completed == all)
        {
            return await all.ConfigureAwait(false);
        }

        timeout.Cancel();
        return writes
            .Select((write, index) => write.IsCompletedSuccessfully
                ? write.Result
                : new CrashRecoveryResult(snapshots[index], CrashRecoveryStatus.TimedOut))
            .ToArray();
    }

    private async Task<CrashRecoveryResult> WriteOneAsync(
        CrashRecoverySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await recovery.WriteAsync(
                    snapshot.Project,
                    snapshot.ProjectPath,
                    RecoverySource.Crash,
                    cancellationToken)
                .ConfigureAwait(false);
            return result.IsSuccess
                ? new CrashRecoveryResult(snapshot, CrashRecoveryStatus.Saved)
                : new CrashRecoveryResult(
                    snapshot,
                    CrashRecoveryStatus.Failed,
                    result.Diagnostics.FirstOrDefault()?.Message);
        }
        catch (OperationCanceledException)
        {
            return new CrashRecoveryResult(snapshot, CrashRecoveryStatus.TimedOut);
        }
    }
}
