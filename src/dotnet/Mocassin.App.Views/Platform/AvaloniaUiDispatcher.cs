// SPDX-License-Identifier: MIT

using Avalonia.Threading;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Views.Platform;

/// <summary>
/// Avalonia implementation of the UI-thread dispatcher boundary.
/// </summary>
internal sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public bool IsOnUiThread => Dispatcher.UIThread.CheckAccess();

    /// <inheritdoc />
    public void Post(Action action) => Dispatcher.UIThread.Post(action);

    /// <inheritdoc />
    public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) =>
        Dispatcher.UIThread.InvokeAsync(action, DispatcherPriority.Normal, cancellationToken).GetTask();

    /// <inheritdoc />
    public Task<T> InvokeAsync<T>(Func<T> function, CancellationToken cancellationToken = default) =>
        Dispatcher.UIThread.InvokeAsync(function, DispatcherPriority.Normal, cancellationToken).GetTask();
}
