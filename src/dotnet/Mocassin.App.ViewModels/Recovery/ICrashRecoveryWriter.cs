// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Recovery;

/// <summary>
/// Captures dirty sessions on the UI thread and writes their crash snapshots with a bounded budget.
/// </summary>
internal interface ICrashRecoveryWriter
{
    /// <summary>
    /// Captures the dirty writable sessions; must be called on the UI thread.
    /// </summary>
    /// <returns>The captured snapshots.</returns>
    public IReadOnlyList<CrashRecoverySnapshot> CaptureDirtySessions();

    /// <summary>
    /// Writes every captured snapshot within the configured budget.
    /// </summary>
    /// <param name="snapshots">The captured snapshots.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The per-session results.</returns>
    public Task<IReadOnlyList<CrashRecoveryResult>> WriteAsync(
        IReadOnlyList<CrashRecoverySnapshot> snapshots,
        CancellationToken cancellationToken = default);
}
