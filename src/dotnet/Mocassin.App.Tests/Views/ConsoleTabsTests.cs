// SPDX-License-Identifier: MIT

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Consoles;
using Mocassin.App.Views.Shell;

namespace Mocassin.App.Tests.Views;

/// <summary>
/// Headless tests for the fixed Console and Validation tabs, persistence, and their keyboard mapping.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ConsoleTabsTests
{
    [Fact]
    public Task TheConsoleHasExactlyTheTwoFixedTabsInOrder() =>
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
                    Assert.Collection(
                        shell.Console.Channels,
                        channel =>
                        {
                            Assert.Equal("application", channel.Id);
                            Assert.Equal("Console", channel.TabTitle);
                        },
                        channel =>
                        {
                            Assert.Equal("problems", channel.Id);
                            Assert.Equal("Validation", channel.TabTitle);
                        });

                    var tabs = FindTabs(window);
                    Assert.Equal(2, tabs.Count);
                    Assert.Equal(["Console", "Validation"], TabLabels(tabs));
                    Assert.All(
                        tabs,
                        tab =>
                        {
                            var slot = tab.GetVisualDescendants()
                                .OfType<ContentControl>()
                                .Single(control => control.Name == "TabIconSlot");
                            Assert.False(slot.IsVisible);
                            Assert.False(slot.Focusable);
                            Assert.False(slot.IsHitTestVisible);
                            Assert.Null(slot.Content);
                            Assert.Equal(16d, slot.Width);
                            Assert.Equal(16d, slot.Height);
                            Assert.Null(Assert.IsAssignableFrom<ConsoleChannelViewModel>(tab.Header).Icon);
                        });
                    Assert.Equal("application", Assert.IsAssignableFrom<ConsoleChannelViewModel>(tabs[0].Header).Id);
                    Assert.Equal("problems", Assert.IsAssignableFrom<ConsoleChannelViewModel>(tabs[1].Header).Id);
                    Assert.DoesNotContain(
                        window.GetVisualDescendants().OfType<ComboBox>(),
                        control => AutomationProperties.GetName(control) == "Console channel");
                    Assert.Equal("Console tabs", AutomationProperties.GetName(FindTabControl(window)));

                    shell.Console.ActiveChannel = shell.Console.Channels[1];
                    HeadlessSession.Pump(window);
                    Assert.Single(window.GetVisualDescendants().OfType<ProblemsView>());
                    Assert.DoesNotContain(window.GetVisualDescendants().OfType<ApplicationConsoleView>(), view => view.IsVisible);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task ArrowKeysSelectAndPlainEnterIsANoOp() =>
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
                    var tabs = FindTabs(window);
                    tabs[0].Focus();
                    HeadlessSession.Pump(window);

                    window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
                    HeadlessSession.Pump(window);
                    Assert.Equal("problems", shell.Console.ActiveChannel?.Id);

                    window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
                    HeadlessSession.Pump(window);
                    Assert.Equal("application", shell.Console.ActiveChannel?.Id);

                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    HeadlessSession.Pump(window);
                    Assert.Equal("application", shell.Console.ActiveChannel?.Id);

                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                    HeadlessSession.Pump(window);
                    Assert.True(FindTabControl(window).IsVisible);
                    Assert.Equal("application", shell.Console.ActiveChannel?.Id);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheTabStripHasNoCloseOrReorderControlsAndIsOneTabStop() =>
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
                    var tabs = FindTabs(window);
                    Assert.All(
                        tabs,
                        tab => Assert.Empty(tab.GetVisualDescendants().OfType<Button>()));
                    Assert.DoesNotContain(
                        FindTabControl(window).GetVisualDescendants().OfType<Button>(),
                        button => AutomationProperties.GetName(button)?.Contains("Close", StringComparison.Ordinal) == true);

                    tabs[0].Focus();
                    HeadlessSession.Pump(window);
                    window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
                    HeadlessSession.Pump(window);
                    var focused = window.FocusManager?.GetFocusedElement() as Control;
                    Assert.NotNull(focused);
                    Assert.IsNotType<TabItem>(focused);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheActiveTabPersistsAcrossARealRestart() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using (var first = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path }))
                {
                    var window = first.Services.GetRequiredService<ShellWindow>();
                    window.Show();
                    HeadlessSession.Pump(window);
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    shell.Console.ActiveChannel = shell.Console.Channels.Single(channel => channel.Id == "problems");
                    HeadlessSession.Pump(window);
                    await shell.PendingLayoutPersist;
                    window.Close();
                }

                using (var second = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path }))
                {
                    var window = second.Services.GetRequiredService<ShellWindow>();
                    window.Show();
                    HeadlessSession.Pump(window);
                    try
                    {
                        var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                        Assert.Equal("problems", shell.Console.ActiveChannel?.Id);
                        Assert.Equal("Validation", FindTabs(window)[1].Header is ConsoleChannelViewModel channel ? channel.TabTitle : null);
                    }
                    finally
                    {
                        window.Close();
                    }
                }
            });

    [Fact]
    public Task AnUnknownStoredTabIdFallsBackToTheFirstTab() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                Directory.CreateDirectory(workspace.Path);
                File.WriteAllText(
                    Path.Combine(workspace.Path, "Mocassin.Settings.json"),
                    """{"format":"mocassin.settings","layout":{"activeConsoleChannel":"missing"}}""");
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    Assert.Equal("application", shell.Console.ActiveChannel?.Id);
                }
                finally
                {
                    window.Close();
                }
            });

    private static TabControl FindTabControl(ShellWindow window) =>
        window.GetVisualDescendants()
            .OfType<TabControl>()
            .Single(control => AutomationProperties.GetName(control) == "Console tabs");

    private static List<TabItem> FindTabs(ShellWindow window) =>
        FindTabControl(window)
            .GetVisualDescendants()
            .OfType<TabItem>()
            .ToList();

    private static List<string> TabLabels(IEnumerable<TabItem> tabs) =>
        tabs.Select(tab => tab.GetVisualDescendants().OfType<TextBlock>().First().Text ?? string.Empty).ToList();
}
