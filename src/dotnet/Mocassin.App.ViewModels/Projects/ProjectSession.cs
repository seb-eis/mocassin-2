// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.App.ViewModels.Diagnostics;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Editing;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// One open project, its document, lock, load diagnostics, and file stamp.
/// </summary>
internal sealed class ProjectSession
{
    private bool isReadOnly;

    private ProjectSession(
        ProjectDocument document,
        string path,
        DateTimeOffset? lastWrittenUtc,
        bool isReadOnly,
        string? lockPath,
        ImmutableArray<Diagnostic> loadDiagnostics,
        ValidationResult? validation,
        ProjectFileStamp stamp,
        ProjectValidationPipeline pipeline,
        TimeProvider timeProvider)
    {
        Document = document;
        Path = path;
        LastWrittenUtc = lastWrittenUtc;
        this.isReadOnly = isReadOnly;
        LockPath = lockPath;
        LoadDiagnostics = loadDiagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : loadDiagnostics;
        Validation = validation;
        Stamp = stamp;
        EditCoordinator = new ProjectEditCoordinator(document, canEdit: !isReadOnly);
        ValidationCoordinator = new ValidationCoordinator(
            EditCoordinator,
            pipeline,
            validation,
            LoadDiagnostics,
            timeProvider);
    }

    /// <summary>
    /// Gets the mutable project document.
    /// </summary>
    public ProjectDocument Document { get; internal set; }

    /// <summary>
    /// Gets the single edit coordinator over <see cref="Document"/>.
    /// </summary>
    public ProjectEditCoordinator EditCoordinator { get; }

    /// <summary>
    /// Gets the per-session live validation surface.
    /// </summary>
    public ValidationCoordinator ValidationCoordinator { get; }

    /// <summary>
    /// Gets the current immutable project.
    /// </summary>
    public Project Project => Document.Project;

    /// <summary>
    /// Gets the project id.
    /// </summary>
    public Guid Id => Project.Metadata.ProjectId;

    /// <summary>
    /// Gets the project name.
    /// </summary>
    public string Name => Project.Metadata.Name;

    /// <summary>
    /// Gets the project file path.
    /// </summary>
    public string Path { get; internal set; }

    /// <summary>
    /// Gets the last successful write time.
    /// </summary>
    public DateTimeOffset? LastWrittenUtc { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the session is read-only.
    /// </summary>
    public bool IsReadOnly
    {
        get => isReadOnly;
        internal set
        {
            if (isReadOnly == value)
            {
                return;
            }

            isReadOnly = value;
            EditCoordinator.SetCanEdit(!value);
        }
    }

    /// <summary>
    /// Gets the lock path, whether or not this session owns the lock.
    /// </summary>
    public string? LockPath { get; internal set; }

    /// <summary>
    /// Gets the owned lock handle, if any.
    /// </summary>
    public ProjectFileLock? FileLock { get; internal set; }

    /// <summary>
    /// Gets the UTC time at which this session acquired its lock.
    /// </summary>
    public DateTimeOffset? LockAcquiredUtc => FileLock?.AcquiredUtc;

    /// <summary>
    /// Gets the load diagnostics retained for Step 23B.
    /// </summary>
    public ImmutableArray<Diagnostic> LoadDiagnostics { get; internal set; }

    /// <summary>
    /// Gets the single validation result produced from the loaded snapshot.
    /// </summary>
    public ValidationResult? Validation { get; internal set; }

    /// <summary>
    /// Gets the current file stamp.
    /// </summary>
    public ProjectFileStamp Stamp { get; internal set; }

    /// <summary>
    /// Gets or sets the last active document key.
    /// </summary>
    public DocumentKey? LastDocumentKey { get; set; }

    /// <summary>
    /// Creates a session from an already loaded project for feature-2 tests and standalone documents.
    /// </summary>
    /// <param name="project">The project value.</param>
    /// <param name="path">The project path.</param>
    /// <param name="lastWrittenUtc">The last write time.</param>
    /// <param name="isReadOnly">Whether the session is read-only.</param>
    /// <param name="lockPath">The optional lock path.</param>
    /// <param name="timeProvider">The clock used by the validation debounce; defaults to the system clock.</param>
    /// <returns>The session.</returns>
    public static ProjectSession CreateStandalone(
        Project project,
        string path,
        DateTimeOffset? lastWrittenUtc = null,
        bool isReadOnly = false,
        string? lockPath = null,
        TimeProvider? timeProvider = null) =>
        new(
            new ProjectDocument(project),
            path,
            lastWrittenUtc,
            isReadOnly,
            lockPath,
            [],
            null,
            ProjectFileStamp.Missing,
            new ProjectValidationPipeline(),
            timeProvider ?? TimeProvider.System);

    /// <summary>
    /// Creates a session from a store load and validates the loaded snapshot exactly once.
    /// </summary>
    /// <param name="load">The store load result.</param>
    /// <param name="path">The full project path.</param>
    /// <param name="lastWrittenUtc">The last write time.</param>
    /// <param name="isReadOnly">Whether the session is read-only.</param>
    /// <param name="lockPath">The optional lock path.</param>
    /// <param name="pipeline">The validation pipeline.</param>
    /// <param name="timeProvider">The clock used by the validation debounce; defaults to the system clock.</param>
    /// <returns>The session.</returns>
    public static ProjectSession CreateFromLoad(
        ProjectLoadResult load,
        string path,
        DateTimeOffset? lastWrittenUtc,
        bool isReadOnly,
        string? lockPath,
        ProjectValidationPipeline pipeline,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(load);
        ArgumentNullException.ThrowIfNull(pipeline);
        if (load.Project is null)
        {
            throw new ArgumentException("A session cannot be created from a failed load.", nameof(load));
        }

        var document = new ProjectDocument(load.Project) { MaxUndoDepth = 200 };
        var validation = pipeline.Validate(load.Project, load.Diagnostics);
        return new ProjectSession(
            document,
            path,
            lastWrittenUtc,
            isReadOnly,
            lockPath,
            load.Diagnostics,
            validation,
            ProjectFileStamp.Missing,
            pipeline,
            timeProvider ?? TimeProvider.System);
    }
}
