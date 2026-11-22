// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Mocassin.App.Tests.LayoutSpike.Editing;
using Mocassin.App.Tests.LayoutSpike.Visuals;

namespace Mocassin.App.Tests.LayoutSpike.Options;

/// <summary>
/// Option 4: a resizable master-detail split with minimum pane sizes, keyboard resize, and a focus adorner, matching
/// the shell's existing splitter conventions.
/// </summary>
internal sealed class SplitPaneOption : SpikeOptionSurface
{
    private readonly ContentControl listHost = new();
    private readonly ContentControl editorHost = new();

    /// <summary>
    /// Initializes the option.
    /// </summary>
    /// <param name="workspace">The shared workspace.</param>
    public SplitPaneOption(SpikeWorkspaceViewModel workspace)
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
            ColumnDefinitions = new ColumnDefinitions("*,6,360"),
            Margin = new Thickness(8),
        };
        grid.ColumnDefinitions[0].MinWidth = 260;
        grid.ColumnDefinitions[2].MinWidth = 280;
        grid.Children.Add(listHost);

        var splitter = new GridSplitter
        {
            Width = 6,
            ResizeDirection = GridResizeDirection.Columns,
            KeyboardIncrement = 16,
            Focusable = true,
            Background = SpikeTheme.Brush("Mocassin.BorderBrush"),
            FocusAdorner = new FuncTemplate<Control>(
                () => new Border
                {
                    BorderBrush = SpikeTheme.Brush("Mocassin.FocusBrush"),
                    BorderThickness = SpikeTheme.Thickness("Mocassin.FocusThickness", new Thickness(2)),
                    CornerRadius = new CornerRadius(1),
                }),
        };
        AutomationProperties.SetName(splitter, "Resize editor pane");
        AutomationProperties.SetHelpText(splitter, "Use the arrow keys to resize the editor pane.");
        Grid.SetColumn(splitter, 1);
        grid.Children.Add(splitter);

        Grid.SetColumn(editorHost, 2);
        grid.Children.Add(editorHost);
        return grid;
    }

    private void ShowActive()
    {
        listHost.Content = new SpikeListSurface(Workspace.ActiveDomain, headerSort: false);
        editorHost.Content = new SpikeEditorView { DataContext = Workspace.Editor };
    }
}
