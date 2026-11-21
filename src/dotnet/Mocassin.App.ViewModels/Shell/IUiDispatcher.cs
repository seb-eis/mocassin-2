// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Abstracts the UI-thread dispatcher so that view models never reference Avalonia.
/// </summary>
internal interface IUiDispatcher
{
    /// <summary>
    /// Gets a value indicating whether the caller runs on the UI thread.
    /// </summary>
    public bool IsOnUiThread { get; }

    /// <summary>
    /// Queues an action on the UI thread without waiting for it.
    /// </summary>
    /// <param name="action">The action to run.</param>
    public void Post(Action action);

    /// <summary>
    /// Runs an action on the UI thread.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the action has run.</returns>
    public Task InvokeAsync(Action action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a function on the UI thread and returns its result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="function">The function to run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes with the function result.</returns>
    public Task<T> InvokeAsync<T>(Func<T> function, CancellationToken cancellationToken = default);
}
