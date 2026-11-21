// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Diagnostics;
using Mocassin.App.ViewModels.Documents.Project;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Consoles;
using Mocassin.App.Views.Documents.Project;
using Mocassin.App.Views.Shell;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Templates;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.App.Tests.Views;

/// <summary>
/// Headless rendering and accessibility checks for the Problems channel and the Overview metadata form.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ValidationSurfaceRenderTests
{
    [Fact]
    public Task TheProblemsChannelRendersFiltersRowsAndTheStatusItem() =>
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
                    var session = CreateDiagnosticSession();
                    shell.Workspace.Add(new ProjectWorkspaceEntry(session));
                    var problems = shell.Console.Channels.OfType<ProblemsChannelViewModel>().Single();
                    shell.Console.ActiveChannel = problems;
                    HeadlessSession.Pump(window);

                    Assert.Single(problems.Items);
                    Assert.Single(window.GetVisualDescendants().OfType<ProblemsView>());
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<ListBox>(),
                        control => AutomationProperties.GetName(control) == "Problem entries");
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<TextBox>(),
                        control => AutomationProperties.GetName(control) == "Problems text filter");
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<Button>(),
                        control => AutomationProperties.GetName(control) == "Revalidate project");
                    var status = shell.StatusBar.Items.Single(
                        item => item.Text.Contains("1 errors", StringComparison.Ordinal));
                    Assert.Equal("✖", status.Glyph);
                    Assert.Equal(StatusBarSeverity.Error, status.Severity);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheOverviewMetadataFormRendersAndCommits() =>
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
                    var session = CreateDiagnosticSession();
                    var entry = new ProjectWorkspaceEntry(session);
                    shell.Workspace.Add(entry);
                    await shell.Navigation.NavigateToAsync(
                        new ModelPath("project", session.Id, "overview"),
                        NavigationIntent.Activate);
                    HeadlessSession.Pump(window);

                    Assert.Single(window.GetVisualDescendants().OfType<ProjectOverviewView>());
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<TextBox>(),
                        control => AutomationProperties.GetName(control) == "Project name");
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<TextBox>(),
                        control => AutomationProperties.GetName(control) == "Project notes");
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<Button>(),
                        control => AutomationProperties.GetName(control) == "Toggle statistics");

                    var overview = Assert.IsType<ProjectOverviewViewModel>(shell.Documents.ActiveDocument);
                    overview.NameDraft = "Renamed";
                    overview.CommitNameCommand.Execute(null);
                    HeadlessSession.Pump(window);

                    Assert.Equal("Renamed", session.Name);
                    Assert.Equal("Renamed", overview.NameDraft);

                    await shell.Navigation.NavigateToAsync(
                        new ModelPath("project", session.Id, "notes"),
                        NavigationIntent.SelectObject);
                    HeadlessSession.Pump(window);

                    Assert.Null(overview.PendingFocus);
                    var focused = window.FocusManager?.GetFocusedElement() as TextBox;
                    Assert.NotNull(focused);
                    Assert.Equal("Project notes", AutomationProperties.GetName(focused));
                    await host.Services.GetRequiredService<IProjectSessionService>().SaveAsync(session);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheSettingsDialogShowsTheAutosaveControls() =>
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
                    shell.Settings.Open();
                    HeadlessSession.Pump(window);

                    Assert.Contains(
                        window.GetVisualDescendants().OfType<CheckBox>(),
                        control => AutomationProperties.GetName(control) == "Enable autosave");
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<TextBox>(),
                        control => AutomationProperties.GetName(control) == "Autosave interval minutes");
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheTabAndProjectSwitcherShowDirtyMarkers() =>
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
                    var session = CreateDiagnosticSession();
                    shell.Workspace.Add(new ProjectWorkspaceEntry(session));
                    await shell.Navigation.NavigateToAsync(
                        new ModelPath("project", session.Id, "overview"),
                        NavigationIntent.Activate);
                    session.EditCoordinator.Apply(
                        "Rename",
                        project => project with { Metadata = project.Metadata with { Name = "Renamed" } });
                    HeadlessSession.Pump(window);

                    Assert.StartsWith("* ", shell.WindowTitle, StringComparison.Ordinal);
                    Assert.Contains(
                        window.GetVisualDescendants().OfType<TextBlock>(),
                        control => AutomationProperties.GetName(control) == "Unsaved changes"
                            && control.IsVisible);
                    await host.Services.GetRequiredService<IProjectSessionService>().SaveAsync(session);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheEditMenuUndoRedoCommandsFollowTheActiveProject() =>
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
                    var session = CreateDiagnosticSession();
                    shell.Workspace.Add(new ProjectWorkspaceEntry(session));
                    await shell.Navigation.NavigateToAsync(
                        new ModelPath("project", session.Id, "overview"),
                        NavigationIntent.Activate);
                    HeadlessSession.Pump(window);
                    var overview = Assert.IsType<ProjectOverviewViewModel>(shell.Documents.ActiveDocument);
                    overview.NameDraft = "Renamed";
                    overview.CommitNameCommand.Execute(null);
                    HeadlessSession.Pump(window);

                    var undo = shell.Commands.Find("edit.undo");
                    var redo = shell.Commands.Find("edit.redo");
                    Assert.NotNull(undo);
                    Assert.NotNull(redo);
                    Assert.True(undo.Command.CanExecute(null));
                    undo.Command.Execute(null);
                    HeadlessSession.Pump(window);
                    Assert.Equal("Diagnostics", session.Name);
                    Assert.True(redo.Command.CanExecute(null));

                    redo.Command.Execute(null);
                    HeadlessSession.Pump(window);
                    Assert.Equal("Renamed", session.Name);
                    await host.Services.GetRequiredService<IProjectSessionService>().SaveAsync(session);
                }
                finally
                {
                    window.Close();
                }
            });

    private static ProjectSession CreateDiagnosticSession()
    {
        var time = new ManualTimeProvider();
        var project = ProjectTemplateFactory.CreateProject(
            ProjectTemplateKind.Blank,
            "Diagnostics",
            time.GetUtcNow());
        var diagnostics = ImmutableArray.Create(
            new Diagnostic(
                "PROJ001",
                DiagnosticSeverity.Error,
                "Loaded project error.",
                new ModelPath("project", project.Metadata.ProjectId, "name")));
        return ProjectSession.CreateFromLoad(
            new ProjectLoadResult(project, diagnostics),
            "/tmp/render-diagnostics.mocprj",
            null,
            isReadOnly: false,
            lockPath: null,
            new ProjectValidationPipeline(),
            time);
    }
}
