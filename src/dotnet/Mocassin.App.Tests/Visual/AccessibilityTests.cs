// SPDX-License-Identifier: MIT

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Settings;
using Mocassin.App.Views.Shell;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Sweeps the primary interactive shell controls for an accessible name.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class AccessibilityTests
{
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
}
