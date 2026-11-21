// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Projects;

namespace Mocassin.App.ViewModels.Navigation;

/// <summary>
/// Contributes project-level explorer nodes without changing the shell.
/// </summary>
internal interface IExplorerNodeProvider
{
    /// <summary>
    /// Creates the nodes for a project.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <returns>The project nodes.</returns>
    public IEnumerable<ExplorerNode> CreateNodes(ProjectWorkspaceEntry project);
}
