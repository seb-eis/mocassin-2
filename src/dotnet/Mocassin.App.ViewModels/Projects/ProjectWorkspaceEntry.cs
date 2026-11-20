// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Documents;
using Mocassin.Core.Project;

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// A loaded project shown in the workspace. Step 23A feature 4 creates entries from project sessions; the
/// entry carries only display and navigation state.
/// </summary>
internal sealed class ProjectWorkspaceEntry
{
    /// <summary>
    /// Initializes a workspace entry.
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
    {
        Project = project ?? throw new ArgumentNullException(nameof(project));
        Path = path;
        LastWrittenUtc = lastWrittenUtc;
        IsReadOnly = isReadOnly;
        LockPath = lockPath;
    }

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
    public string Path { get; }

    /// <summary>
    /// Gets the immutable project value.
    /// </summary>
    public Project Project { get; }

    /// <summary>
    /// Gets the last successful write time.
    /// </summary>
    public DateTimeOffset? LastWrittenUtc { get; }

    /// <summary>
    /// Gets a value indicating whether the project is open read-only.
    /// </summary>
    public bool IsReadOnly { get; }

    /// <summary>
    /// Gets the optional lock path.
    /// </summary>
    public string? LockPath { get; }

    /// <summary>
    /// Gets or sets the last document that was active in this project.
    /// </summary>
    public DocumentKey? LastDocumentKey { get; set; }
}
