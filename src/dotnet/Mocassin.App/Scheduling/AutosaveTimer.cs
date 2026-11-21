// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Scheduling;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Scheduling;

/// <summary>
/// Drives the autosave scheduler from one <see cref="TimeProvider"/> timer on the UI dispatcher.
/// </summary>
internal sealed class AutosaveTimer : IDisposable
{
    private static readonly TimeSpan tick = TimeSpan.FromSeconds(30);
    private readonly AutosaveScheduler scheduler;
    private readonly TimeProvider timeProvider;
    private readonly IUiDispatcher dispatcher;
    private ITimer? timer;
    private bool disposed;

    /// <summary>
    /// Initializes the timer.
    /// </summary>
    /// <param name="scheduler">The autosave scheduler.</param>
    /// <param name="timeProvider">The clock that owns the timer.</param>
    /// <param name="dispatcher">The UI dispatcher.</param>
    public AutosaveTimer(
        AutosaveScheduler scheduler,
        TimeProvider timeProvider,
        IUiDispatcher dispatcher)
    {
        this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <summary>
    /// Starts the periodic timer.
    /// </summary>
    public void Start()
    {
        if (disposed || timer is not null)
        {
            return;
        }

        timer = timeProvider.CreateTimer(OnTick, null, tick, tick);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        timer?.Dispose();
        timer = null;
    }

    private void OnTick(object? state)
    {
        if (disposed)
        {
            return;
        }

        dispatcher.Post(() => _ = scheduler.RunOnceAsync(timeProvider.GetUtcNow()));
    }
}
