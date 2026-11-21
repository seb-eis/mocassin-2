// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Settings;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Shell;

namespace Mocassin.App.Tests.Views;

/// <summary>
/// Headless tests for the proportional shell layout, splitter gestures, visibility, and persistence.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ShellLayoutTests
{
    [Fact]
    public Task TheDefaultFractionsFillTheWorkspaceAtTheDesignSize() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var grid = Find<Grid>(window, "WorkspaceGrid");
                    var (left, centre, right) = Columns(window);
                    var total = left + centre + right;
                    Assert.InRange(left / total, SettingsDefaults.DefaultLeftFraction - 0.005, SettingsDefaults.DefaultLeftFraction + 0.005);
                    Assert.InRange(right / total, SettingsDefaults.DefaultRightFraction - 0.005, SettingsDefaults.DefaultRightFraction + 0.005);
                    Assert.InRange(centre / total, 1d - SettingsDefaults.DefaultLeftFraction - SettingsDefaults.DefaultRightFraction - 0.005, 1d - SettingsDefaults.DefaultLeftFraction - SettingsDefaults.DefaultRightFraction + 0.005);
                    Assert.InRange(left + 6 + centre + 6 + right, grid.Bounds.Width - 0.5, grid.Bounds.Width + 0.5);

                    var rows = Rows(window);
                    var panelHeight = rows.Documents + rows.Console;
                    Assert.InRange(rows.Console / panelHeight, SettingsDefaults.DefaultConsoleFraction - 0.005, SettingsDefaults.DefaultConsoleFraction + 0.005);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task DraggingTheLeftSplitterRebalancesOnlyTheAdjacentPair() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    var (leftBefore, centreBefore, rightBefore) = Columns(window);
                    var rightFractionBefore = shell.Regions.RightFraction;

                    Drag(window, Find<GridSplitter>(window, "ProjectsSplitter"), new Vector(120, 0));
                    HeadlessSession.Pump(window);
                    await shell.PendingLayoutPersist;

                    var (leftAfter, centreAfter, rightAfter) = Columns(window);
                    Assert.InRange(leftAfter - leftBefore, 117, 123);
                    Assert.InRange(centreAfter - centreBefore, -123, -117);
                    Assert.InRange(rightAfter, rightBefore - 1, rightBefore + 1);
                    Assert.InRange(leftAfter + centreAfter + rightAfter, leftBefore + centreBefore + rightBefore - 1, leftBefore + centreBefore + rightBefore + 1);
                    Assert.True(shell.Regions.LeftFraction > SettingsDefaults.DefaultLeftFraction);
                    Assert.InRange(shell.Regions.RightFraction, rightFractionBefore - 0.001, rightFractionBefore + 0.001);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task DraggingTheRightSplitterRebalancesOnlyTheAdjacentPair() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    var (leftBefore, centreBefore, rightBefore) = Columns(window);
                    var leftFractionBefore = shell.Regions.LeftFraction;

                    Drag(window, Find<GridSplitter>(window, "InspectorSplitter"), new Vector(90, 0));
                    HeadlessSession.Pump(window);
                    await shell.PendingLayoutPersist;

                    var (leftAfter, centreAfter, rightAfter) = Columns(window);
                    Assert.InRange(rightAfter - rightBefore, -93, -87);
                    Assert.InRange(centreAfter - centreBefore, 87, 93);
                    Assert.InRange(leftAfter, leftBefore - 1, leftBefore + 1);
                    Assert.True(shell.Regions.RightFraction < SettingsDefaults.DefaultRightFraction);
                    Assert.InRange(shell.Regions.LeftFraction, leftFractionBefore - 0.001, leftFractionBefore + 0.001);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task DraggingTheConsoleSplitterRebalancesRows() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    var before = Rows(window);

                    Drag(window, Find<GridSplitter>(window, "ConsoleSplitter"), new Vector(0, -80));
                    HeadlessSession.Pump(window);
                    await shell.PendingLayoutPersist;

                    var after = Rows(window);
                    Assert.InRange(after.Console - before.Console, 77, 83);
                    Assert.InRange(after.Documents - before.Documents, -83, -77);
                    Assert.True(shell.Regions.ConsoleFraction > SettingsDefaults.DefaultConsoleFraction);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task AWindowResizeKeepsTheDraggedRatio() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    Drag(window, Find<GridSplitter>(window, "ProjectsSplitter"), new Vector(120, 0));
                    HeadlessSession.Pump(window);
                    await shell.PendingLayoutPersist;
                    var fraction = shell.Regions.LeftFraction;

                    window.Width = 1000;
                    window.Height = 700;
                    HeadlessSession.Pump(window);

                    var (left, centre, right) = Columns(window);
                    Assert.InRange(left / (left + centre + right), fraction - 0.005, fraction + 0.005);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task HidingAndShowingProjectsCollapsesAndRestoresTheShare() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    var projects = Find<ProjectsRegionView>(window, "ProjectsRegion");
                    var splitter = Find<GridSplitter>(window, "ProjectsSplitter");
                    var (_, centreBefore, _) = Columns(window);
                    var fraction = shell.Regions.LeftFraction;

                    shell.Regions.ToggleProjectsCommand.Execute(null);
                    HeadlessSession.Pump(window);

                    Assert.False(projects.IsVisible);
                    Assert.False(splitter.IsVisible);
                    var hidden = Columns(window);
                    Assert.Equal(0d, hidden.Left);
                    Assert.True(hidden.Centre > centreBefore);

                    shell.Regions.ToggleProjectsCommand.Execute(null);
                    HeadlessSession.Pump(window);

                    Assert.True(projects.IsVisible);
                    Assert.True(splitter.IsVisible);
                    var restored = Columns(window);
                    Assert.InRange(restored.Left / (restored.Left + restored.Centre + restored.Right), fraction - 0.005, fraction + 0.005);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheProjectsTreeFillsTheStarRowAndTheButtonIsPinned() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var tree = window.GetVisualDescendants()
                        .OfType<TreeView>()
                        .Single(control => AutomationProperties.GetName(control) == "Project explorer");
                    var button = window.GetVisualDescendants()
                        .OfType<Button>()
                        .Single(control => AutomationProperties.GetName(control) == "Open selected project node");
                    var grid = Assert.IsType<Grid>(tree.Parent);
                    Assert.Equal(5, grid.RowDefinitions.Count);
                    Assert.True(grid.RowDefinitions[3].Height.IsStar);
                    Assert.True(grid.RowDefinitions[4].Height.IsAuto);
                    Assert.Equal(3, Grid.GetRow(tree));
                    Assert.Equal(4, Grid.GetRow(button));
                    Assert.Single(grid.RowDefinitions, row => row.Height.IsStar);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task SplittersAreFocusableNamedAndResizeBySixteenPixels() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    var expected = new[]
                    {
                        ("ProjectsSplitter", "Resize projects panel"),
                        ("InspectorSplitter", "Resize inspector panel"),
                        ("ConsoleSplitter", "Resize console"),
                    };
                    foreach (var (name, automationName) in expected)
                    {
                        var splitter = Find<GridSplitter>(window, name);
                        Assert.Equal(automationName, AutomationProperties.GetName(splitter));
                        Assert.True(splitter.Focusable);
                        Assert.Contains("arrow", AutomationProperties.GetHelpText(splitter), StringComparison.OrdinalIgnoreCase);
                        Assert.NotNull(splitter.FocusAdorner);
                        splitter.Focus();
                        HeadlessSession.Pump(window);
                        Assert.True(splitter.IsFocused);
                    }

                    var (leftBefore, _, _) = Columns(window);
                    var projectsSplitter = Find<GridSplitter>(window, "ProjectsSplitter");
                    projectsSplitter.Focus();
                    window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
                    window.KeyRelease(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
                    HeadlessSession.Pump(window);
                    await shell.PendingLayoutPersist;
                    var (leftAfter, _, _) = Columns(window);
                    Assert.InRange(leftAfter - leftBefore, 14, 18);
                    Assert.True(shell.Regions.LeftFraction > SettingsDefaults.DefaultLeftFraction);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task ADraggedFractionSurvivesTheSettingsRoundTrip() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    Drag(window, Find<GridSplitter>(window, "ProjectsSplitter"), new Vector(120, 0));
                    HeadlessSession.Pump(window);
                    await shell.PendingLayoutPersist;

                    var loaded = new AppSettingsStore(host.Paths).Load();
                    Assert.False(loaded.IsFatal);
                    Assert.Equal(shell.Regions.LeftFraction, loaded.Settings.Layout.LeftFraction, 6);
                    Assert.Equal(shell.Regions.RightFraction, loaded.Settings.Layout.RightFraction, 6);
                    Assert.Equal(shell.Regions.ConsoleFraction, loaded.Settings.Layout.ConsoleFraction, 6);
                    var json = await File.ReadAllTextAsync(host.Paths.SettingsFile);
                    Assert.DoesNotContain("\"leftWidth\"", json, StringComparison.Ordinal);
                    Assert.DoesNotContain("\"rightWidth\"", json, StringComparison.Ordinal);
                    Assert.DoesNotContain("\"consoleHeight\"", json, StringComparison.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public void TheFractionHelpersHandleDegenerateInput()
    {
        Assert.Equal(SettingsDefaults.MinSideFraction, ShellLayoutFractions.ClampSide(-1, SettingsDefaults.DefaultLeftFraction));
        Assert.Equal(SettingsDefaults.MaxSideFraction, ShellLayoutFractions.ClampSide(2, SettingsDefaults.DefaultLeftFraction));
        Assert.Equal(SettingsDefaults.DefaultLeftFraction, ShellLayoutFractions.ClampSide(double.NaN, SettingsDefaults.DefaultLeftFraction));
        Assert.Equal(SettingsDefaults.DefaultLeftFraction, ShellLayoutFractions.NormalizeSide(0.9, SettingsDefaults.DefaultLeftFraction));
        Assert.Equal(SettingsDefaults.DefaultConsoleFraction, ShellLayoutFractions.NormalizeConsole(double.PositiveInfinity));
        Assert.Equal(0d, ShellLayoutFractions.Centre(0.6, 0.6));
        Assert.Equal(0.4, ShellLayoutFractions.Documents(0.6), 10);
        Assert.Equal(0.2, ShellLayoutFractions.Share(20, 0, 0.2));
        Assert.Equal(0.2, ShellLayoutFractions.Share(20, 100, 0.2), 10);
        Assert.Equal(SettingsDefaults.MaxSideFraction, ShellLayoutFractions.FromLegacyPixels(9999, 1200, SettingsDefaults.MinSideFraction, SettingsDefaults.MaxSideFraction, SettingsDefaults.DefaultLeftFraction));
    }

    private static (double Left, double Centre, double Right) Columns(ShellWindow window)
    {
        var grid = Find<Grid>(window, "WorkspaceGrid");
        return (grid.ColumnDefinitions[0].ActualWidth, grid.ColumnDefinitions[2].ActualWidth, grid.ColumnDefinitions[4].ActualWidth);
    }

    private static (double Documents, double Console) Rows(ShellWindow window)
    {
        var grid = Find<Grid>(window, "CentreGrid");
        return (grid.RowDefinitions[0].ActualHeight, grid.RowDefinitions[2].ActualHeight);
    }

    private static T Find<T>(ShellWindow window, string name)
        where T : Control =>
        window.GetVisualDescendants()
            .OfType<T>()
            .Single(control => control.Name == name);

    private static void Drag(ShellWindow window, GridSplitter splitter, Vector delta)
    {
        var point = splitter.TranslatePoint(
            new Point(splitter.Bounds.Width / 2, splitter.Bounds.Height / 2),
            window) ?? throw new InvalidOperationException("The splitter is not attached to the window.");
        var target = new Point(point.X + delta.X, point.Y + delta.Y);
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        HeadlessSession.Pump(window);
        window.MouseMove(target, RawInputModifiers.LeftMouseButton);
        HeadlessSession.Pump(window);
        window.MouseUp(target, MouseButton.Left, RawInputModifiers.LeftMouseButton);
    }
}
