// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.Input;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Documents.Project;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Settings.Blocks;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Templates;

namespace Mocassin.App.Tests.Views;

/// <summary>
/// Verifies shell dirty markers, close/reload prompts, clean-exit settings, and unclean-exit restore.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ShellLifecycleTests
{
    [Fact]
    public async Task DirtyMarkersFollowTheActiveProjectAndSaveClearsThem()
    {
        using var host = new ShellTestHost();
        var session = await CreateProjectAsync(host, "Markers");
        await host.Shell.Navigation.NavigateToAsync(
            new ModelPath("project", session.Id, "overview"),
            NavigationIntent.Activate);

        Rename(session);
        Assert.True(host.Workspace.ActiveProject!.IsDirty);
        Assert.StartsWith("* ", host.Shell.WindowTitle, StringComparison.Ordinal);
        Assert.Contains("Unsaved changes", host.Shell.StatusBar.Project.Text, StringComparison.Ordinal);
        var overview = Assert.IsType<ProjectOverviewViewModel>(host.Shell.Documents.ActiveDocument);
        Assert.True(overview.IsDirty);

        var saved = await host.Sessions.SaveAsync(session);

        Assert.True(saved.IsSuccess, saved.Message);
        Assert.False(host.Workspace.ActiveProject!.IsDirty);
        Assert.DoesNotContain("* ", host.Shell.WindowTitle, StringComparison.Ordinal);
        Assert.Contains("Saved", host.Shell.StatusBar.Project.Text, StringComparison.Ordinal);
        Assert.False(overview.IsDirty);
        await host.Shell.RequestCloseAsync();
    }

    [Fact]
    public async Task CloseCancelLeavesTheSessionOpenAndDiscardClosesIt()
    {
        using var host = new ShellTestHost();
        var session = await CreateProjectAsync(host, "Close");
        Rename(session);
        var sidecar = ProjectStore.GetAutosavePath(session.Path);
        var autosave = await host.ProjectStore.WriteAutosaveAsync(session.Project, session.Path);
        Assert.True(autosave.IsSuccess);
        host.Dialogs.ChoiceHandler = _ => new DialogResult(DialogResultKind.ChoiceSelected, ChoiceId: "cancel");

        var cancelled = await host.Shell.RequestCloseAsync();

        Assert.False(cancelled);
        Assert.Single(host.Sessions.Sessions);
        Assert.False(session.IsClosing);
        Assert.False(host.Settings.Current.Session.CleanExit);

        host.Dialogs.ChoiceHandler = _ => new DialogResult(DialogResultKind.ChoiceSelected, ChoiceId: "discard");
        var closed = await host.Shell.RequestCloseAsync();

        Assert.True(closed);
        Assert.Empty(host.Sessions.Sessions);
        Assert.False(File.Exists(sidecar));
        Assert.True(host.Settings.Current.Session.CleanExit);
    }

    [Fact]
    public async Task CloseSaveWritesTheProjectAndClearsTheSidecar()
    {
        using var host = new ShellTestHost();
        var session = await CreateProjectAsync(host, "SaveClose");
        Rename(session);
        var sidecar = ProjectStore.GetAutosavePath(session.Path);
        var autosave = await host.ProjectStore.WriteAutosaveAsync(session.Project, session.Path);
        Assert.True(autosave.IsSuccess);
        host.Dialogs.ChoiceHandler = _ => new DialogResult(DialogResultKind.ChoiceSelected, ChoiceId: "save");

        var closed = await host.Shell.RequestCloseAsync();

        Assert.True(closed);
        Assert.Empty(host.Sessions.Sessions);
        Assert.False(File.Exists(sidecar));
        Assert.True(host.Settings.Current.Session.CleanExit);
        var loaded = await host.ProjectStore.LoadAsync(session.Path);
        Assert.Equal("Renamed", loaded.Project!.Metadata.Name);
    }

    [Fact]
    public async Task ReloadKeepsOnCancelAndReloadsOnConfirm()
    {
        using var host = new ShellTestHost();
        var session = await CreateProjectAsync(host, "Reload");
        Rename(session);
        host.Dialogs.ConfirmResult = false;
        var command = Assert.IsAssignableFrom<IAsyncRelayCommand>(
            host.Shell.Commands.Find("project.reload")!.Command);

        await command.ExecuteAsync(null);

        Assert.Equal("Renamed", session.Name);
        Assert.Single(host.Dialogs.ConfirmRequests);

        host.Dialogs.ConfirmResult = true;
        await command.ExecuteAsync(null);

        Assert.Equal("Reload", session.Name);
        Assert.False(session.IsDirty);
        await host.Shell.RequestCloseAsync();
    }

    [Fact]
    public async Task RestoreSessionOpensTheRememberedProjects()
    {
        using var projectDirectory = new TempWorkspace();
        var path = projectDirectory.Combine("Restore.mocprj");
        var time = new ManualTimeProvider();
        var store = new ProjectStore(time, new ProjectStoreOptions { ValidateOnLoad = false });
        var project = ProjectTemplateFactory.CreateProject(ProjectTemplateKind.Blank, "Restore", time.GetUtcNow());
        var save = await store.SaveAsync(project, path);
        Assert.True(save.IsSuccess);
        var settings = new AppSettings
        {
            Session = new SessionSettings
            {
                OpenProjects = [path],
                ActiveProject = path,
                CleanExit = false,
                SavedUtc = time.GetUtcNow(),
            },
        };
        using var host = new ShellTestHost(settings);

        host.Shell.BeginStartupRestore();
        await host.Shell.StartupRestoreTask;

        var session = Assert.Single(host.Sessions.Sessions);
        Assert.Equal("Restore", session.Name);
        Assert.Equal(path, host.Sessions.ActiveSession!.Path);
        Assert.NotEmpty(host.Dialogs.ChoiceRequests);
    }

    [Fact]
    public async Task RestoreAutomaticallyAppliesTheRecoveryCandidate()
    {
        using var projectDirectory = new TempWorkspace();
        var candidate = await CreateSidecarCandidateAsync(projectDirectory);
        var settings = new AppSettings
        {
            Session = new SessionSettings
            {
                OpenProjects = [candidate.Path],
                ActiveProject = candidate.Path,
                CleanExit = false,
            },
        };
        using var host = new ShellTestHost(settings);

        host.Shell.BeginStartupRestore();
        await host.Shell.StartupRestoreTask;

        var session = Assert.Single(host.Sessions.Sessions);
        Assert.Equal("Recovered", session.Name);
        Assert.True(session.IsDirty);
        Assert.NotEmpty(host.Dialogs.ChoiceRequests);
        await host.Shell.RequestCloseAsync();
    }

    [Fact]
    public async Task RestoreReportsMissingProjectsAndHonorsCleanExit()
    {
        using var projectDirectory = new TempWorkspace();
        var missing = projectDirectory.Combine("Missing.mocprj");
        var dirtySettings = new AppSettings
        {
            Session = new SessionSettings
            {
                OpenProjects = [missing],
                ActiveProject = missing,
                CleanExit = false,
            },
        };
        using var dirtyHost = new ShellTestHost(dirtySettings);

        dirtyHost.Shell.BeginStartupRestore();
        await dirtyHost.Shell.StartupRestoreTask;

        Assert.Empty(dirtyHost.Sessions.Sessions);
        Assert.Single(dirtyHost.Dialogs.ErrorRequests);

        using var cleanHost = new ShellTestHost(
            new AppSettings
            {
                Session = new SessionSettings { OpenProjects = [missing], CleanExit = true },
            });
        cleanHost.Shell.BeginStartupRestore();
        await cleanHost.Shell.StartupRestoreTask;

        Assert.Empty(cleanHost.Dialogs.ChoiceRequests);
    }

    [Fact]
    public async Task RecoveryPromptCanDecideLaterAndPreservesTheSidecar()
    {
        using var projectDirectory = new TempWorkspace();
        var context = await CreateSidecarCandidateAsync(projectDirectory);
        using var host = new ShellTestHost();
        host.Dialogs.ChoiceHandler = _ => new DialogResult(
            DialogResultKind.ChoiceSelected,
            ChoiceId: "later");

        await host.Shell.OpenProjectPathAsync(context.Path);

        var session = Assert.Single(host.Sessions.Sessions);
        Assert.NotNull(session.PendingRecovery);
        Assert.True(File.Exists(context.Sidecar));
        Assert.Contains("Recovery available", host.Shell.StatusBar.Project.Text, StringComparison.Ordinal);
        await host.Shell.RequestCloseAsync();
    }

    [Fact]
    public async Task RecoveryPromptCanDiscardTheSidecar()
    {
        using var projectDirectory = new TempWorkspace();
        var context = await CreateSidecarCandidateAsync(projectDirectory);
        using var host = new ShellTestHost();
        host.Dialogs.ChoiceHandler = _ => new DialogResult(
            DialogResultKind.ChoiceSelected,
            ChoiceId: "discard");

        await host.Shell.OpenProjectPathAsync(context.Path);

        var session = Assert.Single(host.Sessions.Sessions);
        Assert.Null(session.PendingRecovery);
        Assert.False(File.Exists(context.Sidecar));
        await host.Shell.RequestCloseAsync();
    }

    [Fact]
    public async Task RecoveryPromptCanRecoverTheSnapshot()
    {
        using var projectDirectory = new TempWorkspace();
        var context = await CreateSidecarCandidateAsync(projectDirectory);
        using var host = new ShellTestHost();
        host.Dialogs.ChoiceHandler = _ => new DialogResult(
            DialogResultKind.ChoiceSelected,
            ChoiceId: "recover");

        await host.Shell.OpenProjectPathAsync(context.Path);

        var session = Assert.Single(host.Sessions.Sessions);
        Assert.Equal("Recovered", session.Name);
        Assert.True(session.IsDirty);
        Assert.True(File.Exists(context.Sidecar));
        await host.Shell.RequestCloseAsync();
    }

    private static async Task<(string Path, string Sidecar)> CreateSidecarCandidateAsync(
        TempWorkspace projectDirectory)
    {
        var time = new ManualTimeProvider();
        var store = new ProjectStore(time, new ProjectStoreOptions { ValidateOnLoad = false });
        var path = projectDirectory.Combine("Recovery.mocprj");
        var saved = ProjectTemplateFactory.CreateProject(ProjectTemplateKind.Blank, "Original", time.GetUtcNow());
        var save = await store.SaveAsync(saved, path);
        Assert.True(save.IsSuccess);
        var recovered = saved with { Metadata = saved.Metadata with { Name = "Recovered" } };
        var sidecar = await store.WriteAutosaveAsync(recovered, path);
        Assert.True(sidecar.IsSuccess);
        return (path, ProjectStore.GetAutosavePath(path));
    }

    private static async Task<ProjectSession> CreateProjectAsync(ShellTestHost host, string name)
    {
        var created = await host.Sessions.CreateAsync(ProjectTemplateKind.Blank, name, host.DataDirectory);
        // The path is derived by the real session service inside the host data directory.
        Assert.True(created.IsSuccess, created.Message);
        return created.Session!;
    }

    private static void Rename(ProjectSession session) =>
        session.EditCoordinator.Apply(
            "Rename",
            project => project with { Metadata = project.Metadata with { Name = "Renamed" } });
}
