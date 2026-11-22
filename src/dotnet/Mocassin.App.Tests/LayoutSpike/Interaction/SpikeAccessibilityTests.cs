// SPDX-License-Identifier: MIT

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Mocassin.App.Tests.LayoutSpike.Options;
using Mocassin.App.Tests.TestSupport;

namespace Mocassin.App.Tests.LayoutSpike.Interaction;

/// <summary>
/// The §0.2 accessibility sweep: every interactive control carries an automation name, the key surfaces expose a
/// role, and status is glyph plus text rather than colour alone.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class SpikeAccessibilityTests
{
    [Theory]
    [InlineData(SpikeOption.ListDetail)]
    [InlineData(SpikeOption.TreeDetail)]
    [InlineData(SpikeOption.Tabs)]
    [InlineData(SpikeOption.SplitPane)]
    [InlineData(SpikeOption.CustomTable)]
    public Task EveryInteractiveControlIsNamedAndTheKeySurfacesExposeRoles(SpikeOption option) =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                using var host = await SpikeTestHost.OpenAsync(option);
                SpikeInteractions.SelectRow(host, "Ce4+");
                var controls = SpikeInteractions.InteractiveControls(host.Window);
                Assert.True(controls.Count > 8, $"{option} exposed only {controls.Count} interactive controls.");
                foreach (var control in controls)
                {
                    var name = AutomationProperties.GetName(control);
                    Assert.False(
                        string.IsNullOrWhiteSpace(name),
                        $"{option}: {control.GetType().Name} has no automation name.");
                    var peer = ControlAutomationPeer.CreatePeerForElement(control);
                    Assert.NotNull(peer);
                }

                var rows = SpikeInteractions.Rows(host);
                if (option == SpikeOption.TreeDetail)
                {
                    Assert.Equal(
                        AutomationControlType.Tree,
                        Peer(SpikeInteractions.Tree(host)!).GetAutomationControlType());
                }
                else
                {
                    Assert.NotNull(rows);
                    Assert.Equal(AutomationControlType.List, Peer(rows!).GetAutomationControlType());
                }

                if (option == SpikeOption.CustomTable)
                {
                    var table = host.Window.GetVisualDescendants().OfType<SpikeTableView>().Single();
                    Assert.Equal(AutomationControlType.Table, Peer(table).GetAutomationControlType());
                }

                if (option == SpikeOption.SplitPane)
                {
                    var splitter = host.Window.GetVisualDescendants().OfType<GridSplitter>().Single();
                    Assert.Equal("Resize editor pane", AutomationProperties.GetName(splitter));
                    Assert.NotNull(Peer(splitter));
                }

                if (option == SpikeOption.Tabs)
                {
                    var tabs = host.Window.GetVisualDescendants().OfType<TabControl>().ToList();
                    Assert.Equal(2, tabs.Count);
                    Assert.All(tabs, tab => Assert.Equal(AutomationControlType.Tab, Peer(tab).GetAutomationControlType()));
                }

                var combo = host.Window.GetVisualDescendants().OfType<ComboBox>()
                    .First(control => AutomationProperties.GetName(control) == "Domain");
                Assert.Equal(AutomationControlType.ComboBox, Peer(combo).GetAutomationControlType());

                // Status is a glyph plus text, not a colour-only signal.
                var statusBar = host.Window.GetVisualDescendants().OfType<Border>()
                    .Single(border => AutomationProperties.GetName(border) == "Spike status bar");
                var statusBlocks = statusBar.GetVisualDescendants().OfType<TextBlock>().ToList();
                Assert.Contains(statusBlocks, block => block.Text == "◆");
                var statusText = statusBlocks.Single(
                    block => AutomationProperties.GetName(block) == "Spike status");
                Assert.Contains("rows", statusText.Text);
                Assert.DoesNotContain("✕", statusText.Text);
            });

    private static AutomationPeer Peer(Control control) =>
        ControlAutomationPeer.CreatePeerForElement(control)
        ?? throw new InvalidOperationException($"{control.GetType().Name} has no automation peer.");
}
