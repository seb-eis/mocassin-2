// SPDX-License-Identifier: MIT

using System.Globalization;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Mocassin.App.Composition;

/// <summary>
/// Configures the file and console logging sinks.
/// </summary>
internal static class LoggingSetup
{
    /// <summary>
    /// Configures Serilog as the logging provider and enforces the shared log-file budget.
    /// </summary>
    /// <param name="logging">The logging builder.</param>
    /// <param name="paths">The application-data paths.</param>
    /// <param name="minimumLevel">The minimum log level.</param>
    public static void Configure(ILoggingBuilder logging, AppPaths paths, LogLevel minimumLevel)
    {
        paths.EnsureDirectories();
        var activeFileName = string.Create(
            CultureInfo.InvariantCulture,
            $"mocassin-{TimeProvider.System.GetLocalNow():yyyyMMdd}.log");
        LogRetention.Sweep(paths.LogsDirectory, activeFileName);

        var configuration = new LoggerConfiguration()
            .MinimumLevel.Is(ToSerilogLevel(minimumLevel))
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(paths.LogsDirectory, "mocassin-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: false,
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate:
                "{Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");

#if DEBUG
        configuration.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
#endif

        Log.Logger = configuration.CreateLogger();
        logging.ClearProviders();
        logging.AddSerilog(Log.Logger, dispose: true);
    }

    private static LogEventLevel ToSerilogLevel(LogLevel level) =>
        level switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Information,
        };
}
