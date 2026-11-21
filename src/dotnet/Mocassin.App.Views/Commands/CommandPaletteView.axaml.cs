// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Input;
using Mocassin.App.ViewModels.Commands;

namespace Mocassin.App.Views.Commands;

/// <summary>
/// Keyboard command palette.
/// </summary>
internal sealed partial class CommandPaletteView : UserControl
{
    /// <summary>
    /// Initializes the view.
    /// </summary>
    public CommandPaletteView() => InitializeComponent();

    /// <summary>
    /// Focuses the search box and selects the existing query so the first keystroke replaces it.
    /// </summary>
    public void FocusSearch()
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void OnRootKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || DataContext is not CommandPaletteViewModel palette)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            // The palette root owns Enter so it works from the search box and the results list. The view model
            // keeps the palette open when no command is selected.
            e.Handled = palette.SelectedCommand is not null;
            if (e.Handled)
            {
                palette.ExecuteSelectedCommand.Execute(null);
            }

            return;
        }

        if (!SearchBox.IsFocused)
        {
            return;
        }

        if (e.Key == Key.Down)
        {
            MoveSelection(palette, 1);
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            MoveSelection(palette, -1);
            e.Handled = true;
        }
    }

    private void OnListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is CommandPaletteViewModel palette
            && palette.ExecuteSelectedCommand.CanExecute(null))
        {
            palette.ExecuteSelectedCommand.Execute(null);
        }
    }

    private static void MoveSelection(CommandPaletteViewModel palette, int offset)
    {
        if (palette.Results.Count == 0)
        {
            return;
        }

        var current = palette.SelectedCommand is null ? 0 : palette.Results.IndexOf(palette.SelectedCommand);
        var next = Math.Clamp(current + offset, 0, palette.Results.Count - 1);
        palette.SelectedCommand = palette.Results[next];
    }
}
