// SPDX-License-Identifier: MIT

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// Time provider with explicit control over the current UTC time and its timers.
/// </summary>
/// <remarks>
/// <see cref="Task.Delay(TimeSpan, TimeProvider, CancellationToken)"/> and the validation coordinator's debounce
/// use <see cref="CreateTimer"/>; the timers fire only when <see cref="Advance"/> is called. This keeps timer
/// tests deterministic without sleeping.
/// </remarks>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly Lock gate = new();
    private readonly List<ManualTimer> timers = [];
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

    /// <inheritdoc />
    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var timer = new ManualTimer(this, callback, state, dueTime, period);
        lock (gate)
        {
            timers.Add(timer);
        }

        return timer;
    }

    /// <summary>
    /// Advances the clock and fires every due timer.
    /// </summary>
    /// <param name="duration">The duration to advance.</param>
    public void Advance(TimeSpan duration)
    {
        utcNow += duration;
        ManualTimer[] due;
        lock (gate)
        {
            due = [.. timers];
        }

        foreach (var timer in due)
        {
            timer.Fire();
        }
    }

    private void Remove(ManualTimer timer)
    {
        lock (gate)
        {
            timers.Remove(timer);
        }
    }

    private sealed class ManualTimer : ITimer
    {
        private readonly ManualTimeProvider owner;
        private readonly TimerCallback callback;
        private readonly object? state;
        private DateTimeOffset dueAt;
        private TimeSpan period;
        private bool disposed;

        public ManualTimer(
            ManualTimeProvider owner,
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period)
        {
            this.owner = owner;
            this.callback = callback;
            this.state = state;
            this.period = period;
            dueAt = dueTime == Timeout.InfiniteTimeSpan
                ? DateTimeOffset.MaxValue
                : owner.GetUtcNow() + dueTime;
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            if (disposed)
            {
                return false;
            }

            this.period = period;
            dueAt = dueTime == Timeout.InfiniteTimeSpan
                ? DateTimeOffset.MaxValue
                : owner.GetUtcNow() + dueTime;
            return true;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            owner.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        public void Fire()
        {
            if (disposed || owner.GetUtcNow() < dueAt)
            {
                return;
            }

            if (period == Timeout.InfiniteTimeSpan)
            {
                Dispose();
                callback(state);
                return;
            }

            while (!disposed && owner.GetUtcNow() >= dueAt)
            {
                dueAt += period;
                callback(state);
            }
        }
    }
}
