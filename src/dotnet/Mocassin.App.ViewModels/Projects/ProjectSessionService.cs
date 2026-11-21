// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.App.ViewModels.Settings;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Templates;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// Project lifecycle state machine over the shared store and validation pipeline.
/// </summary>
internal sealed class ProjectSessionService : IProjectSessionService
{
    private readonly ProjectStore store;
    private readonly ProjectValidationPipeline pipeline;
    private readonly IProjectFileProbe probe;
    private readonly IProjectWorkspace workspace;
    private readonly ISettingsService settings;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Initializes the session service.
    /// </summary>
    /// <param name="store">The store configured with <see cref="ProjectStoreOptions.ValidateOnLoad"/> disabled.</param>
    /// <param name="pipeline">The in-memory validation pipeline.</param>
    /// <param name="probe">The app-local file probe.</param>
    /// <param name="workspace">The project workspace.</param>
    /// <param name="settings">The application settings.</param>
    /// <param name="timeProvider">The clock.</param>
    public ProjectSessionService(
        ProjectStore store,
        ProjectValidationPipeline pipeline,
        IProjectFileProbe probe,
        IProjectWorkspace workspace,
        ISettingsService settings,
        TimeProvider timeProvider)
    {
        if (store.Options.ValidateOnLoad)
        {
            throw new ArgumentException(
                "The project session service requires ProjectStoreOptions.ValidateOnLoad = false to avoid double validation.",
                nameof(store));
        }

        this.store = store;
        this.pipeline = pipeline;
        this.probe = probe;
        this.workspace = workspace;
        this.settings = settings;
        this.timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public IReadOnlyList<ProjectSession> Sessions =>
        workspace.Projects.Select(entry => entry.Session).ToList();

    /// <inheritdoc />
    public ProjectSession? ActiveSession => workspace.ActiveProject?.Session;

    /// <inheritdoc />
    public async Task<ProjectSessionResult> CreateAsync(
        ProjectTemplateKind kind,
        string name,
        string directory,
        CancellationToken cancellationToken = default)
    {
        var nameResult = ProjectPathRules.NormalizeName(name);
        if (!nameResult.IsSuccess)
        {
            return Failure(ProjectSessionOutcome.InvalidContent, nameResult.Message);
        }

        string fullDirectory;
        try
        {
            fullDirectory = probe.GetFullPath(directory);
        }
        catch (Exception exception) when (exception is ArgumentException
                                              or NotSupportedException
                                              or PathTooLongException)
        {
            return Failure(
                ProjectSessionOutcome.InvalidContent,
                $"The selected target directory is not valid: {exception.Message}");
        }

        var targetPath = probe.Combine(fullDirectory, nameResult.Name + ".mocprj");
        var target = ProjectPathRules.ValidateTarget(
            fullDirectory,
            nameResult,
            probe.GetTargetState(targetPath));
        if (!target.IsSuccess)
        {
            return target.Failure switch
            {
                ProjectPathFailure.ExistingTarget => Failure(ProjectSessionOutcome.ExistingTarget, target.Message),
                ProjectPathFailure.InvalidTargetPath => Failure(ProjectSessionOutcome.Io, target.Message),
                ProjectPathFailure.IoFailure => Failure(ProjectSessionOutcome.Io, target.Message),
                _ => Failure(ProjectSessionOutcome.InvalidContent, target.Message),
            };
        }

        if (!probe.TryCreateDirectory(fullDirectory, out var directoryError))
        {
            return Failure(ProjectSessionOutcome.Io, directoryError);
        }

        var project = ProjectTemplateFactory.CreateProject(kind, nameResult.Name, timeProvider.GetUtcNow());
        var save = await store.SaveAsync(project, target.FullPath, cancellationToken);
        if (!save.IsSuccess)
        {
            return Failure(ProjectSessionOutcome.SaveFailed, save.Diagnostics);
        }

        var load = new ProjectLoadResult(save.SavedProject, ImmutableArray<Diagnostic>.Empty);
        var session = ProjectSession.CreateFromLoad(
            load,
            target.FullPath,
            probe.GetStamp(target.FullPath).LastWriteUtc,
            isReadOnly: false,
            lockPath: null,
            pipeline,
            timeProvider);
        session.Stamp = probe.GetStamp(target.FullPath);
        session.LastWrittenUtc = session.Stamp.LastWriteUtc;
        await AcquireLockAsync(session, cancellationToken);
        AddSession(session);
        await RecordOpenAsync(session, cancellationToken);
        return new ProjectSessionResult(ProjectSessionOutcome.Created, session);
    }

    /// <inheritdoc />
    public async Task<ProjectSessionResult> OpenAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var fullPath = probe.GetFullPath(path);
        if (FindByPath(fullPath) is { } existing)
        {
            return await ActivateSessionAsync(existing, cancellationToken);
        }

        var load = await store.LoadAsync(fullPath, cancellationToken);
        if (load.Project is null)
        {
            return Failure(MapFailure(load.FailureKind), load.Diagnostics, load.Diagnostics.FirstOrDefault()?.Message);
        }

        var stamp = probe.GetStamp(fullPath);
        var session = ProjectSession.CreateFromLoad(
            load,
            fullPath,
            stamp.LastWriteUtc,
            isReadOnly: false,
            lockPath: null,
            pipeline,
            timeProvider);
        session.Stamp = stamp;
        var lockResult = await store.AcquireLockAsync(fullPath, cancellationToken);
        if (lockResult.IsSuccess)
        {
            session.FileLock = lockResult.Lock;
            session.LockPath = lockResult.Lock!.Path;
        }
        else
        {
            session.IsReadOnly = true;
            session.LockPath = ProjectStore.GetLockPath(fullPath);
        }

        AddSession(session);
        await RecordOpenAsync(session, cancellationToken);
        return new ProjectSessionResult(
            session.IsReadOnly ? ProjectSessionOutcome.OpenedReadOnly : ProjectSessionOutcome.Opened,
            session,
            lockResult.Diagnostics);
    }

    /// <inheritdoc />
    public async Task<ProjectSessionResult> ActivateAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var fullPath = probe.GetFullPath(path);
        var session = FindByPath(fullPath);
        return session is null
            ? Failure(ProjectSessionOutcome.NotFound, "The project is not open.")
            : await ActivateSessionAsync(session, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProjectSessionResult> SaveAsync(
        ProjectSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.IsReadOnly || session.FileLock is null)
        {
            return Failure(ProjectSessionOutcome.Locked, "The project is read-only.");
        }

        var change = CheckExternalChange(session);
        if (change != ProjectExternalChange.Unchanged)
        {
            return Failure(
                ProjectSessionOutcome.SaveFailed,
                change == ProjectExternalChange.Deleted
                    ? "The project file was deleted; use Save As."
                    : "The project file changed externally; reload it before saving.");
        }

        var result = await store.SaveAsync(session.Project, session.Path, cancellationToken);
        if (!result.IsSuccess)
        {
            return Failure(ProjectSessionOutcome.SaveFailed, result.Diagnostics);
        }

        session.Stamp = probe.GetStamp(session.Path);
        session.LastWrittenUtc = session.Stamp.LastWriteUtc;
        session.Document.MarkSaved();
        session.EditCoordinator.NotifySaved();
        await RecordSavedAsync(session, cancellationToken);
        return new ProjectSessionResult(ProjectSessionOutcome.Saved, session);
    }

    /// <inheritdoc />
    public async Task<ProjectSessionResult> SaveAsAsync(
        ProjectSession session,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        var fullPath = probe.GetFullPath(path);
        var other = FindByPath(fullPath);
        if (other is not null && !ReferenceEquals(other, session))
        {
            return Failure(ProjectSessionOutcome.Locked, "Another open session already owns that project path.");
        }

        var directory = probe.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return Failure(ProjectSessionOutcome.Io, "The target path has no directory.");
        }

        if (!probe.TryCreateDirectory(directory, out var directoryError))
        {
            return Failure(ProjectSessionOutcome.Io, directoryError);
        }

        var newLock = await store.AcquireLockAsync(fullPath, cancellationToken);
        if (!newLock.IsSuccess)
        {
            return Failure(ProjectSessionOutcome.Locked, newLock.Diagnostics);
        }

        var result = await store.SaveAsync(session.Project, fullPath, cancellationToken);
        if (!result.IsSuccess)
        {
            newLock.Lock?.Dispose();
            return Failure(ProjectSessionOutcome.SaveFailed, result.Diagnostics);
        }

        session.FileLock?.Dispose();
        session.FileLock = newLock.Lock;
        session.LockPath = newLock.Lock!.Path;
        session.Path = fullPath;
        session.IsReadOnly = false;
        session.Stamp = probe.GetStamp(fullPath);
        session.LastWrittenUtc = session.Stamp.LastWriteUtc;
        session.Document.MarkSaved();
        session.EditCoordinator.NotifySaved();
        await RecordSavedAsync(session, cancellationToken);
        return new ProjectSessionResult(ProjectSessionOutcome.Saved, session);
    }

    /// <inheritdoc />
    public async Task<ProjectSessionResult> ReloadAsync(
        ProjectSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        var load = await store.LoadAsync(session.Path, cancellationToken);
        if (load.Project is null)
        {
            return Failure(MapFailure(load.FailureKind), load.Diagnostics, load.Diagnostics.FirstOrDefault()?.Message);
        }

        session.LoadDiagnostics = load.Diagnostics;
        session.Validation = pipeline.Validate(
            load.Project,
            load.Diagnostics,
            cancellationToken: cancellationToken);
        session.ValidationCoordinator.Reseed(session.Validation, load.Diagnostics);
        var document = new ProjectDocument(load.Project) { MaxUndoDepth = 200 };
        session.Document = document;
        session.EditCoordinator.ReplaceDocument(document, canEdit: !session.IsReadOnly);
        session.Stamp = probe.GetStamp(session.Path);
        session.LastWrittenUtc = session.Stamp.LastWriteUtc;
        return new ProjectSessionResult(ProjectSessionOutcome.Reloaded, session, load.Diagnostics);
    }

    /// <inheritdoc />
    public Task<ProjectSessionResult> CloseAsync(
        ProjectSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();
        session.FileLock?.Dispose();
        session.FileLock = null;
        workspace.Remove(session.Id);
        return Task.FromResult(new ProjectSessionResult(ProjectSessionOutcome.Closed, session));
    }

    /// <inheritdoc />
    public ProjectExternalChange CheckExternalChange(ProjectSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var stamp = probe.GetStamp(session.Path);
        if (!stamp.Exists)
        {
            return ProjectExternalChange.Deleted;
        }

        return stamp == session.Stamp
            ? ProjectExternalChange.Unchanged
            : ProjectExternalChange.Modified;
    }

    private async Task<ProjectSessionResult> ActivateSessionAsync(
        ProjectSession session,
        CancellationToken cancellationToken)
    {
        if (workspace.Find(session.Id) is { } entry)
        {
            workspace.Activate(entry);
        }

        await RecordOpenAsync(session, cancellationToken);
        return new ProjectSessionResult(ProjectSessionOutcome.Activated, session);
    }

    private void AddSession(ProjectSession session)
    {
        var entry = new ProjectWorkspaceEntry(session);
        workspace.Add(entry);
        workspace.Activate(entry);
    }

    private ProjectSession? FindByPath(string fullPath) =>
        Sessions.FirstOrDefault(
            session => string.Equals(
                probe.GetFullPath(session.Path),
                fullPath,
                probe.PathComparison));

    private async Task AcquireLockAsync(ProjectSession session, CancellationToken cancellationToken)
    {
        var lockResult = await store.AcquireLockAsync(session.Path, cancellationToken);
        if (lockResult.IsSuccess)
        {
            session.FileLock = lockResult.Lock;
            session.LockPath = lockResult.Lock!.Path;
            return;
        }

        session.IsReadOnly = true;
        session.LockPath = ProjectStore.GetLockPath(session.Path);
    }

    private async Task RecordOpenAsync(ProjectSession session, CancellationToken cancellationToken)
    {
        var directory = probe.GetDirectoryName(session.Path);
        var paths = settings.Current.Paths;
        var updated = string.IsNullOrWhiteSpace(directory)
            ? settings.Current
            : settings.Current with { Paths = paths with { LastOpenDirectory = directory } };
        await settings.UpdateAsync(updated, cancellationToken);
        await settings.AddRecentFileAsync(session.Path, timeProvider.GetUtcNow(), cancellationToken);
    }

    private async Task RecordSavedAsync(ProjectSession session, CancellationToken cancellationToken)
    {
        var directory = probe.GetDirectoryName(session.Path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            await settings.UpdateAsync(
                settings.Current with { Paths = settings.Current.Paths with { LastSaveDirectory = directory } },
                cancellationToken);
        }

        await settings.AddRecentFileAsync(session.Path, timeProvider.GetUtcNow(), cancellationToken);
    }

    private static ProjectSessionOutcome MapFailure(ProjectLoadFailureKind failure) =>
        failure switch
        {
            ProjectLoadFailureKind.NotFound => ProjectSessionOutcome.NotFound,
            ProjectLoadFailureKind.TooLarge => ProjectSessionOutcome.TooLarge,
            ProjectLoadFailureKind.Io => ProjectSessionOutcome.Io,
            _ => ProjectSessionOutcome.InvalidContent,
        };

    private static ProjectSessionResult Failure(
        ProjectSessionOutcome outcome,
        string? message = null,
        ImmutableArray<Diagnostic> diagnostics = default) =>
        new(outcome, null, diagnostics, message);

    private static ProjectSessionResult Failure(
        ProjectSessionOutcome outcome,
        ImmutableArray<Diagnostic> diagnostics,
        string? message = null) =>
        new(outcome, null, diagnostics, message);
}
