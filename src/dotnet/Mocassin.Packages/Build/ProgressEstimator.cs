// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Build;

/// <summary>
/// Throttles progress events to the configured interval and always emits the last progress of a stage.
/// </summary>
internal sealed class ProgressEstimator
{
    private readonly TimeProvider timeProvider;
    private readonly TimeSpan interval;
    private long lastTimestamp;

    /// <summary>
    /// Initializes the estimator.
    /// </summary>
    /// <param name="timeProvider">The clock.</param>
    /// <param name="interval">The minimum interval between two events.</param>
    public ProgressEstimator(TimeProvider timeProvider, TimeSpan interval)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        this.timeProvider = timeProvider;
        this.interval = interval;
        lastTimestamp = timeProvider.GetTimestamp();
    }

    /// <summary>
    /// Returns whether a progress event should be emitted now.
    /// </summary>
    /// <param name="isLast">Whether this is the last progress of the stage.</param>
    /// <returns><see langword="true"/> when the caller should emit.</returns>
    public bool ShouldEmit(bool isLast)
    {
        var now = timeProvider.GetTimestamp();
        if (!isLast && timeProvider.GetElapsedTime(lastTimestamp, now) < interval)
        {
            return false;
        }

        lastTimestamp = now;
        return true;
    }
}
