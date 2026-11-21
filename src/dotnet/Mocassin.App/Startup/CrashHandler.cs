// SPDX-License-Identifier: MIT

using System.Globalization;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Mocassin.App.Composition;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Recovery;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Startup;

/// <summary>
/// Global exception boundary. It logs every unhandled failure and reports it through the crash dialog or a crash
/// report when the UI thread is no longer available. Recovery writing belongs to Step 23B.
/// </summary>
internal sealed partial class CrashHandler
{
    private static int crashReportSequence;
    private readonly ILogger<CrashHandler> logger;
    private readonly IDialogService dialogs;
    private readonly IShellLifetime lifetime;
    private readonly IUiDispatcher dispatcher;
    private readonly AppPaths paths;
    private readonly TimeProvider timeProvider;
    private readonly ICrashRecoveryWriter? recoveryWriter;

    /// <summary>
    /// Initializes a new crash handler.
    /// </summary>
    /// <param name="logger">The crash logger.</param>
    /// <param name="dialogs">The dialog service.</param>
    /// <param name="lifetime">The application lifetime.</param>
    /// <param name="dispatcher">The UI dispatcher.</param>
    /// <param name="paths">The application-data paths.</param>
    /// <param name="timeProvider">The time provider used for crash-report names.</param>
    /// <param name="recoveryWriter">The optional bounded crash-recovery writer.</param>
    public CrashHandler(
        ILogger<CrashHandler> logger,
        IDialogService dialogs,
        IShellLifetime lifetime,
        IUiDispatcher dispatcher,
        AppPaths paths,
        TimeProvider timeProvider,
        ICrashRecoveryWriter? recoveryWriter = null)
    {
        this.logger = logger;
        this.dialogs = dialogs;
        this.lifetime = lifetime;
        this.dispatcher = dispatcher;
        this.paths = paths;
        this.timeProvider = timeProvider;
        this.recoveryWriter = recoveryWriter;
    }

    /// <summary>
    /// Installs the process-wide handlers.
    /// </summary>
    public void Install()
    {
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandled;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandled;
    }

    /// <summary>
    /// Logs the exception, shows the crash dialog, and exits with code 1.
    /// </summary>
    /// <param name="exception">The unhandled exception.</param>
    /// <param name="source">The handler that observed the exception.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes after the dialog has been shown.</returns>
    public async Task ReportAsync(
        Exception exception,
        string source,
        CancellationToken cancellationToken = default)
    {
        LogUnhandled(logger, exception, source);
        var snapshots = recoveryWriter?.CaptureDirtySessions() ?? [];
        try
        {
            await dialogs.ShowCrashAsync(
                new CrashDialogRequest(
                    "MOCASSIN encountered an unexpected error and must close.",
                    paths.LogsDirectory,
                    exception.ToString()),
                snapshots,
                cancellationToken);
        }
#pragma warning disable CA1031 // The crash boundary must not throw while reporting a crash.
        catch (Exception dialogException)
#pragma warning restore CA1031
        {
            LogDialogFailed(logger, dialogException);
            WriteCrashReport(exception, source);
        }

        lifetime.Shutdown(1);
    }

    /// <summary>
    /// Logs the exception, writes a crash report, and exits with code 1 when the UI thread cannot report it.
    /// </summary>
    /// <param name="exception">The unhandled exception.</param>
    /// <param name="source">The handler that observed the exception.</param>
    public void ReportFatal(Exception exception, string source)
    {
        LogUnhandled(logger, exception, source);
        var reportPath = WriteCrashReport(exception, source);
        Console.Error.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"MOCASSIN terminated after an unhandled exception. Crash report: {reportPath}"));
        lifetime.Shutdown(1);
    }

    /// <summary>
    /// Writes a crash report next to the log files.
    /// </summary>
    /// <param name="exception">The unhandled exception.</param>
    /// <param name="source">The handler that observed the exception.</param>
    /// <returns>The crash-report path.</returns>
    public string WriteCrashReport(Exception exception, string source)
    {
        paths.EnsureDirectories();
        var timestamp = timeProvider.GetUtcNow();
        var sequence = Interlocked.Increment(ref crashReportSequence);
        var path = Path.Combine(
            paths.LogsDirectory,
            string.Create(
                CultureInfo.InvariantCulture,
                $"crash-{timestamp:yyyyMMddTHHmmssfffZ}-{sequence}.txt"));
        var text = string.Create(
            CultureInfo.InvariantCulture,
            $"MOCASSIN crash report{Environment.NewLine}"
            + $"utc: {timestamp:O}{Environment.NewLine}"
            + $"source: {source}{Environment.NewLine}"
            + $"exception: {exception}{Environment.NewLine}");
        File.WriteAllText(path, text);
        return path;
    }

    private void OnAppDomainUnhandled(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            ReportFatal(exception, "AppDomain.UnhandledException");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogUnobservedTask(logger, e.Exception);
        e.SetObserved();
    }

    private void OnDispatcherUnhandled(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        if (dispatcher.IsOnUiThread)
        {
            _ = ReportAsync(e.Exception, "Dispatcher.UIThread.UnhandledException");
            return;
        }

        ReportFatal(e.Exception, "Dispatcher.UIThread.UnhandledException");
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Critical, Message = "Unhandled exception from {Source}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string source);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "The crash dialog could not be shown.")]
    private static partial void LogDialogFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Unobserved task exception")]
    private static partial void LogUnobservedTask(ILogger logger, Exception exception);
}
