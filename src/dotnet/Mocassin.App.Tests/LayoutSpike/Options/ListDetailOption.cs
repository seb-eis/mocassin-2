// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Controls;
using Mocassin.App.Tests.LayoutSpike.Editing;
using Mocassin.App.Tests.LayoutSpike.Visuals;

namespace Mocassin.App.Tests.LayoutSpike.Options;

/// <summary>
/// Option 1: the ADR 0003 baseline. A shared header row above a virtualized ListBox selects, and a fixed-width
/// detail pane hosts the type-specific editor.
/// </summary>
internal sealed class ListDetailOption : SpikeOptionSurface
{
    private readonly ContentControl listHost = new();
    private readonly ContentControl editorHost = new();

    /// <summary>
    /// Initializes the option.
    /// </summary>
    /// <param name="workspace">The shared workspace.</param>
    public ListDetailOption(SpikeWorkspaceViewModel workspace)
        : base(workspace)
    {
    }

    /// <inheritdoc />
    public override Control Build()
    {
        ShowActive();
        Workspace.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SpikeWorkspaceViewModel.ActiveDomain))
            {
                ShowActive();
            }
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,8,360"),
            Margin = new Thickness(8),
        };
        Grid.SetColumn(editorHost, 2);
        grid.Children.Add(listHost);
        grid.Children.Add(editorHost);
        return grid;
    }

    private void ShowActive()
    {
        listHost.Content = new SpikeListSurface(Workspace.ActiveDomain, headerSort: true)
        {
            Margin = new Thickness(0, 0, 0, 0),
        };
        editorHost.Content = new SpikeEditorView { DataContext = Workspace.Editor };
    }
}
