// SPDX-License-Identifier: MIT

using Avalonia.Controls;

namespace Mocassin.App.Views.Commands;

/// <summary>
/// Shortcut cheat-sheet overlay.
/// </summary>
internal sealed partial class ShortcutCheatSheetView : UserControl
{
    /// <summary>
    /// Initializes the view.
    /// </summary>
    public ShortcutCheatSheetView() => InitializeComponent();

    /// <summary>
    /// Focuses the Close button, the layer's only actionable control.
    /// </summary>
    public void FocusInitial() => CloseButton.Focus();
}
