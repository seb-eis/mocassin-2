// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Projects;

/// <summary>
/// Result of loading a project file.
/// </summary>
public sealed record ProjectLoadResult
{
    /// <summary>
    /// Initializes a load result.
    /// </summary>
    /// <param name="project">The loaded project, or <see langword="null"/> when parsing failed.</param>
    /// <param name="diagnostics">Diagnostics produced while loading and validating.</param>
    public ProjectLoadResult(Project? project, ImmutableArray<Diagnostic> diagnostics)
    {
        Project = project;
        Diagnostics = diagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : diagnostics;
    }

    /// <summary>
    /// Gets the loaded project, or <see langword="null"/> when parsing failed.
    /// </summary>
    public Project? Project { get; }

    /// <summary>
    /// Gets the load and validation diagnostics.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets a value indicating whether a project was loaded without errors.
    /// </summary>
    public bool IsSuccess => Project is not null && !Diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}

/// <summary>
/// Result of saving a project file.
/// </summary>
public sealed record ProjectSaveResult
{
    /// <summary>
    /// Initializes a save result.
    /// </summary>
    /// <param name="savedProject">The project value that was written, or <see langword="null"/> on failure.</param>
    /// <param name="diagnostics">Diagnostics produced while saving.</param>
    public ProjectSaveResult(Project? savedProject, ImmutableArray<Diagnostic> diagnostics)
    {
        SavedProject = savedProject;
        Diagnostics = diagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : diagnostics;
    }

    /// <summary>
    /// Gets the project value that was written, or <see langword="null"/> on failure.
    /// </summary>
    public Project? SavedProject { get; }

    /// <summary>
    /// Gets the save diagnostics.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets a value indicating whether the save completed without errors.
    /// </summary>
    public bool IsSuccess => SavedProject is not null && !Diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}

/// <summary>
/// Result of acquiring a project editing lock.
/// </summary>
public sealed record ProjectLockResult
{
    /// <summary>
    /// Initializes a lock result.
    /// </summary>
    /// <param name="lockHandle">The acquired lock, or <see langword="null"/> when another session owns it.</param>
    /// <param name="diagnostics">Diagnostics produced while acquiring the lock.</param>
    public ProjectLockResult(ProjectFileLock? lockHandle, ImmutableArray<Diagnostic> diagnostics)
    {
        Lock = lockHandle;
        Diagnostics = diagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : diagnostics;
    }

    /// <summary>
    /// Gets the acquired lock, or <see langword="null"/> when another session owns it.
    /// </summary>
    public ProjectFileLock? Lock { get; }

    /// <summary>
    /// Gets the lock diagnostics.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets a value indicating whether the lock was acquired.
    /// </summary>
    public bool IsSuccess => Lock is not null;
}
