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
    /// Dispatches an asynchronous action onto the headless UI thread and awaits its body.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <returns>A task that completes when the action's body has completed.</returns>
    /// <remarks>
    /// The delegate must be passed to the <c>Dispatch&lt;T&gt;(Func&lt;Task&lt;T&gt;&gt;)</c> overload. Passing it
    /// to the plain <c>Dispatch(Func&lt;Task&gt;)</c> path would expose a <see cref="Task{Task}"/> as a
    /// <see cref="Task"/> and silently drop the inner task's exceptions and assertions.
    /// </remarks>
    public static Task DispatchAsync(Func<Task> action) =>
        session.Value.Dispatch(
            async () =>
            {
                await action().ConfigureAwait(true);
                return true;
            },
            CancellationToken.None);

    /// <summary>
    /// Runs pending dispatcher jobs and forces a layout/render pass so freshly attached controls are realized.
    /// </summary>
    /// <param name="topLevel">The window whose visual tree is pumped.</param>
    public static void Pump(Avalonia.Controls.TopLevel topLevel)
    {
        for (var pass = 0; pass < 3; pass++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            using var frame = topLevel.CaptureRenderedFrame();
        }

        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
    }
}
