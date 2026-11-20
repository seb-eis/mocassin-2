// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Documents;
using Mocassin.Core.Project;

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// A project shown in the workspace. Feature 4 wraps a <see cref="ProjectSession"/>; the feature-2 constructor
/// remains for standalone documents and tests.
/// </summary>
internal sealed class ProjectWorkspaceEntry
{
    /// <summary>
    /// Initializes a workspace entry from a session.
    /// </summary>
    /// <param name="session">The project session.</param>
    public ProjectWorkspaceEntry(ProjectSession session)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>
    /// Initializes a standalone workspace entry from a loaded project.
    /// </summary>
    /// <param name="project">The loaded immutable project.</param>
    /// <param name="path">The project file path.</param>
    /// <param name="lastWrittenUtc">The last successful write time, if any.</param>
    /// <param name="isReadOnly">Whether the project is open read-only.</param>
    /// <param name="lockPath">The optional lock path.</param>
    public ProjectWorkspaceEntry(
        Project project,
        string path,
        DateTimeOffset? lastWrittenUtc = null,
        bool isReadOnly = false,
        string? lockPath = null)
        : this(ProjectSession.CreateStandalone(project, path, lastWrittenUtc, isReadOnly, lockPath))
    {
    }

    /// <summary>
    /// Gets the project session.
    /// </summary>
    public ProjectSession Session { get; }

    /// <summary>
    /// Gets the project id.
    /// </summary>
    public Guid Id => Session.Id;

    /// <summary>
    /// Gets the project name.
    /// </summary>
    public string Name => Session.Name;

    /// <summary>
    /// Gets the project file path.
    /// </summary>
    public string Path => Session.Path;

    /// <summary>
    /// Gets the immutable project value.
    /// </summary>
    public Project Project => Session.Project;

    /// <summary>
    /// Gets the last successful write time.
    /// </summary>
    public DateTimeOffset? LastWrittenUtc => Session.LastWrittenUtc;

    /// <summary>
    /// Gets a value indicating whether the project is open read-only.
    /// </summary>
    public bool IsReadOnly => Session.IsReadOnly;

    /// <summary>
    /// Gets the lock path.
    /// </summary>
    public string? LockPath => Session.LockPath;

    /// <summary>
    /// Gets or sets the last document that was active in this project.
    /// </summary>
    public DocumentKey? LastDocumentKey
    {
        get => Session.LastDocumentKey;
        set => Session.LastDocumentKey = value;
    }
}
