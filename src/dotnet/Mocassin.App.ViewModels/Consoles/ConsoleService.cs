// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.ViewModels.Consoles;

/// <summary>
/// Thread-safe console service that marshals writes to the UI thread.
/// </summary>
internal sealed class ConsoleService : IConsoleService
{
    private readonly IUiDispatcher dispatcher;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Initializes a new console service.
    /// </summary>
    /// <param name="dispatcher">The UI dispatcher.</param>
    /// <param name="timeProvider">The time provider used for entry timestamps.</param>
    public ConsoleService(IUiDispatcher dispatcher, TimeProvider timeProvider)
    {
        this.dispatcher = dispatcher;
        this.timeProvider = timeProvider;
        ApplicationChannel = new ConsoleChannelViewModel("application", "Application");
    }

    /// <inheritdoc />
    public ConsoleChannelViewModel ApplicationChannel { get; }

    /// <inheritdoc />
    public IReadOnlyList<ConsoleChannelViewModel> Channels => [ApplicationChannel];

    /// <inheritdoc />
    public void Write(ConsoleSeverity severity, string source, string message)
    {
        var entry = new ConsoleEntryViewModel(timeProvider.GetUtcNow(), severity, source, message);
        if (dispatcher.IsOnUiThread)
        {
            ApplicationChannel.Add(entry);
            return;
        }

        dispatcher.Post(() => ApplicationChannel.Add(entry));
    }

    /// <inheritdoc />
    public void Clear()
    {
        foreach (var channel in Channels)
        {
            channel.Clear();
        }
    }
}
