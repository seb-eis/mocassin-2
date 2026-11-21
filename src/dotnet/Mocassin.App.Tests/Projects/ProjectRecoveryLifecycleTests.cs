// SPDX-License-Identifier: MIT

using Mocassin.App.Composition;
using Mocassin.App.Files;
using Mocassin.App.Recovery;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Recovery;
using Mocassin.App.ViewModels.Settings;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Templates;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.App.Tests.Projects;

/// <summary>
/// Verifies the dirty close/reload guards, recovery application/discard, sidecar detection, and pending candidates.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ProjectRecoveryLifecycleTests
{
    [Fact]
    public async Task CancellingTheGateLeavesTheSessionOpenAndNotClosing()
    {
        using var context = new LifecycleContext();
        var session = await context.CreateAsync("Cancel");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.CloseAsync(session, cancellationToken: cancellation.Token));

        Assert.False(session.IsClosing);
        Assert.Contains(session, context.Service.Sessions);
    }

    [Fact]
    public async Task ADirtyCloseReturnsUnsavedChangesAndDiscardCloses()
    {
        using var context = new LifecycleContext();
        var session = await context.CreateAsync("Close");
        Rename(session);
        var sidecar = ProjectStore.GetAutosavePath(session.Path);
        await context.WriteSidecarAsync(session);

        var guarded = await context.Service.CloseAsync(session);

        Assert.Equal(ProjectSessionOutcome.UnsavedChanges, guarded.Outcome);
        Assert.False(session.IsClosing);
        Assert.Contains(session, context.Service.Sessions);
        Assert.True(File.Exists(sidecar));

        var closed = await context.Service.CloseAsync(session, discardChanges: true);

        Assert.Equal(ProjectSessionOutcome.Closed, closed.Outcome);
        Assert.Empty(context.Service.Sessions);
        Assert.False(File.Exists(sidecar));
    }

    [Fact]
    public async Task ADirtyReloadReturnsUnsavedChangesAndDiscardReloadsCleanly()
    {
        using var context = new LifecycleContext();
        var session = await context.CreateAsync("Reload");
        Rename(session);
        await context.WriteSidecarAsync(session);
        var sidecar = ProjectStore.GetAutosavePath(session.Path);

        var guarded = await context.Service.ReloadAsync(session);

        Assert.Equal(ProjectSessionOutcome.UnsavedChanges, guarded.Outcome);
        Assert.False(session.IsClosing);
        Assert.Equal("Renamed", session.Name);

        var reloaded = await context.Service.ReloadAsync(session, discardChanges: true);

        Assert.Equal(ProjectSessionOutcome.Reloaded, reloaded.Outcome);
        Assert.False(session.Document.IsDirty);
        Assert.Equal("Reload", session.Name);
        Assert.False(File.Exists(sidecar));
        Assert.False(session.IsClosing);
    }

    [Fact]
    public async Task RecoverAppliesTheSnapshotAndTheNextSaveDeletesIt()
    {
        using var context = new LifecycleContext();
        var session = await context.CreateAsync("Recover");
        Rename(session);
        var write = await context.Recovery.WriteAsync(session.Project, session.Path, RecoverySource.Crash);
        Assert.True(write.IsSuccess);
        var candidate = Assert.Single(await context.Recovery.ListAsync());
        session.PendingRecovery = candidate;
        session.AvailableRecovery = candidate;

        var recovered = await context.Service.RecoverAsync(session, candidate);

        Assert.Equal(ProjectSessionOutcome.Recovered, recovered.Outcome);
        Assert.True(session.IsDirty);
        Assert.Equal("Renamed", session.Name);
        Assert.Equal("Recover", session.Document.LastSavedProject.Metadata.Name);
        Assert.Null(session.PendingRecovery);
        Assert.Null(session.AvailableRecovery);
        Assert.NotNull(session.Validation);
        Assert.Same(session.Validation, session.ValidationCoordinator.Result);
        Assert.Empty(session.Document.UndoStack);

        var saved = await context.Service.SaveAsync(session);
        Assert.True(saved.IsSuccess, saved.Message);
        Assert.Empty(await context.Recovery.ListAsync());
        var loaded = await context.ProjectStore.LoadAsync(session.Path);
        Assert.Equal("Renamed", loaded.Project!.Metadata.Name);
    }

    [Fact]
    public async Task DiscardRecoveryDeletesTheSnapshotAndClearsTheCandidate()
    {
        using var context = new LifecycleContext();
        var session = await context.CreateAsync("Discard");
        Rename(session);
        var write = await context.Recovery.WriteAsync(session.Project, session.Path, RecoverySource.Crash);
        Assert.True(write.IsSuccess);
        var candidate = Assert.Single(await context.Recovery.ListAsync());
        session.PendingRecovery = candidate;

        var discarded = await context.Service.DiscardRecoveryAsync(session, candidate);

        Assert.Equal(ProjectSessionOutcome.Discarded, discarded.Outcome);
        Assert.Null(session.PendingRecovery);
        Assert.Empty(await context.Recovery.ListAsync());
    }

    [Fact]
    public async Task ASidecarCandidateIsDetectedOnOpenAndRecoveryKeepsTheSavePoint()
    {
        using var context = new LifecycleContext();
        var first = await context.CreateAsync("Sidecar");
        Rename(first);
        await context.WriteSidecarAsync(first);
        var sidecar = ProjectStore.GetAutosavePath(first.Path);
        File.Delete(ProjectStore.GetLockPath(first.Path));

        var secondService = context.CreateService();
        var opened = await secondService.OpenAsync(first.Path);

        Assert.True(opened.IsSuccess, opened.Message);
        var second = opened.Session!;
        Assert.NotNull(second.AvailableRecovery);
        Assert.Equal(RecoverySource.Sidecar, second.AvailableRecovery.Source);
        Assert.Equal(sidecar, second.AvailableRecovery.SnapshotPath);

        var recovered = await secondService.RecoverAsync(second, second.AvailableRecovery);

        Assert.Equal(ProjectSessionOutcome.Recovered, recovered.Outcome);
        Assert.True(second.IsDirty);
        Assert.Equal("Renamed", second.Name);
        Assert.Equal("Sidecar", second.Document.LastSavedProject.Metadata.Name);
    }

    [Fact]
    public async Task AManifestCandidateIsDetectedAndValidationIsSetBeforeReplaced()
    {
        using var context = new LifecycleContext();
        var first = await context.CreateAsync("Manifest");
        Rename(first);
        var write = await context.Recovery.WriteAsync(first.Project, first.Path, RecoverySource.Crash);
        Assert.True(write.IsSuccess);
        File.Delete(ProjectStore.GetLockPath(first.Path));

        var secondService = context.CreateService();
        var opened = await secondService.OpenAsync(first.Path);
        Assert.True(opened.IsSuccess, opened.Message);
        var second = opened.Session!;
        Assert.NotNull(second.AvailableRecovery);
        Assert.Equal(RecoverySource.Crash, second.AvailableRecovery.Source);

        Mocassin.Packages.Projects.Validation.ValidationResult? atReplaced = null;
        second.EditCoordinator.ProjectChanged += (_, args) =>
        {
            if (args.Kind == Mocassin.App.ViewModels.Editing.ProjectChangeKind.Replaced)
            {
                atReplaced = second.ValidationCoordinator.Result;
            }
        };

        var recovered = await secondService.RecoverAsync(second, second.AvailableRecovery);

        Assert.Equal(ProjectSessionOutcome.Recovered, recovered.Outcome);
        Assert.Same(second.Validation, atReplaced);
        Assert.True(second.IsDirty);
    }

    [Fact]
    public async Task ATimestampOnlyManifestCandidateIsNotOffered()
    {
        using var context = new LifecycleContext();
        var first = await context.CreateAsync("Timestamp");
        var timestampOnly = first.Project with
        {
            Metadata = first.Project.Metadata with
            {
                ModifiedUtc = first.Project.Metadata.ModifiedUtc.AddMinutes(5),
            },
        };
        var write = await context.Recovery.WriteAsync(timestampOnly, first.Path, RecoverySource.Crash);
        Assert.True(write.IsSuccess);
        File.Delete(ProjectStore.GetLockPath(first.Path));

        var secondService = context.CreateService();
        var opened = await secondService.OpenAsync(first.Path);

        Assert.True(opened.IsSuccess, opened.Message);
        Assert.Null(opened.Session!.AvailableRecovery);
    }

    [Fact]
    public async Task ACleanClosePreservesAPendingCandidateAndReopenOffersIt()
    {
        using var context = new LifecycleContext();
        var session = await context.CreateAsync("PendingClose");
        Rename(session, "First");
        await context.WriteSidecarAsync(session);
        Rename(session, "Second");
        var sidecar = ProjectStore.GetAutosavePath(session.Path);
        var candidate = new RecoveryCandidate(
            session.Id,
            session.Path,
            sidecar,
            context.Time.GetUtcNow(),
            RecoverySource.Sidecar);
        session.PendingRecovery = candidate;
        var saved = await context.Service.SaveAsync(session);
        Assert.True(saved.IsSuccess, saved.Message);
        Assert.True(File.Exists(sidecar));

        var closed = await context.Service.CloseAsync(session);

        Assert.Equal(ProjectSessionOutcome.Closed, closed.Outcome);
        Assert.True(File.Exists(sidecar));

        File.Delete(ProjectStore.GetLockPath(session.Path));
        var secondService = context.CreateService();
        var reopened = await secondService.OpenAsync(session.Path);

        Assert.True(reopened.IsSuccess, reopened.Message);
        Assert.NotNull(reopened.Session!.AvailableRecovery);
        Assert.Equal(RecoverySource.Sidecar, reopened.Session.AvailableRecovery.Source);
    }

    [Fact]
    public async Task MissingProjectFilesLeaveTheRecoveryCandidateForTheShellToReport()
    {
        using var context = new LifecycleContext();
        var session = await context.CreateAsync("Missing");
        Rename(session);
        var write = await context.Recovery.WriteAsync(session.Project, "/tmp/missing.mocprj", RecoverySource.Crash);
        Assert.True(write.IsSuccess);

        var candidate = Assert.Single(await context.Service.ListRecoveryAsync());
        Assert.Equal("/tmp/missing.mocprj", candidate.ProjectPath);
    }

    private static void Rename(ProjectSession session, string name = "Renamed") =>
        session.EditCoordinator.Apply(
            "Rename project",
            project => project with { Metadata = project.Metadata with { Name = name } });

    private sealed class LifecycleContext : IDisposable
    {
        private readonly TempWorkspace workspace = new();
        private readonly AppPaths paths;

        public LifecycleContext()
        {
            Time = new ManualTimeProvider();
            paths = new AppPaths(workspace.Path);
            Settings = new SettingsService(new InMemorySettingsStore(), AppSettingsLoadResult.Missing());
            ProjectStore = new ProjectStore(Time, new ProjectStoreOptions { ValidateOnLoad = false });
            Recovery = new RecoveryStore(paths, ProjectStore, Time);
            Service = CreateService();
        }

        public ManualTimeProvider Time { get; }

        public SettingsService Settings { get; }

        public ProjectStore ProjectStore { get; }

        public RecoveryStore Recovery { get; }

        public ProjectSessionService Service { get; }

        public TempWorkspace Workspace => workspace;

        public ProjectSessionService CreateService() =>
            new(
                ProjectStore,
                new ProjectValidationPipeline(),
                new ProjectFileProbe(),
                new WorkspaceViewModel(),
                Settings,
                Time,
                Recovery);

        public async Task<ProjectSession> CreateAsync(string name)
        {
            var created = await Service.CreateAsync(ProjectTemplateKind.Blank, name, workspace.Path);
            Assert.True(created.IsSuccess, created.Message);
            return created.Session!;
        }

        public async Task WriteSidecarAsync(ProjectSession session)
        {
            var result = await ProjectStore.WriteAutosaveAsync(session.Project, session.Path);
            Assert.True(result.IsSuccess);
        }

        public void Dispose() => workspace.Dispose();
    }
}
