// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using Mocassin.App.Composition;
using Mocassin.App.Startup;
using Mocassin.App.Tests.TestSupport;

namespace Mocassin.App.Tests.Startup;

/// <summary>
/// Verifies the global crash boundary.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class CrashHandlerTests
{
    [Fact]
    public async Task AnUnhandledExceptionIsLoggedReportedAndExitsWithOne()
    {
        using var workspace = new TempWorkspace();
        var loggerProvider = new RecordingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(loggerProvider);
            builder.SetMinimumLevel(LogLevel.Trace);
        });
        var dialogs = new RecordingDialogService();
        var lifetime = new RecordingShellLifetime();
        var paths = new AppPaths(workspace.Path);
        var handler = new CrashHandler(
            loggerFactory.CreateLogger<CrashHandler>(),
            dialogs,
            lifetime,
            new InlineUiDispatcher(),
            paths,
            new ManualTimeProvider());

        await handler.ReportAsync(new InvalidOperationException("boom"), "unit-test");

        Assert.Contains(
            loggerProvider.Entries,
            entry => entry.Level == LogLevel.Critical && entry.Message.Contains("Unhandled exception", StringComparison.Ordinal));
        var crash = Assert.Single(dialogs.CrashRequests);
        Assert.Equal(paths.LogsDirectory, crash.LogFilePath);
        Assert.Equal(1, lifetime.ExitCode);
    }

    [Fact]
    public void AFatalExceptionWritesACrashReportAndExitsWithOne()
    {
        using var workspace = new TempWorkspace();
        using var loggerFactory = LoggerFactory.Create(builder => builder.ClearProviders());
        var lifetime = new RecordingShellLifetime();
        var paths = new AppPaths(workspace.Path);
        var handler = new CrashHandler(
            loggerFactory.CreateLogger<CrashHandler>(),
            new RecordingDialogService(),
            lifetime,
            new InlineUiDispatcher(),
            paths,
            new ManualTimeProvider());

        var reportPath = handler.WriteCrashReport(new InvalidOperationException("fatal"), "unit-test");

        Assert.True(File.Exists(reportPath));
        Assert.Contains("fatal", File.ReadAllText(reportPath), StringComparison.Ordinal);

        handler.ReportFatal(new InvalidOperationException("second"), "unit-test");

        Assert.Equal(1, lifetime.ExitCode);
        Assert.Equal(2, Directory.GetFiles(paths.LogsDirectory, "crash-*.txt").Length);
    }
}
