// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Mocassin.App.Views.Consoles;

/// <summary>
/// Application console view. The tab strip selects the application console and the Validation content; plain
/// <c>Enter</c> on a tab is deliberately inert, matching the interaction contract.
/// </summary>
internal sealed partial class ConsoleView : UserControl
{
    /// <summary>
    /// Initializes the view.
    /// </summary>
    public ConsoleView() => InitializeComponent();

    private void OnTabsKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.KeyModifiers != KeyModifiers.None)
        {
            return;
        }

        if ((e.Source as Visual)?.FindAncestorOfType<TabItem>(includeSelf: true) is not null)
        {
            e.Handled = true;
        }
    }
}
