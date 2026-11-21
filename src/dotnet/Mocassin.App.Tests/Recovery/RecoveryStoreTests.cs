// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using Mocassin.App.Composition;
using Mocassin.App.Recovery;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Recovery;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Templates;

namespace Mocassin.App.Tests.Recovery;

/// <summary>
/// Verifies the real recovery store, manifest bounds, deletion, and seven-day retention.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class RecoveryStoreTests
{
    [Fact]
    public async Task ARecoverySnapshotRoundTripsWithItsManifest()
    {
        using var context = new RecoveryContext();
        var project = CreateProject("Recovery", context.Time.GetUtcNow());

        var write = await context.Store.WriteAsync(project, "/tmp/recovery.mocprj", RecoverySource.Crash);

        Assert.True(write.IsSuccess, string.Join(", ", write.Diagnostics));
        Assert.NotNull(write.SnapshotPath);
        Assert.True(File.Exists(write.SnapshotPath));
        var manifest = Directory.GetFiles(context.Paths.RecoveryDirectory, "*.json").Single();
        Assert.True(File.Exists(manifest));

        var candidates = await context.Store.ListAsync();
        var candidate = Assert.Single(candidates);
        Assert.Equal(project.Metadata.ProjectId, candidate.ProjectId);
        Assert.Equal("/tmp/recovery.mocprj", candidate.ProjectPath);
        Assert.Equal(RecoverySource.Crash, candidate.Source);
        Assert.Equal(context.Time.GetUtcNow(), candidate.SavedUtc);

        var loaded = await context.ProjectStore.LoadAsync(candidate.SnapshotPath);
        Assert.NotNull(loaded.Project);
        Assert.Equal(project.Metadata.ProjectId, loaded.Project.Metadata.ProjectId);
        Assert.Equal(project.Metadata.Name, loaded.Project.Metadata.Name);
    }

    [Fact]
    public async Task DeletingTheCandidateRemovesBothFiles()
    {
        using var context = new RecoveryContext();
        var candidate = await WriteOne(context);

        Assert.True(await context.Store.DeleteAsync(candidate));
        Assert.Empty(await context.Store.ListAsync());
        Assert.False(File.Exists(candidate.SnapshotPath));
        Assert.Empty(Directory.GetFiles(context.Paths.RecoveryDirectory, "*.json"));
    }

    [Fact]
    public async Task SweepingRemovesOnlyOldRecoveryFilesAndNeverSidecars()
    {
        using var context = new RecoveryContext();
        var project = CreateProject("Sweep", context.Time.GetUtcNow());
        var write = await context.Store.WriteAsync(project, "/tmp/sweep.mocprj", RecoverySource.AutosaveFallback);
        Assert.True(write.IsSuccess);
        var sidecar = Path.Combine(context.Paths.RootDirectory, "sweep.mocprj.autosave");
        await File.WriteAllTextAsync(sidecar, "sidecar");

        context.Time.Advance(TimeSpan.FromDays(6));
        Assert.Equal(0, await context.Store.SweepAsync(context.Time.GetUtcNow(), RecoveryRetentionPolicy.Retention));
        Assert.Single(await context.Store.ListAsync());

        context.Time.Advance(TimeSpan.FromDays(2));
        Assert.Equal(1, await context.Store.SweepAsync(context.Time.GetUtcNow(), RecoveryRetentionPolicy.Retention));
        Assert.Empty(await context.Store.ListAsync());
        Assert.True(File.Exists(sidecar));
    }

    [Fact]
    public async Task MalformedAndOrphanedRecoveryFilesAreIgnoredAndLeftAlone()
    {
        using var context = new RecoveryContext();
        Directory.CreateDirectory(context.Paths.RecoveryDirectory);
        var malformed = Path.Combine(context.Paths.RecoveryDirectory, "malformed.json");
        await File.WriteAllTextAsync(malformed, "{ not-json");
        var oversized = Path.Combine(context.Paths.RecoveryDirectory, "oversized.json");
        await File.WriteAllBytesAsync(oversized, new byte[70 * 1024]);
        var manifestWithoutSnapshot = Path.Combine(
            context.Paths.RecoveryDirectory,
            Guid.NewGuid().ToString("D") + ".json");
        await File.WriteAllTextAsync(
            manifestWithoutSnapshot,
            """{"format":"mocassin.recovery","version":1,"projectId":"018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e6f","projectPath":"/tmp/a.mocprj","savedUtc":"2026-11-20T12:00:00Z","source":"Crash"}""");

        Assert.Empty(await context.Store.ListAsync());
        Assert.Equal(0, await context.Store.SweepAsync(context.Time.GetUtcNow(), RecoveryRetentionPolicy.Retention));
        Assert.True(File.Exists(malformed));
        Assert.True(File.Exists(oversized));
        Assert.True(File.Exists(manifestWithoutSnapshot));
        Assert.Contains(
            context.LoggerProvider.Entries,
            entry => entry.Level == LogLevel.Warning
                     && entry.Message.Contains("recovery manifest", StringComparison.Ordinal));
    }

    private static async Task<RecoveryCandidate> WriteOne(RecoveryContext context)
    {
        var project = CreateProject("Delete", context.Time.GetUtcNow());
        var write = await context.Store.WriteAsync(project, "/tmp/delete.mocprj", RecoverySource.Crash);
        Assert.True(write.IsSuccess);
        return Assert.Single(await context.Store.ListAsync());
    }

    private static Project CreateProject(string name, DateTimeOffset now) =>
        ProjectTemplateFactory.CreateProject(ProjectTemplateKind.Blank, name, now);

    private sealed class RecoveryContext : IDisposable
    {
        private readonly TempWorkspace workspace = new();

        public RecoveryContext()
        {
            Paths = new AppPaths(workspace.Path);
            Directory.CreateDirectory(Paths.RecoveryDirectory);
            Time = new ManualTimeProvider();
            ProjectStore = new ProjectStore(Time, new ProjectStoreOptions { ValidateOnLoad = false });
            LoggerProvider = new RecordingLoggerProvider();
            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(
                builder => builder.AddProvider(LoggerProvider));
            Store = new RecoveryStore(
                Paths,
                ProjectStore,
                Time,
                LoggerFactory.CreateLogger<RecoveryStore>());
        }

        public AppPaths Paths { get; }

        public ManualTimeProvider Time { get; }

        public ProjectStore ProjectStore { get; }

        public RecordingLoggerProvider LoggerProvider { get; }

        public ILoggerFactory LoggerFactory { get; }

        public RecoveryStore Store { get; }

        public void Dispose()
        {
            LoggerFactory.Dispose();
            workspace.Dispose();
        }
    }
}
