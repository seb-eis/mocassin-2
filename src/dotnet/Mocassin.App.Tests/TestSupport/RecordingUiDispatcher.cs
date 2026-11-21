// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// UI-dispatcher boundary recorder that runs actions inline and counts the marshalled updates.
/// </summary>
internal sealed class RecordingUiDispatcher : IUiDispatcher
{
    private readonly List<string> actions = [];

    /// <summary>
    /// Gets the number of marshalled actions.
    /// </summary>
    public int InvokeCount => actions.Count;

    /// <inheritdoc />
    public bool IsOnUiThread => false;

    /// <inheritdoc />
    public void Post(Action action)
    {
        actions.Add(nameof(Post));
        action();
    }

    /// <inheritdoc />
    public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        actions.Add(nameof(InvokeAsync));
        action();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<T> InvokeAsync<T>(Func<T> function, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        actions.Add(nameof(InvokeAsync));
        return Task.FromResult(function());
    }
}
