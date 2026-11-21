// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mocassin.App.ViewModels.Consoles;

/// <summary>
/// Holds the console channels and the active channel.
/// </summary>
internal sealed partial class ConsoleViewModel : ObservableObject
{
    [ObservableProperty]
    private ConsoleChannelViewModel? activeChannel;

    /// <summary>
    /// Initializes a new console view model.
    /// </summary>
    /// <param name="consoleService">The console service that owns the channels.</param>
    /// <param name="providers">The optional channel providers.</param>
    public ConsoleViewModel(
        IConsoleService consoleService,
        IEnumerable<IConsoleChannelProvider>? providers = null)
    {
        var channels = providers is null
            ? consoleService.Channels.ToList()
            : providers.SelectMany(provider => provider.CreateChannels()).ToList();
        if (!channels.Contains(consoleService.ApplicationChannel))
        {
            channels.Insert(0, consoleService.ApplicationChannel);
        }

        Channels = new ObservableCollection<ConsoleChannelViewModel>(channels);
        ActiveChannel = Channels.FirstOrDefault();
    }

    /// <summary>
    /// Gets the console channels.
    /// </summary>
    public ObservableCollection<ConsoleChannelViewModel> Channels { get; }
}
