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
    public CommandPaletteView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => SearchBox.Focus();
        PropertyChanged += (_, args) =>
        {
            if (args.Property == IsVisibleProperty && IsVisible)
            {
                SearchBox.Focus();
            }
        };
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not CommandPaletteViewModel palette)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            palette.ExecuteSelectedCommand.Execute(null);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            palette.CancelCommand.Execute(null);
        }
        else if (e.Key == Key.Down)
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
