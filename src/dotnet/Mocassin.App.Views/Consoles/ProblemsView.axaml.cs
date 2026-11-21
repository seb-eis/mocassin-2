// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Input;
using Mocassin.App.ViewModels.Diagnostics;

namespace Mocassin.App.Views.Consoles;

/// <summary>
/// Problems channel view with filters, sorting, navigation, and help links.
/// </summary>
internal sealed partial class ProblemsView : UserControl
{
    /// <summary>
    /// Initializes the view.
    /// </summary>
    public ProblemsView() => InitializeComponent();

    private void OnItemsDoubleTapped(object? sender, TappedEventArgs e) => NavigateSelected(sender);

    private void OnItemsKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        NavigateSelected(sender);
    }

    private static void NavigateSelected(object? sender)
    {
        if (sender is ListBox { SelectedItem: DiagnosticItemViewModel item })
        {
            item.NavigateCommand.Execute(null);
        }
    }
}
