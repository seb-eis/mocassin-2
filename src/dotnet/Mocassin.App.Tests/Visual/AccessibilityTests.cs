// SPDX-License-Identifier: MIT

using System.Globalization;
using Avalonia;
using System.Text.RegularExpressions;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Icons;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Consoles;
using Mocassin.App.Views.Documents;
using Mocassin.App.Views.Settings;
using Mocassin.App.Views.Shell;
using Mocassin.App.Views.Theming;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Sweeps the primary interactive shell controls for an accessible name.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class AccessibilityTests
{
    private static readonly string[] regionNames =
        ["Projects panel", "Documents panel", "Inspector panel", "Console panel"];

    private static readonly string[] explorerLabels = ["Overview", "Data (JSON)"];

    [Fact]
    public Task EveryPrimaryInteractiveControlHasAnAccessibleName() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();

                try
                {
                    var controls = window.GetVisualDescendants()
                        .OfType<Control>()
                        .Where(control => control is Button or TextBox or ComboBox or CheckBox or Slider
                            or ListBox or TreeView or Menu or TabControl)
                        .Where(control => !control.Name?.StartsWith("PART_", StringComparison.Ordinal) ?? true);
                    foreach (var control in controls)
                    {
                        var name = AutomationProperties.GetName(control);
                        var fallback = control switch
                        {
                            Button button => button.Content as string,
                            TextBox textBox => textBox.PlaceholderText,
                            _ => null,
                        };
                        Assert.False(
                            string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(fallback),
                            $"A {control.GetType().Name} has no accessible name.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task PrimaryControlsExposeAutomationRoles() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();

                try
                {
                    var expectations = new (string Name, Func<Control, bool> Match, AutomationControlType Type)[]
                    {
                        ("Main menu", control => control is Menu, AutomationControlType.Menu),
                        ("Document tabs", control => control is TabControl, AutomationControlType.Tab),
                        ("Open projects", control => control is ListBox, AutomationControlType.List),
                        ("Project explorer", control => control is TreeView, AutomationControlType.Tree),
                        ("Application console filter", control => control is TextBox, AutomationControlType.Edit),
                    };
                    foreach (var (name, match, type) in expectations)
                    {
                        var control = window.GetVisualDescendants()
                            .OfType<Control>()
                            .Single(element => AutomationProperties.GetName(element) == name && match(element));
                        var peer = ControlAutomationPeer.CreatePeerForElement(control);
                        Assert.NotNull(peer);
                        Assert.Equal(type, peer.GetAutomationControlType());
                    }
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task OverlayRootsHaveAccessibleNames() =>
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
                    shell.ShowPaletteCommand.Execute(null);
                    HeadlessSession.Pump(window);
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<Grid>(),
                        control => AutomationProperties.GetName(control) == "Command palette");
                    shell.ClosePaletteCommand.Execute(null);

                    shell.ShowShortcutsCommand.Execute(null);
                    HeadlessSession.Pump(window);
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<Grid>(),
                        control => AutomationProperties.GetName(control) == "Keyboard shortcuts");
                    shell.CloseShortcutsCommand.Execute(null);

                    shell.Settings.Open();
                    HeadlessSession.Pump(window);
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<Grid>(),
                        control => AutomationProperties.GetName(control) == "Settings");
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TabOrderStaysInsideTheOpenLayer() =>
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
                    shell.Settings.Open();
                    HeadlessSession.Pump(window);
                    var layer = Assert.Single(window.GetVisualDescendants().OfType<SettingsDialogView>());

                    for (var step = 0; step < 8; step++)
                    {
                        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
                        HeadlessSession.Pump(window);
                        var focused = Assert.IsAssignableFrom<Control>(window.FocusManager?.GetFocusedElement());
                        Assert.Contains(layer, focused.GetVisualAncestors());
                    }
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task RegionCardsAndTheConsoleTabStripExposeTheirNames() =>
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
                    var borders = window.GetVisualDescendants().OfType<Border>().ToList();
                    foreach (var name in regionNames)
                    {
                        Assert.Contains(borders, border => AutomationProperties.GetName(border) == name);
                    }

                    var console = window.GetVisualDescendants()
                        .OfType<TabControl>()
                        .Single(control => AutomationProperties.GetName(control) == "Console tabs");
                    var tabs = console.GetVisualDescendants().OfType<TabItem>().ToList();
                    Assert.Equal(["Console", "Validation"], tabs.Select(tab => tab.Header?.ToString()).ToList());
                    Assert.All(tabs, tab => Assert.False(string.IsNullOrWhiteSpace(tab.Header?.ToString())));
                    Assert.DoesNotContain(
                        window.GetVisualDescendants().OfType<ComboBox>(),
                        control => AutomationProperties.GetName(control) == "Console channel");
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheValidationContentUsesValidationLabelsAndAutomationNames() =>
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
                    shell.Console.ActiveChannel = shell.Console.Channels.Single(channel => channel.Id == "problems");
                    HeadlessSession.Pump(window);
                    Assert.Single(window.GetVisualDescendants().OfType<ProblemsView>());
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<TextBox>(),
                        control => AutomationProperties.GetName(control) == "Validation text filter");
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<ListBox>(),
                        control => AutomationProperties.GetName(control) == "Validation entries");
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<ComboBox>(),
                        control => AutomationProperties.GetName(control) == "Validation severity filter");
                    Assert.DoesNotContain(
                        window.GetVisualDescendants().OfType<TextBlock>(),
                        control => control.Text?.StartsWith("Problems —", StringComparison.Ordinal) == true);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task PrimaryLabelsDoNotClipAtOneAndAHalfScaleInCompactDensity() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                File.WriteAllText(
                    Path.Combine(workspace.Path, "Mocassin.Settings.json"),
                    """{"format":"mocassin.settings","fontScale":1.5,"density":"compact"}""");
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                new AvaloniaThemeApplier().Apply(
                    AppSettings.Default with { FontScale = 1.5, Density = Density.Compact, Theme = ThemeSelection.Light });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    shell.Settings.Open();
                    HeadlessSession.Pump(window);

                    var labels = new List<TextBlock>();
                    labels.AddRange(window.GetVisualDescendants().OfType<StatusBarView>().SelectMany(view => view.GetVisualDescendants().OfType<TextBlock>()));
                    labels.AddRange(
                        window.GetVisualDescendants()
                            .OfType<TabControl>()
                            .SelectMany(control => control.GetVisualDescendants().OfType<TabItem>())
                            .SelectMany(tab => tab.GetVisualDescendants().OfType<TextBlock>()));
                    labels.AddRange(
                        window.GetVisualDescendants()
                            .OfType<SettingsDialogView>()
                            .SelectMany(view => view.GetVisualDescendants().OfType<Button>())
                            .SelectMany(button => button.GetVisualDescendants().OfType<TextBlock>()));
                    labels.AddRange(window.GetVisualDescendants().OfType<WelcomeDocumentView>().First()
                        .GetVisualDescendants().OfType<Button>().SelectMany(button => button.GetVisualDescendants().OfType<TextBlock>()));

                    var checkedLabels = 0;
                    foreach (var label in labels.Where(label => !string.IsNullOrWhiteSpace(label.Text))
                                 .Where(label => label.IsEffectivelyVisible)
                                 .Where(label => label.TextWrapping == TextWrapping.NoWrap)
                                 .Where(label => label.TextTrimming == TextTrimming.None))
                    {
                        var formatted = new FormattedText(
                            label.Text!,
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface(label.FontFamily, label.FontStyle, label.FontWeight),
                            label.FontSize,
                            label.Foreground);
                        Assert.True(
                            label.Bounds.Width + 0.5 >= formatted.Width,
                            $"'{label.Text}' is clipped horizontally ({label.Bounds.Width:F1} < {formatted.Width:F1}).");
                        Assert.True(
                            label.Bounds.Height + 0.5 >= formatted.Height,
                            $"'{label.Text}' is clipped vertically ({label.Bounds.Height:F1} < {formatted.Height:F1}).");
                        checkedLabels++;
                    }

                    Assert.True(checkedLabels >= 8, $"Only {checkedLabels} primary labels were checked.");
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public void ViewXamlUsesTokensInsteadOfColourLiterals()
    {
        var views = RepositoryPaths.Combine("src", "dotnet", "Mocassin.App.Views");
        var app = RepositoryPaths.Combine("src", "dotnet", "Mocassin.App", "App.axaml");
        var files = Directory.EnumerateFiles(views, "*.axaml", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.Combine("Themes", string.Empty), StringComparison.Ordinal))
            .Where(path => !path.Contains(Path.Combine("Styles", string.Empty), StringComparison.Ordinal))
            .Append(app);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            var literal = Regex.Match(text, "#[0-9A-Fa-f]{6,8}");
            Assert.False(
                literal.Success,
                $"{file} contains the colour literal {literal.Value}; use a Mocassin.* role.");
        }
    }

    [Fact]
    public Task TabsShowAFocusRingAndKeepTheIconSlotDecorative() =>
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
                    var console = window.GetVisualDescendants()
                        .OfType<TabControl>()
                        .Single(control => AutomationProperties.GetName(control) == "Console tabs");
                    var consoleTabs = console.GetVisualDescendants().OfType<TabItem>().ToList();
                    Assert.Equal(["Console", "Validation"], consoleTabs.Select(tab => tab.Header?.ToString()).ToList());

                    var documents = window.GetVisualDescendants()
                        .OfType<TabControl>()
                        .Single(control => AutomationProperties.GetName(control) == "Document tabs");
                    var documentTabs = documents.GetVisualDescendants().OfType<TabItem>().ToList();
                    Assert.NotEmpty(documentTabs);

                    foreach (var tab in consoleTabs)
                    {
                        var slot = tab.GetVisualDescendants()
                            .OfType<ContentControl>()
                            .Single(control => control.Name == "TabIconSlot");
                        AssertDecorativeIconSlot(slot);
                        Assert.False(slot.IsVisible);
                        Assert.Null(slot.Content);
                    }

                    foreach (var tab in documentTabs)
                    {
                        var slot = tab.GetVisualDescendants()
                            .OfType<ContentControl>()
                            .Single(control => control.Name == "TabIconSlot");
                        AssertDecorativeIconSlot(slot);
                        Assert.True(slot.IsVisible);
                        Assert.IsType<AppIcon>(slot.Content);
                    }

                    var selectedHeader = consoleTabs.Single(tab => tab.IsSelected).Header;
                    var focusedTab = consoleTabs[0];
                    focusedTab.Focus();
                    HeadlessSession.Pump(window);
                    Assert.True(focusedTab.IsFocused);
                    Assert.NotNull(focusedTab.FocusAdorner);
                    var adorner = Assert.IsType<Border>(focusedTab.FocusAdorner!.Build());
                    var probe = new Window { Content = adorner, Width = 120, Height = 120 };
                    probe.Show();
                    HeadlessSession.Pump(probe);
                    Assert.Equal(new Thickness(2), adorner.BorderThickness);
                    Assert.Equal(new CornerRadius(4, 4, 0, 0), adorner.CornerRadius);
                    var focusBrush = Assert.IsAssignableFrom<ISolidColorBrush>(adorner.BorderBrush);
                    Assert.Equal(
                        Assert.IsAssignableFrom<ISolidColorBrush>(
                            Application.Current!.Resources.TryGetResource("Mocassin.FocusBrush", null, out var value)
                                ? value
                                : null).Color,
                        focusBrush.Color);
                    probe.Close();
                    Assert.Equal(selectedHeader, consoleTabs.Single(tab => tab.IsSelected).Header);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TabOrderIsDeterministic() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                static List<string> CaptureOrder()
                {
                    using var workspace = new TempWorkspace();
                    using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                    var window = host.Services.GetRequiredService<ShellWindow>();
                    window.Show();
                    try
                    {
                        var order = new List<string>();
                        for (var step = 0; step < 8; step++)
                        {
                            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
                            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                            var focused = window.FocusManager?.GetFocusedElement() as Control;
                            Assert.NotNull(focused);
                            order.Add(
                                string.Join(
                                    ":",
                                    focused.GetType().Name,
                                    AutomationProperties.GetName(focused) ?? focused.Name ?? "-"));
                        }

                        Assert.True(order.Distinct(StringComparer.Ordinal).Count() >= 4, string.Join(", ", order));
                        return order;
                    }
                    finally
                    {
                        window.Close();
                    }
                }

                var first = CaptureOrder();
                var second = CaptureOrder();
                Assert.Equal(first, second);
            });

    [Fact]
    public Task ExplorerAndProjectRowIconSlotsAreDecorativeAndFixedAtScale() =>
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
                    var entry = await ProjectFixtures.LoadCeriaAsync();
                    shell.Workspace.Add(entry);
                    new AvaloniaThemeApplier().Apply(
                        AppSettings.Default with { Density = Density.Compact, FontScale = 1.5 });
                    HeadlessSession.Pump(window);

                    var tree = window.GetVisualDescendants()
                        .OfType<TreeView>()
                        .Single(control => AutomationProperties.GetName(control) == "Project explorer");
                    var nodes = tree.GetVisualDescendants()
                        .OfType<TreeViewItem>()
                        .Where(node => node.GetVisualDescendants().OfType<ContentControl>().Any(control => control.Name == "NodeIconSlot" && control.IsEffectivelyVisible))
                        .ToList();
                    Assert.Equal(2, nodes.Count);
                    foreach (var node in nodes)
                    {
                        var slot = node.GetVisualDescendants()
                            .OfType<ContentControl>()
                            .Single(control => control.Name == "NodeIconSlot");
                        AssertDecorativeIconSlot(slot);
                        Assert.True(slot.IsEffectivelyVisible);
                        Assert.Equal(16d, slot.Bounds.Width);
                        Assert.Equal(16d, slot.Bounds.Height);
                        var glyph = slot.GetVisualDescendants()
                            .OfType<Avalonia.Controls.Shapes.Path>()
                            .Single(path => path.IsVisible);
                        Assert.True(glyph.Bounds.Width <= 16.5, $"Node glyph is clipped ({glyph.Bounds.Width}).");
                        Assert.True(glyph.Bounds.Height <= 16.5, $"Node glyph is clipped ({glyph.Bounds.Height}).");
                        var label = node.GetVisualDescendants().OfType<TextBlock>().First().Text;
                        Assert.Contains(label, explorerLabels);
                    }

                    var projects = window.GetVisualDescendants()
                        .OfType<ListBox>()
                        .Single(control => AutomationProperties.GetName(control) == "Open projects");
                    var row = Assert.Single(projects.GetVisualDescendants().OfType<ListBoxItem>());
                    var rowSlot = row.GetVisualDescendants()
                        .OfType<ContentControl>()
                        .Single(control => control.Name == "ProjectRowIconSlot");
                    AssertDecorativeIconSlot(rowSlot);
                    Assert.True(rowSlot.IsEffectivelyVisible);
                    Assert.Equal(16d, rowSlot.Bounds.Width);
                    Assert.Equal(16d, rowSlot.Bounds.Height);
                }
                finally
                {
                    window.Close();
                }
            });

    private static void AssertDecorativeIconSlot(ContentControl slot)
    {
        Assert.Equal(16d, slot.Width);
        Assert.Equal(16d, slot.Height);
        Assert.False(slot.Focusable);
        Assert.False(slot.IsHitTestVisible);
        Assert.Empty(AutomationProperties.GetName(slot) ?? string.Empty);
        Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(slot));
    }
}
