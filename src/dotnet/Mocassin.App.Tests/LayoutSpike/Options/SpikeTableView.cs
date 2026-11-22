// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Mocassin.App.Tests.LayoutSpike.Visuals;

namespace Mocassin.App.Tests.LayoutSpike.Options;

/// <summary>
/// Option 5: a reusable table surface owned by a <see cref="TemplatedControl"/>. The control owns the header, the
/// column policy, selection forwarding, the accessibility identity, and the sort hook; virtualization and row
/// keyboard navigation are delegated to the inner list so the spike can measure the marginal cost honestly.
/// </summary>
internal sealed class SpikeTableView : TemplatedControl
{
    /// <summary>Identifies the <see cref="ItemsSource"/> property.</summary>
    public static readonly StyledProperty<IEnumerable<SpikeRow>?> ItemsSourceProperty =
        AvaloniaProperty.Register<SpikeTableView, IEnumerable<SpikeRow>?>(nameof(ItemsSource));

    /// <summary>Identifies the <see cref="Columns"/> property.</summary>
    public static readonly StyledProperty<ImmutableArray<SpikeColumn>> ColumnsProperty =
        AvaloniaProperty.Register<SpikeTableView, ImmutableArray<SpikeColumn>>(nameof(Columns));

    /// <summary>Identifies the <see cref="SelectionMode"/> property.</summary>
    public static readonly StyledProperty<SelectionMode> SelectionModeProperty =
        AvaloniaProperty.Register<SpikeTableView, SelectionMode>(nameof(SelectionMode));

    /// <summary>Identifies the <see cref="SelectedItem"/> property.</summary>
    public static readonly StyledProperty<SpikeRow?> SelectedItemProperty =
        AvaloniaProperty.Register<SpikeTableView, SpikeRow?>(
            nameof(SelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Identifies the <see cref="SelectedRows"/> property.</summary>
    public static readonly DirectProperty<SpikeTableView, IReadOnlyList<SpikeRow>> SelectedRowsProperty =
        AvaloniaProperty.RegisterDirect<SpikeTableView, IReadOnlyList<SpikeRow>>(
            nameof(SelectedRows),
            table => table.SelectedRows);

    private IReadOnlyList<SpikeRow> selectedRows = [];
    private bool syncing;

    /// <summary>
    /// Initializes the table control.
    /// </summary>
    public SpikeTableView()
    {
        Template = new FuncControlTemplate<SpikeTableView>((view, scope) => view.BuildTemplate(scope));
    }

    /// <summary>Raised when a header cell is clicked.</summary>
    public event Action<string>? SortRequested;

    /// <summary>Raised after the row selection changed.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Gets the selected rows in the current selection order.</summary>
    public IReadOnlyList<SpikeRow> SelectedRows
    {
        get => selectedRows;
        private set => SetAndRaise(SelectedRowsProperty, ref selectedRows, value);
    }

    /// <summary>Gets or sets the rows.</summary>
    public IEnumerable<SpikeRow>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Gets or sets the shared columns.</summary>
    public ImmutableArray<SpikeColumn> Columns
    {
        get => GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>Gets or sets the selection mode.</summary>
    public SelectionMode SelectionMode
    {
        get => GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    /// <summary>Gets or sets the selected row.</summary>
    public SpikeRow? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>Gets the realized row list, for virtualization and keyboard evidence.</summary>
    public ListBox? Rows { get; private set; }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new TableAutomationPeer(this);

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Rows = e.NameScope.Find<ListBox>("PART_Rows");
        if (Rows is null)
        {
            return;
        }

        Rows.ItemsSource = ItemsSource;
        Rows.SelectedItem = SelectedItem;
        Rows.SelectionChanged += (_, _) =>
        {
            if (syncing)
            {
                return;
            }

            syncing = true;
            try
            {
                var rows = Rows.SelectedItems?.OfType<SpikeRow>().ToArray() ?? [];
                SelectedRows = rows;
                SetCurrentValue(SelectedItemProperty, rows.Length == 0 ? null : rows[^1]);
            }
            finally
            {
                syncing = false;
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    /// <summary>
    /// Applies a selection from the view model without raising a selection feedback loop.
    /// </summary>
    /// <param name="rows">The rows to select.</param>
    public void SyncSelection(IEnumerable<SpikeRow> rows)
    {
        if (Rows is null)
        {
            return;
        }

        var wanted = rows.ToArray();
        syncing = true;
        try
        {
            Rows.Selection.Clear();
            var index = 0;
            foreach (var item in Rows.Items)
            {
                if (item is SpikeRow row && wanted.Any(candidate => ReferenceEquals(candidate, row)))
                {
                    Rows.Selection.Select(index);
                }

                index++;
            }

            SelectedRows = wanted;
            SetCurrentValue(SelectedItemProperty, wanted.Length == 0 ? null : wanted[^1]);
        }
        finally
        {
            syncing = false;
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (Rows is null)
        {
            return;
        }

        if (change.Property == ItemsSourceProperty)
        {
            Rows.ItemsSource = ItemsSource;
        }
        else if (change.Property == SelectedItemProperty && !syncing && !ReferenceEquals(Rows.SelectedItem, SelectedItem))
        {
            syncing = true;
            try
            {
                Rows.SelectedItem = SelectedItem;
            }
            finally
            {
                syncing = false;
            }
        }
    }

    private Grid BuildTemplate(Avalonia.Controls.INameScope scope)
    {
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        var header = SpikeColumnLayout.BuildHeader(
            Columns,
            headerText => SortRequested?.Invoke(headerText));
        root.Children.Add(header);

        var list = new ListBox
        {
            SelectionMode = SelectionMode,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            ItemTemplate = new FuncDataTemplate<SpikeRow>(
                (row, _) => SpikeColumnLayout.BuildRow(row, Columns),
                supportsRecycling: true),
        };
        list.Classes.Add("scroll-gutter-right");
        list.Styles.Add(
            new Style(selector => selector.OfType<ListBoxItem>())
            {
                Setters =
                {
                    new Setter(TemplatedControl.PaddingProperty, new Thickness(0)),
                    new Setter(TemplatedControl.MarginProperty, new Thickness(0)),
                    new Setter(TemplatedControl.MinHeightProperty, SpikeColumnLayout.RowHeight),
                    new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch),
                },
            });
        AutomationProperties.SetName(list, "Table rows");
        AutomationProperties.SetAutomationId(list, "SpikeCustomTableRows");
        AutomationProperties.SetHelpText(
            list,
            "Rows belong to the custom TableView control; keyboard navigation is delegated to the inner list.");
        scope.Register("PART_Rows", list);
        Grid.SetRow(list, 1);
        root.Children.Add(list);
        return root;
    }

    private sealed class TableAutomationPeer(SpikeTableView owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Table;

        protected override string GetClassNameCore() => nameof(SpikeTableView);

        protected override string? GetNameCore() =>
            AutomationProperties.GetName(owner) is { Length: > 0 } name ? name : "Model table";
    }
}
