// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace Mocassin.App.Tests.LayoutSpike.Visuals;

/// <summary>
/// The virtualized table surface used by the composed layouts: a shared header row plus a virtualizing
/// <see cref="ListBox"/>.
/// </summary>
/// <remarks>
/// The header and the rows share one column policy. The list reserves the production scrollbar gutter so the
/// columns do not drift when the vertical scrollbar appears. Sorting is wired by the caller: composed options pass
/// <see langword="null"/> and sort from the toolbar, while the baseline and the custom table also sort on a header
/// click.
/// </remarks>
internal sealed class SpikeListSurface : Grid
{
    private readonly SpikeDomainViewModel domain;
    private readonly TextBlock emptyText;
    private bool syncing;

    /// <summary>
    /// Initializes the surface for one domain.
    /// </summary>
    /// <param name="domain">The domain surface.</param>
    /// <param name="headerSort">Whether a header click sorts.</param>
    public SpikeListSurface(SpikeDomainViewModel domain, bool headerSort)
    {
        this.domain = domain;
        Background = SpikeTheme.Brush("Mocassin.WorkspaceBackground");
        RowDefinitions = new RowDefinitions("Auto,*,Auto");

        var header = SpikeColumnLayout.BuildHeader(
            domain.Columns,
            headerSort ? domain.CycleSort : null);
        Children.Add(header);

        List = new ListBox
        {
            ItemsSource = domain.Items,
            SelectionMode = domain.SupportsMultiSelect ? SelectionMode.Multiple : SelectionMode.Single,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            ItemTemplate = new FuncDataTemplate<SpikeRow>(
                (row, _) => SpikeColumnLayout.BuildRow(row, domain.Columns),
                supportsRecycling: true),
        };
        List.Classes.Add("scroll-gutter-right");
        List.Styles.Add(
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
        AutomationProperties.SetName(List, $"{domain.Title} rows");
        AutomationProperties.SetAutomationId(List, $"Spike{domain.Domain}Rows");
        AutomationProperties.SetHelpText(
            List,
            "Use the arrow keys, Home, and End to move; the selected row opens in the type editor.");
        List.SelectionChanged += OnSelectionChanged;
        Grid.SetRow(List, 1);
        Children.Add(List);

        emptyText = new TextBlock
        {
            Text = "No rows match the search or filter.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = SpikeTheme.Brush("Mocassin.ForegroundMutedBrush"),
            IsVisible = false,
        };
        Grid.SetRow(emptyText, 2);
        Children.Add(emptyText);

        domain.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SpikeDomainViewModel.StatusText))
            {
                SyncFromViewModel();
                UpdateEmptyState();
            }
        };
    }

    /// <summary>Gets the row list, for keyboard and virtualization evidence.</summary>
    public ListBox List { get; }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (syncing || domain.IsRefreshing)
        {
            return;
        }

        syncing = true;
        try
        {
            domain.SetSelection(List.SelectedItems?.OfType<SpikeRow>() ?? []);
        }
        finally
        {
            syncing = false;
        }
    }

    private void SyncFromViewModel()
    {
        if (domain.IsRefreshing)
        {
            return;
        }

        syncing = true;
        try
        {
            List.Selection.Clear();
            foreach (var row in domain.SelectedRows)
            {
                var index = domain.Items.IndexOf(row);
                if (index >= 0)
                {
                    List.Selection.Select(index);
                }
            }
        }
        finally
        {
            syncing = false;
        }
    }

    private void UpdateEmptyState() => emptyText.IsVisible = domain.Items.Count == 0;
}
