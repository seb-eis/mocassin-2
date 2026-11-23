// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.Views.Editors;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Units;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// Headless tests for the production table surface: one shared column list, alignment at two widths, the
/// one-tab-stop rule with <c>Home</c>/<c>End</c> + <c>Tab</c>, and bounded virtualization.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ModelTableSurfaceTests
{
    [Theory]
    [InlineData(1280)]
    [InlineData(720)]
    public async Task HeaderAndRowsShareTheColumnListAndStayAligned(int width)
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        await HeadlessSession.DispatchAsync(
            () =>
            {
                using var editor = new TestParticleEditor(entry.Session, new Mocassin.App.ViewModels.Shell.ShellRegionsViewModel());
                var (window, surface, _) = CreateWindow(editor, width, 640);
                try
                {
                    Assert.Equal(TestParticleEditor.TestColumns.Count, surface.Header.ColumnDefinitions.Count);
                    var rowGrid = FirstRowGrid(surface);
                    Assert.NotNull(rowGrid);
                    Assert.Equal(surface.Header.ColumnDefinitions.Count, rowGrid!.ColumnDefinitions.Count);
                    for (var index = 0; index < surface.Header.ColumnDefinitions.Count; index++)
                    {
                        Assert.Equal(
                            surface.Header.ColumnDefinitions[index].Width,
                            rowGrid.ColumnDefinitions[index].Width);
                        Assert.Equal(
                            surface.Header.ColumnDefinitions[index].MinWidth,
                            rowGrid.ColumnDefinitions[index].MinWidth);
                    }

                    Assert.True(surface.Header.Bounds.Width > 0d);
                    Assert.InRange(Math.Abs(surface.Header.Bounds.Width - rowGrid.Bounds.Width), 0d, 2d);
                }
                finally
                {
                    window.Close();
                }
            });
    }

    [Theory]
    [InlineData(Key.Home, PhysicalKey.Home)]
    [InlineData(Key.End, PhysicalKey.End)]
    public async Task HomeOrEndThenTabReachesTheDetailPane(Key key, PhysicalKey physicalKey)
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        await HeadlessSession.DispatchAsync(
            () =>
            {
                using var editor = new TestParticleEditor(entry.Session, new Mocassin.App.ViewModels.Shell.ShellRegionsViewModel());
                var (window, surface, detail) = CreateWindow(editor, 900, 640);
                try
                {
                    Assert.All(
                        surface.Rows.GetVisualDescendants().OfType<ListBoxItem>(),
                        item => Assert.False(item.IsTabStop));

                    surface.Rows.Focus();
                    HeadlessSession.Pump(window);
                    Assert.True(
                        surface.Rows.IsKeyboardFocusWithin,
                        $"List not focused: {window.FocusManager?.GetFocusedElement()?.GetType().Name}");
                    window.KeyPress(key, RawInputModifiers.None, physicalKey, null);
                    HeadlessSession.Pump(window);
                    window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
                    HeadlessSession.Pump(window);

                    var focused = window.FocusManager?.GetFocusedElement();
                    Assert.True(
                        ReferenceEquals(detail, focused),
                        $"Focused {focused?.GetType().Name} name='{(focused as Control)?.Name}' "
                        + $"withinList={surface.Rows.IsKeyboardFocusWithin} detailAttached={detail.IsAttachedToVisualTree()}");

                }
                finally
                {
                    window.Close();
                }
            });
    }

    [Fact]
    public async Task OneThousandRowsAreVirtualizedAndTheLastRowIsSelectable()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var particles = Enumerable.Range(0, 1_000)
            .Select(index => new Particle(
                ParticleId.New(),
                $"Particle {index}",
                "X",
                new ChargeElementary(0d),
                false))
            .ToImmutableArray();
        var project = entry.Project with
        {
            Model = entry.Project.Model with { Particles = particles },
        };
        var session = ProjectSession.CreateStandalone(project, "thousand.mocprj");

        await HeadlessSession.DispatchAsync(
            () =>
            {
                using var editor = new TestParticleEditor(session, new Mocassin.App.ViewModels.Shell.ShellRegionsViewModel());
                var (window, surface, _) = CreateWindow(editor, 900, 360);
                try
                {
                    Assert.Equal(1_000, surface.Rows.ItemCount);
                    var realized = surface.Rows.GetVisualDescendants().OfType<ListBoxItem>().Count();
                    Assert.InRange(realized, 1, 999);

                    var last = particles[^1];
                    Assert.True(editor.RequestSelection(last.Id.Value));
                    HeadlessSession.Pump(window);
                    Assert.Equal(last.Id.Value, editor.Table.SelectedRow?.ObjectId);

                    surface.Rows.ScrollIntoView(editor.Table.SelectedRow!);
                    HeadlessSession.Pump(window);
                    var lastContainer = surface.Rows.GetVisualDescendants()
                        .OfType<ListBoxItem>()
                        .FirstOrDefault(item => ReferenceEquals(item.DataContext, editor.Table.SelectedRow));
                    Assert.NotNull(lastContainer);
                }
                finally
                {
                    window.Close();
                }
            });
    }

    [Fact]
    public async Task SearchAndTypedSortOperateOnTheRowProjection()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        await HeadlessSession.DispatchAsync(
            () =>
            {
                using var editor = new TestParticleEditor(entry.Session, new Mocassin.App.ViewModels.Shell.ShellRegionsViewModel());
                var total = editor.Table.TotalCount;

                editor.Table.SearchText = "Ce";
                Assert.True(editor.Table.MatchCount < total);
                Assert.All(
                    editor.Table.Rows,
                    row => Assert.Contains(
                        "Ce",
                        row.Cells[0].Text,
                        StringComparison.OrdinalIgnoreCase));

                editor.Table.ClearFilters();
                editor.Table.SortBy(TestParticleEditor.TestColumns[1]);
                var charges = editor.Table.Rows.Select(row => (double)row.Cells[1].SortKey!).ToArray();
                Assert.Equal(charges.Order(), charges);
            });
    }

    private static (Window Window, ModelTableSurface Surface, TextBox Detail) CreateWindow(
        TestParticleEditor editor,
        int width,
        int height)
    {
        var detail = new TextBox { Name = "name" };
        var pane = new StackPanel();
        pane.Children.Add(detail);
        var surface = new ModelTableSurface
        {
            DataContext = editor,
            DetailPane = pane,
        };
        var root = new Grid();
        root.Children.Add(surface);
        root.Children.Add(new Border
        {
            Child = pane,
            Width = 240,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
        });
        var window = new Window
        {
            Width = width,
            Height = height,
            Content = root,
        };
        window.Show();
        HeadlessSession.Pump(window);
        return (window, surface, detail);
    }

    private static Grid? FirstRowGrid(ModelTableSurface surface) =>
        surface.Rows.GetVisualDescendants()
            .OfType<ListBoxItem>()
            .SelectMany(item => item.GetVisualDescendants().OfType<Grid>())
            .FirstOrDefault(grid => grid.Classes.Contains("model-row"));
}
