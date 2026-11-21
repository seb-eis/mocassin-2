// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Consoles;

/// <summary>
/// Receives application messages and exposes the console channels.
/// </summary>
internal interface IConsoleService
{
    /// <summary>
    /// Gets the Application channel.
    /// </summary>
    public ConsoleChannelViewModel ApplicationChannel { get; }

    /// <summary>
    /// Gets every registered channel.
    /// </summary>
    public IReadOnlyList<ConsoleChannelViewModel> Channels { get; }

    /// <summary>
    /// Writes one message to the Application channel.
    /// </summary>
    /// <param name="severity">The message severity.</param>
    /// <param name="source">The source category.</param>
    /// <param name="message">The message text.</param>
    public void Write(ConsoleSeverity severity, string source, string message);

    /// <summary>
    /// Removes every message from every channel.
    /// </summary>
    public void Clear();
}
