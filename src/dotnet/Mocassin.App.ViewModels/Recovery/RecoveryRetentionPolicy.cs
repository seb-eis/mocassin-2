// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Recovery;

/// <summary>
/// The seven-day retention policy for recovery snapshots. Autosave sidecars are never swept.
/// </summary>
internal static class RecoveryRetentionPolicy
{
    /// <summary>
    /// The recovery retention duration.
    /// </summary>
    public static TimeSpan Retention { get; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Determines whether a snapshot is older than the retention.
    /// </summary>
    /// <param name="savedUtc">The snapshot write time.</param>
    /// <param name="now">The current UTC time.</param>
    /// <param name="retention">The optional retention override.</param>
    /// <returns><see langword="true"/> when the snapshot has expired.</returns>
    public static bool IsExpired(DateTimeOffset savedUtc, DateTimeOffset now, TimeSpan? retention = null) =>
        now - savedUtc > (retention ?? Retention);
}
