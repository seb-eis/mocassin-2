// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Mocassin.App.Tests.LayoutSpike.Editing;

namespace Mocassin.App.Tests.LayoutSpike.Options;

/// <summary>
/// Option 5 composition: the custom <see cref="SpikeTableView"/> as the selection surface and the shared editor in
/// the detail pane.
/// </summary>
internal sealed class CustomTableOption : SpikeOptionSurface
{
    private readonly ContentControl tableHost = new();
    private readonly ContentControl editorHost = new();
    private SpikeTableView? table;

    /// <summary>
    /// Initializes the option.
    /// </summary>
    /// <param name="workspace">The shared workspace.</param>
    public CustomTableOption(SpikeWorkspaceViewModel workspace)
        : base(workspace)
    {
    }

    /// <inheritdoc />
    public override Control Build()
    {
        ShowActive();
        Workspace.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(SpikeWorkspaceViewModel.ActiveDomain)
                or nameof(SpikeWorkspaceViewModel.GeneratedBreadth))
            {
                ShowActive();
            }
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,8,360"),
            Margin = new Thickness(8),
        };
        grid.Children.Add(tableHost);
        Grid.SetColumn(editorHost, 2);
        grid.Children.Add(editorHost);
        return grid;
    }

    private void ShowActive()
    {
        var domain = Workspace.ActiveDomain;
        table = new SpikeTableView
        {
            Columns = domain.Columns,
            ItemsSource = domain.Items,
            SelectionMode = domain.SupportsMultiSelect ? SelectionMode.Multiple : SelectionMode.Single,
            SelectedItem = domain.SelectedRow,
        };
        AutomationProperties.SetName(table, $"{domain.Title} table");
        table.SortRequested += domain.CycleSort;
        table.SelectionChanged += (_, _) =>
        {
            domain.SetSelection(table.SelectedRows);
        };
        domain.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SpikeDomainViewModel.StatusText))
            {
                table.SyncSelection(domain.SelectedRows);
            }
        };
        tableHost.Content = table;
        editorHost.Content = new SpikeEditorView { DataContext = domain.Editor };
    }
}
