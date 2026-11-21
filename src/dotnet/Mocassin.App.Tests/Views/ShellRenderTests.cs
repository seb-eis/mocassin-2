// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Shell;

namespace Mocassin.App.Tests.Views;

/// <summary>
/// Headless smoke tests for the real shell host.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ShellRenderTests
{
    [Fact]
    public Task TheShellRendersEveryRegionAndTheStatusBar() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();

                try
                {
                    var descendants = window.GetVisualDescendants().ToList();
                    Assert.NotEmpty(descendants.OfType<ProjectsRegionView>());
                    Assert.NotEmpty(descendants.OfType<DocumentsRegionView>());
                    Assert.NotEmpty(descendants.OfType<InspectorRegionView>());
                    Assert.NotEmpty(descendants.OfType<ConsoleRegionView>());
                    Assert.NotEmpty(descendants.OfType<StatusBarView>());
                    Assert.NotEmpty(descendants.OfType<Menu>());
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheShellStartsWithTheApplicationConsoleAndTheRegionToggles() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    Assert.Contains(
                        shell.Console.ActiveChannel!.VisibleEntries,
                        entry => entry.Message.Contains("shell started", StringComparison.Ordinal));

                    var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("shell-test");
#pragma warning disable CA1848 // Test-only log entry to verify the console provider.
                    logger.LogInformation("console forwarding works");
#pragma warning restore CA1848
                    Assert.Contains(
                        shell.Console.ActiveChannel.VisibleEntries,
                        entry => entry.Message.Contains("console forwarding works", StringComparison.Ordinal));

                    shell.Regions.ToggleProjectsCommand.Execute(null);
                    Assert.False(shell.Regions.IsProjectsVisible);
                    Assert.False(window.GetVisualDescendants().OfType<ProjectsRegionView>().Single().IsVisible);
                    await shell.PendingLayoutPersist;
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task RapidRegionTogglesCoalesceIntoTheLastPersistedLayout() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    shell.Regions.ToggleProjectsCommand.Execute(null);
                    shell.Regions.ToggleProjectsCommand.Execute(null);
                    await shell.PendingLayoutPersist;

                    var loaded = new Mocassin.App.Settings.AppSettingsStore(host.Paths).Load();
                    Assert.True(loaded.Settings.Layout.LeftVisible);
                    Assert.Empty(
                        Directory.EnumerateFiles(workspace.Path, "*.tmp", SearchOption.AllDirectories));
                }
                finally
                {
                    window.Close();
                }
            });
}
