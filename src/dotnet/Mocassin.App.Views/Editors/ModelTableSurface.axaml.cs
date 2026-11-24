// SPDX-License-Identifier: MIT

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mocassin.App.ViewModels.Editors;
using Mocassin.App.ViewModels.Editors.ModelTable;

namespace Mocassin.App.Views.Editors;

/// <summary>
/// The production model-table surface: one header Grid, one virtualized <see cref="ListBox"/>, one shared column
/// policy, the 20 px scroll gutter, multi-select, and the one-tab-stop rule.
/// </summary>
/// <remarks>
/// The surface is a composition, not a <c>TemplatedControl</c>. Header and rows are built from the same
/// <see cref="ModelTableViewModel.Columns"/> list. Row containers are not tab stops; <c>Tab</c> from the list
/// always moves to the detail pane's first actionable field, including after <c>Home</c>/<c>End</c>.
/// </remarks>
internal sealed partial class ModelTableSurface : UserControl
{
    private ModelEditorViewModel? viewModel;
    private bool syncingSelection;

    /// <summary>
    /// Initializes the surface.
    /// </summary>
    public ModelTableSurface()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachModel();
        RowList.SelectionChanged += OnSelectionChanged;
        RowList.AddHandler(KeyDownEvent, OnRowKeyDown, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        RowList.DoubleTapped += OnRowDoubleTapped;
    }

    /// <summary>
    /// Gets or sets the detail pane whose first actionable field receives focus on <c>Tab</c>.
    /// </summary>
    public Control? DetailPane { get; set; }

    /// <summary>
    /// Raised when the surface wants the host to move focus into the detail pane and no pane is assigned.
    /// </summary>
    public event EventHandler? TabToDetailRequested;

    /// <summary>
    /// Gets the realized row list, used by the headless tests.
    /// </summary>
    internal ListBox Rows => RowList;

    /// <summary>
    /// Gets the realized header grid, used by the headless tests.
    /// </summary>
    internal Grid Header => HeaderGrid;

    /// <summary>
    /// Moves focus to the detail pane's first actionable field.
    /// </summary>
    /// <returns><see langword="true"/> when a field received focus.</returns>
    public bool FocusDetailFirstField()
    {
        if (DetailPane is null)
        {
            TabToDetailRequested?.Invoke(this, EventArgs.Empty);
            return false;
        }

        var target = DetailPane.GetVisualDescendants()
            .OfType<Control>()
            .FirstOrDefault(control => control.Focusable && control.IsEffectivelyEnabled && control.IsEffectivelyVisible);
        if (target is null)
        {
            return false;
        }

        target.Focus();
        return true;
    }

    /// <summary>
    /// Focuses a named detail field, used by diagnostic navigation.
    /// </summary>
    /// <param name="property">The field name or tag.</param>
    /// <returns><see langword="true"/> when a field received focus.</returns>
    public bool FocusDetailField(string property)
    {
        if (DetailPane is null || string.IsNullOrWhiteSpace(property))
        {
            return false;
        }

        var target = DetailPane.GetVisualDescendants()
            .OfType<Control>()
            .FirstOrDefault(control => control.Focusable
                && (string.Equals(control.Name, property, StringComparison.Ordinal)
                    || string.Equals(control.Tag as string, property, StringComparison.Ordinal)));
        if (target is null)
        {
            return false;
        }

        target.Focus();
        return true;
    }

    /// <summary>
    /// Rebuilds the header and rows after the model or its column policy changes.
    /// </summary>
    internal void RefreshProjection()
    {
        BuildHeader();
        BuildRowTemplate();
        ApplySelectionMode();
        SyncSelectionFromModel();
    }

    private void AttachModel()
    {
        if (viewModel is not null)
        {
            viewModel.PropertyChanged -= OnEditorPropertyChanged;
            viewModel.Table.PropertyChanged -= OnTablePropertyChanged;
            viewModel.FocusRequested -= OnFocusRequested;
        }

        viewModel = DataContext as ModelEditorViewModel;
        if (viewModel is not null)
        {
            viewModel.PropertyChanged += OnEditorPropertyChanged;
            viewModel.Table.PropertyChanged += OnTablePropertyChanged;
            viewModel.FocusRequested += OnFocusRequested;
        }

        RefreshProjection();
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(ModelEditorViewModel.Detail))
        {
            SyncSelectionFromModel();
        }
    }

    private void OnTablePropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (viewModel is null)
        {
            return;
        }

        switch (args.PropertyName)
        {
            case nameof(ModelTableViewModel.Columns):
            case nameof(ModelTableViewModel.SortColumn):
            case nameof(ModelTableViewModel.SortDescending):
                BuildHeader();
                BuildRowTemplate();
                break;
            case nameof(ModelTableViewModel.SelectedRow):
                SyncSelectionFromModel();
                break;
        }
    }

    private void OnFocusRequested(object? sender, string property) =>
        Dispatcher.UIThread.Post(() => FocusDetailField(property));

    private void BuildHeader()
    {
        HeaderGrid.Children.Clear();
        HeaderGrid.ColumnDefinitions.Clear();
        if (viewModel is null)
        {
            return;
        }

        var columns = viewModel.Table.Columns;
        AddColumnDefinitions(HeaderGrid, columns);
        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            var active = ReferenceEquals(viewModel.Table.SortColumn, column)
                || viewModel.Table.SortColumn == column;
            var indicator = active ? viewModel.Table.SortDescending ? " ▼" : " ▲" : string.Empty;
            var button = new Button
            {
                Content = column.Header + indicator,
                Focusable = false,
                IsTabStop = false,
                IsEnabled = column.IsSortable,
                Classes = { "model-column-header" },
            };
            AutomationProperties.SetName(button, $"{column.Header} column header");
            button.Click += (_, _) =>
            {
                viewModel.Table.SortBy(column);
                BuildHeader();
                BuildRowTemplate();
            };
            Grid.SetColumn(button, index);
            HeaderGrid.Children.Add(button);
        }
    }

    private void BuildRowTemplate()
    {
        if (viewModel is null)
        {
            RowList.ItemTemplate = null;
            return;
        }

        RowList.ItemTemplate = new FuncDataTemplate<ModelTableRow>(
            (row, _) => BuildRow(row),
            supportsRecycling: true);
    }

    private Grid BuildRow(ModelTableRow row)
    {
        var columns = viewModel?.Table.Columns ?? [];
        var grid = new Grid { MinHeight = 24, Classes = { "model-row" } };
        AddColumnDefinitions(grid, columns);
        for (var index = 0; index < columns.Count; index++)
        {
            var text = index < row.Cells.Length ? row.Cells[index].Text : string.Empty;
            var cell = new TextBlock
            {
                Text = text,
                Margin = new Thickness(6, 3),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
            };
            Grid.SetColumn(cell, index);
            grid.Children.Add(cell);
        }

        var announcement = string.Join(" | ", row.Cells.Select(static cell => cell.Text));
        if (!string.IsNullOrWhiteSpace(row.SeverityText))
        {
            announcement = $"{announcement} — {row.SeverityText}";
            grid.Classes.Add(row.HasError ? "model-row-error" : "model-row-warning");
        }

        AutomationProperties.SetName(grid, announcement);
        return grid;
    }

    private void ApplySelectionMode()
    {
        if (viewModel is null)
        {
            return;
        }

        RowList.SelectionMode = viewModel.Table.IsMultiSelectEnabled
            ? SelectionMode.Multiple
            : SelectionMode.Single;
    }

    private void SyncSelectionFromModel()
    {
        if (viewModel is null || syncingSelection)
        {
            return;
        }

        syncingSelection = true;
        try
        {
            RowList.SelectedItems?.Clear();
            foreach (var row in viewModel.Table.SelectedRows)
            {
                RowList.SelectedItems?.Add(row);
            }

            if (viewModel.Table.SelectedRow is { } selected && !ReferenceEquals(RowList.SelectedItem, selected))
            {
                RowList.SelectedItem = selected;
            }
        }
        finally
        {
            syncingSelection = false;
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (viewModel is null || syncingSelection)
        {
            return;
        }

        var selected = RowList.SelectedItems?.OfType<ModelTableRow>() ?? [];
        viewModel.Table.UpdateSelection(selected);
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key != Key.Tab
            || (args.KeyModifiers & (KeyModifiers.Shift | KeyModifiers.Control)) != 0
            || !RowList.IsKeyboardFocusWithin)
        {
            return;
        }

        if (FocusDetailFirstField())
        {
            args.Handled = true;
        }
    }

    private void OnRowKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key == Key.Tab && (args.KeyModifiers & (KeyModifiers.Shift | KeyModifiers.Control)) == 0)
        {
            args.Handled = true;
            FocusDetailFirstField();
            return;
        }

        if (args.Key == Key.Enter)
        {
            args.Handled = true;
            FocusDetailFirstField();
        }
    }

    private void OnRowDoubleTapped(object? sender, TappedEventArgs args)
    {
        args.Handled = true;
        FocusDetailFirstField();
    }

    private static void AddColumnDefinitions(Grid grid, IReadOnlyList<ModelColumn> columns)
    {
        foreach (var column in columns)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = column.Width.IsStar
                    ? new GridLength(column.Width.Weight, GridUnitType.Star)
                    : new GridLength(column.Width.Pixels, GridUnitType.Pixel),
                MinWidth = column.Minimum,
            });
        }
    }
}
