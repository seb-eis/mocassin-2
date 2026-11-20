// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// Inline UI dispatcher for view-model and boundary tests.
/// </summary>
internal sealed class InlineUiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public bool IsOnUiThread => true;

    /// <inheritdoc />
    public void Post(Action action) => action();

    /// <inheritdoc />
    public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        action();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<T> InvokeAsync<T>(Func<T> function, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(function());
    }
}
