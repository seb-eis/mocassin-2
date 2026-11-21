// SPDX-License-Identifier: MIT

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Documents;
using Mocassin.App.Views.Shell;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.Tests.Views;

/// <summary>
/// Headless workspace tests for tabs, the project explorer, and the palette overlay.
/// </summary>
[Trait("Tier", "PR-fast")]
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
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<ListBox>(),
                        control => AutomationProperties.GetName(control) == "Open projects");
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<TreeView>(),
                        control => AutomationProperties.GetName(control) == "Project explorer");
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<TextBox>(),
                        control => AutomationProperties.GetName(control) == "Application console filter");
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<ComboBox>(),
                        control => AutomationProperties.GetName(control) == "Theme");

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
    public Task CtrlNumberActivatesATab() =>
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
                    var entry = await ProjectFixtures.LoadCeriaAsync();
                    shell.Workspace.Add(entry);
                    await shell.Navigation.NavigateToAsync(
                        new ModelPath("project", entry.Id, "overview"),
                        NavigationIntent.Activate);
                    await shell.Navigation.NavigateToAsync(
                        new ModelPath("project", entry.Id, "json"),
                        NavigationIntent.Activate);

                    window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
                    window.KeyPress(Key.D2, RawInputModifiers.Control, PhysicalKey.Digit2, "2");

                    Assert.Equal(DocumentKinds.ProjectOverview, shell.Documents.ActiveDocument!.Key.Kind);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheProjectArgumentOpensAtStartup() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                var target = workspace.Combine("startup.mocprj");
                File.Copy(RepositoryPaths.Combine("samples", "ceria", "ceria.mocprj"), target);
                using var host = AppHost.Build(
                    new AppStartupOptions { DataDirectory = workspace.Path, ProjectPath = target });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    await shell.StartupProjectTask;
                    var session = host.Services.GetRequiredService<IProjectSessionService>().ActiveSession;
                    Assert.NotNull(session);
                    Assert.Equal(session.Id, shell.Workspace.ActiveProject!.Id);
                    Assert.Contains(
                        shell.Documents.Documents,
                        document => document.Key.ScopeId == session.Id);
                }
                finally
                {
                    var sessions = host.Services.GetRequiredService<IProjectSessionService>();
                    if (sessions.ActiveSession is { } active)
                    {
                        await sessions.CloseAsync(active);
                    }

                    window.Close();
                }
            });

    [Fact]
    public Task AMissingStartupProjectDoesNotPreventStartup() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(
                    new AppStartupOptions
                    {
                        DataDirectory = workspace.Path,
                        ProjectPath = workspace.Combine("missing.mocprj"),
                    });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    await shell.StartupProjectTask;
                    Assert.Null(host.Services.GetRequiredService<IProjectSessionService>().ActiveSession);
                    Assert.Contains(
                        shell.Console.ActiveChannel!.VisibleEntries,
                        entry => entry.Severity >= Mocassin.App.ViewModels.Consoles.ConsoleSeverity.Warning);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheWelcomeDocumentCanBeDisabledBySettings() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                File.WriteAllText(
                    workspace.Combine("Mocassin.Settings.json"),
                    """{"format":"mocassin.settings","ui":{"showWelcomeOnStart":false}}""");
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
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
