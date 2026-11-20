// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using Mocassin.App.ViewModels.Consoles;

namespace Mocassin.App.Composition;

/// <summary>
/// Forwards Microsoft.Extensions.Logging records to the Application console channel.
/// </summary>
internal sealed class ConsoleLoggerProvider : ILoggerProvider
{
    private readonly IConsoleService console;

    /// <summary>
    /// Initializes a new console logging provider.
    /// </summary>
    /// <param name="console">The application console service.</param>
    public ConsoleLoggerProvider(IConsoleService console)
    {
        this.console = console;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new ConsoleLogger(console, categoryName);

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private sealed class ConsoleLogger(IConsoleService console, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = exception is null
                ? formatter(state, exception)
                : formatter(state, exception) + Environment.NewLine + exception;
            console.Write(ToConsoleSeverity(logLevel), categoryName, message);
        }

        private static ConsoleSeverity ToConsoleSeverity(LogLevel level) =>
            level switch
            {
                LogLevel.Trace => ConsoleSeverity.Trace,
                LogLevel.Debug => ConsoleSeverity.Debug,
                LogLevel.Information => ConsoleSeverity.Information,
                LogLevel.Warning => ConsoleSeverity.Warning,
                LogLevel.Error => ConsoleSeverity.Error,
                LogLevel.Critical => ConsoleSeverity.Critical,
                _ => ConsoleSeverity.Trace,
            };
    }
}
