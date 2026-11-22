// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Mocassin.App.Tests.LayoutSpike.Visuals;

/// <summary>
/// The option-6 data-shape control: a small, reusable matrix surface for chain occupation, exchange previews, and
/// Step 25's site-energy matrices.
/// </summary>
internal sealed class SpikeMatrixControl : Grid
{
    /// <summary>
    /// Initializes the matrix control.
    /// </summary>
    /// <param name="matrix">The matrix value.</param>
    public SpikeMatrixControl(SpikeMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        RowDefinitions = new RowDefinitions();
        ColumnDefinitions = new ColumnDefinitions();
        Background = SpikeTheme.Brush("Mocassin.WorkspaceBackground");
        ColumnDefinitions.Add(new ColumnDefinition(new GridLength(34)));
        foreach (var _ in matrix.ColumnLabels)
        {
            ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        AddCell(
            new TextBlock { Text = "Pos.", FontWeight = FontWeight.SemiBold },
            0,
            0,
            header: true);
        for (var column = 0; column < matrix.ColumnLabels.Length; column++)
        {
            AddCell(
                new TextBlock
                {
                    Text = matrix.ColumnLabels[column],
                    FontWeight = FontWeight.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                },
                0,
                column + 1,
                header: true);
        }

        for (var row = 0; row < matrix.Rows.Length; row++)
        {
            RowDefinitions.Add(new RowDefinition(new GridLength(26)));
            var value = matrix.Rows[row];
            AddCell(new TextBlock { Text = value.Label }, row + 1, 0, header: true);
            for (var column = 0; column < value.Cells.Length; column++)
            {
                var cell = new TextBlock
                {
                    Text = value.Cells[column],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                };
                AddCell(cell, row + 1, column + 1, header: false);
            }
        }

        AutomationProperties.SetName(this, matrix.Title);
        AutomationProperties.SetHelpText(this, "Rows are chain positions; columns are state-change groups.");
        AutomationProperties.SetAutomationId(this, "SpikeOccupationMatrix");
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new SpikeMatrixAutomationPeer(this);

    private void AddCell(Control content, int row, int column, bool header)
    {
        var border = new Border
        {
            BorderBrush = SpikeTheme.Brush("Mocassin.BorderBrush"),
            BorderThickness = new Thickness(header ? 1 : 0, 0, 1, 1),
            Background = header ? SpikeTheme.Brush("Mocassin.CardBackground") : Brushes.Transparent,
            Padding = new Thickness(4, 2),
            Child = content,
        };
        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        Children.Add(border);
    }

    private sealed class SpikeMatrixAutomationPeer(SpikeMatrixControl owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Table;

        protected override string GetClassNameCore() => nameof(SpikeMatrixControl);
    }
}
