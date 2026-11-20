// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Consoles;

/// <summary>
/// Contributes the Application console channel.
/// </summary>
internal sealed class ApplicationConsoleChannelProvider : IConsoleChannelProvider
{
    private readonly IConsoleService console;

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="console">The console service.</param>
    public ApplicationConsoleChannelProvider(IConsoleService console)
    {
        this.console = console;
    }

    /// <inheritdoc />
    public IEnumerable<ConsoleChannelViewModel> CreateChannels() => [console.ApplicationChannel];
}
