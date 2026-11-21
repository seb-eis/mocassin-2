// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Consoles;

/// <summary>
/// Contributes console channels without changing the shell.
/// </summary>
internal interface IConsoleChannelProvider
{
    /// <summary>
    /// Creates the contributed channels.
    /// </summary>
    /// <returns>The console channels.</returns>
    public IEnumerable<ConsoleChannelViewModel> CreateChannels();
}
