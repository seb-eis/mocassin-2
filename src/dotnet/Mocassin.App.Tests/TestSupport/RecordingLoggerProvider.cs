// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// A logging-boundary recorder for assertions about logged failures.
/// </summary>
internal sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly List<RecordedLogEntry> entries = [];

    /// <summary>
    /// Gets the recorded entries.
    /// </summary>
    public IReadOnlyList<RecordedLogEntry> Entries => entries;

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, entries);

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private sealed class RecordingLogger(string category, List<RecordedLogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Add(new RecordedLogEntry(category, logLevel, formatter(state, exception), exception));
        }
    }
}

/// <summary>
/// One recorded log entry.
/// </summary>
/// <param name="Category">The logger category.</param>
/// <param name="Level">The log level.</param>
/// <param name="Message">The formatted message.</param>
/// <param name="Exception">The optional exception.</param>
internal sealed record RecordedLogEntry(string Category, LogLevel Level, string Message, Exception? Exception);
