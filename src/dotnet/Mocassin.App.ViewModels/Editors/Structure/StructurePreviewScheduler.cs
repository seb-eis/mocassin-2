// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.ViewModels.Editors.Structure;

/// <summary>
/// Debounces structure-preview computations and discards a superseded result. The timer is driven by the injected
/// <see cref="TimeProvider"/>, so tests advance the clock instead of sleeping; the apply callback is marshalled
/// through <see cref="IUiDispatcher"/>.
/// </summary>
internal sealed class StructurePreviewScheduler : IDisposable
{
    private readonly TimeProvider timeProvider;
    private readonly IUiDispatcher dispatcher;
    private readonly TimeSpan delay;
    private ITimer? timer;
    private long version;
    private bool disposed;

    /// <summary>
    /// Initializes a scheduler.
    /// </summary>
    /// <param name="timeProvider">The clock that drives the debounce.</param>
    /// <param name="dispatcher">The UI dispatcher for the apply callback.</param>
    /// <param name="delay">The debounce delay; defaults to 250 ms.</param>
    public StructurePreviewScheduler(
        TimeProvider timeProvider,
        IUiDispatcher dispatcher,
        TimeSpan? delay = null)
    {
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        this.delay = delay ?? TimeSpan.FromMilliseconds(250);
    }

    /// <summary>
    /// Schedules one computation, cancelling the pending one.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="compute">The off-UI computation.</param>
    /// <param name="apply">The UI apply callback, invoked only for the newest computation.</param>
    public void Schedule<T>(Func<T> compute, Action<T> apply)
    {
        ArgumentNullException.ThrowIfNull(compute);
        ArgumentNullException.ThrowIfNull(apply);
        if (disposed)
        {
            return;
        }

        var token = Interlocked.Increment(ref version);
        timer?.Dispose();
        timer = timeProvider.CreateTimer(
            _ => Run(token, compute, apply),
            null,
            delay,
            Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Gets the debounce delay.
    /// </summary>
    public TimeSpan Delay => delay;

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Interlocked.Increment(ref version);
        timer?.Dispose();
        timer = null;
    }

    private void Run<T>(long token, Func<T> compute, Action<T> apply)
    {
        if (disposed || token != Volatile.Read(ref version))
        {
            return;
        }

        var result = compute();
        dispatcher.Post(
            () =>
            {
                if (!disposed && token == Volatile.Read(ref version))
                {
                    apply(result);
                }
            });
    }
}
