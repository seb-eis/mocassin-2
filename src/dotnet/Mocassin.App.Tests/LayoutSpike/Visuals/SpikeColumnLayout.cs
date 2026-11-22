// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Mocassin.App.Tests.LayoutSpike.Visuals;

/// <summary>
/// The shared column policy plumbing: one column definition list applied to both the header and every row, so no
/// option can drift through two independent auto-size passes.
/// </summary>
internal static class SpikeColumnLayout
{
    /// <summary>Gets the row height used by the spike's table rows.</summary>
    public const double RowHeight = 28;

    /// <summary>Gets the reserved scrollbar gutter, matching the shell's scroll-gutter style.</summary>
    public const double ScrollGutter = 20;

    /// <summary>
    /// Applies the shared widths to a grid.
    /// </summary>
    /// <param name="grid">The target grid.</param>
    /// <param name="columns">The shared columns.</param>
    public static void Apply(Grid grid, ImmutableArray<SpikeColumn> columns)
    {
        foreach (var column in columns)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(column.Width));
        }
    }

    /// <summary>
    /// Builds a header row. When a sort callback is supplied, each header is a real sort button.
    /// </summary>
    /// <param name="columns">The shared columns.</param>
    /// <param name="sort">The header-click sort callback, or <see langword="null"/> for a static header.</param>
    /// <param name="reserveGutter">Whether to reserve the scrollbar gutter on the right.</param>
    /// <returns>The header control.</returns>
    public static Control BuildHeader(
        ImmutableArray<SpikeColumn> columns,
        Action<string>? sort,
        bool reserveGutter = true)
    {
        var grid = new Grid
        {
            Height = RowHeight,
            Background = SpikeTheme.Brush("Mocassin.CardBackground"),
            Margin = new Thickness(0, 0, reserveGutter ? ScrollGutter : 0, 0),
            ClipToBounds = true,
        };
        Apply(grid, columns);
        for (var index = 0; index < columns.Length; index++)
        {
            var header = columns[index].Header;
            var label = CellText(header, bold: true, muted: false);
            AutomationProperties.SetName(label, $"Header {header}");
            Control cell = label;
            if (sort is not null)
            {
                var button = new Button
                {
                    Content = label,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    BorderThickness = new Thickness(0),
                    Background = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Center,
                };
                AutomationProperties.SetName(button, $"Sort by {header}");
                AutomationProperties.SetHelpText(button, $"Sorts the table by {header}; click again to reverse.");
                button.Click += (_, _) => sort(header);
                cell = button;
            }

            Grid.SetColumn(cell, index);
            grid.Children.Add(cell);
        }

        return new Border
        {
            BorderBrush = SpikeTheme.Brush("Mocassin.BorderBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid,
        };
    }

    /// <summary>
    /// Builds one row from the shared widths.
    /// </summary>
    /// <param name="row">The row value.</param>
    /// <param name="columns">The shared columns.</param>
    /// <returns>The row control.</returns>
    public static Control BuildRow(SpikeRow row, ImmutableArray<SpikeColumn> columns)
    {
        var grid = new Grid
        {
            Height = RowHeight,
            MinHeight = RowHeight,
            Background = Brushes.Transparent,
        };
        Apply(grid, columns);
        for (var index = 0; index < columns.Length; index++)
        {
            var cell = CellText(
                index < row.Cells.Length ? row.Cells[index] : string.Empty,
                bold: false,
                muted: row.IsReadOnly);
            Grid.SetColumn(cell, index);
            grid.Children.Add(cell);
        }

        AutomationProperties.SetName(grid, $"{row.Title} row");
        return grid;
    }

    /// <summary>
    /// Gets the shared cell text style.
    /// </summary>
    /// <param name="text">The cell text.</param>
    /// <returns>The text block.</returns>
    public static TextBlock CellText(string text) => CellText(text, bold: false, muted: false);

    private static TextBlock CellText(string text, bool bold, bool muted) =>
        new()
        {
            Text = text,
            Margin = new Thickness(6, 0, 6, 0),
            Padding = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            TextTrimming = TextTrimming.CharacterEllipsis,
            FontSize = SpikeTheme.Double("Mocassin.BodyFontSize", 14),
            FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal,
            Foreground = SpikeTheme.Brush(muted ? "Mocassin.ForegroundMutedBrush" : "Mocassin.ForegroundBrush"),
        };
}
