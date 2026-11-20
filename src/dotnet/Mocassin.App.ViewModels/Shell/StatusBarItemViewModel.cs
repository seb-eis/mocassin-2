// SPDX-License-Identifier: MIT

using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// One status-bar item.
/// </summary>
internal sealed partial class StatusBarItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string text;

    /// <summary>
    /// Initializes a status-bar item.
    /// </summary>
    /// <param name="text">The initial text.</param>
    /// <param name="toolTip">The optional tooltip.</param>
    /// <param name="command">The optional command.</param>
    public StatusBarItemViewModel(string text, string? toolTip = null, ICommand? command = null)
    {
        this.text = text;
        ToolTip = toolTip;
        Command = command;
    }

    /// <summary>
    /// Gets the optional tooltip.
    /// </summary>
    public string? ToolTip { get; }

    /// <summary>
    /// Gets the optional command.
    /// </summary>
    public ICommand? Command { get; }
}
