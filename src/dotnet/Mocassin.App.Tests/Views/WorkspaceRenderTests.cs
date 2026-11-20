// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Documents;
using Mocassin.App.Views.Shell;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.Tests.Views;

/// <summary>
/// Headless workspace tests for tabs, the project explorer, and the palette overlay.
/// </summary>
public sealed class WorkspaceRenderTests
{
    [Fact]
    public Task TheShellShowsTheWelcomeTabAndProjectNodes() =>
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
                    var welcome = Assert.Single(shell.Documents.Documents);
                    Assert.Equal(DocumentKinds.Welcome, welcome.Key.Kind);
                    Assert.NotEmpty(window.GetVisualDescendants().OfType<TabControl>());
                    Assert.NotEmpty(window.GetVisualDescendants().OfType<WelcomeDocumentView>());

                    var entry = await ProjectFixtures.LoadCeriaAsync();
                    shell.Workspace.Add(entry);
                    Assert.Equal(2, shell.Explorer.Nodes.Count);

                    await shell.Navigation.NavigateToAsync(
                        new ModelPath("project", entry.Id, "overview"),
                        NavigationIntent.Activate);

                    var overview = Assert.Single(shell.Documents.Documents);
                    Assert.Equal(DocumentKinds.ProjectOverview, overview.Key.Kind);
                    Assert.Contains(entry.Name, overview.Title, StringComparison.Ordinal);

                    await shell.Navigation.NavigateToAsync(
                        new ModelPath("project", entry.Id, "overview"),
                        NavigationIntent.Activate);
                    Assert.Single(shell.Documents.Documents);

                    await shell.Documents.CloseAllAsync();
                    Assert.Empty(shell.Documents.Documents);
                    Assert.Null(shell.Documents.ActiveDocument);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task ThePaletteAndCheatSheetOpenFromTheShell() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    shell.ShowPaletteCommand.Execute(null);
                    Assert.True(shell.IsPaletteOpen);
                    Assert.NotEmpty(shell.Palette.Results);

                    shell.Palette.SearchText = "exit";
                    Assert.Contains(
                        shell.Palette.Results,
                        command => command.Id == "shell.exit");

                    shell.ClosePaletteCommand.Execute(null);
                    shell.ShowShortcutsCommand.Execute(null);
                    Assert.True(shell.IsCheatSheetOpen);
                    Assert.NotEmpty(shell.Shortcuts.Groups);
                }
                finally
                {
                    window.Close();
                }
            });
}
