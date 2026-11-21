// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Recovery;

/// <summary>
/// The outcome of one crash-recovery write.
/// </summary>
internal enum CrashRecoveryStatus
{
    /// <summary>
    /// The snapshot and manifest were written.
    /// </summary>
    Saved,

    /// <summary>
    /// The write failed.
    /// </summary>
    Failed,

    /// <summary>
    /// The write budget expired.
    /// </summary>
    TimedOut,
}

/// <summary>
/// The result of one captured session's crash write.
/// </summary>
/// <param name="Snapshot">The captured session.</param>
/// <param name="Status">The outcome.</param>
/// <param name="Message">The optional failure message.</param>
internal sealed record CrashRecoveryResult(
    CrashRecoverySnapshot Snapshot,
    CrashRecoveryStatus Status,
    string? Message = null);
