// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Shell;
using Mocassin.App.Views.Theming;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Reads the realized Fluent TabItem parts and verifies the tab treatment in light, dark, and high contrast.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class TabTreatmentTests
{
    private static readonly ThemeCase[] cases =
    [
        new("light", new AppSettings { Theme = ThemeSelection.Light }, ThemeVariant.Light, "#FFFFFF", "#ECECEC"),
        new("dark", new AppSettings { Theme = ThemeSelection.Dark }, ThemeVariant.Dark, "#252526", "#2D2D30"),
        new(
            "high contrast",
            new AppSettings { Theme = ThemeSelection.Dark, HighContrast = true, Density = Density.Compact },
            ThemeVariant.Dark,
            "#000000",
            "#000000"),
    ];

    [Fact]
    public Task TheSelectedAndInactiveTabsRealizeTheHeaderTreatment() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                foreach (var theme in cases)
                {
                    using var fixture = new ShellFixture(theme.Settings);
                    var tabs = Tabs(fixture.Window, "Console tabs");
                    var selected = Assert.Single(tabs, tab => tab.IsSelected);
                    var inactive = Assert.Single(tabs, tab => !tab.IsSelected);
                    var control = fixture.Window.GetVisualDescendants()
                        .OfType<TabControl>()
                        .Single(tab => AutomationProperties.GetName(tab) == "Console tabs");
                    var contentHost = control.GetVisualDescendants()
                        .OfType<ContentPresenter>()
                        .Single(presenter => presenter.Name == "PART_SelectedContentHost");
                    var headerMargin = Assert.IsType<Thickness>(
                        ResolveResource("TabItemHeaderMargin", theme.Variant));

                    AssertTab(theme, selected, selected: true, headerMargin);
                    AssertTab(theme, inactive, selected: false, headerMargin);

                    var selectedRoot = Part<Border>(selected, "PART_LayoutRoot");
                    var inactiveRoot = Part<Border>(inactive, "PART_LayoutRoot");
                    AssertColor(theme.ActiveFill, selectedRoot.Background);
                    AssertColor(theme.InactiveFill, inactiveRoot.Background);
                    Assert.Equal(new CornerRadius(4, 4, 0, 0), selectedRoot.CornerRadius);
                    Assert.Equal(new CornerRadius(4, 4, 0, 0), inactiveRoot.CornerRadius);

                    var pipe = Part<Border>(selected, "PART_SelectedPipe");
                    Assert.True(pipe.IsVisible);
                    AssertColor(ResolveHex("Mocassin.FocusBrush", theme.Variant), pipe.Background);
                    Assert.Equal(2d, pipe.Height);
                    Assert.Equal(VerticalAlignment.Top, pipe.VerticalAlignment);
                    Assert.Equal(new Thickness(4, 0, 4, 0), pipe.Margin);
                    Assert.Equal(default, pipe.CornerRadius);
                    Assert.False(Part<Border>(inactive, "PART_SelectedPipe").IsVisible);

                    AssertGeometry(fixture.Window, selected, contentHost, pipe);
                    Assert.Equal("Console", selected.Header?.ToString());
                    Assert.Equal("Validation", inactive.Header?.ToString());
                }
            });

    [Fact]
    public Task PointerOverAndPressUseTheStateTokens() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                foreach (var theme in cases)
                {
                    using var fixture = new ShellFixture(theme.Settings);
                    var tabs = Tabs(fixture.Window, "Console tabs");
                    var selected = Assert.Single(tabs, tab => tab.IsSelected);
                    var inactive = Assert.Single(tabs, tab => !tab.IsSelected);
                    var selectedRoot = Part<Border>(selected, "PART_LayoutRoot");
                    var inactiveRoot = Part<Border>(inactive, "PART_LayoutRoot");

                    var inactivePoint = Centre(inactive, fixture.Window);
                    fixture.Window.MouseMove(inactivePoint, RawInputModifiers.None);
                    HeadlessSession.Pump(fixture.Window);
                    AssertColor(ResolveHex("Mocassin.HoverBackground", theme.Variant), inactiveRoot.Background);

                    // A tab selects on pointer press, so a pressed inactive tab has already become the selected tab.
                    fixture.Window.MouseDown(inactivePoint, MouseButton.Left);
                    HeadlessSession.Pump(fixture.Window);
                    Assert.True(inactive.IsSelected);
                    Assert.False(selected.IsSelected);
                    AssertColor(theme.ActiveFill, inactiveRoot.Background);
                    fixture.Window.MouseUp(inactivePoint, MouseButton.Left);
                    HeadlessSession.Pump(fixture.Window);

                    var selectedPoint = Centre(inactive, fixture.Window);
                    fixture.Window.MouseMove(selectedPoint, RawInputModifiers.None);
                    HeadlessSession.Pump(fixture.Window);
                    AssertColor(theme.ActiveFill, inactiveRoot.Background);

                    // The unselected pressed key is not reachable with a pointer because press selects the tab;
                    // the equality guard keeps it pinned to PressedBackground.
                    AssertColor(
                        ResolveHex("Mocassin.PressedBackground", theme.Variant),
                        ResolveBrush("TabItemHeaderBackgroundUnselectedPressed", theme.Variant));
                }
            });

    [Fact]
    public Task TheTabRolesEqualTheRealizedFluentKeysInEveryTheme() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                var applier = new AvaloniaThemeApplier();
                foreach (var theme in cases)
                {
                    applier.Apply(theme.Settings);

                    AssertColor(ResolveHex("Mocassin.TabActiveBackground", theme.Variant), ResolveBrush("TabItemHeaderBackgroundSelected", theme.Variant));
                    AssertColor(ResolveHex("Mocassin.WorkspaceBackground", theme.Variant), ResolveBrush("TabItemHeaderBackgroundSelected", theme.Variant));
                    AssertColor(ResolveHex("Mocassin.TabInactiveBackground", theme.Variant), ResolveBrush("TabItemHeaderBackgroundUnselected", theme.Variant));
                    AssertColor(ResolveHex("Mocassin.TabActiveForeground", theme.Variant), ResolveBrush("TabItemHeaderForegroundSelected", theme.Variant));
                    AssertColor(ResolveHex("Mocassin.TabInactiveForeground", theme.Variant), ResolveBrush("TabItemHeaderForegroundUnselected", theme.Variant));
                    AssertColor(ResolveHex("Mocassin.FocusBrush", theme.Variant), ResolveBrush("TabItemHeaderSelectedPipeFill", theme.Variant));
                    AssertColor(ResolveHex("Mocassin.HoverBackground", theme.Variant), ResolveBrush("TabItemHeaderBackgroundUnselectedPointerOver", theme.Variant));
                    AssertColor(ResolveHex("Mocassin.PressedBackground", theme.Variant), ResolveBrush("TabItemHeaderBackgroundUnselectedPressed", theme.Variant));
                    AssertColor(ResolveHex("Mocassin.TabActiveBackground", theme.Variant), ResolveBrush("TabItemHeaderBackgroundSelectedPointerOver", theme.Variant));
                    AssertColor(ResolveHex("Mocassin.TabActiveBackground", theme.Variant), ResolveBrush("TabItemHeaderBackgroundSelectedPressed", theme.Variant));

                    Assert.Equal(
                        Assert.IsType<double>(ResolveResource("Mocassin.TabHeight", theme.Variant)),
                        Assert.IsType<double>(ResolveResource("TabItemMinHeight", theme.Variant)));
                    Assert.Equal(
                        new Thickness(4, 0, 4, 0),
                        Assert.IsType<Thickness>(ResolveResource("Mocassin.TabIndicatorInset", theme.Variant)));
                    Assert.Equal(
                        new Thickness(0),
                        Assert.IsType<Thickness>(ResolveResource("TabControlTopPlacementItemMargin", theme.Variant)));
                }
            });

    [Fact]
    public Task TheRetiredStripSurfaceIsGoneFromEveryDictionary() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                foreach (var theme in cases)
                {
                    new AvaloniaThemeApplier().Apply(theme.Settings);
                    Assert.False(
                        Application.Current!.Resources.TryGetResource("Mocassin.TabStripBackground", theme.Variant, out _),
                        $"{theme.Name}: Mocassin.TabStripBackground still resolves.");
                }
            });

    private static void AssertTab(ThemeCase theme, TabItem tab, bool selected, Thickness headerMargin)
    {
        Assert.Equal(new Thickness(0), tab.Padding);
        Assert.Equal(new CornerRadius(4, 4, 0, 0), tab.CornerRadius);
        var presenter = Part<ContentPresenter>(tab, "PART_ContentPresenter");
        Assert.Equal(headerMargin, presenter.Margin);
        var expected = selected
            ? ResolveHex("Mocassin.TabActiveForeground", theme.Variant)
            : ResolveHex("Mocassin.TabInactiveForeground", theme.Variant);
        AssertColor(expected, presenter.Foreground ?? tab.Foreground);
    }

    private static void AssertGeometry(
        ShellWindow window,
        TabItem selected,
        ContentPresenter contentHost,
        Border pipe)
    {
        var tabBottom = Translate(selected, new Point(0, selected.Bounds.Height), window).Y;
        var contentTop = Translate(contentHost, new Point(0, 0), window).Y;
        Assert.True(
            Math.Abs(tabBottom - contentTop) <= 1,
            $"The selected tab bottom ({tabBottom:F1}) does not meet the content top ({contentTop:F1}).");

        var tabTop = Translate(selected, new Point(0, 0), window).Y;
        var pipeTop = Translate(pipe, new Point(0, 0), window).Y;
        Assert.True(
            Math.Abs(tabTop - pipeTop) <= 1,
            $"The indicator top ({pipeTop:F1}) is not at the tab top ({tabTop:F1}).");
    }

    private static Point Centre(Control control, TopLevel topLevel) =>
        Translate(control, new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), topLevel);

    private static Point Translate(Avalonia.Visual visual, Point point, Avalonia.Visual relativeTo) =>
        visual.TranslatePoint(point, relativeTo)
        ?? throw new InvalidOperationException("The control is not attached to the window.");

    private static T Part<T>(TabItem tab, string name)
        where T : Control =>
        tab.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static List<TabItem> Tabs(ShellWindow window, string name) =>
        window.GetVisualDescendants()
            .OfType<TabControl>()
            .Single(control => AutomationProperties.GetName(control) == name)
            .GetVisualDescendants()
            .OfType<TabItem>()
            .ToList();

    private static ISolidColorBrush ResolveBrush(string key, ThemeVariant variant)
    {
        return Assert.IsAssignableFrom<ISolidColorBrush>(ResolveResource(key, variant));
    }

    private static string ResolveHex(string key, ThemeVariant variant)
    {
        var color = ResolveBrush(key, variant).Color;
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static object ResolveResource(string key, ThemeVariant variant)
    {
        Assert.True(
            Application.Current!.Resources.TryGetResource(key, variant, out var value),
            $"{key} did not resolve under {variant}.");
        Assert.NotNull(value);
        return value;
    }

    private static void AssertColor(string expected, IBrush? brush)
    {
        Assert.NotNull(brush);
        Assert.Equal(Color.Parse(expected), Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color);
    }

    private sealed record ThemeCase(
        string Name,
        AppSettings Settings,
        ThemeVariant Variant,
        string ActiveFill,
        string InactiveFill)
    {
        public Thickness HeaderMargin => Name == "high contrast" ? new Thickness(6, 2) : new Thickness(8, 3);
    }

    private sealed class ShellFixture : IDisposable
    {
        public ShellFixture(AppSettings settings)
        {
            Workspace = new TempWorkspace();
            Host = AppHost.Build(new AppStartupOptions { DataDirectory = Workspace.Path });
            new AvaloniaThemeApplier().Apply(settings);
            Window = Host.Services.GetRequiredService<ShellWindow>();
            Window.Width = 1000;
            Window.Height = 700;
            Window.Show();
            HeadlessSession.Pump(Window);
            Shell = Assert.IsType<ShellViewModel>(Window.DataContext);
        }

        public TempWorkspace Workspace { get; }

        public AppHost Host { get; }

        public ShellWindow Window { get; }

        public ShellViewModel Shell { get; }

        public void Dispose()
        {
            Window.Close();
            Host.Dispose();
            Workspace.Dispose();
        }
    }
}
