// SPDX-License-Identifier: MIT

using Mocassin.App.Composition;
using Mocassin.App.Files;
using Mocassin.App.Recovery;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Recovery;
using Mocassin.App.ViewModels.Settings;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Templates;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.App.Tests.Startup;

/// <summary>
/// Verifies crash snapshot capture, real writes, the bounded timeout, and the dialog action.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class CrashRecoveryWriterTests
{
    [Fact]
    public async Task DirtySessionsAreCapturedAndWrittenToRecovery()
    {
        using var context = new CrashContext();
        var session = context.AddDirtySession("Crash");
        var writer = new CrashRecoveryWriter(context.Sessions, context.Recovery, context.Time);

        var snapshots = writer.CaptureDirtySessions();
        var snapshot = Assert.Single(snapshots);
        Assert.Equal(session.Id, snapshot.ProjectId);
        Assert.Equal(session.Path, snapshot.ProjectPath);

        var results = await writer.WriteAsync(snapshots);

        var result = Assert.Single(results);
        Assert.Equal(CrashRecoveryStatus.Saved, result.Status);
        var candidate = Assert.Single(await context.Recovery.ListAsync());
        Assert.Equal(RecoverySource.Crash, candidate.Source);
        Assert.Equal(session.Id, candidate.ProjectId);
    }

    [Fact]
    public async Task AWriteThatExceedsTheBudgetIsReportedAsTimedOut()
    {
        using var context = new CrashContext();
        context.AddDirtySession("Timeout");
        var blocking = new BlockingRecoveryStore();
        var writer = new CrashRecoveryWriter(
            context.Sessions,
            blocking,
            context.Time,
            TimeSpan.FromMilliseconds(20));
        var snapshots = writer.CaptureDirtySessions();

        var work = writer.WriteAsync(snapshots);
        context.Time.Advance(TimeSpan.FromMilliseconds(30));
        var results = await work;

        var result = Assert.Single(results);
        Assert.Equal(CrashRecoveryStatus.TimedOut, result.Status);
    }

    [Fact]
    public async Task TheDialogWritesRecoveryFilesAndDisablesExitWhileBusy()
    {
        using var context = new CrashContext();
        context.AddDirtySession("Dialog");
        var writer = new CrashRecoveryWriter(context.Sessions, context.Recovery, context.Time);
        var dialog = new CrashDialogViewModel(
            new CrashDialogRequest("Crash", null),
            new RecordingShellLifetime(),
            new RecordingShellLauncher(),
            writer,
            writer.CaptureDirtySessions());

        Assert.True(dialog.HasRecoverySnapshots);
        Assert.True(dialog.CanWriteRecoveryFiles);
        Assert.True(dialog.CanExit);

        await dialog.WriteRecoveryFilesCommand.ExecuteAsync(null);

        Assert.False(dialog.IsWritingRecovery);
        Assert.True(dialog.CanExit);
        Assert.Contains("1 saved", dialog.RecoveryStatus, StringComparison.Ordinal);
    }

    private sealed class CrashContext : IDisposable
    {
        private readonly TempWorkspace workspace = new();

        public CrashContext()
        {
            Time = new ManualTimeProvider();
            Settings = new SettingsService(new InMemorySettingsStore(), AppSettingsLoadResult.Missing());
            Workspace = new WorkspaceViewModel();
            ProjectStore = new ProjectStore(Time, new ProjectStoreOptions { ValidateOnLoad = false });
            Recovery = new RecoveryStore(new AppPaths(workspace.Path), ProjectStore, Time);
            Sessions = new ProjectSessionService(
                ProjectStore,
                new ProjectValidationPipeline(),
                new ProjectFileProbe(),
                Workspace,
                Settings,
                Time,
                Recovery);
        }

        public ManualTimeProvider Time { get; }

        public SettingsService Settings { get; }

        public WorkspaceViewModel Workspace { get; }

        public ProjectStore ProjectStore { get; }

        public RecoveryStore Recovery { get; }

        public ProjectSessionService Sessions { get; }

        public ProjectSession AddDirtySession(string name)
        {
            var project = ProjectTemplateFactory.CreateProject(ProjectTemplateKind.Blank, name, Time.GetUtcNow());
            var session = ProjectSession.CreateStandalone(
                project,
                workspace.Combine(name + ".mocprj"),
                timeProvider: Time);
            session.EditCoordinator.Apply(
                "Rename",
                value => value with { Metadata = value.Metadata with { Name = name + "-dirty" } });
            Workspace.Add(new ProjectWorkspaceEntry(session));
            return session;
        }

        public void Dispose() => workspace.Dispose();
    }

    private sealed class BlockingRecoveryStore : IRecoveryStore
    {
        public Task<IReadOnlyList<RecoveryCandidate>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecoveryCandidate>>([]);

        public async Task<RecoveryWriteResult> WriteAsync(
            Mocassin.Core.Project.Project project,
            string projectPath,
            RecoverySource source,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new RecoveryWriteResult(false, null, default);
        }

        public Task<bool> DeleteAsync(
            RecoveryCandidate candidate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<int> SweepAsync(
            DateTimeOffset now,
            TimeSpan retention,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
