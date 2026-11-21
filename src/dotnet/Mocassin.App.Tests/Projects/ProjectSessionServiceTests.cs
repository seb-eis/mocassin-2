// SPDX-License-Identifier: MIT

using Mocassin.App.Files;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Settings;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Templates;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.App.Tests.Projects;

/// <summary>
/// Verifies the real project-session lifecycle over temporary files and store locks.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ProjectSessionServiceTests
{
    [Fact]
    public async Task ANewProjectIsWrittenAndReopensWithTheSameContent()
    {
        var context = CreateContext(out var workspace);
        using var temp = new TempWorkspace();

        var created = await context.Service.CreateAsync(ProjectTemplateKind.Blank, "Created", temp.Path);
        Assert.True(created.IsSuccess, created.Message);
        Assert.NotNull(created.Session);
        Assert.True(File.Exists(created.Session.Path));
        Assert.Single(workspace.Projects);
        Assert.Same(created.Session, workspace.ActiveProject!.Session);

        await context.Service.CloseAsync(created.Session);
        var reopened = await context.Service.OpenAsync(created.Session.Path);
        Assert.True(reopened.IsSuccess, reopened.Message);
        Assert.Equal(
            ProjectHasher.ComputeDocument(created.Session.Project),
            ProjectHasher.ComputeDocument(reopened.Session!.Project));

        await context.Service.CloseAsync(reopened.Session!);
    }

    [Fact]
    public async Task OpeningAnAlreadyOpenPathActivatesTheSameSession()
    {
        var context = CreateContext(out _);
        using var temp = new TempWorkspace();
        var created = await context.Service.CreateAsync(ProjectTemplateKind.Blank, "Activate", temp.Path);

        var activated = await context.Service.OpenAsync(created.Session!.Path);

        Assert.Equal(ProjectSessionOutcome.Activated, activated.Outcome);
        Assert.Same(created.Session, activated.Session);
        Assert.Single(context.Service.Sessions);
        await context.Service.CloseAsync(created.Session);
    }

    [Fact]
    public async Task AnInvalidTargetDirectoryIsRejectedWithoutThrowing()
    {
        var context = CreateContext(out _);

        var result = await context.Service.CreateAsync(ProjectTemplateKind.Blank, "Invalid", "\0");

        Assert.False(result.IsSuccess);
        Assert.Equal(ProjectSessionOutcome.InvalidContent, result.Outcome);
        Assert.Contains("not valid", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NewRefusesAnExistingTargetWithoutChangingIt()
    {
        var context = CreateContext(out _);
        using var temp = new TempWorkspace();
        var first = await context.Service.CreateAsync(ProjectTemplateKind.Blank, "Taken", temp.Path);
        var original = await File.ReadAllTextAsync(first.Session!.Path);

        var second = await context.Service.CreateAsync(ProjectTemplateKind.Ceria, "Taken", temp.Path);

        Assert.False(second.IsSuccess);
        Assert.Equal(ProjectSessionOutcome.ExistingTarget, second.Outcome);
        Assert.Contains("already exists", second.Message, StringComparison.Ordinal);
        Assert.Equal(original, await File.ReadAllTextAsync(first.Session.Path));
        await context.Service.CloseAsync(first.Session);
    }

    [Fact]
    public async Task SaveMarksTheDocumentCleanAndUpdatesTheStamp()
    {
        var context = CreateContext(out _);
        using var temp = new TempWorkspace();
        var created = await context.Service.CreateAsync(ProjectTemplateKind.Blank, "Saved", temp.Path);
        var session = created.Session!;
        session.Document.Apply(
            "Rename",
            project => project with { Metadata = project.Metadata with { Name = "Renamed" } });
        Assert.True(session.Document.IsDirty);

        var saved = await context.Service.SaveAsync(session);

        Assert.True(saved.IsSuccess, saved.Message);
        Assert.False(session.Document.IsDirty);
        Assert.Equal("Renamed", session.Name);
        Assert.NotEqual(ProjectFileStamp.Missing, session.Stamp);

        Assert.True(session.Document.Undo());
        Assert.True(session.Document.IsDirty);
        Assert.True(session.Document.Redo());
        Assert.False(session.Document.IsDirty);

        await context.Service.CloseAsync(session);
    }

    [Fact]
    public async Task SaveAsMigratesTheLockAndUpdatesThePath()
    {
        var context = CreateContext(out _);
        using var temp = new TempWorkspace();
        var created = await context.Service.CreateAsync(ProjectTemplateKind.Blank, "Original", temp.Path);
        var session = created.Session!;
        var oldPath = session.Path;
        var newPath = Path.Combine(temp.Path, "Moved.mocprj");

        var saved = await context.Service.SaveAsAsync(session, newPath);

        Assert.True(saved.IsSuccess, saved.Message);
        Assert.Equal(newPath, session.Path);
        Assert.True(File.Exists(newPath));
        Assert.False(File.Exists(ProjectStore.GetLockPath(oldPath)));
        Assert.True(File.Exists(ProjectStore.GetLockPath(newPath)));
        Assert.Contains(
            context.Settings.Current.RecentFiles,
            file => string.Equals(file.Path, newPath, StringComparison.Ordinal));
        await context.Service.CloseAsync(session);
    }

    [Fact]
    public async Task AStaleLockIsReclaimedByTheNextSession()
    {
        var settings = new SettingsService(new InMemorySettingsStore(), AppSettingsLoadResult.Missing());
        var firstContext = CreateContext(
            out _,
            settings,
            new ManualTimeProvider(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        var secondContext = CreateContext(out _, settings, new ManualTimeProvider());
        using var temp = new TempWorkspace();
        var created = await firstContext.Service.CreateAsync(ProjectTemplateKind.Blank, "Stale", temp.Path);
        var first = created.Session!;
        await File.WriteAllTextAsync(
            ProjectStore.GetLockPath(first.Path),
            """{"pid":999999,"host":"other-host","token":"foreign","acquiredUtc":"2020-01-01T00:00:00.0000000Z"}""");

        var second = await secondContext.Service.OpenAsync(first.Path);

        Assert.Equal(ProjectSessionOutcome.Opened, second.Outcome);
        Assert.False(second.Session!.IsReadOnly);
        await secondContext.Service.CloseAsync(second.Session);
        await firstContext.Service.CloseAsync(first);
    }

    [Fact]
    public async Task ASecondSessionOpensReadOnlyAndCannotSave()
    {
        var firstContext = CreateContext(out _);
        var secondContext = CreateContext(out _);
        using var temp = new TempWorkspace();
        var created = await firstContext.Service.CreateAsync(ProjectTemplateKind.Blank, "Locked", temp.Path);
        var first = created.Session!;
        var second = await secondContext.Service.OpenAsync(first.Path);

        Assert.Equal(ProjectSessionOutcome.OpenedReadOnly, second.Outcome);
        Assert.True(second.Session!.IsReadOnly);
        var save = await secondContext.Service.SaveAsync(second.Session);
        Assert.Equal(ProjectSessionOutcome.Locked, save.Outcome);

        await secondContext.Service.CloseAsync(second.Session);
        await firstContext.Service.CloseAsync(first);
    }

    [Fact]
    public async Task ExternalChangeIsDetectedAndReloadReplacesTheDocument()
    {
        var context = CreateContext(out _);
        using var temp = new TempWorkspace();
        var created = await context.Service.CreateAsync(ProjectTemplateKind.Blank, "External", temp.Path);
        var session = created.Session!;
        var changed = session.Project with { Metadata = session.Project.Metadata with { Name = "Changed" } };
        var bytes = ProjectJson.SerializeToUtf8Bytes(changed, indented: true);
        await File.WriteAllBytesAsync(session.Path, bytes);

        Assert.Equal(ProjectExternalChange.Modified, context.Service.CheckExternalChange(session));
        var refused = await context.Service.SaveAsync(session);
        Assert.Equal(ProjectSessionOutcome.SaveFailed, refused.Outcome);

        var reloaded = await context.Service.ReloadAsync(session);

        Assert.True(reloaded.IsSuccess, reloaded.Message);
        Assert.Equal("Changed", session.Name);
        Assert.Equal(ProjectExternalChange.Unchanged, context.Service.CheckExternalChange(session));
        await context.Service.CloseAsync(session);
    }

    [Fact]
    public async Task ADeletedFileIsReportedAndCannotBeSaved()
    {
        var context = CreateContext(out _);
        using var temp = new TempWorkspace();
        var created = await context.Service.CreateAsync(ProjectTemplateKind.Blank, "Deleted", temp.Path);
        var session = created.Session!;
        File.Delete(session.Path);

        Assert.Equal(ProjectExternalChange.Deleted, context.Service.CheckExternalChange(session));
        var save = await context.Service.SaveAsync(session);
        Assert.Equal(ProjectSessionOutcome.SaveFailed, save.Outcome);
        await context.Service.CloseAsync(session);
    }

    [Fact]
    public async Task OpenRecordsTheLastDirectoryAndRecentFile()
    {
        var store = new InMemorySettingsStore();
        var settings = new SettingsService(store, AppSettingsLoadResult.Missing());
        var context = CreateContext(out _, settings);
        using var temp = new TempWorkspace();
        var created = await context.Service.CreateAsync(ProjectTemplateKind.Blank, "Recent", temp.Path);
        await context.Service.CloseAsync(created.Session!);

        var opened = await context.Service.OpenAsync(created.Session!.Path);

        Assert.True(opened.IsSuccess);
        Assert.Equal(temp.Path, settings.Current.Paths.LastOpenDirectory);
        Assert.Contains(
            settings.Current.RecentFiles,
            file => string.Equals(file.Path, created.Session.Path, StringComparison.Ordinal));
        await context.Service.CloseAsync(opened.Session!);
    }

    [Fact]
    public async Task ACleanLoadSkipsTheLoadStageAndARecordedLoadDiagnosticCompletesIt()
    {
        var context = CreateContext(out _);
        using var temp = new TempWorkspace();
        var created = await context.Service.CreateAsync(ProjectTemplateKind.Blank, "Validation", temp.Path);
        var session = created.Session!;

        var clean = session.Validation!;
        Assert.Equal(ValidationStageStatus.Skipped, clean.Stages[0].Status);
        Assert.Equal(0, clean.Stages[0].DiagnosticCount);

        var warning = new Diagnostic(
            "PROJ999",
            DiagnosticSeverity.Warning,
            "Synthetic load warning.");
        var project = session.Project;
        var withWarning = ProjectSession.CreateFromLoad(
            new ProjectLoadResult(project, [warning]),
            session.Path,
            session.LastWrittenUtc,
            session.IsReadOnly,
            session.LockPath,
            new ProjectValidationPipeline());

        Assert.Equal(ValidationStageStatus.Completed, withWarning.Validation!.Stages[0].Status);
        Assert.Equal(1, withWarning.Validation.Stages[0].DiagnosticCount);

        var error = new Diagnostic("PROJ998", DiagnosticSeverity.Error, "Synthetic load error.");
        var withError = ProjectSession.CreateFromLoad(
            new ProjectLoadResult(project, [error]),
            session.Path,
            session.LastWrittenUtc,
            session.IsReadOnly,
            session.LockPath,
            new ProjectValidationPipeline());
        Assert.Equal(ValidationStageStatus.Failed, withError.Validation!.Stages[0].Status);
        await context.Service.CloseAsync(session);
    }

    [Fact]
    public void TheSessionServiceRejectsAStoreThatValidatesOnLoad()
    {
        using var temp = new TempWorkspace();
        var store = new ProjectStore(
            new ManualTimeProvider(),
            new ProjectStoreOptions { ValidateOnLoad = true });

        var exception = Assert.Throws<ArgumentException>(
            () => new ProjectSessionService(
                store,
                new ProjectValidationPipeline(),
                new ProjectFileProbe(),
                new WorkspaceViewModel(),
                new SettingsService(new InMemorySettingsStore(), AppSettingsLoadResult.Missing()),
                new ManualTimeProvider()));

        Assert.Contains("ValidateOnLoad", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheWorkspaceSeamExposesTheLiveSession()
    {
        var context = CreateContext(out var workspace);
        using var temp = new TempWorkspace();

        var created = await context.Service.CreateAsync(ProjectTemplateKind.Blank, "Seam", temp.Path);

        Assert.Same(created.Session, workspace.ActiveProject!.Session);
        Assert.Equal(created.Session!.Path, workspace.ActiveProject.Path);
        Assert.Equal(created.Session.Name, workspace.ActiveProject.Name);
        await context.Service.CloseAsync(created.Session);
    }

    private static Context CreateContext(out WorkspaceViewModel workspace)
    {
        var store = new InMemorySettingsStore();
        var settings = new SettingsService(store, AppSettingsLoadResult.Missing());
        return CreateContext(out workspace, settings);
    }

    private static Context CreateContext(
        out WorkspaceViewModel workspace,
        SettingsService settings,
        TimeProvider? timeProvider = null)
    {
        workspace = new WorkspaceViewModel();
        var time = timeProvider ?? new ManualTimeProvider();
        var service = new ProjectSessionService(
            new ProjectStore(time, new ProjectStoreOptions { ValidateOnLoad = false }),
            new ProjectValidationPipeline(),
            new ProjectFileProbe(),
            workspace,
            settings,
            time);
        return new Context(service, settings);
    }

    private sealed record Context(ProjectSessionService Service, SettingsService Settings);
}
