// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Consoles;

namespace Mocassin.App.ViewModels.Diagnostics;

/// <summary>
/// Contributes the Problems channel to the existing console region.
/// </summary>
internal sealed class ProblemsChannelProvider : IConsoleChannelProvider
{
    private readonly ProblemsChannelViewModel channel;

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="channel">The singleton Problems channel.</param>
    public ProblemsChannelProvider(ProblemsChannelViewModel channel)
    {
        this.channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    /// <inheritdoc />
    public IEnumerable<ConsoleChannelViewModel> CreateChannels() => [channel];
}
