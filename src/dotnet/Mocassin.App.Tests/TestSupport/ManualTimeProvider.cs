// SPDX-License-Identifier: MIT

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// Time provider with explicit control over the current UTC time.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset utcNow;

    /// <summary>
    /// Initializes a new manual time provider.
    /// </summary>
    /// <param name="start">The initial UTC time.</param>
    public ManualTimeProvider(DateTimeOffset? start = null)
    {
        utcNow = start ?? new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => utcNow;

    /// <summary>
    /// Advances the clock.
    /// </summary>
    /// <param name="duration">The duration to advance.</param>
    public void Advance(TimeSpan duration) => utcNow += duration;
}
