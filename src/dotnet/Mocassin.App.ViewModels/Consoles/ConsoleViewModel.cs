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
    public ConsoleViewModel(IConsoleService consoleService)
    {
        Channels = new ObservableCollection<ConsoleChannelViewModel>(consoleService.Channels);
        ActiveChannel = Channels.FirstOrDefault();
    }

    /// <summary>
    /// Gets the console channels.
    /// </summary>
    public ObservableCollection<ConsoleChannelViewModel> Channels { get; }
}
