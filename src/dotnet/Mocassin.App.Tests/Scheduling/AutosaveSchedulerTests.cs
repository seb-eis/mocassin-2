// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using Mocassin.App.Composition;
using Mocassin.App.Files;
using Mocassin.App.Recovery;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Scheduling;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Templates;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.App.Tests.Scheduling;

/// <summary>
/// Verifies autosave timing, dirty-only writes, sidecar cleanup, and session-gate skipping.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class AutosaveSchedulerTests
{
    [Fact]
    public async Task OnlyDirtyDueSessionsWriteAndSaveDeletesTheSidecar()
    {
        using var context = new AutosaveContext();
        var session = await context.CreateAsync("Autosave");
        var sidecar = ProjectStore.GetAutosavePath(session.Path);

        await context.Scheduler.RunOnceAsync(context.Time.GetUtcNow());
        Assert.Equal(0, context.Scheduler.WriteAttempts);
        Assert.False(File.Exists(sidecar));

        Rename(session);
        await context.Scheduler.RunOnceAsync(context.Time.GetUtcNow());
        Assert.Equal(1, context.Scheduler.WriteAttempts);
        Assert.True(File.Exists(sidecar));
        Assert.True(session.IsDirty);
        Assert.True(session.Document.LastAutosaveUtc is not null);

        await context.Scheduler.RunOnceAsync(context.Time.GetUtcNow());
        Assert.Equal(1, context.Scheduler.WriteAttempts);

        context.Time.Advance(TimeSpan.FromMinutes(6));
        await context.Scheduler.RunOnceAsync(context.Time.GetUtcNow());
        Assert.Equal(2, context.Scheduler.WriteAttempts);

        var saved = await context.Service.SaveAsync(session);
        Assert.True(saved.IsSuccess, saved.Message);
        Assert.False(session.IsDirty);
        Assert.False(File.Exists(sidecar));
    }

    [Fact]
    public async Task ReadOnlyClosingAndBusySessionsAreSkipped()
    {
        using var context = new AutosaveContext();
        var readOnly = ProjectSession.CreateStandalone(
            ProjectTemplateFactory.CreateProject(ProjectTemplateKind.Blank, "ReadOnly", context.Time.GetUtcNow()),
            context.Workspace.Combine("readonly.mocprj"),
            isReadOnly: true,
            timeProvider: context.Time);
        readOnly.Document.Apply(
            "Rename",
            project => project with { Metadata = project.Metadata with { Name = "ReadOnlyDirty" } });
        context.WorkspaceViewModel.Add(new ProjectWorkspaceEntry(readOnly));

        await context.Scheduler.RunOnceAsync(context.Time.GetUtcNow());
        Assert.False(File.Exists(ProjectStore.GetAutosavePath(readOnly.Path)));

        var session = await context.CreateAsync("Busy");
        Rename(session);
        session.IsClosing = true;
        await context.Scheduler.RunOnceAsync(context.Time.GetUtcNow());
        Assert.Equal(0, context.Scheduler.WriteAttempts);

        session.IsClosing = false;
        await session.Gate.WaitAsync();
        try
        {
            await context.Scheduler.RunOnceAsync(context.Time.GetUtcNow());
            Assert.Equal(0, context.Scheduler.WriteAttempts);
        }
        finally
        {
            session.Gate.Release();
        }

        await context.Scheduler.RunOnceAsync(context.Time.GetUtcNow());
        Assert.Equal(1, context.Scheduler.WriteAttempts);
    }

    [Fact]
    public async Task AnEditAndUndoBackToTheSavePointDeleteTheSidecar()
    {
        using var context = new AutosaveContext();
        var session = await context.CreateAsync("Cleanup");
        Rename(session);
        await context.Scheduler.RunOnceAsync(context.Time.GetUtcNow());
        var sidecar = ProjectStore.GetAutosavePath(session.Path);
        Assert.True(File.Exists(sidecar));

        Assert.True(session.EditCoordinator.Undo());
        await context.Service.WhenMaintenanceIdleAsync();

        Assert.False(session.IsDirty);
        Assert.False(File.Exists(sidecar));
    }

    [Fact]
    public async Task ANonNoOpPendingCandidateSurvivesSaveButAFirstEditClearsIt()
    {
        using var context = new AutosaveContext();
        var session = await context.CreateAsync("Pending");
        Rename(session);
        await context.Scheduler.RunOnceAsync(context.Time.GetUtcNow());
        var sidecar = ProjectStore.GetAutosavePath(session.Path);
        session.PendingRecovery = new Mocassin.App.ViewModels.Recovery.RecoveryCandidate(
            session.Id,
            session.Path,
            sidecar,
            context.Time.GetUtcNow(),
            Mocassin.App.ViewModels.Recovery.RecoverySource.Sidecar);

        var saved = await context.Service.SaveAsync(session);
        Assert.True(saved.IsSuccess, saved.Message);
        Assert.True(File.Exists(sidecar));

        Rename(session, "After save");
        Assert.Null(session.PendingRecovery);
        Assert.True(session.EditCoordinator.Undo());
        await context.Service.WhenMaintenanceIdleAsync();
        Assert.False(File.Exists(sidecar));
    }

    [Fact]
    public async Task ANoOpEditDoesNotClearAPendingCandidate()
    {
        using var context = new AutosaveContext();
        var session = await context.CreateAsync("NoOp");
        Rename(session);
        await context.Scheduler.RunOnceAsync(context.Time.GetUtcNow());
        var sidecar = ProjectStore.GetAutosavePath(session.Path);
        session.PendingRecovery = new Mocassin.App.ViewModels.Recovery.RecoveryCandidate(
            session.Id,
            session.Path,
            sidecar,
            context.Time.GetUtcNow(),
            Mocassin.App.ViewModels.Recovery.RecoverySource.Sidecar);

        var result = session.EditCoordinator.Apply("No-op", static project => project);

        Assert.Equal(Mocassin.App.ViewModels.Editing.EditApplyOutcome.Applied, result.Outcome);
        Assert.False(result.HasChange);
        Assert.NotNull(session.PendingRecovery);
        Assert.True(File.Exists(sidecar));
    }

    [Fact]
    public async Task AutosaveMarshalsStatusAndMarkAutosavedThroughTheDispatcher()
    {
        using var context = new AutosaveContext();
        var session = await context.CreateAsync("Marshalled");
        Rename(session);

        await Task.Run(() => context.Scheduler.RunOnceAsync(context.Time.GetUtcNow()));

        Assert.True(context.Dispatcher.InvokeCount >= 2);
        Assert.True(session.Document.LastAutosaveUtc is not null);
        Assert.Equal(StatusBarSeverity.Success, context.Scheduler.Severity);
    }

    [Fact]
    public async Task AFailedSidecarWriteFallsBackToRecoveryAndIsLogged()
    {
        using var context = new AutosaveContext();
        var session = await context.CreateAsync("Fallback");
        Rename(session);
        Directory.CreateDirectory(ProjectStore.GetAutosavePath(session.Path));

        await context.Scheduler.RunOnceAsync(context.Time.GetUtcNow());

        Assert.Contains(
            context.LoggerProvider.Entries,
            entry => entry.Level == LogLevel.Warning
                     && entry.Message.Contains("Autosave", StringComparison.Ordinal));
        Assert.Single(await context.Recovery.ListAsync());
        Assert.True(session.Document.LastAutosaveUtc is not null);
        Assert.Equal(StatusBarSeverity.Success, context.Scheduler.Severity);
    }

    private static void Rename(ProjectSession session, string name = "Renamed") =>
        session.EditCoordinator.Apply(
            "Rename project",
            project => project with { Metadata = project.Metadata with { Name = name } });

    private sealed class AutosaveContext : IDisposable
    {
        private readonly TempWorkspace workspace = new();

        public AutosaveContext()
        {
            Time = new ManualTimeProvider();
            Settings = new SettingsService(new InMemorySettingsStore(), AppSettingsLoadResult.Missing());
            WorkspaceViewModel = new WorkspaceViewModel();
            ProjectStore = new ProjectStore(Time, new ProjectStoreOptions { ValidateOnLoad = false });
            var paths = new AppPaths(workspace.Path);
            Recovery = new RecoveryStore(paths, ProjectStore, Time);
            Dispatcher = new RecordingUiDispatcher();
            LoggerProvider = new RecordingLoggerProvider();
            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(
                builder => builder.AddProvider(LoggerProvider));
            Service = new ProjectSessionService(
                ProjectStore,
                new ProjectValidationPipeline(),
                new ProjectFileProbe(),
                WorkspaceViewModel,
                Settings,
                Time,
                Recovery);
            Scheduler = new AutosaveScheduler(
                Service,
                Settings,
                ProjectStore,
                Recovery,
                Time,
                Dispatcher,
                LoggerFactory.CreateLogger<AutosaveScheduler>());
        }

        public ManualTimeProvider Time { get; }

        public SettingsService Settings { get; }

        public WorkspaceViewModel WorkspaceViewModel { get; }

        public ProjectStore ProjectStore { get; }

        public RecoveryStore Recovery { get; }

        public RecordingUiDispatcher Dispatcher { get; }

        public RecordingLoggerProvider LoggerProvider { get; }

        public ILoggerFactory LoggerFactory { get; }

        public ProjectSessionService Service { get; }

        public AutosaveScheduler Scheduler { get; }

        public TempWorkspace Workspace => workspace;

        public async Task<ProjectSession> CreateAsync(string name)
        {
            var created = await Service.CreateAsync(ProjectTemplateKind.Blank, name, workspace.Path);
            Assert.True(created.IsSuccess, created.Message);
            return created.Session!;
        }

        public void Dispose()
        {
            LoggerFactory.Dispose();
            workspace.Dispose();
        }
    }
}
