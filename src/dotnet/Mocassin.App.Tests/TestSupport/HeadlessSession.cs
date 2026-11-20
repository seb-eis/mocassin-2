// SPDX-License-Identifier: MIT

using Avalonia.Headless;

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// Owns the single Avalonia headless session for the test assembly and dispatches test bodies onto its UI thread.
/// </summary>
/// <remarks>
/// The project uses xunit 2, while the Avalonia 12 <c>Avalonia.Headless.XUnit</c> package requires xunit v3. The
/// first-party <see cref="HeadlessUnitTestSession"/> from <c>Avalonia.Headless</c> provides the same UI-thread
/// dispatch without a test-framework change.
/// </remarks>
internal static class HeadlessSession
{
    private static readonly Lazy<HeadlessUnitTestSession> session =
        new(() => HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder)));

    /// <summary>
    /// Dispatches an action onto the headless UI thread.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <returns>A task that completes when the action has run.</returns>
    public static Task DispatchAsync(Action action) =>
        session.Value.Dispatch(
            () =>
            {
                action();
                return Task.CompletedTask;
            },
            CancellationToken.None);

    /// <summary>
    /// Dispatches an asynchronous action onto the headless UI thread.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <returns>A task that completes when the action has run.</returns>
    public static Task DispatchAsync(Func<Task> action) => session.Value.Dispatch(action, CancellationToken.None);
}
