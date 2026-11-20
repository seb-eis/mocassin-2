// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// The set of loaded projects and the active project. The project lifecycle feature fills it.
/// </summary>
internal interface IProjectWorkspace
{
    /// <summary>
    /// Gets the loaded projects.
    /// </summary>
    public IReadOnlyList<ProjectWorkspaceEntry> Projects { get; }

    /// <summary>
    /// Gets the active project.
    /// </summary>
    public ProjectWorkspaceEntry? ActiveProject { get; }

    /// <summary>
    /// Occurs when the active project changes.
    /// </summary>
    public event EventHandler? ActiveProjectChanged;

    /// <summary>
    /// Adds a loaded project.
    /// </summary>
    /// <param name="project">The project to add.</param>
    public void Add(ProjectWorkspaceEntry project);

    /// <summary>
    /// Removes a project.
    /// </summary>
    /// <param name="projectId">The project id.</param>
    /// <returns><see langword="true"/> when the project was removed.</returns>
    public bool Remove(Guid projectId);

    /// <summary>
    /// Activates a loaded project.
    /// </summary>
    /// <param name="project">The project to activate.</param>
    public void Activate(ProjectWorkspaceEntry project);

    /// <summary>
    /// Finds a loaded project.
    /// </summary>
    /// <param name="projectId">The project id.</param>
    /// <returns>The project, or <see langword="null"/>.</returns>
    public ProjectWorkspaceEntry? Find(Guid projectId);
}
