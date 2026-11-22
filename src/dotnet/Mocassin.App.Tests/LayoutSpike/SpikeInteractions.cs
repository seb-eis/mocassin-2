// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;

using Mocassin.App.Tests.LayoutSpike.Editing;

namespace Mocassin.App.Tests.LayoutSpike;

/// <summary>
/// Control lookup and real-key helpers shared by the spike's evidence tests.
/// </summary>
internal static class SpikeInteractions
{
    /// <summary>
    /// Gets the active option's row list (a list surface or the custom table's inner list).
    /// </summary>
    /// <param name="host">The open spike host.</param>
    /// <returns>The row list, or <see langword="null"/> for the tree option.</returns>
    public static ListBox? Rows(SpikeTestHost host)
    {
        var expected = host.Stage.Option == SpikeOption.CustomTable
            ? "Table rows"
            : $"{host.Workspace.ActiveDomain.Title} rows";
        return host.Window.GetVisualDescendants()
            .OfType<ListBox>()
            .FirstOrDefault(list => AutomationProperties.GetName(list) == expected);
    }

    /// <summary>
    /// Gets the tree option's tree.
    /// </summary>
    /// <param name="host">The open spike host.</param>
    /// <returns>The tree, or <see langword="null"/>.</returns>
    public static TreeView? Tree(SpikeTestHost host) =>
        host.Window.GetVisualDescendants().OfType<TreeView>().FirstOrDefault();

    /// <summary>
    /// Finds a realized row container by row key.
    /// </summary>
    /// <param name="host">The open spike host.</param>
    /// <param name="row">The row.</param>
    /// <returns>The container, or <see langword="null"/> when the row is not realized.</returns>
    public static TreeViewItem? TreeItem(SpikeTestHost host, SpikeRow row) =>
        Tree(host)?.GetVisualDescendants()
            .OfType<TreeViewItem>()
            .FirstOrDefault(container => ReferenceEquals(container.DataContext, row));

    /// <summary>
    /// Selects a row by title with the option-appropriate surface and focuses it for real-key input.
    /// </summary>
    /// <param name="host">The open spike host.</param>
    /// <param name="title">The row title.</param>
    /// <returns>The selected row.</returns>
    public static SpikeRow SelectRow(SpikeTestHost host, string title)
    {
        var domain = host.Workspace.ActiveDomain;
        var index = -1;
        for (var candidate = 0; candidate < domain.Items.Count; candidate++)
        {
            if (string.Equals(domain.Items[candidate].Title, title, StringComparison.Ordinal))
            {
                index = candidate;
                break;
            }
        }

        Assert.True(index >= 0, $"The row '{title}' is not in the {domain.Title} surface.");
        var row = domain.Items[index];
        if (host.Stage.Option == SpikeOption.TreeDetail)
        {
            var item = TreeItem(host, row);
            for (var attempt = 0; attempt < 8 && item is null; attempt++)
            {
                host.Pump();
                item = TreeItem(host, row);
            }

            Assert.NotNull(item);
            item.IsSelected = true;
            item.BringIntoView();
            host.Pump();
            if (!item.Focus())
            {
                Tree(host)?.Focus();
                host.Pump();
            }

            Assert.True(
                item.Focus() || item.IsFocused,
                $"The tree row '{title}' could not take focus "
                + $"(attached={item.IsAttachedToVisualTree()}, visible={item.IsEffectivelyVisible}, "
                + $"focusable={item.Focusable}).");
            host.Pump();
            return row;
        }

        var list = Rows(host);
        for (var attempt = 0; attempt < 8 && list is null; attempt++)
        {
            host.Pump();
            list = Rows(host);
        }

        Assert.NotNull(list);
        list.SelectedIndex = index;
        host.Pump();
        var container = list.ContainerFromIndex(index) as ListBoxItem;
        for (var attempt = 0; attempt < 8 && container is null; attempt++)
        {
            list.ScrollIntoView(index);
            host.Pump();
            container = list.ContainerFromIndex(index) as ListBoxItem;
        }

        Assert.NotNull(container);
        Assert.True(container.Focus(), $"The row '{title}' could not take focus.");
        host.Pump();
        Assert.Equal(title, domain.SelectedRow?.Title);
        return row;
    }

    /// <summary>
    /// Gets the editor view.
    /// </summary>
    /// <param name="host">The open spike host.</param>
    /// <returns>The editor view.</returns>
    public static SpikeEditorView Editor(SpikeTestHost host) =>
        host.Window.GetVisualDescendants()
            .OfType<SpikeEditorView>()
            .Last(view => ReferenceEquals(view.DataContext, host.Workspace.Editor) && view.IsEffectivelyVisible);

    /// <summary>
    /// Finds a named control in the editor.
    /// </summary>
    /// <typeparam name="T">The control type.</typeparam>
    /// <param name="host">The open spike host.</param>
    /// <param name="automationName">The automation name.</param>
    /// <returns>The control.</returns>
    public static T EditorControl<T>(SpikeTestHost host, string automationName)
        where T : Control =>
        Editor(host).GetVisualDescendants()
            .OfType<T>()
            .First(control => AutomationProperties.GetName(control) == automationName);

    /// <summary>
    /// Gets the X offset of a cell's left edge inside the window.
    /// </summary>
    /// <param name="cell">The cell control.</param>
    /// <param name="window">The window.</param>
    /// <returns>The X offset.</returns>
    public static double X(Control cell, Window window) =>
        cell.TranslatePoint(new Point(0, 0), window)?.X
        ?? throw new InvalidOperationException("The control is not in the window's visual tree.");

    /// <summary>
    /// Gets the header cells in column order.
    /// </summary>
    /// <param name="host">The open spike host.</param>
    /// <returns>The header text blocks.</returns>
    public static IReadOnlyList<TextBlock> HeaderCells(SpikeTestHost host)
    {
        var columns = host.Workspace.ActiveDomain.Columns;
        return
        [
            .. columns.Select(column => host.Window.GetVisualDescendants()
                .OfType<TextBlock>()
                .First(text => AutomationProperties.GetName(text) == $"Header {column.Header}")),
        ];
    }

    /// <summary>
    /// Gets a realized row grid by row title.
    /// </summary>
    /// <param name="host">The open spike host.</param>
    /// <param name="row">The row.</param>
    /// <returns>The row grid.</returns>
    public static Grid RowGrid(SpikeTestHost host, SpikeRow row) =>
        host.Window.GetVisualDescendants()
            .OfType<Grid>()
            .First(grid => AutomationProperties.GetName(grid) == $"{row.Title} row");

    /// <summary>
    /// Presses one real key on the headless window.
    /// </summary>
    /// <param name="host">The open spike host.</param>
    /// <param name="key">The key.</param>
    /// <param name="modifiers">The modifiers.</param>
    public static void Press(SpikeTestHost host, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var physical = key switch
        {
            Key.Down => PhysicalKey.ArrowDown,
            Key.Up => PhysicalKey.ArrowUp,
            Key.Left => PhysicalKey.ArrowLeft,
            Key.Right => PhysicalKey.ArrowRight,
            Key.Home => PhysicalKey.Home,
            Key.End => PhysicalKey.End,
            Key.Enter => PhysicalKey.Enter,
            Key.Escape => PhysicalKey.Escape,
            Key.Tab => PhysicalKey.Tab,
            Key.Z => PhysicalKey.Z,
            Key.Y => PhysicalKey.Y,
            _ => PhysicalKey.None,
        };
        host.Window.KeyPress(key, modifiers, physical, null);
        host.Pump();
    }

    /// <summary>
    /// Gets every interactive control in the window, excluding template internals.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <returns>The interactive controls.</returns>
    public static IReadOnlyList<Control> InteractiveControls(Window window) =>
    [
        .. window.GetVisualDescendants()
            .OfType<Control>()
            .Where(control => control is Button or TextBox or ComboBox or CheckBox or ListBox or TreeView
                or TabControl or GridSplitter)
            .Where(control => !control.Name?.StartsWith("PART_", StringComparison.Ordinal) ?? true),
    ];

    /// <summary>
    /// Gets the number of realized row containers for the active option.
    /// </summary>
    /// <param name="host">The open spike host.</param>
    /// <returns>The realized container count.</returns>
    public static int RealizedContainers(SpikeTestHost host) =>
        host.Stage.Option == SpikeOption.TreeDetail
            ? host.Window.GetVisualDescendants().OfType<TreeViewItem>().Count()
            : host.Window.GetVisualDescendants().OfType<ListBoxItem>().Count();
}
