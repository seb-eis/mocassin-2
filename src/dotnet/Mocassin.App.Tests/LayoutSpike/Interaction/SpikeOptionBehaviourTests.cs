// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mocassin.App.Tests.TestSupport;

using Mocassin.App.Tests.LayoutSpike.Editing;

namespace Mocassin.App.Tests.LayoutSpike.Interaction;

/// <summary>
/// The §0.2 checklist per layout option: selection, real-key navigation, focus, multi-select, 1,000+ row
/// virtualization, header/row alignment, and search/filter/sort.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class SpikeOptionBehaviourTests
{
    [Theory]
    [InlineData(SpikeOption.ListDetail)]
    [InlineData(SpikeOption.TreeDetail)]
    [InlineData(SpikeOption.Tabs)]
    [InlineData(SpikeOption.SplitPane)]
    [InlineData(SpikeOption.CustomTable)]
    public Task TheOptionSelectsNavigatesFiltersAndSortsWithRealKeys(SpikeOption option) =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                using var host = await SpikeTestHost.OpenAsync(option);
                var workspace = host.Workspace;

                // Selection opens the type-specific editor for the bound ceria document.
                SpikeInteractions.SelectRow(host, "Ce4+");
                Assert.Equal("Ce4+", workspace.ActiveDomain.SelectedRow?.Title);
                Assert.Contains("Particles", workspace.Editor.HeaderText);
                Assert.NotEmpty(workspace.Editor.Fields);

                // Real-key arrow navigation moves the selection on every option.
                SpikeInteractions.Press(host, Key.Down);
                Assert.NotEqual("Ce4+", workspace.ActiveDomain.SelectedRow?.Title);
                SpikeInteractions.Press(host, Key.Up);
                if (option != SpikeOption.TreeDetail)
                {
                    Assert.Equal("Ce4+", workspace.ActiveDomain.SelectedRow?.Title);
                }

                // End and Home move to the last and first row on the list surfaces. The tree moves the tree
                // cursor to its first/last item instead, which is not the row model the editor edits.
                SpikeInteractions.Press(host, Key.End);
                if (option == SpikeOption.TreeDetail)
                {
                    Assert.NotNull(workspace.ActiveDomain.SelectedRow);
                }
                else
                {
                    Assert.Equal("Vacancy", workspace.ActiveDomain.SelectedRow?.Title);
                }

                SpikeInteractions.Press(host, Key.Home);
                if (option == SpikeOption.TreeDetail)
                {
                    Assert.NotNull(workspace.ActiveDomain.SelectedRow);
                }
                else
                {
                    Assert.Equal("Void", workspace.ActiveDomain.SelectedRow?.Title);
                }

                // Tab from a freshly selected row reaches the editor in one press; option 4 visits the splitter
                // first. After Home/End the list keeps Tab inside itself (recorded in the POC report).
                SpikeInteractions.SelectRow(host, "Ce4+");
                SpikeInteractions.Press(host, Key.Tab);
                if (option == SpikeOption.SplitPane)
                {
                    var splitter = host.Window.FocusManager?.GetFocusedElement();
                    Assert.IsType<GridSplitter>(splitter);
                    SpikeInteractions.Press(host, Key.Tab);
                }

                var focused = host.Window.FocusManager?.GetFocusedElement() as Control;
                Assert.NotNull(focused);
                Assert.Contains(focused.GetVisualAncestors(), ancestor => ancestor is SpikeEditorView);
                Assert.DoesNotContain(
                    focused.GetVisualAncestors(),
                    ancestor => ancestor is ListBox or TreeView);

                // Enter applies and Escape reverts through the shared editor on every option.
                SpikeInteractions.SelectRow(host, "Ce4+");
                var name = SpikeInteractions.EditorControl<TextBox>(host, "Name field");
                name.Focus();
                host.Pump();
                name = SpikeInteractions.EditorControl<TextBox>(host, "Name field");
                name.Text = "Ce4+ (POC)";
                SpikeInteractions.Press(host, Key.Enter);
                Assert.Equal("Ce4+ (POC)", workspace.ActiveDomain.SelectedRow?.Title);
                SpikeInteractions.EditorControl<Button>(host, "Undo edit").RaiseEvent(
                    new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                host.Pump();
                Assert.Equal("Ce4+", workspace.ActiveDomain.SelectedRow?.Title);
                name = SpikeInteractions.EditorControl<TextBox>(host, "Name field");
                name.Focus();
                host.Pump();
                name = SpikeInteractions.EditorControl<TextBox>(host, "Name field");
                name.Text = "Discarded";
                SpikeInteractions.Press(host, Key.Escape);
                Assert.Equal(
                    "Ce4+",
                    SpikeInteractions.EditorControl<TextBox>(host, "Name field").Text);

                // Generated breadth: the same surface holds 1,024 rows.
                workspace.ActiveDomain = workspace.Domains[2];
                workspace.GeneratedBreadth = true;
                host.Pump();
                Assert.Equal(1024, workspace.ActiveDomain.Items.Count);
                var realized = SpikeInteractions.RealizedContainers(host);
                if (option == SpikeOption.TreeDetail)
                {
                    // The TreeView realizes every expanded row: this option fails the virtualization requirement.
                    Assert.True(realized >= 1024, $"The tree realized only {realized} rows.");
                }
                else
                {
                    Assert.True(realized < 200, $"{option} realized {realized} of 1,024 rows.");
                }

                // The last row is reachable and selectable; the containers stay bounded on virtualized options.
                SpikeInteractions.SelectRow(host, "O1024");
                Assert.Equal("O1024", workspace.ActiveDomain.SelectedRow?.Title);
                if (option != SpikeOption.TreeDetail)
                {
                    Assert.True(
                        SpikeInteractions.RealizedContainers(host) < 200,
                        $"{option} lost virtualization after scrolling to the last row.");
                }

                // Header and row cells share one column policy; the tree rows are indented and cannot align.
                var headerCells = SpikeInteractions.HeaderCells(host);
                var rowGrid = SpikeInteractions.RowGrid(host, workspace.ActiveDomain.SelectedRow!);
                for (var column = 0; column < headerCells.Count; column++)
                {
                    var headerX = SpikeInteractions.X(headerCells[column], host.Window);
                    var cellX = SpikeInteractions.X(rowGrid.Children[column], host.Window);
                    if (option == SpikeOption.TreeDetail)
                    {
                        Assert.True(
                            cellX - headerX > 8,
                            $"The tree row {column} is unexpectedly aligned ({cellX - headerX:F1}).");
                    }
                    else
                    {
                        Assert.InRange(cellX - headerX, -1.5, 1.5);
                    }
                }

                // Search narrows the same row set on every option.
                workspace.ActiveDomain.SearchText = "O01";
                host.Pump();
                Assert.NotEmpty(workspace.ActiveDomain.Items);
                Assert.True(workspace.ActiveDomain.Items.Count < 1024);
                Assert.All(
                    workspace.ActiveDomain.Items,
                    row => Assert.Contains("o01", row.SearchText, StringComparison.OrdinalIgnoreCase));

                // The domain filter is a view-model predicate over row tags.
                workspace.ActiveDomain.SearchText = string.Empty;
                workspace.ActiveDomain.Filter = workspace.ActiveDomain.Filters.Single(
                    filter => filter.Label == "Unstable");
                host.Pump();
                Assert.All(
                    workspace.ActiveDomain.Items,
                    row => Assert.Contains("unstable", row.Tags));

                // Sorting is view-model code; the header button is the second entry point on options 1 and 5.
                workspace.ActiveDomain.Filter = workspace.ActiveDomain.Filters[0];
                workspace.ActiveDomain.SortHeader = "Name";
                workspace.ActiveDomain.SortDescending = true;
                host.Pump();
                Assert.Equal("O1024", workspace.ActiveDomain.Items[0].Title);
                workspace.ActiveDomain.SortDescending = false;
                host.Pump();
                Assert.Equal("O0001", workspace.ActiveDomain.Items[0].Title);
            });

    [Theory]
    [InlineData(SpikeOption.ListDetail)]
    [InlineData(SpikeOption.Tabs)]
    [InlineData(SpikeOption.SplitPane)]
    [InlineData(SpikeOption.CustomTable)]
    public Task MultiSelectIsOfferedWhereBulkDeleteIsSafe(SpikeOption option) =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                using var host = await SpikeTestHost.OpenAsync(option);
                var domain = host.Workspace.ActiveDomain;
                SpikeInteractions.SelectRow(host, "Ce4+");
                var list = SpikeInteractions.Rows(host);
                Assert.NotNull(list);
                Assert.Equal(SelectionMode.Multiple, list.SelectionMode);
                var second = domain.Items.Select((row, index) => (row, index))
                    .First(pair => pair.row.Title == "O2-");
                list.Selection.Select(second.index);
                host.Pump();
                Assert.Equal(2, domain.SelectedRows.Count);

                // Deleting referenced particles is refused by the real validator; nothing changes.
                domain.RemoveSelectedCommand.Execute(null);
                host.Pump();
                Assert.StartsWith("Removal refused", domain.ActionText);
                Assert.False(host.Workspace.Editor.Coordinator?.CanUndo ?? false);
                Assert.Equal(3, domain.Editable!.Document.Project.Particles.Length);
            });

    [Theory]
    [InlineData(SpikeOption.ListDetail)]
    [InlineData(SpikeOption.CustomTable)]
    public Task AHeaderClickSortsAndReverses(SpikeOption option) =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                using var host = await SpikeTestHost.OpenAsync(option);
                var domain = host.Workspace.ActiveDomain;
                domain.SortHeader = "Symbol";
                domain.SortDescending = true;
                host.Pump();
                var button = host.Window.GetVisualDescendants()
                    .OfType<Button>()
                    .First(control => Avalonia.Automation.AutomationProperties.GetName(control) == "Sort by Name");
                button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent, button));
                host.Pump();
                Assert.Equal("Name", domain.SortHeader);
                Assert.False(domain.SortDescending);
                button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent, button));
                host.Pump();
                Assert.True(domain.SortDescending);
            });

    [Theory]
    [InlineData(SpikeOption.SplitPane)]
    public Task TheSplitterResizesFromTheKeyboardAndKeepsMinimumPanes(SpikeOption option) =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                using var host = await SpikeTestHost.OpenAsync(option);
                var splitter = host.Window.GetVisualDescendants().OfType<GridSplitter>().Single();
                Assert.True(splitter.Focusable);
                splitter.Focus();
                host.Pump();
                var editor = SpikeInteractions.Editor(host);
                var before = editor.Bounds.Width;
                host.Window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
                host.Window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
                host.Pump();
                var after = SpikeInteractions.Editor(host).Bounds.Width;
                Assert.True(after > before, $"The editor pane did not grow ({before:F1} → {after:F1}).");
                Assert.True(after >= 280, $"The editor pane fell below its minimum ({after:F1}).");
            });

    [Theory]
    [InlineData(SpikeOption.Tabs)]
    public Task TheNestedTabsExposeTwoTabLevelsAndSixDomains(SpikeOption option) =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                using var host = await SpikeTestHost.OpenAsync(option);
                var tabs = host.Window.GetVisualDescendants().OfType<TabControl>().ToList();
                Assert.Equal(2, tabs.Count);
                var inner = tabs.Single(
                    tab => Avalonia.Automation.AutomationProperties.GetName(tab) == "Domain tabs (nested)");
                Assert.Equal(
                    ["Particles", "Particle sets", "Sites", "State changes", "Chains", "Transitions"],
                    inner.Items.OfType<TabItem>().Select(item => item.Header?.ToString()));
                tabs.Single(tab => Avalonia.Automation.AutomationProperties.GetName(tab) == "Domain tabs (nested)")
                    .SelectedItem = inner.Items.OfType<TabItem>().ToList()[2];
                host.Pump();
                Assert.Equal(SpikeDomain.Sites, host.Workspace.ActiveDomain.Domain);
            });
}
