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

    [ObservableProperty]
    private StatusBarSeverity severity;

    [ObservableProperty]
    private string? glyph;

    /// <summary>
    /// Initializes a status-bar item.
    /// </summary>
    /// <param name="text">The initial text.</param>
    /// <param name="toolTip">The optional tooltip.</param>
    /// <param name="command">The optional command.</param>
    /// <param name="severity">The optional severity used for the glyph colour.</param>
    /// <param name="glyph">The optional status glyph.</param>
    public StatusBarItemViewModel(
        string text,
        string? toolTip = null,
        ICommand? command = null,
        StatusBarSeverity severity = StatusBarSeverity.Normal,
        string? glyph = null)
    {
        this.text = text;
        this.severity = severity;
        this.glyph = glyph;
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
