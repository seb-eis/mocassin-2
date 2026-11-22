// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Settings.Dialog;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Shell;
using Mocassin.App.Views.Theming;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Reads the realized shipped-control template parts and checks the fixed accent family delivery in Light, Dark,
/// and high contrast. These are headless PR-fast tests over the real shell and palette.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class AccentDeliveryTests
{
    private static readonly ThemeCase[] cases =
    [
        new(
            "light",
            new AppSettings { Theme = ThemeSelection.Light },
            ThemeVariant.Light,
            "#007ACC",
            "#007ACC",
            "#CCE5FF",
            "#1F1F1F",
            "#000000",
            "#FFFFFF",
            "#FFFFFF"),
        new(
            "dark",
            new AppSettings { Theme = ThemeSelection.Dark },
            ThemeVariant.Dark,
            "#007ACC",
            "#248DD3",
            "#094771",
            "#FFFFFF",
            "#FFFFFF",
            "#FFFFFF",
            "#FFFFFF"),
        new(
            "high contrast",
            new AppSettings { Theme = ThemeSelection.Dark, HighContrast = true },
            ThemeVariant.Dark,
            "#FFD700",
            "#FFD700",
            "#FFD700",
            "#000000",
            "#FFFFFF",
            "#000000",
            "#000000"),
    ];

    private static readonly string[] darkFocusSurfaces = ["#1E1E1E", "#252526", "#2D2D30", "#3C3C3C", "#37373D"];
    private static readonly string[] lightFocusSurfaces = ["#F3F3F3", "#FFFFFF"];

    [Fact]
    public Task TheSettingsSurfaceHasNoAccentChoiceAndTheDeterministicTabOrderSkipsIt() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var fixture = new ShellFixture(cases[0].Settings);
                fixture.Shell.Settings.Open();
                HeadlessSession.Pump(fixture.Window);

                var view = fixture.Window.GetVisualDescendants()
                    .OfType<Mocassin.App.Views.Settings.SettingsDialogView>().Single();
                var names = view.GetVisualDescendants().OfType<Control>()
                    .Where(control => control.Focusable && control.IsTabStop
                        && control.IsEffectivelyVisible && control.IsEffectivelyEnabled)
                    .Select(TabName)
                    .Where(name => name is not null)
                    .ToList();

                Assert.Equal(
                    [
                        "Theme",
                        "Density",
                        "Font scale",
                        "Log level",
                        "High contrast",
                        "Reduce motion",
                        "Confirm before reloading an externally changed project",
                        "Show the welcome document on a fresh start",
                        "Enable autosave",
                        "Autosave interval minutes",
                        "Clear recent",
                        "Open config folder",
                        "Clear logs and cache",
                        "Reset to defaults",
                        "Cancel",
                        "Save",
                    ],
                    names);
                Assert.DoesNotContain("Accent", names);
                Assert.DoesNotContain(
                    fixture.Window.GetVisualDescendants().OfType<Control>(),
                    control => AutomationProperties.GetName(control) == "Accent");
            });

    [Fact]
    public void TheSettingsModelHasNoAccentSelectionState()
    {
        Assert.Null(typeof(AppSettings).GetProperty("Accent"));
        Assert.Null(typeof(SettingsViewModel).GetProperty("SelectedAccent"));
        Assert.Null(typeof(SettingsViewModel).GetProperty("Accents"));
    }

    [Fact]
    public void NoAccentSelectionPathRemainsInTheApplicationSources()
    {
        var patterns = new[]
        {
            "AccentPalette",
            "SelectedAccent",
            @"\bAccents\b",
            @"\.Accent\b",
            @"\bAccent\s*=",
        };
        foreach (var project in new[] { "Mocassin.App", "Mocassin.App.ViewModels", "Mocassin.App.Views" })
        {
            var root = RepositoryPaths.Combine("src", "dotnet", project);
            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                         .Where(path => path.EndsWith(".cs", StringComparison.Ordinal)
                             || path.EndsWith(".axaml", StringComparison.Ordinal))
                         .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                             && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
            {
                var text = File.ReadAllText(file);
                foreach (var pattern in patterns)
                {
                    Assert.False(
                        System.Text.RegularExpressions.Regex.IsMatch(text, pattern),
                        $"{Path.GetRelativePath(RepositoryPaths.Root, file)} contains the removed accent-selection path '{pattern}'.");
                }
            }
        }
    }

    [Fact]
    public Task TheSettingsCheckBoxSliderAndTextBoxUseTheFixedFamily() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                foreach (var theme in cases)
                {
                    using var fixture = new ShellFixture(theme.Settings);
                    fixture.Shell.Settings.Open();
                    HeadlessSession.Pump(fixture.Window);

                    var checkBox = fixture.Window.GetVisualDescendants().OfType<CheckBox>()
                        .Single(control => AutomationProperties.GetName(control) == "High contrast");
                    var normal = checkBox.GetVisualDescendants().OfType<Border>()
                        .Single(border => border.Name == "NormalRectangle");
                    var glyph = checkBox.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>()
                        .Single(path => path.Name == "CheckGlyph");
                    var label = checkBox.GetVisualDescendants().OfType<ContentPresenter>()
                        .Single(presenter => presenter.Name == "PART_ContentPresenter");

                    checkBox.IsChecked = true;
                    HeadlessSession.Pump(fixture.Window);
                    AssertColor(theme.Accent, normal.Background);
                    AssertColor(theme.Accent, normal.BorderBrush);
                    AssertColor(theme.Glyph, glyph.Fill);
                    AssertColor(theme.Label, label.Foreground);

                    checkBox.IsChecked = null;
                    HeadlessSession.Pump(fixture.Window);
                    AssertColor(theme.Accent, normal.Background);
                    AssertColor(theme.Accent, normal.BorderBrush);
                    AssertColor(theme.Glyph, glyph.Fill);

                    var slider = fixture.Window.GetVisualDescendants().OfType<Slider>()
                        .Single(control => AutomationProperties.GetName(control) == "Font scale");
                    var thumb = slider.GetVisualDescendants().OfType<Thumb>().Single();
                    var decrease = slider.GetVisualDescendants().OfType<RepeatButton>()
                        .Single(button => button.Name == "PART_DecreaseButton");
                    AssertColor(theme.Accent, thumb.Background);
                    AssertColor(theme.Accent, decrease.Background);

                    var textBox = fixture.Window.GetVisualDescendants().OfType<TextBox>()
                        .Single(control => AutomationProperties.GetName(control) == "Autosave interval minutes");
                    AssertColor(theme.Accent, textBox.SelectionBrush);
                    AssertColor(theme.OnAccent, textBox.SelectionForegroundBrush);

                    AssertNotPlatformAccent(normal.Background, glyph.Fill, thumb.Background, textBox.SelectionBrush);
                }
            });

    [Fact]
    public Task TheSelectedTreeItemUsesTheTargetedStylesAndTheFixedSelectionTokens() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                foreach (var theme in cases)
                {
                    using var fixture = new ShellFixture(theme.Settings);
                    var entry = await ProjectFixtures.LoadCeriaAsync();
                    fixture.Shell.Workspace.Add(entry);
                    var parent = new ExplorerNode(
                        "accent.parent",
                        "Accent parent",
                        Children: [new ExplorerNode("accent.child", "Accent child")]);
                    fixture.Shell.Explorer.Nodes.Add(parent);
                    fixture.Shell.Explorer.SelectedNode = parent;
                    HeadlessSession.Pump(fixture.Window);

                    var tree = fixture.Window.GetVisualDescendants().OfType<TreeView>()
                        .Single(control => AutomationProperties.GetName(control) == "Project explorer");
                    var item = tree.GetVisualDescendants().OfType<TreeViewItem>()
                        .Single(container => ReferenceEquals(container.DataContext, parent));
                    item.IsExpanded = true;
                    HeadlessSession.Pump(fixture.Window);

                    var layoutRoot = item.GetVisualDescendants().OfType<Border>()
                        .First(border => border.Name == "PART_LayoutRoot");
                    var header = item.GetVisualDescendants().OfType<ContentPresenter>()
                        .First(presenter => presenter.Name == "PART_HeaderPresenter");
                    var chevron = item.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>()
                        .First(path => path.Name == "ChevronPath");

                    AssertColor(theme.SelectedFill, layoutRoot.Background);
                    AssertColor(theme.SelectedText, header.Foreground);
                    AssertColor(theme.SelectedText, chevron.Fill);
                    Assert.True(
                        Contrast(theme.SelectedText, theme.SelectedFill) >= 3.0,
                        $"{theme.Name}: selected tree text contrast was {Contrast(theme.SelectedText, theme.SelectedFill):F2}.");
                    AssertNotPlatformAccent(layoutRoot.Background);
                }
            });

    [Fact]
    public Task TheHighContrastPressedAndDisabledCheckBoxLabelsUseTheOverrides() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                var theme = cases[2];
                using var fixture = new ShellFixture(theme.Settings);
                fixture.Shell.Settings.Open();
                HeadlessSession.Pump(fixture.Window);

                var checkBox = fixture.Window.GetVisualDescendants().OfType<CheckBox>()
                    .Single(control => AutomationProperties.GetName(control) == "High contrast");
                var normal = checkBox.GetVisualDescendants().OfType<Border>()
                    .Single(border => border.Name == "NormalRectangle");
                var label = checkBox.GetVisualDescendants().OfType<ContentPresenter>()
                    .Single(presenter => presenter.Name == "PART_ContentPresenter");
                AssertColor("#FFFFFF", ResolveBrush("CheckBoxForegroundUncheckedPressed", ThemeVariant.Dark));

                var point = checkBox.TranslatePoint(
                    new Point(checkBox.Bounds.Width / 2, checkBox.Bounds.Height / 2),
                    fixture.Window);
                Assert.NotNull(point);
                fixture.Window.MouseDown(point.Value, MouseButton.Left);
                HeadlessSession.Pump(fixture.Window);
                AssertColor("#FFFFFF", label.Foreground);
                AssertColor("#FFD700", normal.Background);
                fixture.Window.MouseUp(point.Value, MouseButton.Left);
                HeadlessSession.Pump(fixture.Window);

                checkBox.IsEnabled = false;
                void AssertDisabledLabel(object? state)
                {
                    checkBox.IsChecked = state switch
                    {
                        "checked" => true,
                        "indeterminate" => null,
                        _ => false,
                    };
                    HeadlessSession.Pump(fixture.Window);
                    AssertColor("#A0A0A0", label.Foreground);
                }

                AssertDisabledLabel("unchecked");
                AssertDisabledLabel("checked");
                AssertDisabledLabel("indeterminate");
                checkBox.IsEnabled = true;
            });

    [Fact]
    public Task TheThemeSwitchUpdatesTheRealizedPartsWithoutAnAccentOverlay() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var fixture = new ShellFixture(cases[0].Settings);
                fixture.Shell.Settings.Open();
                HeadlessSession.Pump(fixture.Window);

                var checkBox = fixture.Window.GetVisualDescendants().OfType<CheckBox>()
                    .Single(control => AutomationProperties.GetName(control) == "High contrast");
                var normal = checkBox.GetVisualDescendants().OfType<Border>()
                    .Single(border => border.Name == "NormalRectangle");
                var slider = fixture.Window.GetVisualDescendants().OfType<Slider>()
                    .Single(control => AutomationProperties.GetName(control) == "Font scale");
                var thumb = slider.GetVisualDescendants().OfType<Thumb>().Single();

                checkBox.IsChecked = true;
                HeadlessSession.Pump(fixture.Window);
                AssertColor("#007ACC", normal.Background);
                AssertColor("#007ACC", thumb.Background);

                new AvaloniaThemeApplier().Apply(cases[1].Settings);
                HeadlessSession.Pump(fixture.Window);
                AssertColor("#007ACC", normal.Background);
                AssertColor("#007ACC", thumb.Background);
                AssertColor("#248DD3", ResolveBrush("Mocassin.FocusBrush", ThemeVariant.Dark));
                Assert.False(Application.Current!.Resources.ContainsKey("Mocassin.AccentBrush"));
                Assert.False(Application.Current.Resources.ContainsKey("Mocassin.OnAccentBrush"));
            });

    [Fact]
    public Task TheSplitterFocusAdornerAndSelectedTabPipeUseTheFocusToken() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                foreach (var theme in cases)
                {
                    using var fixture = new ShellFixture(theme.Settings);
                    var splitter = fixture.Window.GetVisualDescendants().OfType<GridSplitter>().First();
                    Assert.NotNull(splitter.FocusAdorner);
                    var adorner = Assert.IsType<Border>(splitter.FocusAdorner!.Build());
                    var probe = new Window { Content = adorner, Width = 120, Height = 120 };
                    probe.Show();
                    HeadlessSession.Pump(probe);
                    AssertColor(theme.Focus, adorner.BorderBrush);
                    Assert.Equal(new Thickness(2), adorner.BorderThickness);
                    probe.Close();

                    var selectedTab = fixture.Window.GetVisualDescendants().OfType<TabItem>()
                        .First(tab => tab.IsSelected);
                    var pipe = selectedTab.GetVisualDescendants().OfType<Border>()
                        .First(border => border.Name == "PART_SelectedPipe");
                    AssertColor(theme.Focus, pipe.Background);
                    var surface = theme.Name switch
                    {
                        "light" => "#F3F3F3",
                        "high contrast" => "#000000",
                        _ => "#3C3C3C",
                    };
                    Assert.True(
                        Contrast(theme.Focus, surface) >= 3.0,
                        $"{theme.Name}: focus token contrast was {Contrast(theme.Focus, surface):F2}.");
                }
            });

    [Fact]
    public Task TheProgressDialogProgressBarUsesTheFocusAccent() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                foreach (var theme in cases)
                {
                    using var fixture = new ShellFixture(theme.Settings);
                    using var progress = fixture.Host.Services.GetRequiredService<IDialogService>()
                        .ShowProgress(new ProgressDialogRequest("Working", "Working", CanCancel: false));
                    HeadlessSession.Pump(fixture.Window);

                    var content = fixture.Window.GetVisualDescendants().OfType<ContentControl>()
                        .Single(control => control.Name == "DialogContent");
                    var bar = content.GetVisualDescendants().OfType<ProgressBar>().Single();
                    AssertColor(theme.Focus, bar.Foreground);
                    AssertNotPlatformAccent(bar.Foreground);
                }
            });

    [Fact]
    public Task TheSelectedComboBoxItemUsesTheSelectedFillAndText() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                foreach (var theme in cases)
                {
                    using var fixture = new ShellFixture(theme.Settings);
                    fixture.Shell.Settings.Open();
                    HeadlessSession.Pump(fixture.Window);
                    var combo = fixture.Window.GetVisualDescendants().OfType<ComboBox>()
                        .Single(control => AutomationProperties.GetName(control) == "Log level");
                    combo.SelectedIndex = 1;
                    combo.IsDropDownOpen = true;
                    HeadlessSession.Pump(fixture.Window);

                    var item = Assert.IsType<ComboBoxItem>(combo.ContainerFromIndex(1));
                    var presenter = item.GetVisualDescendants().OfType<ContentPresenter>()
                        .Single(presenter => presenter.Name == "PART_ContentPresenter");
                    AssertColor(theme.SelectedFill, presenter.Background);
                    AssertColor(theme.SelectedText, presenter.Foreground);
                    Assert.True(
                        Contrast(theme.SelectedText, theme.SelectedFill) >= 4.5,
                        $"{theme.Name}: selected combo text contrast was {Contrast(theme.SelectedText, theme.SelectedFill):F2}.");
                    combo.IsDropDownOpen = false;
                }
            });

    [Fact]
    public Task TheNeutralFocusPairMeasuresByLayerInEveryTheme() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                var applier = new AvaloniaThemeApplier();
                foreach (var theme in cases)
                {
                    applier.Apply(theme.Settings);
                    var primary = ResolveHex("SystemControlFocusVisualPrimaryBrush", theme.Variant);
                    var secondary = ResolveHex("SystemControlFocusVisualSecondaryBrush", theme.Variant);
                    var surfaces = theme.Name == "light" ? lightFocusSurfaces
                        : theme.Name == "dark" ? darkFocusSurfaces
                        : ["#000000"];
                    foreach (var surface in surfaces)
                    {
                        AssertAtLeast(primary, surface, 3.0);
                    }

                    AssertAtLeast(secondary, primary, 3.0);
                }
            });

    private static string? TabName(Control control) =>
        AutomationProperties.GetName(control) is { Length: > 0 } name
            ? name
            : control is Button { Content: string content }
                ? content
                : null;

    private static void AssertColor(string expected, IBrush? brush)
    {
        Assert.NotNull(brush);
        var color = Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
        Assert.Equal(Color.Parse(expected), color);
    }

    private static void AssertNotPlatformAccent(params IBrush?[] brushes)
    {
        foreach (var brush in brushes)
        {
            if (brush is ISolidColorBrush solid)
            {
                Assert.NotEqual(Color.Parse("#0078D7"), solid.Color);
            }
        }
    }

    private static ISolidColorBrush ResolveBrush(string key, ThemeVariant variant)
    {
        Assert.True(
            Application.Current!.Resources.TryGetResource(key, variant, out var value),
            $"{key} did not resolve under {variant}.");
        return Assert.IsAssignableFrom<ISolidColorBrush>(value);
    }

    private static string ResolveHex(string key, ThemeVariant variant) =>
        ResolveBrush(key, variant).Color.ToString();

    private static void AssertAtLeast(string foreground, string background, double minimum) =>
        Assert.True(
            Contrast(foreground, background) >= minimum,
            $"{foreground} on {background} was {Contrast(foreground, background):F2}, expected {minimum}.");

    private static double Contrast(string first, string second) =>
        Contrast(Color.Parse(first), Color.Parse(second));

    private static double Contrast(Color first, Color second)
    {
        var light = Math.Max(Luminance(first), Luminance(second));
        var dark = Math.Min(Luminance(first), Luminance(second));
        return (light + 0.05) / (dark + 0.05);
    }

    private static double Luminance(Color color) =>
        (0.2126 * Linear(color.R)) + (0.7152 * Linear(color.G)) + (0.0722 * Linear(color.B));

    private static double Linear(byte channel)
    {
        var value = channel / 255d;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private sealed record ThemeCase(
        string Name,
        AppSettings Settings,
        ThemeVariant Variant,
        string Accent,
        string Focus,
        string SelectedFill,
        string SelectedText,
        string Label,
        string Glyph,
        string OnAccent);

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
